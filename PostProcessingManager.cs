using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPPostProcessingManager : MonoBehaviour
{
    [Header("Volume Reference")]
    public VolumeProfile volumeProfile;
    public Volume volume;
    public bool enableEffects = true;

    [Header("Bloom")]
    public bool enableBloom = true;
    [Range(0f, 10f)]
    public float bloomIntensity = 1f;
    [Range(0f, 10f)]
    public float bloomThreshold = 1f;
    [Range(0f, 1f)]
    public float bloomScatter = 0.7f;
    [Range(0f, 1f)]
    public float bloomClamp = 1f;

    [Header("Chromatic Aberration")]
    public bool enableChromaticAberration = true;
    [Range(0f, 1f)]
    public float chromaticAberrationIntensity = 0.3f;

    [Header("Color Adjustments")]
    public bool enableColorAdjustments = true;
    [Range(-100f, 100f)]
    public float postExposure = 0f;
    [Range(-100f, 100f)]
    public float contrast = 0f;
    [Range(-180f, 180f)]
    public float hueShift = 0f;
    [Range(-100f, 100f)]
    public float saturation = 0f;

    [Header("Color Curves")]
    public bool enableColorCurves = true;

    [Header("Depth of Field")]
    public bool enableDepthOfField = true;
    [Range(0.1f, 50f)]
    public float focusDistance = 10f;
    [Range(0.1f, 32f)]
    public float aperture = 5.6f;
    [Range(1f, 300f)]
    public float focalLength = 50f;

    [Header("Film Grain")]
    public bool enableFilmGrain = true;
    [Range(0f, 1f)]
    public float grainIntensity = 0.3f;
    [Range(0f, 1f)]
    public float grainResponse = 0.8f;

    [Header("Lens Distortion")]
    public bool enableLensDistortion = true;
    [Range(-100f, 100f)]
    public float lensDistortionIntensity = 0f;
    [Range(0f, 1f)]
    public float lensDistortionScale = 1f;

    [Header("Motion Blur")]
    public bool enableMotionBlur = true;
    [Range(0f, 1f)]
    public float motionBlurIntensity = 0.5f;
    [Range(3, 32)]
public MotionBlurQuality motionBlurQuality = MotionBlurQuality.Medium;

    [Header("Panini Projection")]
    public bool enablePaniniProjection = true;
    [Range(0f, 1f)]
    public float paniniDistance = 0f;
    [Range(0f, 1f)]
    public float paniniCropToFit = 1f;

    [Header("Shadows Midtones Highlights")]
    public bool enableShadowsMidtonesHighlights = true;
    public Color shadowsColor = Color.black;
    public Color highlightsColor = Color.white;

    [Header("Split Toning")]
    public bool enableSplitToning = true;
    public Color splitToningShadows = Color.grey;
    public Color splitToningHighlights = Color.grey;
    [Range(-100, 100)]
    public float splitToningBalance = 0f;

    [Header("Tonemapping")]
    public bool enableTonemapping = true;
    public TonemappingMode tonemappingMode = TonemappingMode.Neutral;

    [Header("Vignette")]
    public bool enableVignette = true;
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.4f;
    public Color vignetteColor = Color.black;
    [Range(0f, 1f)]
    public float vignetteSmoothness = 0.2f;
    [Range(0f, 1f)]
    public float vingetteCenter = 0.5f;  // Changed from roundness to center

    [Header("White Balance")]
    public bool enableWhiteBalance = true;
    [Range(-100f, 100f)]
    public float temperature = 0f;
    [Range(-100f, 100f)]
    public float tint = 0f;

    // Effect references
    private Bloom bloomLayer;
    private ChromaticAberration chromaticAberrationLayer;
    private ColorAdjustments colorAdjustmentsLayer;
    private ColorCurves colorCurvesLayer;
    private DepthOfField depthOfFieldLayer;
    private FilmGrain filmGrainLayer;
    private LensDistortion lensDistortionLayer;
    private MotionBlur motionBlurLayer;
    private PaniniProjection paniniProjectionLayer;
    private ShadowsMidtonesHighlights shadowsMidtonesHighlightsLayer;
    private SplitToning splitToningLayer;
    private Tonemapping tonemappingLayer;
    private Vignette vignetteLayer;
    private WhiteBalance whiteBalanceLayer;

    private void Start()
    {
        InitializePostProcessing();
    }

    private void InitializePostProcessing()
    {
        // First try to get Volume component
        if (volume == null)
        {
            volume = GetComponent<Volume>();
            if (volume == null)
            {
                volume = gameObject.AddComponent<Volume>();
            }
        }

        // Try to get the profile
        if (volumeProfile != null)
        {
            volume.profile = volumeProfile;
            GetEffectReferences();
            UpdateAllSettings();
        }
        else
        {
            Debug.LogError("Please assign a Volume Profile!");
        }
    }

    private void GetEffectReferences()
    {
        volumeProfile.TryGet(out bloomLayer);
        volumeProfile.TryGet(out chromaticAberrationLayer);
        volumeProfile.TryGet(out colorAdjustmentsLayer);
        volumeProfile.TryGet(out colorCurvesLayer);
        volumeProfile.TryGet(out depthOfFieldLayer);
        volumeProfile.TryGet(out filmGrainLayer);
        volumeProfile.TryGet(out lensDistortionLayer);
        volumeProfile.TryGet(out motionBlurLayer);
        volumeProfile.TryGet(out paniniProjectionLayer);
        volumeProfile.TryGet(out shadowsMidtonesHighlightsLayer);
        volumeProfile.TryGet(out splitToningLayer);
        volumeProfile.TryGet(out tonemappingLayer);
        volumeProfile.TryGet(out vignetteLayer);
        volumeProfile.TryGet(out whiteBalanceLayer);
    }

    private void UpdateAllSettings()
    {
        if (volumeProfile == null) return;

        UpdateBloom();
        UpdateChromaticAberration();
        UpdateColorAdjustments();
        UpdateColorCurves();
        UpdateDepthOfField();
        UpdateFilmGrain();
        UpdateLensDistortion();
        UpdateMotionBlur();
        UpdatePaniniProjection();
        UpdateShadowsMidtonesHighlights();
        UpdateSplitToning();
        UpdateTonemapping();
        UpdateVignette();
        UpdateWhiteBalance();
    }

    private void UpdateBloom()
    {
        if (bloomLayer != null)
        {
            bloomLayer.active = enableBloom;
            bloomLayer.intensity.value = bloomIntensity;
            bloomLayer.threshold.value = bloomThreshold;
            bloomLayer.scatter.value = bloomScatter;
            bloomLayer.clamp.value = bloomClamp;
        }
    }

    private void UpdateChromaticAberration()
    {
        if (chromaticAberrationLayer != null)
        {
            chromaticAberrationLayer.active = enableChromaticAberration;
            chromaticAberrationLayer.intensity.value = chromaticAberrationIntensity;
        }
    }

    private void UpdateColorAdjustments()
    {
        if (colorAdjustmentsLayer != null)
        {
            colorAdjustmentsLayer.active = enableColorAdjustments;
            colorAdjustmentsLayer.postExposure.value = postExposure;
            colorAdjustmentsLayer.contrast.value = contrast;
            colorAdjustmentsLayer.hueShift.value = hueShift;
            colorAdjustmentsLayer.saturation.value = saturation;
        }
    }

    private void UpdateColorCurves()
    {
        if (colorCurvesLayer != null)
        {
            colorCurvesLayer.active = enableColorCurves;
        }
    }

    private void UpdateDepthOfField()
    {
        if (depthOfFieldLayer != null)
        {
            depthOfFieldLayer.active = enableDepthOfField;
            depthOfFieldLayer.focusDistance.value = focusDistance;
            depthOfFieldLayer.aperture.value = aperture;
            depthOfFieldLayer.focalLength.value = focalLength;
        }
    }

    private void UpdateFilmGrain()
    {
        if (filmGrainLayer != null)
        {
            filmGrainLayer.active = enableFilmGrain;
            filmGrainLayer.intensity.value = grainIntensity;
            filmGrainLayer.response.value = grainResponse;
        }
    }

    private void UpdateLensDistortion()
    {
        if (lensDistortionLayer != null)
        {
            lensDistortionLayer.active = enableLensDistortion;
            lensDistortionLayer.intensity.value = lensDistortionIntensity;
            lensDistortionLayer.scale.value = lensDistortionScale;
        }
    }

    private void UpdateMotionBlur()
    {
        if (motionBlurLayer != null)
        {
            motionBlurLayer.active = enableMotionBlur;
            motionBlurLayer.intensity.value = motionBlurIntensity;
            motionBlurLayer.quality.value = motionBlurQuality;  // Changed from sampleCount to quality
        }
    }

    private void UpdatePaniniProjection()
    {
        if (paniniProjectionLayer != null)
        {
            paniniProjectionLayer.active = enablePaniniProjection;
            paniniProjectionLayer.distance.value = paniniDistance;
            paniniProjectionLayer.cropToFit.value = paniniCropToFit;
        }
    }

    private void UpdateShadowsMidtonesHighlights()
    {
        if (shadowsMidtonesHighlightsLayer != null)
        {
            shadowsMidtonesHighlightsLayer.active = enableShadowsMidtonesHighlights;
            shadowsMidtonesHighlightsLayer.shadows.value = shadowsColor;
            shadowsMidtonesHighlightsLayer.highlights.value = highlightsColor;
        }
    }

    private void UpdateSplitToning()
    {
        if (splitToningLayer != null)
        {
            splitToningLayer.active = enableSplitToning;
            splitToningLayer.shadows.value = splitToningShadows;
            splitToningLayer.highlights.value = splitToningHighlights;
            splitToningLayer.balance.value = splitToningBalance;
        }
    }

    private void UpdateTonemapping()
    {
        if (tonemappingLayer != null)
        {
            tonemappingLayer.active = enableTonemapping;
            tonemappingLayer.mode.value = tonemappingMode;
        }
    }

    private void UpdateVignette()
    {
        if (vignetteLayer != null)
        {
            vignetteLayer.active = enableVignette;
            vignetteLayer.intensity.value = vignetteIntensity;
            vignetteLayer.color.value = vignetteColor;
            vignetteLayer.smoothness.value = vignetteSmoothness;
            vignetteLayer.center.value = new Vector2(vingetteCenter, vingetteCenter);  // Changed from roundness to center
        }
    }

    private void UpdateWhiteBalance()
    {
        if (whiteBalanceLayer != null)
        {
            whiteBalanceLayer.active = enableWhiteBalance;
            whiteBalanceLayer.temperature.value = temperature;
            whiteBalanceLayer.tint.value = tint;
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateAllSettings();
        }
    }

    // Public methods for runtime control
    public void SetBloomIntensity(float intensity)
    {
        bloomIntensity = Mathf.Clamp(intensity, 0f, 10f);
        UpdateBloom();
    }

    public void SetChromaticAberration(float intensity)
    {
        chromaticAberrationIntensity = Mathf.Clamp(intensity, 0f, 1f);
        UpdateChromaticAberration();
    }

    public void SetVignetteIntensity(float intensity)
    {
        vignetteIntensity = Mathf.Clamp(intensity, 0f, 1f);
        UpdateVignette();
    }

    public void EnableEffect(string effectName, bool enable)
    {
        switch (effectName.ToLower())
        {
            case "bloom":
                enableBloom = enable;
                UpdateBloom();
                break;
            case "chromatic":
                enableChromaticAberration = enable;
                UpdateChromaticAberration();
                break;
            case "vignette":
                enableVignette = enable;
                UpdateVignette();
                break;
            // Add more cases as needed
        }
    }
}