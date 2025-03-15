using UnityEngine;

public class PlayerSpeedController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float baseSpeed = 5f;         // Base movement speed
    public float maxSpeed = 20f;         // Maximum allowed speed
    public float minSpeed = 1f;          // Minimum speed (used for gravel)
    public float accelerationFactor = 1f; // How quickly the car gains speed
    public float brakingFactor = 1f;     // How quickly the car loses speed
    public float moveSpeed = 10f;        // Movement speed

    private float currentSpeedMultiplier = 1f;
    
    private AudioManager audioManager;

    private void Awake()
    {
        audioManager = Object.FindFirstObjectByType<AudioManager>();
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        Debug.Log($"Setting speed multiplier to: {multiplier}");
        currentSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        UpdateSpeedBasedEffects();
    }

    public float GetEffectiveSpeed(int gridScale)
    {
        // Calculate base speed with multiplier
        float adjustedSpeed = baseSpeed * currentSpeedMultiplier;
        
        // Apply grid scale
        float scaledSpeed = adjustedSpeed * gridScale;
        
        // Clamp between min and max speeds
        float clampedSpeed = Mathf.Clamp(scaledSpeed, minSpeed * gridScale, maxSpeed * gridScale);
        
        Debug.Log($"Effective Speed - Base: {baseSpeed}, Multiplier: {currentSpeedMultiplier}, " +
                 $"Adjusted: {adjustedSpeed}, Final: {clampedSpeed}");
        
        return clampedSpeed;
    }

    private void UpdateSpeedBasedEffects()
    {
        moveSpeed = baseSpeed * currentSpeedMultiplier;
        
        if (audioManager != null)
        {
            float speedRatio = GetEffectiveSpeed(PlayerMovement.GRID_SCALE) / (maxSpeed * PlayerMovement.GRID_SCALE);
            audioManager.CurrentSpeedRatio = speedRatio;
        }
    }
}