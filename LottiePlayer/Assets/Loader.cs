using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

public class Loader : MonoBehaviour
{
    public UIDocument uiRoot;

    [SerializeField] private string assetName = "square";
    
    [Header("Size")]
    [SerializeField] private float width = 100;
    [SerializeField] private float height = 100;
    
    [Header("Import Settings")]
    
    [SerializeField] private VectorUtils.Alignment Alignment;
    [SerializeField] private Vector2 CustomPivot;
    [SerializeField] private bool GeneratePhysicsShape;
    [SerializeField] private ViewportOptions ViewportOptions = ViewportOptions.DontPreserve;

    
    [SerializeField] private bool AdvancedMode = false;
    
    [SerializeField] private int TargetResolution = 1080;
    [SerializeField] private float ResolutionMultiplier = 1.0f;
    
    [SerializeField] private uint GradientResolution = 64;
    
    [SerializeField] private float StepDistance = 10.0f;
    [SerializeField] private float SamplingStepDistance = 100.0f;
    [SerializeField] private float MaxCordDeviation = 1.0f;
    [SerializeField] private bool MaxCordDeviationEnabled = false;
    [SerializeField] private bool MaxTangentAngleEnabled = false;
    [SerializeField] private float MaxTangentAngle = 5.0f;


    void Start()
    {
        //Read string data
        var data = Resources.Load<TextAsset>(assetName);
        
        //Convert into SceneInfo
        var sceneInfo = LoadSVG(data.text);

        //TODO Actual animation

        //Convert Scene into List<Geometry>
        var geoms = VectorUtils.TessellateScene(sceneInfo.Scene, GetTesselationOptions(sceneInfo));
        
        var rect = Rect.zero;
        if (ViewportOptions == ViewportOptions.PreserveViewport)
            rect = sceneInfo.SceneViewport;

        // **GRAPHICS**

        //UXML Usage
        //Convert Geometry into VectorImage
        var vectorImage = GenerateVectorImageAsset(geoms, rect);

        //Display VectorImage as background of UXML Element
        var element = uiRoot.rootVisualElement.Q<VisualElement>("image");
        element.style.backgroundImage = new StyleBackground(vectorImage);

        element.style.width = width;
        element.style.height = height;
    }

    private SVGParser.SceneInfo LoadSVG(string data) {
        
        SVGParser.SceneInfo sceneInfo;
        using (var reader = new StringReader(data))
            sceneInfo = SVGParser.ImportSVG(reader, ViewportOptions, 0, 1, 100, 100);

        if (sceneInfo.Scene == null || sceneInfo.Scene.Root == null)
            throw new Exception("Wowzers!");

        return sceneInfo;
    }

    private VectorUtils.TessellationOptions GetTesselationOptions(SVGParser.SceneInfo sceneInfo)
    {
        float stepDist = StepDistance;
        float samplingStepDist = SamplingStepDistance;
        float maxCord = MaxCordDeviationEnabled ? MaxCordDeviation : float.MaxValue;
        float maxTangent = MaxTangentAngleEnabled ? MaxTangentAngle : Mathf.PI * 0.5f;

        if (!AdvancedMode)
        {
            // Automatically compute sensible tessellation options from the
            // vector scene's bouding box and target resolution
            ComputeTessellationOptions(sceneInfo, TargetResolution, ResolutionMultiplier, out stepDist, out maxCord, out maxTangent);
        }

        var tessOptions = new VectorUtils.TessellationOptions();
        tessOptions.MaxCordDeviation = maxCord;
        tessOptions.MaxTanAngleDeviation = maxTangent;
        tessOptions.SamplingStepSize = 1.0f / (float)samplingStepDist;
        tessOptions.StepDistance = stepDist;

        return tessOptions;
    }
    
    private void ComputeTessellationOptions(SVGParser.SceneInfo sceneInfo, int targetResolution, float multiplier, out float stepDist, out float maxCord, out float maxTangent)
    {
        float ppu = 1.0f;

        var bbox = VectorUtils.ApproximateSceneNodeBounds(sceneInfo.Scene.Root);
        float maxDim = Mathf.Max(bbox.width, bbox.height) / ppu;

        // The scene ratio gives a rough estimate of coverage % of the vector scene on the screen.
        // Higher values should result in a more dense tessellation.
        float sceneRatio = maxDim / (targetResolution * multiplier);

        stepDist = float.MaxValue; // No need for uniform step distance
        maxCord = Mathf.Max(0.01f, 2.0f * sceneRatio);
        maxTangent = Mathf.Max(0.1f, 3.0f * sceneRatio);
    }

    private VectorImage GenerateVectorImageAsset(List<VectorUtils.Geometry> geometry, Rect rect)
    {
        UnityEngine.Object asset;
        Texture2D texAtlas;
        VectorImageUtils.MakeVectorImageAsset(geometry, rect, GradientResolution, out asset, out texAtlas);

        if (asset == null)
        {
            Debug.LogError("UIElement asset generation failed");
            return null;
        }

        if (texAtlas != null)
            texAtlas.name = name + "Atlas";

        return (VectorImage)asset;
    }
    
    
    ///
    ///
    ///
    private SVGParser.SceneInfo GenerateSVG() {
        
        Dictionary<SceneNode, float> nodeOpacities = new Dictionary<SceneNode, float>();
        Dictionary<string, SceneNode> nodeIDs = new Dictionary<string, SceneNode>();


        var scene = new Scene();

        var root = new SceneNode();
        root.Transform = new Matrix2D();
        root.Shapes = new List<Shape>();
        var shape = new Shape();
        shape.PathProps = new PathProperties();
        root.Shapes.Add(shape);
        scene.Root = root;

        return new SVGParser.SceneInfo(scene, new Rect(0, 0, 15, 15), nodeOpacities, nodeIDs);
    }

}