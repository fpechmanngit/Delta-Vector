using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class HoverLineVisualizer : MonoBehaviour
{
    private GameObject hoverLineObject;
    private GameObject arrowTipObject;
    private LineRenderer lineRenderer;
    private SpriteRenderer arrowTipRenderer;
    private PlayerMovement playerMovement;
    
    [Header("Line Settings")]
    public Color lineColor = Color.red;
    public float lineWidth = 0.1f;

    [Header("Arrow Settings")]
    public Sprite arrowSprite; // Assign in inspector
    public float arrowSize = 0.5f;
    public Color arrowColor = Color.red;

    [Header("Animation")]
    [Range(0.5f, 1f)]
    public float minAlpha = 0.7f;
    [Range(0.8f, 1f)]
    public float maxAlpha = 1f;
    public float pulseSpeed = 3f;

    private float currentAlpha = 1f;
    private Material lineMaterial;
    private bool isInitialized = false;
    
    private void Start()
    {
        InitializeLine();
        InitializeArrow();
        HideLine(); // Start hidden
        isInitialized = true;
        Debug.Log("HoverLineVisualizer initialized");
    }

    private void InitializeLine()
    {
        // Create line object
        hoverLineObject = new GameObject("HoverLine");
        hoverLineObject.transform.SetParent(transform);
        
        // Setup line renderer
        lineRenderer = hoverLineObject.AddComponent<LineRenderer>();
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.renderQueue = 3000; // Ensure it renders on top
        
        lineRenderer.material = lineMaterial;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.sortingOrder = 5000;
        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false; // Start disabled
        
        UpdateColors();
        Debug.Log("Line initialized with sorting order " + lineRenderer.sortingOrder);
    }

    private void InitializeArrow()
    {
        // Create arrow object
        arrowTipObject = new GameObject("ArrowTip");
        arrowTipObject.transform.SetParent(transform);
        
        // Setup sprite renderer
        arrowTipRenderer = arrowTipObject.AddComponent<SpriteRenderer>();
        arrowTipRenderer.sortingOrder = 5001; // Above line
        arrowTipRenderer.material = new Material(Shader.Find("Sprites/Default"));
        arrowTipRenderer.material.renderQueue = 3001; // Above line
        
        // If no sprite assigned, create a default triangle
        if (arrowSprite == null)
        {
            CreateDefaultArrowSprite();
        }
        else
        {
            arrowTipRenderer.sprite = arrowSprite;
        }
        
        arrowTipObject.transform.localScale = Vector3.one * arrowSize;
        UpdateColors();
        arrowTipObject.SetActive(false); // Start hidden
        Debug.Log("Arrow initialized with sorting order " + arrowTipRenderer.sortingOrder);
    }

    private void CreateDefaultArrowSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        for (int y = 0; y < tex.height; y++)
        {
            for (int x = 0; x < tex.width; x++)
            {
                // Create a simple triangle shape
                if (x <= y && x <= (tex.width - y))
                {
                    tex.SetPixel(x, y, Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        arrowSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0, 0.5f), 100);
        arrowTipRenderer.sprite = arrowSprite;
    }

    private void Update()
    {
        if (lineRenderer != null && lineRenderer.enabled)
        {
            // Update alpha with sin wave
            currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            UpdateColors();
        }
    }

    private void UpdateColors()
    {
        if (lineRenderer != null)
        {
            Color colorWithAlpha = lineColor;
            colorWithAlpha.a = currentAlpha;
            lineRenderer.startColor = colorWithAlpha;
            lineRenderer.endColor = colorWithAlpha;
        }

        if (arrowTipRenderer != null)
        {
            Color arrowColorWithAlpha = arrowColor;
            arrowColorWithAlpha.a = currentAlpha;
            arrowTipRenderer.color = arrowColorWithAlpha;
        }
    }

    public void ShowLine(Vector2Int targetPosition)
    {
        if (!isInitialized)
        {
            Debug.LogError("HoverLineVisualizer not initialized!");
            return;
        }

        // Convert target position from grid coordinates to world coordinates
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(
            targetPosition.x / (float)PlayerMovement.GRID_SCALE,
            targetPosition.y / (float)PlayerMovement.GRID_SCALE,
            transform.position.z
        );
        
        // Enable and update line
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // Update arrow
        Vector3 direction = (endPos - startPos).normalized;
        arrowTipObject.transform.position = endPos;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrowTipObject.transform.rotation = Quaternion.Euler(0, 0, angle);
        arrowTipObject.SetActive(true);

        Debug.Log($"Showing line from {startPos} to {endPos} with arrow at {angle} degrees. Grid position: {targetPosition}, Grid Scale: {PlayerMovement.GRID_SCALE}");
    }

    public void HideLine()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        if (arrowTipObject != null)
        {
            arrowTipObject.SetActive(false);
        }
        Debug.Log("Line and arrow hidden");
    }

    private void OnValidate()
    {
        if (arrowTipObject != null)
        {
            arrowTipObject.transform.localScale = Vector3.one * arrowSize;
        }
        UpdateColors();
    }

    private void OnDestroy()
    {
        if (hoverLineObject != null) Destroy(hoverLineObject);
        if (arrowTipObject != null) Destroy(arrowTipObject);
        if (lineMaterial != null) Destroy(lineMaterial);
        if (arrowSprite != null && !arrowSprite.name.Contains("Assets")) Destroy(arrowSprite);
    }
}