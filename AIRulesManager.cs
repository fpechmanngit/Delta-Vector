using UnityEngine;
using DeltaVector.AI;

/// <summary>
/// Central manager for all AI rules and settings in Delta Vector.
/// This singleton persists between scenes and provides global configuration for all AI components.
/// Refactored to use AISettings and AISettingsUI components for improved code organization.
/// Modified to not auto-create components - they must be manually added in the editor.
/// </summary>
public class AIRulesManager : MonoBehaviour
{
    // Singleton instance
    private static AIRulesManager _instance;
    public static AIRulesManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                // Try to find existing instance
                _instance = FindFirstObjectByType<AIRulesManager>();
                
                // CHANGED: Do NOT automatically create a new instance
                // If none exists, return null instead of creating one
                if (_instance == null)
                {
                    Debug.LogWarning("AIRulesManager requested but no instance exists in the scene. " +
                                     "Add an AIRulesManager component manually to a game object in your scene.");
                    return null;
                }
            }
            return _instance;
        }
    }
    
    // Reference to components
    private AISettings settings;
    private AISettingsUI settingsUI;
    
    // References to these settings for quick access (synced with settings component)
    [Header("Proxied Core Settings")]
    public bool enableTestFeatures = false;
    public int pathfindingDepth = 5;
    public bool manualStepMode = true;
    public bool showPathVisualization = true;
    public bool createPathVisualizer = true;
    
    // Proxied evaluation weights (synced with settings component)
    [Header("Proxied Weights")]
    public float distanceWeight = 5f;
    public float speedWeight = 6f;
    public float terrainWeight = 10f;
    public float directionWeight = 3f;
    public float pathWeight = 8f;
    public float returnToAsphaltWeight = 12f;
    public float centerTrackWeight = 4f;
    public float trackExitPenaltyWeight = 15f;
    
    // Proxied speed settings (synced with settings component)
    [Header("Proxied Speed Settings")]
    public float maxStraightSpeed = 7.0f;
    public float maxTurnSpeed = 3.5f;
    
    // Proxied performance settings (synced with settings component)
    [Header("Proxied Performance Settings")]
    public bool adaptiveRendering = true;
    public bool drawPathNodes = true;
    
    // Proxied pruning settings (synced with settings component)
    [Header("Proxied Pruning Settings")]
    public bool enablePathPruning = true;
    public int offTrackToleranceCount = 1;
    public float minTerrainQualityThreshold = 0.1f;
    
    // Proxied enhanced pruning settings (synced with settings component)
    [Header("Proxied Enhanced Pruning Settings")]
    public bool enableAggressivePruning = true;
    public float scorePruningThreshold = 0.3f;
    public float depthPruningFactor = 0.5f;
    public bool enableLookAheadPruning = true;
    public int lookAheadDistance = 2;
    public bool pruneIneffcientMovements = true;
    public bool pruneExcessiveSpeedAtTurns = true;
    
    // Proxied chunked processing settings (synced with settings component)
    [Header("Proxied Chunked Processing Settings")]
    public bool enableChunkedProcessing = true;
    public float targetThinkingTime = 16.0f;
    public float postThinkingDelay = 0.1f;
    public int maxPathsPerFrame = 200;

    // Proxied testing settings
    [Header("Testing Settings")]
    [Tooltip("When enabled, Player1's turn is automatically skipped")]
    public bool skipPlayer1Turn = false;

    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Get references to components
        settings = GetComponent<AISettings>();
        if (settings == null)
        {
            Debug.LogWarning("AISettings component not found. Settings will use default values.");
        }
        
        settingsUI = GetComponent<AISettingsUI>();
        if (settingsUI == null)
        {
            Debug.LogWarning("AISettingsUI component not found. Settings UI will not be available.");
        }
        
        // Sync settings from the settings component if it exists
        if (settings != null)
        {
            SyncSettingsFromComponent();
        }
    }
    
    private void Start()
    {
        // Apply settings to all AI components at startup
        ApplySettingsToAll();
        
        // Specifically handle test enablers
        ApplyTestEnablerSettings();
    }
    
    /// <summary>
    /// Sync settings from the AISettings component to the proxy variables
    /// </summary>
    private void SyncSettingsFromComponent()
    {
        if (settings == null) return;
        
        // Sync core settings
        enableTestFeatures = settings.enableTestFeatures;
        pathfindingDepth = settings.pathfindingDepth;
        manualStepMode = settings.manualStepMode;
        showPathVisualization = settings.showPathVisualization;
        createPathVisualizer = settings.createPathVisualizer;
        
        // Sync weights
        distanceWeight = settings.distanceWeight;
        speedWeight = settings.speedWeight;
        terrainWeight = settings.terrainWeight;
        directionWeight = settings.directionWeight;
        pathWeight = settings.pathWeight;
        returnToAsphaltWeight = settings.returnToAsphaltWeight;
        centerTrackWeight = settings.centerTrackWeight;
        trackExitPenaltyWeight = settings.trackExitPenaltyWeight;
        
        // Sync speed settings
        maxStraightSpeed = settings.maxStraightSpeed;
        maxTurnSpeed = settings.maxTurnSpeed;
        
        // Sync performance settings
        adaptiveRendering = settings.adaptiveRendering;
        drawPathNodes = settings.drawPathNodes;
        
        // Sync pruning settings
        enablePathPruning = settings.enablePathPruning;
        offTrackToleranceCount = settings.offTrackToleranceCount;
        minTerrainQualityThreshold = settings.minTerrainQualityThreshold;
        
        // Sync enhanced pruning settings
        enableAggressivePruning = settings.enableAggressivePruning;
        scorePruningThreshold = settings.scorePruningThreshold;
        depthPruningFactor = settings.depthPruningFactor;
        enableLookAheadPruning = settings.enableLookAheadPruning;
        lookAheadDistance = settings.lookAheadDistance;
        pruneIneffcientMovements = settings.pruneIneffcientMovements;
        pruneExcessiveSpeedAtTurns = settings.pruneExcessiveSpeedAtTurns;
        
        // Sync chunked processing settings
        enableChunkedProcessing = settings.enableChunkedProcessing;
        targetThinkingTime = settings.targetThinkingTime;
        postThinkingDelay = settings.postThinkingDelay;
        maxPathsPerFrame = settings.maxPathsPerFrame;
        
        // Sync testing settings
        skipPlayer1Turn = settings.skipPlayer1Turn;
    }
    
    /// <summary>
    /// Sync settings from the proxy variables to the AISettings component
    /// </summary>
    private void SyncSettingsToComponent()
    {
        if (settings == null) return;
        
        // Sync core settings
        settings.enableTestFeatures = enableTestFeatures;
        settings.pathfindingDepth = pathfindingDepth;
        settings.manualStepMode = manualStepMode;
        settings.showPathVisualization = showPathVisualization;
        settings.createPathVisualizer = createPathVisualizer;
        
        // Sync weights
        settings.distanceWeight = distanceWeight;
        settings.speedWeight = speedWeight;
        settings.terrainWeight = terrainWeight;
        settings.directionWeight = directionWeight;
        settings.pathWeight = pathWeight;
        settings.returnToAsphaltWeight = returnToAsphaltWeight;
        settings.centerTrackWeight = centerTrackWeight;
        settings.trackExitPenaltyWeight = trackExitPenaltyWeight;
        
        // Sync speed settings
        settings.maxStraightSpeed = maxStraightSpeed;
        settings.maxTurnSpeed = maxTurnSpeed;
        
        // Sync performance settings
        settings.adaptiveRendering = adaptiveRendering;
        settings.drawPathNodes = drawPathNodes;
        
        // Sync pruning settings
        settings.enablePathPruning = enablePathPruning;
        settings.offTrackToleranceCount = offTrackToleranceCount;
        settings.minTerrainQualityThreshold = minTerrainQualityThreshold;
        
        // Sync enhanced pruning settings
        settings.enableAggressivePruning = enableAggressivePruning;
        settings.scorePruningThreshold = scorePruningThreshold;
        settings.depthPruningFactor = depthPruningFactor;
        settings.enableLookAheadPruning = enableLookAheadPruning;
        settings.lookAheadDistance = lookAheadDistance;
        settings.pruneIneffcientMovements = pruneIneffcientMovements;
        settings.pruneExcessiveSpeedAtTurns = pruneExcessiveSpeedAtTurns;
        
        // Sync chunked processing settings
        settings.enableChunkedProcessing = enableChunkedProcessing;
        settings.targetThinkingTime = targetThinkingTime;
        settings.postThinkingDelay = postThinkingDelay;
        settings.maxPathsPerFrame = maxPathsPerFrame;
        
        // Sync testing settings
        settings.skipPlayer1Turn = skipPlayer1Turn;
    }
    
    private void OnValidate()
    {
        // Sync settings to the component
        SyncSettingsToComponent();
    }
    
    /// <summary>
    /// Get the AISettings component reference
    /// </summary>
    public AISettings GetSettings()
    {
        return settings;
    }
    
    /// <summary>
    /// Apply settings to all AI components in the scene
    /// </summary>
    public void ApplySettingsToAll()
    {
        // Make sure our settings are in sync
        SyncSettingsFromComponent();
        
        // Find all UnifiedVectorAI components
        UnifiedVectorAI[] aiComponents = FindObjectsByType<UnifiedVectorAI>(FindObjectsSortMode.None);
        
        foreach (var ai in aiComponents)
        {
            ApplySettingsTo(ai);
        }
        
        // Find all AIPathGenerator components
        AIPathGenerator[] pathGenerators = FindObjectsByType<AIPathGenerator>(FindObjectsSortMode.None);
        
        foreach (var generator in pathGenerators)
        {
            generator.UpdateSettings(this);
        }
        
        // Find all visualizers to update
        AIPathVisualizer[] visualizers = FindObjectsByType<AIPathVisualizer>(FindObjectsSortMode.None);
        
        foreach (var visualizer in visualizers)
        {
            if (visualizer != null)
            {
                visualizer.enabled = showPathVisualization;
                visualizer.adaptiveRendering = adaptiveRendering;
                visualizer.drawPathNodes = drawPathNodes;
            }
        }
        
        Debug.Log($"Applied AI settings to {aiComponents.Length} AI components, {pathGenerators.Length} generators, and {visualizers.Length} visualizers");
    }
    
    /// <summary>
    /// Apply settings to a specific AI component
    /// </summary>
    public void ApplySettingsTo(UnifiedVectorAI ai)
    {
        if (ai == null) return;
        
        // Apply basic settings
        ai.manualStepMode = manualStepMode;
        ai.showPathVisualization = showPathVisualization;
    }
    
    /// <summary>
    /// Apply test enabler settings to all components that might be in test mode
    /// </summary>
    public void ApplyTestEnablerSettings()
    {
        // Find all AITestEnabler components
        AITestEnabler[] testEnablers = FindObjectsByType<AITestEnabler>(FindObjectsSortMode.None);
        
        foreach (var testEnabler in testEnablers)
        {
            if (testEnabler != null)
            {
                // If test features are disabled, force override to false
                if (!enableTestFeatures)
                {
                    testEnabler.overrideWithAI = false;
                    
                    // Disable AI components that might have been added
                    DisableAIComponentsOn(testEnabler.gameObject);
                }
                
                Debug.Log($"Applied test mode setting to {testEnabler.gameObject.name}: Test features {(enableTestFeatures ? "allowed" : "disabled")}");
            }
        }
    }
    
    /// <summary>
    /// Disable AI components on a given GameObject
    /// </summary>
    private void DisableAIComponentsOn(GameObject targetObject)
    {
        if (targetObject == null) return;
        
        // Disable UnifiedVectorAI if present
        UnifiedVectorAI vectorAI = targetObject.GetComponent<UnifiedVectorAI>();
        if (vectorAI != null)
        {
            vectorAI.enabled = false;
        }
        
        // Disable AIPathVisualizer if present
        AIPathVisualizer visualizer = targetObject.GetComponent<AIPathVisualizer>();
        if (visualizer != null)
        {
            visualizer.enabled = false;
        }
        
        // Disable AIPathGenerator if present
        AIPathGenerator generator = targetObject.GetComponent<AIPathGenerator>();
        if (generator != null)
        {
            generator.enabled = false;
        }
    }
}