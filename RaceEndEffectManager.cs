using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class RaceEndEffectManager : MonoBehaviour
{
    [Header("Debug")]
    public bool logEveryFrame = true;
    public bool forceEffectInEditor = true;

    [Header("Effect Settings")]
    [Range(0f, 1f)]
    public float effectStrength = 0f;

    private Material effectMaterial;
    private bool isInitialized = false;
    private Coroutine fadeCoroutine;

    void OnEnable()
    {
        Debug.Log("[EFFECT-DEBUG] OnEnable called");
        CreateMaterial();
    }

    void CreateMaterial()
    {
        Debug.Log("[EFFECT-DEBUG] Creating material...");
        if (effectMaterial != null)
        {
            DestroyImmediate(effectMaterial);
        }

        // Try to find shader
        Shader shader = Shader.Find("Custom/RaceEndEffect");
        if (shader == null)
        {
            Debug.LogError("[EFFECT-DEBUG] Failed to find shader 'Custom/RaceEndEffect'!");
            return;
        }
        Debug.Log("[EFFECT-DEBUG] Found shader: " + shader.name);

        // Create material
        effectMaterial = new Material(shader);
        effectMaterial.hideFlags = HideFlags.HideAndDontSave;
        Debug.Log("[EFFECT-DEBUG] Created material");

        isInitialized = true;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (logEveryFrame)
        {
            Debug.Log($"[EFFECT-DEBUG] OnRenderImage - Material: {(effectMaterial != null ? "Valid" : "Null")}, Strength: {effectStrength}");
        }

        if (!Application.isPlaying && !forceEffectInEditor)
        {
            Graphics.Blit(src, dest);
            return;
        }

        if (effectMaterial == null)
        {
            Debug.LogWarning("[EFFECT-DEBUG] Material is null in OnRenderImage, recreating...");
            CreateMaterial();
        }

        if (effectMaterial != null)
        {
            effectMaterial.SetFloat("_EffectStrength", effectStrength);
            Graphics.Blit(src, dest, effectMaterial);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }

    public void EnableEffect()
    {
        Debug.Log("[EFFECT-DEBUG] EnableEffect called");
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeEffect(true));
    }

    public void DisableEffect()
    {
        Debug.Log("[EFFECT-DEBUG] DisableEffect called");
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeEffect(false));
    }

    private IEnumerator FadeEffect(bool fadeIn)
    {
        float startValue = fadeIn ? 0f : effectStrength;
        float endValue = fadeIn ? 1f : 0f;
        float duration = 0.5f;
        float elapsed = 0f;

        Debug.Log($"[EFFECT-DEBUG] Starting fade - In: {fadeIn}, From: {startValue}, To: {endValue}");

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            effectStrength = Mathf.Lerp(startValue, endValue, elapsed / duration);
            yield return null;
        }

        effectStrength = endValue;
        fadeCoroutine = null;
        Debug.Log($"[EFFECT-DEBUG] Fade complete - Strength: {effectStrength}");
    }

    void OnDisable()
    {
        Debug.Log("[EFFECT-DEBUG] OnDisable called");
        if (effectMaterial != null)
        {
            DestroyImmediate(effectMaterial);
            effectMaterial = null;
        }
    }
}