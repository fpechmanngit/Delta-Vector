using UnityEngine;
using System.Collections;

public enum CarAnimationMode
{
    MultipleSprites,  // Uses multiple sprites for different angles
    SingleSprite      // Uses a single sprite that rotates
}

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public float animationSpeed = 0.5f;  // Animation speed multiplier
    public CarAnimationMode animationMode = CarAnimationMode.MultipleSprites;

    [Header("Sprite Animation")]
    public Sprite[] carDirectionSprites; // Array to hold the 48 directional sprites
    public Sprite singleSprite;          // Single sprite for rotation mode
    private SpriteRenderer spriteRenderer;

    // 48 sprites for full 360 degrees
    private const float ANGLE_PER_FRAME = 7.5f; // 360 / 48 sprites

    private TrailManager trailManager;
    private CameraController cameraController;
    private CarSurfaceEffects surfaceEffects;
    private CarExhaustEffect exhaustEffect;
    private CarShadowEffect shadowEffect;
    private float currentRotation = 0f;  // Track current rotation for single sprite mode

    private void Awake()
    {
        trailManager = GetComponent<TrailManager>();
        surfaceEffects = GetComponent<CarSurfaceEffects>();
        cameraController = Camera.main?.GetComponent<CameraController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        exhaustEffect = GetComponent<CarExhaustEffect>();
        shadowEffect = GetComponent<CarShadowEffect>();

        if (trailManager == null) Debug.LogError("TrailManager component missing!");
        if (cameraController == null) Debug.LogError("CameraController not found on main camera!");
        if (surfaceEffects == null) Debug.LogError("CarSurfaceEffects component missing!");
        if (spriteRenderer == null) Debug.LogError("SpriteRenderer component missing!");
        if (exhaustEffect == null) Debug.LogError("CarExhaustEffect component missing!");
        if (shadowEffect == null) Debug.LogError("CarShadowEffect component missing!");

        // Validate sprite settings based on animation mode
        ValidateSpriteSettings();
    }

    private void ValidateSpriteSettings()
    {
        if (animationMode == CarAnimationMode.MultipleSprites)
        {
            if (carDirectionSprites == null || carDirectionSprites.Length != 48)
            {
                Debug.LogError("Multiple sprites mode requires exactly 48 directional sprites!");
            }
        }
        else // SingleSprite mode
        {
            if (singleSprite == null)
            {
                Debug.LogError("Single sprite mode requires a sprite to be assigned!");
            }
            else
            {
                // Set the single sprite
                spriteRenderer.sprite = singleSprite;
            }

            // Configure shadow effect for single sprite mode if available
            if (shadowEffect != null)
            {
                shadowEffect.ConfigureForSingleSprite(true);
            }
        }
    }

    public void OnSpawn(Vector3 spawnPosition)
    {
        if (cameraController != null)
        {
            cameraController.OnPlayerSpawn();
        }
        
        // Reset rotation and sprite based on animation mode
        if (animationMode == CarAnimationMode.MultipleSprites)
        {
            UpdateSprite(0f);
            transform.rotation = Quaternion.identity;
        }
        else
        {
            currentRotation = 0f;
            transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            if (shadowEffect != null)
            {
                shadowEffect.UpdateShadowForRotation(currentRotation);
            }
        }
    }

    public IEnumerator AnimateMovement(Vector3 startPosition, Vector3 endPosition, float effectiveSpeed)
    {
        if (cameraController != null)
        {
            cameraController.OnPlayerMove();
        }

        Vector3 moveDirection = (endPosition - startPosition).normalized;
        float targetAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

        if (surfaceEffects != null)
        {
            surfaceEffects.StartTrackGeneration(Vector2Int.RoundToInt(endPosition));
        }

        // Update sprite or rotation based on animation mode
        if (animationMode == CarAnimationMode.MultipleSprites)
        {
            UpdateSprite(targetAngle);
        }
        else
        {
            UpdateRotation(targetAngle);
        }

        float moveDuration = 1f / (effectiveSpeed * animationSpeed);
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / moveDuration;
            float currentProgress = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(startPosition, endPosition, currentProgress);

            if (surfaceEffects != null)
            {
                surfaceEffects.UpdateTrackProgress(currentProgress);
            }

            yield return null;
        }

        transform.position = endPosition;

        if (surfaceEffects != null)
        {
            surfaceEffects.FinishTrackGeneration();
        }

        if (trailManager != null)
        {
            trailManager.AddTrailPoint(transform.position);
        }
    }

    private void UpdateSprite(float angle)
    {
        if (spriteRenderer == null || carDirectionSprites == null || carDirectionSprites.Length != 48)
            return;

        // Normalize angle to 0-360 range
        angle = (angle + 360f) % 360f;

        // Calculate sprite index based on angle
        int spriteIndex = Mathf.RoundToInt(angle / ANGLE_PER_FRAME) % 48;
        
        // Update the sprite
        spriteRenderer.sprite = carDirectionSprites[spriteIndex];
        transform.rotation = Quaternion.identity; // Keep rotation neutral

        // Update exhaust position based on current angle
        if (exhaustEffect != null)
        {
            exhaustEffect.UpdateExhaustPosition(angle);
        }
    }

    private void UpdateRotation(float angle)
    {
        if (spriteRenderer == null || singleSprite == null)
            return;

        // Normalize angle to 0-360 range
        angle = (angle + 360f) % 360f;
        
        // Update rotation
        currentRotation = angle;
        transform.rotation = Quaternion.Euler(0, 0, currentRotation);

        // Update exhaust position based on current angle
        if (exhaustEffect != null)
        {
            exhaustEffect.UpdateExhaustPosition(angle);
        }

        // Update shadow rotation for single sprite mode
        if (shadowEffect != null)
        {
            shadowEffect.UpdateShadowForRotation(currentRotation);
        }
    }

    private void OnValidate()
    {
        // Update sprite settings when animation mode changes in inspector
        if (spriteRenderer != null)
        {
            ValidateSpriteSettings();
        }
    }
}