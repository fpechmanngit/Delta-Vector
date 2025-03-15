using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper component that can be attached to any car to override its control mode.
/// Modified to not automatically add AI components - they must be added manually first.
/// </summary>
public class AITestEnabler : MonoBehaviour
{
    [Header("AI Test Settings")]
    [Tooltip("Enable this to make this car controlled by AI, even in non-AI modes")]
    public bool overrideWithAI = false;
    
    [Tooltip("Show debug logs for AI test mode")]
    public bool showDebugLogs = true;
    
    [Header("AI Configuration")]
    [Tooltip("Step-by-step control with spacebar")]
    public bool enableManualStepMode = true;
    
    [Tooltip("Pathfinding depth (how many moves to look ahead)")]
    [Range(1, 15)]
    public int pathfindingDepth = 3;
    
    [Tooltip("Show visual representation of the AI's thought process")]
    public bool showPathVisualization = true;
    
    [Header("Runtime Status")]
    [SerializeField, ReadOnly]
    private bool isEnabled = false;
    
    [SerializeField, ReadOnly]
    private bool isPlayer1 = false;
    
    [SerializeField, ReadOnly]
    private bool aiComponentsAdded = false;
    
    // References to components
    private PlayerInput playerInput;
    private UnifiedVectorAI vectorAI;
    private AIPathVisualizer visualizer;
    
    // Reference to game manager
    private GameManager gameManager;
    
    // Reference to rules manager
    private AIRulesManager rulesManager;
    
    // For UI status display
    private Text statusText;
    private GameObject statusPanel;

    private void Start()
    {
        // Get component references
        playerInput = GetComponent<PlayerInput>();
        
        // Check if this is Player1
        isPlayer1 = gameObject.CompareTag("Player1");
        
        // Find game manager
        gameManager = FindFirstObjectByType<GameManager>();
        
        // Find rules manager (don't auto-create it)
        rulesManager = FindFirstObjectByType<AIRulesManager>();
        
        // Set up UI status if we're showing visualization
        if (showPathVisualization)
        {
            CreateStatusUI();
        }
        
        // Check for AIRulesManager to sync settings from global
        SyncSettingsFromGlobal();
        
        // Set up initial state based on the inspector toggle and global test mode
        CheckAndEnableAI();
        
        LogStatus("AI Test Enabler initialized");
    }

    /// <summary>
    /// Synchronize settings from global AIRulesManager if available
    /// </summary>
    private void SyncSettingsFromGlobal()
    {
        if (rulesManager != null)
        {
            // Update local settings from global manager
            pathfindingDepth = rulesManager.pathfindingDepth;
            enableManualStepMode = rulesManager.manualStepMode;
            showPathVisualization = rulesManager.showPathVisualization;
            
            LogStatus($"Synchronized settings from AIRulesManager: depth={pathfindingDepth}, manualMode={enableManualStepMode}");
        }
        else
        {
            LogStatus("AIRulesManager not found, using local settings");
        }
    }

    private void Update()
    {
        // Check for global test mode disable
        if (rulesManager != null && !rulesManager.enableTestFeatures && isEnabled)
        {
            DisableAIControl();
            return;
        }
        
        // Check if the toggle has changed
        if (overrideWithAI && !isEnabled && IsTestModeAllowed())
        {
            EnableAIControl();
        }
        else if ((!overrideWithAI || !IsTestModeAllowed()) && isEnabled)
        {
            DisableAIControl();
        }
        
        // If we're AI controlled and player's turn, ensure we get control
        if (isEnabled && gameManager != null && playerInput != null)
        {
            // In Time Trial or if it's our turn in other modes
            bool shouldHaveControl = gameManager.IsSpawnPhaseComplete && 
                                    (GameInitializationManager.SelectedGameMode == GameMode.TimeTrial ||
                                     (isPlayer1 && gameManager.IsPlayer1Turn) || 
                                     (!isPlayer1 && !gameManager.IsPlayer1Turn));
                                     
            if (shouldHaveControl && !playerInput.enabled)
            {
                // Update the input state to prevent player inputs
                playerInput.enabled = false;
                
                // Start AI turn if we just got control and we're not already processing
                if (vectorAI != null && !vectorAI.IsProcessingTurn)
                {
                    // Small delay to ensure all components are ready
                    Invoke("StartAITurn", 0.1f);
                }
            }
        }
        
        // Update status panel
        UpdateStatusUI();
    }
    
    /// <summary>
    /// Check if test mode is allowed by global settings
    /// </summary>
    private bool IsTestModeAllowed()
    {
        // If no rules manager or it allows test features, then allowed
        return rulesManager == null || rulesManager.enableTestFeatures;
    }
    
    private void LateUpdate()
    {
        // Extra check to ensure AI gets control when needed
        if (isEnabled && vectorAI != null && !vectorAI.IsProcessingTurn)
        {
            if (gameManager != null && gameManager.IsSpawnPhaseComplete)
            {
                // Force the AI to take its turn
                if (Time.frameCount % 60 == 0) // Check every ~1 second
                {
                    Debug.Log("AITestEnabler forcing AI to take turn");
                    StartAITurn();
                }
            }
        }
    }
    
    /// <summary>
    /// Check configuration and enable AI if conditions are met
    /// </summary>
    private void CheckAndEnableAI()
    {
        if (overrideWithAI && IsTestModeAllowed())
        {
            EnableAIControl();
        }
    }
    
    public void StartAITurn()
    {
        if (vectorAI != null && !vectorAI.IsProcessingTurn)
        {
            vectorAI.StartTurn();
            LogStatus("AI turn started");
        }
    }

    private void EnableAIControl()
    {
        if (isEnabled)
            return;
            
        // Check if test mode is allowed globally
        if (!IsTestModeAllowed())
        {
            LogStatus("AI Test mode is disabled globally");
            return;
        }
            
        // Check required AI components
        CheckRequiredAIComponents();
        
        if (vectorAI != null)
        {
            // Disable player input
            if (playerInput != null)
            {
                playerInput.enabled = false;
                LogStatus("Player input disabled");
            }
            
            // Enable AI components
            vectorAI.enabled = true;
            
            if (visualizer != null)
            {
                visualizer.enabled = showPathVisualization;
            }
            
            isEnabled = true;
            LogStatus("AI control enabled");
        }
        else
        {
            Debug.LogWarning($"Could not enable AI control on {gameObject.name}: Missing UnifiedVectorAI component");
        }
    }

    private void DisableAIControl()
    {
        if (!isEnabled)
            return;
            
        // Enable player input
        if (playerInput != null)
        {
            playerInput.enabled = true;
            LogStatus("Player input enabled");
        }
        
        // Disable AI components
        if (vectorAI != null)
        {
            vectorAI.enabled = false;
        }
        
        if (visualizer != null)
        {
            visualizer.enabled = false;
        }
        
        isEnabled = false;
        LogStatus("AI control disabled");
    }

    private void CheckRequiredAIComponents()
    {
        // Check for UnifiedVectorAI - don't auto-create it
        vectorAI = GetComponent<UnifiedVectorAI>();
        if (vectorAI == null)
        {
            Debug.LogWarning($"AITestEnabler on {gameObject.name} requires a UnifiedVectorAI component. " +
                         "Please add one manually in the inspector.");
            return;
        }
        
        // Configure AI settings using our (now synchronized) settings
        vectorAI.pathfindingDepth = pathfindingDepth;
        vectorAI.manualStepMode = enableManualStepMode;
        vectorAI.showPathVisualization = showPathVisualization;
        
        // Check for visualizer if requested - don't auto-create it
        if (showPathVisualization)
        {
            visualizer = GetComponent<AIPathVisualizer>();
            if (visualizer == null)
            {
                Debug.LogWarning($"AITestEnabler on {gameObject.name} is set to show path visualization " +
                              "but has no AIPathVisualizer component. Please add one manually.");
            }
        }
        
        aiComponentsAdded = (vectorAI != null);
    }

    private void CreateStatusUI()
    {
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UI Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create status panel
        statusPanel = new GameObject("AI Test Status");
        statusPanel.transform.SetParent(canvas.transform, false);
        
        // Set up rect transform
        RectTransform panelRect = statusPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 20);
        panelRect.sizeDelta = new Vector2(400, 40);
        
        // Add background
        Image bg = statusPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
        
        // Add text
        GameObject textObj = new GameObject("Status Text");
        textObj.transform.SetParent(statusPanel.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);
        
        statusText = textObj.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // FIXED: Changed from Arial.ttf
        statusText.fontSize = 16;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        statusText.text = "AI Test Mode: Initializing...";
        
        // Hide initially if not enabled
        statusPanel.SetActive(isEnabled);
    }

    private void UpdateStatusUI()
    {
        if (statusPanel != null)
        {
            statusPanel.SetActive(isEnabled);
            
            if (statusText != null && isEnabled)
            {
                string status = isPlayer1 ? "Player 1" : "Player 2";
                string controlMode = enableManualStepMode ? "MANUAL STEP" : "AUTO";
                
                // Add global test mode indicator
                string globalStatus = IsTestModeAllowed() ? "" : " (GLOBALLY DISABLED)";
                
                statusText.text = $"AI Test Mode: ON{globalStatus} | {status} | {controlMode} | Depth: {pathfindingDepth}";
                statusText.color = enableManualStepMode ? Color.cyan : Color.yellow;
            }
        }
    }

    private void LogStatus(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[AI Test] {message} on {(isPlayer1 ? "Player 1" : "Player 2")}");
        }
    }
    
    private void OnDestroy()
    {
        if (statusPanel != null)
        {
            Destroy(statusPanel);
        }
    }
}

// Utility attribute to display read-only fields in inspector
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(position, property, label);
        EditorGUI.EndDisabledGroup();
    }
}
#endif