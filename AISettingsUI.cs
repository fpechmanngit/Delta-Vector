using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace DeltaVector.AI
{
    /// <summary>
    /// Manages the UI for AI settings configuration
    /// </summary>
    public class AISettingsUI : MonoBehaviour
    {
        [Header("UI References")]
        public Canvas uiCanvas;
        public GameObject settingsPanel;
        
        [Header("UI Settings")]
        public KeyCode toggleUIKey = KeyCode.F1;
        public Vector2 uiPanelPosition = new Vector2(0, 0);
        public Vector2 uiPanelSize = new Vector2(400, 900);
        public Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        public Color headerColor = new Color(0.2f, 0.7f, 1f);
        
        // UI elements for settings panel
        private Toggle testFeaturesToggle;
        private Slider depthSlider;
        private TMP_Text depthText;
        private Toggle manualModeToggle;
        private Toggle showVisualizationToggle;
        private Toggle createVisualizerToggle;
        private Toggle adaptiveRenderingToggle;
        private Toggle drawNodesToggle;
        private Toggle pathPruningToggle;
        private Slider toleranceSlider;
        private TMP_Text toleranceText;
        
        // Enhanced pruning UI elements
        private Toggle aggressivePruningToggle;
        private Slider scorePruningSlider;
        private TMP_Text scorePruningText;
        private Slider depthPruningSlider;
        private TMP_Text depthPruningText;
        private Toggle lookAheadPruningToggle;
        private Slider lookAheadDistanceSlider;
        private TMP_Text lookAheadDistanceText;
        private Toggle inefficientMovementsToggle;
        private Toggle excessiveSpeedToggle;
        
        // Chunked processing UI elements
        private Toggle chunkedProcessingToggle;
        private Slider thinkingTimeSlider;
        private TMP_Text thinkingTimeText;
        private Slider thinkingDelaySlider;
        private TMP_Text thinkingDelayText;
        private Slider pathsPerFrameSlider;
        private TMP_Text pathsPerFrameText;
        
        // Testing UI elements
        private Toggle skipPlayer1TurnToggle;
        
        // Preset elements
        private TMP_Dropdown presetDropdown;
        private TMP_InputField presetNameInput;
        private Button savePresetButton;
        private Button loadPresetButton;
        
        // Reference to the settings and manager
        private AISettings aiSettings;
        private AIRulesManager rulesManager;
        
        // Flag for first-time initialization
        private bool isInitialized = false;
        
        private void Awake()
        {
            rulesManager = AIRulesManager.Instance;
            if (rulesManager != null)
            {
                aiSettings = rulesManager.GetComponent<AISettings>();
            }
        }
        
        private void Start()
        {
            // Initialize UI if references are set
            if (settingsPanel != null)
            {
                InitializeUI();
                settingsPanel.SetActive(false);
            }
        }
        
        private void Update()
        {
            // Toggle settings panel with hotkey
            if (Input.GetKeyDown(toggleUIKey))
            {
                ToggleSettingsPanel();
            }
        }
        
        /// <summary>
        /// Toggle settings panel visibility
        /// </summary>
        public void ToggleSettingsPanel()
        {
            if (settingsPanel == null)
            {
                CreateSettingsUI();
            }
            
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            
            // Update UI with current values
            if (settingsPanel.activeSelf)
            {
                UpdateUIFromSettings();
            }
        }
        
        /// <summary>
        /// Initialize UI elements
        /// </summary>
        private void InitializeUI()
        {
            if (isInitialized) return;
            
            // Connect UI elements with callbacks
            SetupEventListeners();
            
            // Update UI with current values
            UpdateUIFromSettings();
            
            isInitialized = true;
        }
        
        /// <summary>
        /// Create settings UI panel
        /// </summary>
        private void CreateSettingsUI()
        {
            // Find or create canvas
            if (uiCanvas == null)
            {
                Canvas existingCanvas = FindFirstObjectByType<Canvas>();
                
                if (existingCanvas != null && existingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    uiCanvas = existingCanvas;
                }
                else
                {
                    GameObject canvasObject = new GameObject("AI_Settings_Canvas");
                    uiCanvas = canvasObject.AddComponent<Canvas>();
                    uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObject.AddComponent<CanvasScaler>();
                    canvasObject.AddComponent<GraphicRaycaster>();
                    
                    // Don't destroy canvas
                    DontDestroyOnLoad(canvasObject);
                }
            }
            
            // Create settings panel
            GameObject panelObj = new GameObject("AI_Settings_Panel");
            panelObj.transform.SetParent(uiCanvas.transform, false);
            
            // Set up panel rect transform
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = uiPanelPosition;
            panelRect.sizeDelta = uiPanelSize;
            
            // Add background image
            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = panelColor;
            
            // Add vertical layout
            VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 15;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // Add title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panelObj.transform, false);
            
            TMP_Text titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "AI Rules Manager (Enhanced Pruning)";
            titleText.fontSize = 24;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;
            
            RectTransform titleRect = titleText.rectTransform;
            titleRect.sizeDelta = new Vector2(360, 40);
            
            // Add master test mode toggle
            GameObject testFeaturesObj = CreateToggle(panelObj.transform, "Enable AI Test Features");
            testFeaturesToggle = testFeaturesObj.GetComponentInChildren<Toggle>();
            
            // Add description text
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(panelObj.transform, false);
            
            TMP_Text descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = "AI will generate unlimited paths based on depth.\nUse performance settings to manage visualization.";
            descText.fontSize = 16;
            descText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            descText.alignment = TextAlignmentOptions.Center;
            
            RectTransform descRect = descText.rectTransform;
            descRect.sizeDelta = new Vector2(360, 50);
            
            // Add pathfinding depth slider
            GameObject depthSliderObj = CreateSlider(panelObj.transform, "Pathfinding Depth", 1, 15, 5);
            depthSlider = depthSliderObj.GetComponentInChildren<Slider>();
            depthText = depthSliderObj.GetComponentInChildren<TMP_Text>();
            
            // Add manual mode toggle
            GameObject manualModeObj = CreateToggle(panelObj.transform, "Manual Step Mode");
            manualModeToggle = manualModeObj.GetComponentInChildren<Toggle>();
            
            // Add visualization master toggle
            GameObject showVisualizationObj = CreateToggle(panelObj.transform, "Show Path Visualization");
            showVisualizationToggle = showVisualizationObj.GetComponentInChildren<Toggle>();
            
            // Add create visualizer toggle
            GameObject createVisualizerObj = CreateToggle(panelObj.transform, "Create Path Visualizer Components");
            createVisualizerToggle = createVisualizerObj.GetComponentInChildren<Toggle>();
            
            // Add performance toggles
            GameObject adaptiveRenderingObj = CreateToggle(panelObj.transform, "Adaptive Rendering (For Performance)");
            adaptiveRenderingToggle = adaptiveRenderingObj.GetComponentInChildren<Toggle>();
            
            GameObject drawNodesObj = CreateToggle(panelObj.transform, "Draw Path Nodes");
            drawNodesToggle = drawNodesObj.GetComponentInChildren<Toggle>();
            
            // Add basic path pruning toggle
            GameObject pathPruningObj = CreateToggle(panelObj.transform, "Enable Path Pruning");
            pathPruningToggle = pathPruningObj.GetComponentInChildren<Toggle>();
            
            // Add tolerance slider
            GameObject toleranceSliderObj = CreateSlider(panelObj.transform, "Off-Track Tolerance", 1, 3, 1);
            toleranceSlider = toleranceSliderObj.GetComponentInChildren<Slider>();
            toleranceText = toleranceSliderObj.GetComponentInChildren<TMP_Text>();
            
            // --- Add Testing Settings Section ---
            GameObject testingHeaderObj = new GameObject("TestingHeader");
            testingHeaderObj.transform.SetParent(panelObj.transform, false);
            
            TMP_Text testingHeaderText = testingHeaderObj.AddComponent<TextMeshProUGUI>();
            testingHeaderText.text = "Testing Settings";
            testingHeaderText.fontSize = 20;
            testingHeaderText.color = new Color(1f, 0.5f, 0f); // Orange for testing
            testingHeaderText.alignment = TextAlignmentOptions.Center;
            
            RectTransform testingHeaderRect = testingHeaderText.rectTransform;
            testingHeaderRect.sizeDelta = new Vector2(360, 30);
            
            // Add skip Player1 turn toggle
            GameObject skipPlayer1TurnObj = CreateToggle(panelObj.transform, "Skip Player1 Turn (Auto-AI)");
            skipPlayer1TurnToggle = skipPlayer1TurnObj.GetComponentInChildren<Toggle>();
            
            // --- Add Enhanced Pruning Section ---
            GameObject enhancedPruningHeaderObj = new GameObject("EnhancedPruningHeader");
            enhancedPruningHeaderObj.transform.SetParent(panelObj.transform, false);
            
            TMP_Text enhancedPruningHeaderText = enhancedPruningHeaderObj.AddComponent<TextMeshProUGUI>();
            enhancedPruningHeaderText.text = "Enhanced Pruning Settings";
            enhancedPruningHeaderText.fontSize = 20;
            enhancedPruningHeaderText.color = new Color(0.2f, 0.8f, 0.2f); // Green for enhanced
            enhancedPruningHeaderText.alignment = TextAlignmentOptions.Center;
            
            RectTransform enhancedHeaderRect = enhancedPruningHeaderText.rectTransform;
            enhancedHeaderRect.sizeDelta = new Vector2(360, 30);
            
            // Add aggressive pruning toggle
            GameObject aggressivePruningObj = CreateToggle(panelObj.transform, "Enable Aggressive Pruning");
            aggressivePruningToggle = aggressivePruningObj.GetComponentInChildren<Toggle>();
            
            // Add score threshold slider
            GameObject scorePruningSliderObj = CreateSlider(panelObj.transform, "Score Pruning Threshold", 0.1f, 0.7f, 0.3f);
            scorePruningSlider = scorePruningSliderObj.GetComponentInChildren<Slider>();
            scorePruningText = scorePruningSliderObj.GetComponentInChildren<TMP_Text>();
            
            // Add depth factor slider
            GameObject depthPruningSliderObj = CreateSlider(panelObj.transform, "Depth Pruning Factor", 0f, 1f, 0.5f);
            depthPruningSlider = depthPruningSliderObj.GetComponentInChildren<Slider>();
            depthPruningText = depthPruningSliderObj.GetComponentInChildren<TMP_Text>();
            
            // Add look ahead pruning toggle
            GameObject lookAheadPruningObj = CreateToggle(panelObj.transform, "Enable Look Ahead Pruning");
            lookAheadPruningToggle = lookAheadPruningObj.GetComponentInChildren<Toggle>();
            
            // Add look ahead distance slider
            GameObject lookAheadDistanceSliderObj = CreateSlider(panelObj.transform, "Look Ahead Distance", 1, 3, 2);
            lookAheadDistanceSlider = lookAheadDistanceSliderObj.GetComponentInChildren<Slider>();
            lookAheadDistanceText = lookAheadDistanceSliderObj.GetComponentInChildren<TMP_Text>();
            
            // Add inefficient movements toggle
            GameObject inefficientMovementsObj = CreateToggle(panelObj.transform, "Prune Inefficient Movements");
            inefficientMovementsToggle = inefficientMovementsObj.GetComponentInChildren<Toggle>();
            
            // Add excessive speed toggle
            GameObject excessiveSpeedObj = CreateToggle(panelObj.transform, "Prune Excessive Speed at Turns");
            excessiveSpeedToggle = excessiveSpeedObj.GetComponentInChildren<Toggle>();
            
            // --- Add Chunked Processing Section ---
            GameObject chunkedProcessingHeaderObj = new GameObject("ChunkedProcessingHeader");
            chunkedProcessingHeaderObj.transform.SetParent(panelObj.transform, false);
            
            TMP_Text chunkedProcessingHeaderText = chunkedProcessingHeaderObj.AddComponent<TextMeshProUGUI>();
            chunkedProcessingHeaderText.text = "Chunked Processing Settings";
            chunkedProcessingHeaderText.fontSize = 20;
            chunkedProcessingHeaderText.color = new Color(0.2f, 0.2f, 0.8f); // Blue for chunked
            chunkedProcessingHeaderText.alignment = TextAlignmentOptions.Center;
            
            RectTransform chunkedHeaderRect = chunkedProcessingHeaderText.rectTransform;
            chunkedHeaderRect.sizeDelta = new Vector2(360, 30);
            
            // Add chunked processing toggle
            GameObject chunkedProcessingObj = CreateToggle(panelObj.transform, "Enable Chunked Processing");
            chunkedProcessingToggle = chunkedProcessingObj.GetComponentInChildren<Toggle>();
            
            // Add thinking time slider
            GameObject thinkingTimeSliderObj = CreateSlider(panelObj.transform, "Target Thinking Time (ms)", 5, 50, 16f);
            thinkingTimeSlider = thinkingTimeSliderObj.GetComponentInChildren<Slider>();
            thinkingTimeText = thinkingTimeSliderObj.GetComponentInChildren<TMP_Text>();
            
            // Add thinking delay slider
            GameObject thinkingDelaySliderObj = CreateSlider(panelObj.transform, "Post-Thinking Delay (s)", 0, 1, 0.2f);
            thinkingDelaySlider = thinkingDelaySliderObj.GetComponentInChildren<Slider>();
            thinkingDelayText = thinkingDelaySliderObj.GetComponentInChildren<TMP_Text>();
            
            // Add paths per frame slider
            GameObject pathsPerFrameSliderObj = CreateSlider(panelObj.transform, "Max Paths Per Frame", 10, 2000, 200);
            pathsPerFrameSlider = pathsPerFrameSliderObj.GetComponentInChildren<Slider>();
            pathsPerFrameText = pathsPerFrameSliderObj.GetComponentInChildren<TMP_Text>();
            
            // --- Add Preset Section ---
            GameObject presetHeaderObj = new GameObject("PresetHeader");
            presetHeaderObj.transform.SetParent(panelObj.transform, false);
            
            TMP_Text presetHeaderText = presetHeaderObj.AddComponent<TextMeshProUGUI>();
            presetHeaderText.text = "Presets";
            presetHeaderText.fontSize = 20;
            presetHeaderText.color = new Color(0.8f, 0.2f, 0.2f); // Red for presets
            presetHeaderText.alignment = TextAlignmentOptions.Center;
            
            RectTransform presetHeaderRect = presetHeaderText.rectTransform;
            presetHeaderRect.sizeDelta = new Vector2(360, 30);
            
            // Add preset dropdown
            GameObject presetDropdownObj = CreateDropdown(panelObj.transform, "Load Preset");
            presetDropdown = presetDropdownObj.GetComponentInChildren<TMP_Dropdown>();
            
            // Add preset name input field
            GameObject presetNameInputObj = CreateInputField(panelObj.transform, "Preset Name");
            presetNameInput = presetNameInputObj.GetComponentInChildren<TMP_InputField>();
            
            // Add preset buttons container
            GameObject presetButtonsObj = new GameObject("PresetButtons");
            presetButtonsObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform presetButtonsRect = presetButtonsObj.AddComponent<RectTransform>();
            presetButtonsRect.sizeDelta = new Vector2(360, 40);
            
            // Add horizontal layout for preset buttons
            HorizontalLayoutGroup presetButtonsLayout = presetButtonsObj.AddComponent<HorizontalLayoutGroup>();
            presetButtonsLayout.spacing = 20;
            presetButtonsLayout.childForceExpandWidth = true;
            presetButtonsLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // Add save preset button
            GameObject savePresetButtonObj = CreateButton(presetButtonsObj.transform, "Save Preset");
            savePresetButton = savePresetButtonObj.GetComponent<Button>();
            
            // Add load preset button
            GameObject loadPresetButtonObj = CreateButton(presetButtonsObj.transform, "Load Preset");
            loadPresetButton = loadPresetButtonObj.GetComponent<Button>();
            
            // Add buttons container
            GameObject buttonsObj = new GameObject("Buttons");
            buttonsObj.transform.SetParent(panelObj.transform, false);
            
            RectTransform buttonsRect = buttonsObj.AddComponent<RectTransform>();
            buttonsRect.sizeDelta = new Vector2(360, 40);
            
            // Add horizontal layout for buttons
            HorizontalLayoutGroup buttonsLayout = buttonsObj.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 20;
            buttonsLayout.childForceExpandWidth = true;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // Add Apply button
            GameObject applyButtonObj = CreateButton(buttonsObj.transform, "Apply");
            Button applyButton = applyButtonObj.GetComponent<Button>();
            
            // Add Save button
            GameObject saveButtonObj = CreateButton(buttonsObj.transform, "Save");
            Button saveButton = saveButtonObj.GetComponent<Button>();
            
            // Add Close button
            GameObject closeButtonObj = CreateButton(buttonsObj.transform, "Close");
            Button closeButton = closeButtonObj.GetComponent<Button>();
            
            // Set up button handlers
            applyButton.onClick.AddListener(OnApplyClicked);
            saveButton.onClick.AddListener(OnSaveClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
            savePresetButton.onClick.AddListener(OnSavePresetClicked);
            loadPresetButton.onClick.AddListener(OnLoadPresetClicked);
            
            // Store reference to panel
            settingsPanel = panelObj;
            
            // Set up event listeners
            SetupEventListeners();
            
            // Update UI with current values
            UpdateUIFromSettings();
        }
        
        /// <summary>
        /// Set up event listeners for UI elements
        /// </summary>
        private void SetupEventListeners()
        {
            // Set up slider/toggle handlers - Basic settings
            if (testFeaturesToggle != null)
                testFeaturesToggle.onValueChanged.AddListener(OnTestFeaturesChanged);
            if (depthSlider != null)
                depthSlider.onValueChanged.AddListener(OnDepthChanged);
            if (manualModeToggle != null)
                manualModeToggle.onValueChanged.AddListener(OnManualModeChanged);
            if (showVisualizationToggle != null)
                showVisualizationToggle.onValueChanged.AddListener(OnShowVisualizationChanged);
            if (createVisualizerToggle != null)
                createVisualizerToggle.onValueChanged.AddListener(OnCreateVisualizerChanged);
            if (adaptiveRenderingToggle != null)
                adaptiveRenderingToggle.onValueChanged.AddListener(OnAdaptiveRenderingChanged);
            if (drawNodesToggle != null)
                drawNodesToggle.onValueChanged.AddListener(OnDrawNodesChanged);
            if (pathPruningToggle != null)
                pathPruningToggle.onValueChanged.AddListener(OnPathPruningChanged);
            if (toleranceSlider != null)
                toleranceSlider.onValueChanged.AddListener(OnToleranceChanged);
                
            // Set up slider/toggle handlers - Testing settings
            if (skipPlayer1TurnToggle != null)
                skipPlayer1TurnToggle.onValueChanged.AddListener(OnSkipPlayer1TurnChanged);
            
            // Set up slider/toggle handlers - Enhanced pruning
            if (aggressivePruningToggle != null)
                aggressivePruningToggle.onValueChanged.AddListener(OnAggressivePruningChanged);
            if (scorePruningSlider != null)
                scorePruningSlider.onValueChanged.AddListener(OnScorePruningThresholdChanged);
            if (depthPruningSlider != null)
                depthPruningSlider.onValueChanged.AddListener(OnDepthPruningFactorChanged);
            if (lookAheadPruningToggle != null)
                lookAheadPruningToggle.onValueChanged.AddListener(OnLookAheadPruningChanged);
            if (lookAheadDistanceSlider != null)
                lookAheadDistanceSlider.onValueChanged.AddListener(OnLookAheadDistanceChanged);
            if (inefficientMovementsToggle != null)
                inefficientMovementsToggle.onValueChanged.AddListener(OnIneffcientMovementsChanged);
            if (excessiveSpeedToggle != null)
                excessiveSpeedToggle.onValueChanged.AddListener(OnExcessiveSpeedAtTurnsChanged);
            
            // Set up slider/toggle handlers - Chunked processing
            if (chunkedProcessingToggle != null)
                chunkedProcessingToggle.onValueChanged.AddListener(OnChunkedProcessingChanged);
            if (thinkingTimeSlider != null)
                thinkingTimeSlider.onValueChanged.AddListener(OnThinkingTimeChanged);
            if (thinkingDelaySlider != null)
                thinkingDelaySlider.onValueChanged.AddListener(OnThinkingDelayChanged);
            if (pathsPerFrameSlider != null)
                pathsPerFrameSlider.onValueChanged.AddListener(OnPathsPerFrameChanged);
        }
        
        /// <summary>
        /// Update UI elements to match current settings
        /// </summary>
        private void UpdateUIFromSettings()
        {
            if (aiSettings == null) return;
            
            // Basic settings
            if (testFeaturesToggle != null)
                testFeaturesToggle.isOn = aiSettings.enableTestFeatures;
                
            if (depthSlider != null)
                depthSlider.value = aiSettings.pathfindingDepth;
                
            if (manualModeToggle != null)
                manualModeToggle.isOn = aiSettings.manualStepMode;
                
            if (showVisualizationToggle != null)
                showVisualizationToggle.isOn = aiSettings.showPathVisualization;
                
            if (createVisualizerToggle != null)
                createVisualizerToggle.isOn = aiSettings.createPathVisualizer;
                
            if (adaptiveRenderingToggle != null)
                adaptiveRenderingToggle.isOn = aiSettings.adaptiveRendering;
                
            if (drawNodesToggle != null)
                drawNodesToggle.isOn = aiSettings.drawPathNodes;
                
            if (pathPruningToggle != null)
                pathPruningToggle.isOn = aiSettings.enablePathPruning;
                
            if (toleranceSlider != null)
            {
                toleranceSlider.value = aiSettings.offTrackToleranceCount;
                
                if (toleranceText != null)
                    toleranceText.text = aiSettings.offTrackToleranceCount.ToString();
            }
                
            if (depthText != null)
                depthText.text = aiSettings.pathfindingDepth.ToString();
                
            // Testing settings
            if (skipPlayer1TurnToggle != null)
                skipPlayer1TurnToggle.isOn = aiSettings.skipPlayer1Turn;
            
            // Enhanced pruning settings
            if (aggressivePruningToggle != null)
                aggressivePruningToggle.isOn = aiSettings.enableAggressivePruning;
                
            if (scorePruningSlider != null)
            {
                scorePruningSlider.value = aiSettings.scorePruningThreshold;
                if (scorePruningText != null)
                    scorePruningText.text = aiSettings.scorePruningThreshold.ToString("F2");
            }
            
            if (depthPruningSlider != null)
            {
                depthPruningSlider.value = aiSettings.depthPruningFactor;
                if (depthPruningText != null)
                    depthPruningText.text = aiSettings.depthPruningFactor.ToString("F2");
            }
            
            if (lookAheadPruningToggle != null)
                lookAheadPruningToggle.isOn = aiSettings.enableLookAheadPruning;
                
            if (lookAheadDistanceSlider != null)
            {
                lookAheadDistanceSlider.value = aiSettings.lookAheadDistance;
                if (lookAheadDistanceText != null)
                    lookAheadDistanceText.text = aiSettings.lookAheadDistance.ToString();
            }
            
            if (inefficientMovementsToggle != null)
                inefficientMovementsToggle.isOn = aiSettings.pruneIneffcientMovements;
                
            if (excessiveSpeedToggle != null)
                excessiveSpeedToggle.isOn = aiSettings.pruneExcessiveSpeedAtTurns;
                
            // Chunked processing settings
            if (chunkedProcessingToggle != null)
                chunkedProcessingToggle.isOn = aiSettings.enableChunkedProcessing;
                
            if (thinkingTimeSlider != null)
            {
                thinkingTimeSlider.value = aiSettings.targetThinkingTime;
                if (thinkingTimeText != null)
                    thinkingTimeText.text = aiSettings.targetThinkingTime.ToString("F1");
            }
            
            if (thinkingDelaySlider != null)
            {
                thinkingDelaySlider.value = aiSettings.postThinkingDelay;
                if (thinkingDelayText != null)
                    thinkingDelayText.text = aiSettings.postThinkingDelay.ToString("F2");
            }
            
            if (pathsPerFrameSlider != null)
            {
                pathsPerFrameSlider.value = aiSettings.maxPathsPerFrame;
                if (pathsPerFrameText != null)
                    pathsPerFrameText.text = aiSettings.maxPathsPerFrame.ToString();
            }
            
            // Update preset dropdown
            if (presetDropdown != null)
            {
                UpdatePresetDropdown();
            }
        }
        
        /// <summary>
        /// Update settings from UI elements
        /// </summary>
        private void UpdateSettingsFromUI()
        {
            if (aiSettings == null) return;
            
            // Basic settings
            aiSettings.enableTestFeatures = testFeaturesToggle.isOn;
            aiSettings.pathfindingDepth = Mathf.RoundToInt(depthSlider.value);
            aiSettings.manualStepMode = manualModeToggle.isOn;
            aiSettings.showPathVisualization = showVisualizationToggle.isOn;
            aiSettings.createPathVisualizer = createVisualizerToggle.isOn;
            aiSettings.adaptiveRendering = adaptiveRenderingToggle.isOn;
            aiSettings.drawPathNodes = drawNodesToggle.isOn;
            aiSettings.enablePathPruning = pathPruningToggle.isOn;
            aiSettings.offTrackToleranceCount = Mathf.RoundToInt(toleranceSlider.value);
            
            // Testing settings
            aiSettings.skipPlayer1Turn = skipPlayer1TurnToggle.isOn;
            
            // Enhanced pruning settings
            aiSettings.enableAggressivePruning = aggressivePruningToggle.isOn;
            aiSettings.scorePruningThreshold = scorePruningSlider.value;
            aiSettings.depthPruningFactor = depthPruningSlider.value;
            aiSettings.enableLookAheadPruning = lookAheadPruningToggle.isOn;
            aiSettings.lookAheadDistance = Mathf.RoundToInt(lookAheadDistanceSlider.value);
            aiSettings.pruneIneffcientMovements = inefficientMovementsToggle.isOn;
            aiSettings.pruneExcessiveSpeedAtTurns = excessiveSpeedToggle.isOn;
            
            // Chunked processing settings
            aiSettings.enableChunkedProcessing = chunkedProcessingToggle.isOn;
            aiSettings.targetThinkingTime = thinkingTimeSlider.value;
            aiSettings.postThinkingDelay = thinkingDelaySlider.value;
            aiSettings.maxPathsPerFrame = Mathf.RoundToInt(pathsPerFrameSlider.value);
        }
        
        /// <summary>
        /// Update preset dropdown with available presets
        /// </summary>
        private void UpdatePresetDropdown()
        {
            if (presetDropdown == null || aiSettings == null) return;
            
            // Get preset names
            List<string> presetNames = aiSettings.GetPresetNames();
            
            // Clear current options
            presetDropdown.ClearOptions();
            
            // Add preset names
            presetDropdown.AddOptions(presetNames);
            
            // Add default option if no presets
            if (presetNames.Count == 0)
            {
                presetDropdown.AddOptions(new List<string> { "No presets" });
                presetDropdown.interactable = false;
            }
            else
            {
                presetDropdown.interactable = true;
            }
        }
        
        #region UI Element Creation Methods
        
        private GameObject CreateSlider(Transform parent, string label, float min, float max, float value)
        {
            GameObject container = new GameObject(label);
            container.transform.SetParent(parent, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(360, 60);
            
            // Add vertical layout
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // Add label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            
            TMP_Text labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.color = Color.white;
            
            RectTransform labelRect = labelText.rectTransform;
            labelRect.sizeDelta = new Vector2(360, 25);
            
            // Add slider container
            GameObject sliderContainer = new GameObject("Slider_Container");
            sliderContainer.transform.SetParent(container.transform, false);
            
            RectTransform sliderContainerRect = sliderContainer.AddComponent<RectTransform>();
            sliderContainerRect.sizeDelta = new Vector2(360, 30);
            
            // Add horizontal layout
            HorizontalLayoutGroup sliderLayout = sliderContainer.AddComponent<HorizontalLayoutGroup>();
            sliderLayout.spacing = 10;
            sliderLayout.childForceExpandWidth = false;
            
            // Create slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(sliderContainer.transform, false);
            
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(260, 30);
            
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            
            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObj.transform, false);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            // Fill area
            GameObject fillArea = new GameObject("Fill_Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            
            RectTransform fillRect = fillArea.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0.25f);
            fillRect.anchorMax = new Vector2(1, 0.75f);
            fillRect.offsetMin = new Vector2(5, 0);
            fillRect.offsetMax = new Vector2(-5, 0);
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            
            RectTransform fillSize = fill.AddComponent<RectTransform>();
            fillSize.anchorMin = Vector2.zero;
            fillSize.anchorMax = new Vector2(0.5f, 1);
            fillSize.pivot = new Vector2(0, 0.5f);
            fillSize.sizeDelta = Vector2.zero;
            
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.7f, 1f);
            
            // Handle area
            GameObject handleArea = new GameObject("Handle_Slide_Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);
            
            // Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            
            // Connect components
            slider.fillRect = fillSize;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            
            // Add value display
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(sliderContainer.transform, false);
            
            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.sizeDelta = new Vector2(80, 30);
            
            TMP_Text valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = value.ToString("F2");
            valueText.fontSize = 16;
            valueText.color = Color.white;
            valueText.alignment = TextAlignmentOptions.Right;
            
            return container;
        }
        
        private GameObject CreateToggle(Transform parent, string label)
        {
            GameObject container = new GameObject(label);
            container.transform.SetParent(parent, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(360, 40);
            
            // Add horizontal layout
            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            
            // Add label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            
            TMP_Text labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.color = Color.white;
            
            RectTransform labelRect = labelText.rectTransform;
            labelRect.sizeDelta = new Vector2(280, 30);
            
            // Create toggle
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(container.transform, false);
            
            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(40, 30);
            
            Toggle toggle = toggleObj.AddComponent<Toggle>();
            
            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(toggleObj.transform, false);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.1f);
            bgRect.anchorMax = new Vector2(1, 0.9f);
            bgRect.sizeDelta = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            
            RectTransform checkRect = checkmark.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.sizeDelta = Vector2.zero;
            
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.7f, 1f);
            
            // Connect components
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = true; // Default state
            
            return container;
        }
        
        private GameObject CreateButton(Transform parent, string text)
        {
            GameObject buttonObj = new GameObject(text);
            buttonObj.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(100, 40);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            
            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TMP_Text buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            // Button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f);
            colors.selectedColor = new Color(0.3f, 0.3f, 0.3f);
            button.colors = colors;
            
            return buttonObj;
        }
        
        private GameObject CreateDropdown(Transform parent, string label)
        {
            GameObject container = new GameObject(label);
            container.transform.SetParent(parent, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(360, 60);
            
            // Add vertical layout
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // Add label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            
            TMP_Text labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.color = Color.white;
            
            RectTransform labelRect = labelText.rectTransform;
            labelRect.sizeDelta = new Vector2(360, 25);
            
            // Create dropdown
            GameObject dropdownObj = new GameObject("Dropdown");
            dropdownObj.transform.SetParent(container.transform, false);
            
            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(360, 30);
            
            // Add required components for dropdown
            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            
            // Add template
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            templateObj.SetActive(false);
            
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150);
            
            Image templateImage = templateObj.AddComponent<Image>();
            templateImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            
            // Add viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = new Vector2(-18, 0);
            viewportRect.pivot = new Vector2(0, 1);
            
            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            // Add content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 28);
            contentRect.pivot = new Vector2(0.5f, 1);
            
            ToggleGroup toggleGroup = contentObj.AddComponent<ToggleGroup>();
            
            // Add item
            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 20);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            
            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.group = toggleGroup;
            
            // Add item background
            GameObject itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            
            RectTransform itemBgRect = itemBgObj.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            
            Image itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            // Add item checkmark
            GameObject itemCheckObj = new GameObject("Item Checkmark");
            itemCheckObj.transform.SetParent(itemObj.transform, false);
            
            RectTransform itemCheckRect = itemCheckObj.AddComponent<RectTransform>();
            itemCheckRect.anchorMin = new Vector2(0, 0.5f);
            itemCheckRect.anchorMax = new Vector2(0, 0.5f);
            itemCheckRect.sizeDelta = new Vector2(20, 20);
            itemCheckRect.pivot = new Vector2(0.5f, 0.5f);
            itemCheckRect.anchoredPosition = new Vector2(10, 0);
            
            Image itemCheckImage = itemCheckObj.AddComponent<Image>();
            itemCheckImage.color = new Color(0.2f, 0.7f, 1f);
            
            // Add item label
            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            
            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = new Vector2(0, 0);
            itemLabelRect.anchorMax = new Vector2(1, 1);
            itemLabelRect.offsetMin = new Vector2(20, 1);
            itemLabelRect.offsetMax = new Vector2(-10, -2);
            
            TMP_Text itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabel.fontSize = 14;
            itemLabel.color = Color.white;
            
            // Add scrollbar
            GameObject scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(templateObj.transform, false);
            
            RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.sizeDelta = new Vector2(18, 0);
            scrollbarRect.pivot = new Vector2(1, 1);
            
            Image scrollbarImage = scrollbarObj.AddComponent<Image>();
            scrollbarImage.color = new Color(0.3f, 0.3f, 0.3f);
            
            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            
            // Add sliding area
            GameObject slidingAreaObj = new GameObject("Sliding Area");
            slidingAreaObj.transform.SetParent(scrollbarObj.transform, false);
            
            RectTransform slidingAreaRect = slidingAreaObj.AddComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(1, 1);
            slidingAreaRect.offsetMax = new Vector2(-1, -1);
            
            // Add handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(slidingAreaObj.transform, false);
            
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(-6, -6);
            handleRect.offsetMax = new Vector2(6, 6);
            
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f);
            
            // Connect components
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = scrollbar;
            
            itemToggle.targetGraphic = itemBgImage;
            itemToggle.graphic = itemCheckImage;
            itemToggle.isOn = true;
            
            dropdown.template = templateRect;
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabel;
            
            return container;
        }
        
        private GameObject CreateInputField(Transform parent, string label)
        {
            GameObject container = new GameObject(label);
            container.transform.SetParent(parent, false);
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(360, 60);
            
            // Add vertical layout
            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            // Add label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            
            TMP_Text labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.color = Color.white;
            
            RectTransform labelRect = labelText.rectTransform;
            labelRect.sizeDelta = new Vector2(360, 25);
            
            // Create input field
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(container.transform, false);
            
            RectTransform inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(360, 30);
            
            // Add required components for input field
            Image inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.2f);
            
            TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
            
            // Add text area
            GameObject textAreaObj = new GameObject("Text Area");
            textAreaObj.transform.SetParent(inputObj.transform, false);
            
            RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -7);
            
            // Add placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textAreaObj.transform, false);
            
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            
            TMP_Text placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = "Enter preset name...";
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.Left;
            
            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textAreaObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            
            // Connect components
            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.targetGraphic = inputImage;
            
            return container;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnApplyClicked()
        {
            UpdateSettingsFromUI();
            
            if (rulesManager != null)
            {
                rulesManager.ApplySettingsToAll();
                rulesManager.ApplyTestEnablerSettings();
            }
        }
        
        private void OnSaveClicked()
        {
            UpdateSettingsFromUI();
            
            if (aiSettings != null)
            {
                aiSettings.SaveSettings();
            }
            
            if (rulesManager != null)
            {
                rulesManager.ApplySettingsToAll();
                rulesManager.ApplyTestEnablerSettings();
            }
        }
        
        private void OnCloseClicked()
        {
            settingsPanel.SetActive(false);
        }
        
        private void OnSavePresetClicked()
        {
            if (aiSettings != null && presetNameInput != null)
            {
                string presetName = presetNameInput.text;
                if (!string.IsNullOrEmpty(presetName))
                {
                    // Update settings from UI first
                    UpdateSettingsFromUI();
                    
                    // Save preset
                    aiSettings.CreatePreset(presetName);
                    
                    // Clear input field
                    presetNameInput.text = string.Empty;
                    
                    // Update preset dropdown
                    UpdatePresetDropdown();
                }
            }
        }
        
        private void OnLoadPresetClicked()
        {
            if (aiSettings != null && presetDropdown != null)
            {
                // Get selected preset name
                string presetName = presetDropdown.options[presetDropdown.value].text;
                
                // Load preset
                if (aiSettings.LoadPreset(presetName))
                {
                    // Update UI from settings
                    UpdateUIFromSettings();
                    
                    if (rulesManager != null)
                    {
                        rulesManager.ApplySettingsToAll();
                        rulesManager.ApplyTestEnablerSettings();
                    }
                }
            }
        }
        
        // Basic settings event handlers
        
        private void OnTestFeaturesChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.enableTestFeatures = value;
                
                // Apply immediately to components
                if (rulesManager != null)
                {
                    rulesManager.ApplyTestEnablerSettings();
                }
            }
        }
        
        private void OnDepthChanged(float value)
        {
            if (aiSettings != null)
            {
                aiSettings.pathfindingDepth = Mathf.RoundToInt(value);
                
                if (depthText != null)
                    depthText.text = aiSettings.pathfindingDepth.ToString();
            }
        }
        
        private void OnManualModeChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.manualStepMode = value;
            }
        }
        
        private void OnShowVisualizationChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.showPathVisualization = value;
            }
        }
        
        private void OnCreateVisualizerChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.createPathVisualizer = value;
            }
        }
        
        private void OnAdaptiveRenderingChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.adaptiveRendering = value;
            }
        }
        
        private void OnDrawNodesChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.drawPathNodes = value;
            }
        }
        
        private void OnPathPruningChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.enablePathPruning = value;
            }
        }
        
        private void OnToleranceChanged(float value)
        {
            if (aiSettings != null)
            {
                aiSettings.offTrackToleranceCount = Mathf.RoundToInt(value);
                
                if (toleranceText != null)
                    toleranceText.text = aiSettings.offTrackToleranceCount.ToString();
            }
        }
        
        // Testing settings event handlers
        
        private void OnSkipPlayer1TurnChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.skipPlayer1Turn = value;
            }
        }
        
        // Enhanced pruning event handlers
        
        private void OnAggressivePruningChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.enableAggressivePruning = value;
            }
        }
        
        private void OnScorePruningThresholdChanged(float value)
        {
            if (aiSettings != null)
            {
                aiSettings.scorePruningThreshold = value;
                
                if (scorePruningText != null)
                    scorePruningText.text = value.ToString("F2");
            }
        }
        
        private void OnDepthPruningFactorChanged(float value)
        {
            if (aiSettings != null)
            {
                aiSettings.depthPruningFactor = value;
                
                if (depthPruningText != null)
                    depthPruningText.text = value.ToString("F2");
            }
        }
        
        private void OnLookAheadPruningChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.enableLookAheadPruning = value;
            }
        }
        
        private void OnLookAheadDistanceChanged(float value)
        {
            if (aiSettings != null)
            {
                aiSettings.lookAheadDistance = Mathf.RoundToInt(value);
                
                if (lookAheadDistanceText != null)
                    lookAheadDistanceText.text = aiSettings.lookAheadDistance.ToString();
            }
        }
        
        private void OnIneffcientMovementsChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.pruneIneffcientMovements = value;
            }
        }
        
        private void OnExcessiveSpeedAtTurnsChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.pruneExcessiveSpeedAtTurns = value;
            }
        }
        
        // Chunked processing event handlers
        
        private void OnChunkedProcessingChanged(bool value)
        {
            if (aiSettings != null)
            {
                aiSettings.enableChunkedProcessing = value;
            }
        }
        
        private void OnThinkingTimeChanged(float value)
        {
            if (aiSettings != null)
            {
                aiSettings.targetThinkingTime = value;
                
                if (thinkingTimeText != null)
                    thinkingTimeText.text = value.ToString("F1");
            }
        }
        
        private void OnThinkingDelayChanged(float value)
        {
            if (aiSettings != null)
            {
                aiSettings.postThinkingDelay = value;
                
                if (thinkingDelayText != null)
                    thinkingDelayText.text = value.ToString("F2");
            }
        }
        
        private void OnPathsPerFrameChanged(float value)
        {
            if (aiSettings != null)
            {
                aiSettings.maxPathsPerFrame = Mathf.RoundToInt(value);
                
                if (pathsPerFrameText != null)
                    pathsPerFrameText.text = aiSettings.maxPathsPerFrame.ToString();
            }
        }
        
        #endregion
    }
}