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
    
    [Header("Import Settings")]
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
        var data = Resources.Load<TextAsset>("circle");

        var sceneInfo = loadSVG(data.text);

        //TODO Actual animation
        
        var geoms = VectorUtils.TessellateScene(sceneInfo.Scene, GetTesselationOptions(sceneInfo));
        
        var rect = Rect.zero;
        //if (ViewportOptions == ViewportOptions.PreserveViewport)
        rect = sceneInfo.SceneViewport;
        var vectorImage = GenerateVectorImageAsset(geoms, rect);

        var element = uiRoot.rootVisualElement.Q<VisualElement>("image");
        element.style.backgroundImage = new StyleBackground(vectorImage);
    }
    
    private SVGParser.SceneInfo loadSVG(string data) {
        using (var reader = new StringReader(data)) { // not strictly needed but in case switch later.
            return SVGParser.ImportSVG(reader);
        }
    }

    private VectorUtils.TessellationOptions GetTesselationOptions(SVGParser.SceneInfo sceneInfo)
    {
        float stepDist = StepDistance;
        float samplingStepDist = SamplingStepDistance;
        float maxCord = MaxCordDeviationEnabled ? MaxCordDeviation : float.MaxValue;
        float maxTangent = MaxTangentAngleEnabled ? MaxTangentAngle : Mathf.PI * 0.5f;

        ComputeTessellationOptions(sceneInfo, TargetResolution, ResolutionMultiplier, out stepDist, out maxCord, out maxTangent);

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
}