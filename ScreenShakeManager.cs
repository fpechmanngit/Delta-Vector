using UnityEngine;
using System.Collections;

public class ScreenShakeManager : MonoBehaviour
{
    // Static instance for easy access
    private static ScreenShakeManager instance;
    public static ScreenShakeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ScreenShakeManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ScreenShakeManager");
                    instance = go.AddComponent<ScreenShakeManager>();
                    Debug.LogWarning("ScreenShakeManager was missing from scene - created one automatically");
                }
            }
            return instance;
        }
    }

    [Header("Shake Settings")]
    [Tooltip("How strong the shake is")]
    public float shakeStrength = 0.5f;
    
    [Tooltip("How long the shake lasts in seconds")]
    public float shakeDuration = 0.1f;
    
    [Tooltip("How fast the shake fades out")]
    public float shakeFadeTime = 0.05f;

    private Camera mainCamera;
    private Vector3 originalPosition;
    private bool isShaking = false;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        // Ensure we only have one instance
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        
        // Find and store the main camera reference
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found - screen shake won't work!");
            return;
        }

        Debug.Log($"ScreenShakeManager initialized with camera: {mainCamera.name}");
    }

    public void ShakeScreen()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Can't shake - no camera reference!");
            return;
        }

        Debug.Log($"Shake triggered with strength: {shakeStrength}, duration: {shakeDuration}");

        // Store original position if not already shaking
        if (!isShaking)
        {
            originalPosition = mainCamera.transform.position;
        }

        // Stop any existing shake
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        // Start new shake
        shakeCoroutine = StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0f;

        Debug.Log("Starting shake coroutine");

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float strength = Mathf.Lerp(shakeStrength, 0f, elapsed / shakeDuration);

            // Generate random offset
            float offsetX = Random.Range(-1f, 1f) * strength;
            float offsetY = Random.Range(-1f, 1f) * strength;

            // Apply offset to camera
            mainCamera.transform.position = new Vector3(
                originalPosition.x + offsetX,
                originalPosition.y + offsetY,
                originalPosition.z
            );

            yield return null;
        }

        // Smoothly return to original position
        float returnElapsed = 0f;
        Vector3 currentPos = mainCamera.transform.position;

        while (returnElapsed < shakeFadeTime)
        {
            returnElapsed += Time.deltaTime;
            float t = returnElapsed / shakeFadeTime;

            mainCamera.transform.position = Vector3.Lerp(
                currentPos,
                originalPosition,
                t
            );

            yield return null;
        }

        // Ensure we're exactly back at the original position
        mainCamera.transform.position = originalPosition;
        isShaking = false;
        Debug.Log("Shake complete");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        // Clean up coroutine if necessary
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
    }
}