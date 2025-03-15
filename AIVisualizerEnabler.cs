using UnityEngine;

/// <summary>
/// Simple helper component to add and configure enhanced AI visualizations.
/// Modified to not automatically add AI components - only use existing ones.
/// </summary>
public class AIVisualizerEnabler : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Enable to add visualization components to this car")]
    public bool enableVisualizations = true;
    
    [Tooltip("Show rich UI with path statistics")]
    public bool showDebugUI = true;
    
    [Tooltip("Line width for the best path")]
    [Range(0.05f, 0.3f)]
    public float bestPathWidth = 0.15f;
    
    [Header("Color Settings")]
    [Tooltip("Color for the best path")]
    public Color bestPathColor = new Color(0, 1, 0, 0.8f);  // Bright green
    
    [Tooltip("Color for good paths")]
    public Color goodPathColor = new Color(0, 0.8f, 0, 0.6f);  // Green
    
    [Tooltip("Color for average paths")]
    public Color mediumPathColor = new Color(1, 0.8f, 0, 0.6f);  // Orange/Yellow
    
    [Tooltip("Color for poor paths")]
    public Color badPathColor = new Color(1, 0, 0, 0.6f);  // Red
    
    private UnifiedVectorAI vectorAI;
    private AIPathVisualizer pathVisualizer;
    private AIRulesManager rulesManager;
    
    void Start()
    {
        // Check global settings first - don't auto-create AIRulesManager
        rulesManager = FindFirstObjectByType<AIRulesManager>();
        bool globalVisualizerEnabled = true;
        
        if (rulesManager != null)
        {
            // Check if visualizers are globally allowed
            globalVisualizerEnabled = rulesManager.createPathVisualizer;
            
            if (!globalVisualizerEnabled)
            {
                Debug.Log($"AIVisualizerEnabler on {gameObject.name} disabled: createPathVisualizer is OFF in AIRulesManager");
                enabled = false;
                return;
            }
        }
        
        if (!enableVisualizations) return;
        
        // Find existing UnifiedVectorAI component - DON'T add if missing
        vectorAI = GetComponent<UnifiedVectorAI>();
        if (vectorAI == null)
        {
            Debug.LogWarning($"No UnifiedVectorAI component found on {gameObject.name}. " +
                            "Add this component manually before using AIVisualizerEnabler.");
            enabled = false;
            return;
        }
        
        // Find existing path visualizer - DON'T add if missing
        pathVisualizer = GetComponent<AIPathVisualizer>();
        if (pathVisualizer != null)
        {
            // Configure existing visualizer
            pathVisualizer.autoCreateVisuals = true;
            pathVisualizer.showDebugUI = showDebugUI;
            pathVisualizer.bestPathWidth = bestPathWidth;
            pathVisualizer.bestPathColor = bestPathColor;
            pathVisualizer.goodPathColor = goodPathColor;
            pathVisualizer.mediumPathColor = mediumPathColor;
            pathVisualizer.badPathColor = badPathColor;
            
            Debug.Log($"Configured existing AIPathVisualizer on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"No AIPathVisualizer component found on {gameObject.name}. " +
                            "Add this component manually before using AIVisualizerEnabler.");
        }
        
        // Ensure vectorAI is using correct colors when visualizer exists
        if (pathVisualizer != null)
        {
            vectorAI.bestPathColor = bestPathColor;
            vectorAI.goodPathColor = goodPathColor;
            vectorAI.mediumPathColor = mediumPathColor;
            vectorAI.badPathColor = badPathColor;
        }
    }
    
    void SetupAfterDelay()
    {
        // Check if global visualizer creation is enabled
        if (rulesManager != null && !rulesManager.createPathVisualizer)
        {
            Debug.Log($"AIVisualizerEnabler setup canceled: createPathVisualizer is OFF in AIRulesManager");
            return;
        }
        
        // Retry finding the AI component
        vectorAI = GetComponent<UnifiedVectorAI>();
        if (vectorAI != null)
        {
            // Check for existing path visualizer
            pathVisualizer = GetComponent<AIPathVisualizer>();
            if (pathVisualizer != null)
            {
                // Configure existing visualizer
                pathVisualizer.autoCreateVisuals = true;
                pathVisualizer.showDebugUI = showDebugUI;
                pathVisualizer.bestPathWidth = bestPathWidth;
                pathVisualizer.bestPathColor = bestPathColor;
                pathVisualizer.goodPathColor = goodPathColor;
                pathVisualizer.mediumPathColor = mediumPathColor;
                pathVisualizer.badPathColor = badPathColor;
                
                Debug.Log($"Configured existing AIPathVisualizer on {gameObject.name} after delay");
            }
            else
            {
                Debug.LogWarning($"No AIPathVisualizer component found on {gameObject.name} after delay. " +
                                "Add this component manually.");
            }
        }
        else
        {
            Debug.LogWarning("Could not find UnifiedVectorAI component after delay");
        }
    }
    
    [ContextMenu("Refresh Visualization")]
    public void RefreshVisualization()
    {
        if (pathVisualizer != null)
        {
            pathVisualizer.enabled = false;
            pathVisualizer.enabled = true;
            Debug.Log("Visualization refreshed");
        }
    }
}