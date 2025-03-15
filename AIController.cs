using UnityEngine;

/// <summary>
/// Legacy AI controller class for compatibility with existing code
/// </summary>
public class AIController : MonoBehaviour
{
    // Static properties for controlling AI testing mode
    public static bool testingMode = false;
    public static bool manualStepMode = false;
    public static bool isPaused = false;
    
    // Testing speed multiplier used by TestingModeManager
    public float testingSpeedMultiplier = 2.0f;
    
    // Debug visualization toggle
    public bool showDebugVisuals = false;
    
    private void Awake()
    {
        Debug.Log("AIController: Legacy component initialized. Consider migrating to UnifiedVectorAI.");
    }
    
    private void OnEnable()
    {
        // Intentionally empty
    }
    
    private void OnDisable()
    {
        // Intentionally empty
    }
}