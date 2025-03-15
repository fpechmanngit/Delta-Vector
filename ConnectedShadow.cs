using UnityEngine;

public class ConnectedShadowEffect : MonoBehaviour
{
    [Header("Shadow Settings")]
    [Tooltip("Color and opacity of the shadow")]
    public Color shadowColor = new Color(0, 0, 0, 0.0f);
    
    [Tooltip("How far the shadow extends")]
    public float shadowLength = 0.1f;
    
    [Tooltip("Angle at which the shadow is cast (0 = right, 90 = up)")]
    [Range(-180f, 180f)]
    public float shadowAngle = -45f;
    
    [Tooltip("Width of the shadow at its base (1 = same as sprite)")]
    public float baseWidth = 1f;
    
    [Tooltip("Width of the shadow at its end (1 = same as base)")]
    public float tipWidth = 0.5f;

    private SpriteRenderer spriteRenderer;
    private GameObject shadowObject;
    private LineRenderer shadowRenderer;
    private Vector3[] shadowPoints;
    private const int SHADOW_RESOLUTION = 2; // Number of points in the shadow (2 for straight line)

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found!");
            return;
        }

        CreateShadow();
        UpdateShadow();
    }

    private void CreateShadow()
    {
        // Create shadow object
        shadowObject = new GameObject($"{gameObject.name}_ConnectedShadow");
        shadowObject.transform.SetParent(transform);
        
        // Set up line renderer for shadow
        shadowRenderer = shadowObject.AddComponent<LineRenderer>();
        shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        shadowRenderer.positionCount = SHADOW_RESOLUTION;
        shadowRenderer.useWorldSpace = true;
        
        // Create material for shadow
        Material shadowMaterial = new Material(Shader.Find("Sprites/Default"));
        shadowRenderer.material = shadowMaterial;

        // Initialize shadow points array
        shadowPoints = new Vector3[SHADOW_RESOLUTION];
    }

    private void UpdateShadow()
    {
        if (shadowRenderer == null) return;

        // Get sprite bounds
        Bounds spriteBounds = spriteRenderer.bounds;
        
        // Calculate the bottom center of the sprite
        Vector3 spriteBase = new Vector3(
            transform.position.x,
            spriteBounds.min.y,
            transform.position.z
        );

        // Calculate shadow direction
        float angleRad = shadowAngle * Mathf.Deg2Rad;
        Vector3 shadowDir = new Vector3(
            Mathf.Cos(angleRad),
            Mathf.Sin(angleRad),
            0
        );

        // Set shadow points
        shadowPoints[0] = spriteBase;
        shadowPoints[1] = spriteBase + (shadowDir * shadowLength);

        // Update line renderer
        shadowRenderer.SetPositions(shadowPoints);
        
        // Update width gradient
        shadowRenderer.startWidth = spriteBounds.size.x * baseWidth;
        shadowRenderer.endWidth = spriteBounds.size.x * tipWidth;
        
        // Update color
        shadowRenderer.startColor = shadowColor;
        Color endColor = shadowColor;
        endColor.a *= 0.5f; // Fade out the tip
        shadowRenderer.endColor = endColor;
    }

    private void LateUpdate()
    {
        UpdateShadow();
    }

    private void OnDestroy()
    {
        if (shadowObject != null)
        {
            if (shadowRenderer != null && shadowRenderer.material != null)
            {
                Destroy(shadowRenderer.material);
            }
            Destroy(shadowObject);
        }
    }

    private void OnValidate()
    {
        if (shadowRenderer != null)
        {
            UpdateShadow();
        }
    }

    // Public method to update shadow properties at runtime
    public void UpdateShadowProperties(float length, float angle, Color color)
    {
        shadowLength = length;
        shadowAngle = angle;
        shadowColor = color;
        UpdateShadow();
    }
}