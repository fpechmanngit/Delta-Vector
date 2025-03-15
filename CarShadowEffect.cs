using UnityEngine;

public class CarShadowEffect : MonoBehaviour
{
    [Header("Shadow Settings")]
    public Color shadowColor = new Color(0, 0, 0, 0.3f);  // Black with 30% opacity
    public float northOffset = 0.3f;    // How far north the shadow appears
    public float shadowScale = 1.1f;    // Make shadow slightly larger than the car

    private SpriteRenderer carSpriteRenderer;
    private SpriteRenderer shadowSpriteRenderer;
    private GameObject shadowObject;
    private bool isSingleSpriteMode = false;

    private void Start()
    {
        carSpriteRenderer = GetComponent<SpriteRenderer>();
        if (carSpriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found on car!");
            return;
        }

        CreateShadow();
    }

    private void CreateShadow()
    {
        // Create the shadow as a separate object, NOT parented to the car
        shadowObject = new GameObject("CarShadow");
        
        // Copy the sprite renderer settings
        shadowSpriteRenderer = shadowObject.AddComponent<SpriteRenderer>();
        shadowSpriteRenderer.sprite = carSpriteRenderer.sprite;
        shadowSpriteRenderer.color = shadowColor;
        shadowSpriteRenderer.sortingOrder = carSpriteRenderer.sortingOrder - 1;
        
        // Set initial scale
        shadowObject.transform.localScale = Vector3.one * shadowScale;
        
        // Initial position update
        UpdateShadowPosition();
    }

    public void ConfigureForSingleSprite(bool isSingleSprite)
    {
        isSingleSpriteMode = isSingleSprite;
    }

    private void LateUpdate()
    {
        // Keep shadow updated
        if (shadowSpriteRenderer != null && carSpriteRenderer != null)
        {
            shadowSpriteRenderer.sprite = carSpriteRenderer.sprite;
            UpdateShadowPosition();
        }
    }

    private void UpdateShadowPosition()
    {
        if (shadowObject == null) return;

        // Position shadow at car's position
        Vector3 shadowPos = transform.position;
        
        // Add the north offset in world space
        shadowPos.y += northOffset;
        shadowPos.z = transform.position.z + 0.1f;  // Slightly behind car
        
        shadowObject.transform.position = shadowPos;
    }

    public void UpdateShadowForRotation(float carRotation)
    {
        if (!isSingleSpriteMode || shadowObject == null) return;

        // Set the shadow's world rotation directly, not relative to car
        shadowObject.transform.rotation = Quaternion.Euler(0, 0, carRotation);
    }

    private void OnDestroy()
    {
        if (shadowObject != null)
        {
            Destroy(shadowObject);
        }
    }
}