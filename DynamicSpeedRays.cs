using UnityEngine;
using System.Collections.Generic;

public class DynamicSpeedRays : MonoBehaviour
{
    [Header("Ray Settings")]
    [Tooltip("How many rays to create around the screen")]
    public int numberOfRays = 24;
    
    [Tooltip("Base width of each ray")]
    public float rayWidth = 1f;
    
    [Tooltip("How far the rays extend from screen edge (in % of screen)")]
    [Range(0.1f, 0.5f)]
    public float rayLength = 0.2f;
    
    [Header("Speed Settings")]
    [Tooltip("Speed at which rays start appearing")]
    public float minSpeedThreshold = 10f;
    
    [Tooltip("Speed at which rays reach max intensity")]
    public float maxSpeedThreshold = 40f;
    
    [Header("Animation")]
    [Tooltip("How fast rays pulse")]
    public float pulseSpeed = 3f;
    
    [Tooltip("How much rays pulse in size")]
    [Range(0f, 1f)]
    public float pulseAmount = 0.2f;
    
    [Header("Color Settings")]
    public Color rayColor = Color.white;

    private List<LineRenderer> rays = new List<LineRenderer>();
    private PlayerMovement playerMovement;
    private Camera mainCamera;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;
    private float currentTime = 0f;
    private Canvas canvas;

    private void Start()
    {
        Initialize();
        CreateRays();
    }

    private void Initialize()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("No PlayerMovement found!");
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
            return;
        }

        // Create a canvas that stays in front of everything
        GameObject canvasObj = new GameObject("RaysCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        transform.SetParent(canvas.transform);
    }

    private void CreateRays()
    {
        float angleStep = 360f / numberOfRays;

        for (int i = 0; i < numberOfRays; i++)
        {
            GameObject rayObj = new GameObject($"SpeedRay_{i}");
            rayObj.transform.SetParent(transform);

            LineRenderer line = rayObj.AddComponent<LineRenderer>();
            
            // Create and set material
            Material rayMaterial = new Material(Shader.Find("Sprites/Default"));
            rayMaterial.color = rayColor;
            rayMaterial.renderQueue = 4000;
            line.material = rayMaterial;
            
            // Configure line renderer
            line.startWidth = rayWidth;
            line.endWidth = rayWidth * 0.5f;
            line.positionCount = 2;
            line.useWorldSpace = false;
            line.sortingOrder = 100;

            // Initially hide the ray
            Color startColor = rayColor;
            startColor.a = 0;
            line.startColor = startColor;
            line.endColor = Color.clear;

            rays.Add(line);
        }
    }

    private void Update()
    {
        if (playerMovement == null || mainCamera == null) return;

        // Only show rays when the car is actually moving
        if (playerMovement.IsMoving)
        {
            UpdateIntensity();
            AnimateRays();
        }
        else
        {
            // Hide rays when not moving
            foreach (var ray in rays)
            {
                if (ray != null)
                {
                    ray.startColor = Color.clear;
                    ray.endColor = Color.clear;
                }
            }
            currentIntensity = 0f;
        }
    }

    private void UpdateIntensity()
    {
        Vector2Int velocity = playerMovement.CurrentVelocity;
        float currentSpeed = Mathf.Max(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y));

        if (currentSpeed < minSpeedThreshold)
        {
            targetIntensity = 0f;
        }
        else
        {
            targetIntensity = Mathf.Clamp01((currentSpeed - minSpeedThreshold) / (maxSpeedThreshold - minSpeedThreshold));
        }

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 5f);
    }

    private void AnimateRays()
    {
        currentTime += Time.deltaTime;
        float pulse = 1f + (Mathf.Sin(currentTime * pulseSpeed) * pulseAmount);

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        float maxScreenDim = Mathf.Max(Screen.width, Screen.height);
        float currentRayLength = maxScreenDim * rayLength * pulse;

        for (int i = 0; i < rays.Count; i++)
        {
            LineRenderer ray = rays[i];
            if (ray == null) continue;

            // Calculate angle for this ray
            float angle = ((360f / numberOfRays) * i);
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            // Calculate start point (on screen edge)
            Vector2 edgePoint = screenCenter + direction * (maxScreenDim * 0.5f);
            Vector2 innerPoint = edgePoint - direction * currentRayLength;

            // Convert to world space points
            Vector3 startWorld = mainCamera.ScreenToWorldPoint(new Vector3(edgePoint.x, edgePoint.y, 10));
            Vector3 endWorld = mainCamera.ScreenToWorldPoint(new Vector3(innerPoint.x, innerPoint.y, 10));

            // Set positions
            ray.SetPosition(0, startWorld);
            ray.SetPosition(1, endWorld);

            // Set colors with current intensity
            Color startColor = rayColor;
            startColor.a = rayColor.a * currentIntensity;
            ray.startColor = startColor;
            ray.endColor = Color.clear;
        }
    }

    private void OnDestroy()
    {
        foreach (var ray in rays)
        {
            if (ray != null)
            {
                if (ray.material != null)
                    Destroy(ray.material);
                Destroy(ray.gameObject);
            }
        }
        rays.Clear();

        if (canvas != null)
            Destroy(canvas.gameObject);
    }
}