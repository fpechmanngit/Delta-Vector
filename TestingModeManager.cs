using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages testing mode for AI behavior observation and development
/// </summary>
public class TestingModeManager : MonoBehaviour
{
    [Header("Testing Mode Settings")]
    [Tooltip("Enable to have both cars controlled by AI")]
    public bool enableTestingMode = false;
    
    [Header("Manual Step Mode")]
    [Tooltip("Enable step-by-step control with spacebar")]
    public bool enableManualStepMode = false;
    
    [Tooltip("How much faster cars should move in testing mode")]
    [Range(1f, 5f)]
    public float speedMultiplier = 2f;
    
    [Header("UI Elements")]
    public Toggle testingModeToggle;
    public Toggle manualStepToggle;
    public Slider speedMultiplierSlider;
    public TMP_Text speedValueText;
    public GameObject uiPanelPrefab;
    
    [Header("UI Settings")]
    public KeyCode toggleUIKey = KeyCode.T;
    public Vector2 uiPanelPosition = new Vector2(20, 60);
    public Vector2 uiPanelSize = new Vector2(300, 400);
    public Color uiBackgroundColor = new Color(0, 0, 0, 0.8f);
    public Color uiHeaderColor = new Color(0.2f, 0.6f, 1f);
    public int fontSize = 16;
    
    // Global testing settings - maintained for backward compatibility
    public static bool testingMode = false;
    public static bool manualStepMode = false;
    public static bool isPaused = false;
    
    private GameManager gameManager;
    private Canvas canvas;
    private GameObject uiPanel;
    private bool uiInitialized = false;
    
    private void Awake()
    {
        // Initialize testing mode from inspector settings
        testingMode = enableTestingMode;
        manualStepMode = enableManualStepMode;
        
        // Find the game manager
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogWarning("TestingModeManager: GameManager not found!");
        }
        
        Debug.Log($"TestingModeManager initialized. Testing Mode: {(enableTestingMode ? "ENABLED" : "DISABLED")}");
    }
    
    private void Start()
    {
        // Delay UI creation to ensure all components are ready
        Invoke(nameof(InitializeUI), 0.1f);
        
        // Apply testing mode to existing AI controllers
        UpdateAllAIControllers();
    }
    
    private void Update()
    {
        // Toggle visibility with hotkey
        if (Input.GetKeyDown(toggleUIKey))
        {
            ToggleUIVisibility();
        }
        
        // Always sync inspector values with static values
        if (testingMode != enableTestingMode)
        {
            enableTestingMode = testingMode;
            UpdateAllAIControllers();
            UpdateUI();
        }
        
        if (manualStepMode != enableManualStepMode)
        {
            enableManualStepMode = manualStepMode;
            UpdateUI();
        }
    }
    
    private void InitializeUI()
    {
        if (uiInitialized) return;
        
        // Create canvas if not present
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("TestingModeCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            canvas.sortingOrder = 100; // Ensure it renders on top
        }
        
        if (uiPanelPrefab != null)
        {
            uiPanel = Instantiate(uiPanelPrefab, canvas.transform);
        }
        else
        {
            // Create a default UI panel if prefab not provided
            CreateDefaultUIPanel();
        }
        
        // Set up event listeners
        if (testingModeToggle != null)
        {
            testingModeToggle.isOn = enableTestingMode;
            testingModeToggle.onValueChanged.AddListener(OnTestingModeToggled);
        }
        
        if (manualStepToggle != null)
        {
            manualStepToggle.isOn = enableManualStepMode;
            manualStepToggle.onValueChanged.AddListener(OnManualStepToggled);
        }
        
        if (speedMultiplierSlider != null)
        {
            speedMultiplierSlider.value = speedMultiplier;
            speedMultiplierSlider.onValueChanged.AddListener(OnSpeedMultiplierChanged);
        }
        
        // Update UI with current values
        UpdateUI();
        
        // Hide UI initially if testing mode is not enabled
        if (uiPanel != null && !enableTestingMode)
        {
            uiPanel.SetActive(false);
        }
        
        uiInitialized = true;
        Debug.Log("TestingModeManager: UI initialized");
    }
    
    private void CreateDefaultUIPanel()
    {
        uiPanel = new GameObject("Testing Mode Panel");
        uiPanel.transform.SetParent(canvas.transform, false);
        
        // Add rect transform and make it top-left anchored
        RectTransform panelRect = uiPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = uiPanelPosition;
        panelRect.sizeDelta = uiPanelSize;
        
        // Add a background image
        Image bgImage = uiPanel.AddComponent<Image>();
        bgImage.color = uiBackgroundColor;
        
        // Add vertical layout
        VerticalLayoutGroup layout = uiPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        layout.childForceExpandWidth = true;
        layout.childControlWidth = true;
        
        // Create header
        GameObject headerObj = CreateTextObject("Header", "AI TESTING TOOLS");
        headerObj.transform.SetParent(uiPanel.transform, false);
        TMP_Text headerText = headerObj.GetComponent<TMP_Text>();
        headerText.fontSize = fontSize + 4;
        headerText.color = uiHeaderColor;
        headerText.fontStyle = FontStyles.Bold;
        
        // Create toggle for testing mode
        GameObject testingToggleObj = CreateToggle("Enable Testing Mode");
        testingToggleObj.transform.SetParent(uiPanel.transform, false);
        testingModeToggle = testingToggleObj.GetComponent<Toggle>();
        
        // Create toggle for manual step mode
        GameObject manualToggleObj = CreateToggle("Manual Step Mode");
        manualToggleObj.transform.SetParent(uiPanel.transform, false);
        manualStepToggle = manualToggleObj.GetComponent<Toggle>();
        
        // Create slider for speed multiplier
        GameObject sliderObj = CreateSlider("Speed Multiplier");
        sliderObj.transform.SetParent(uiPanel.transform, false);
        speedMultiplierSlider = sliderObj.GetComponent<Slider>();
        speedValueText = sliderObj.GetComponentInChildren<TMP_Text>();
        
        // Add instructions panel
        GameObject instructionsObj = CreateTextObject("Instructions", 
            "CONTROLS:\n" +
            "• Press SPACE to pause/unpause\n" +
            "• In manual mode, SPACE advances step by step\n" +
            "• Press T to toggle this panel\n\n" +
            "TIP: Manual mode helps understand AI decisions");
        instructionsObj.transform.SetParent(uiPanel.transform, false);
        TMP_Text instructionsText = instructionsObj.GetComponent<TMP_Text>();
        instructionsText.fontSize = fontSize - 2;
        instructionsText.color = Color.yellow;
    }
    
    private GameObject CreateTextObject(string name, string text)
    {
        GameObject obj = new GameObject(name);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(uiPanelSize.x - 20, 100);
        
        TMP_Text textComponent = obj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Left;
        
        return obj;
    }
    
    private GameObject CreateToggle(string label)
    {
        GameObject obj = new GameObject(label + " Toggle");
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(uiPanelSize.x - 20, 30);
        
        // Create horizontal layout for toggle + label
        HorizontalLayoutGroup layout = obj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
        
        // Create toggle
        GameObject toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(obj.transform, false);
        
        RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(30, 30);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(toggleObj.transform, false);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = Color.white;
        
        // Create checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform, false);
        
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.1f, 0.1f);
        checkRect.anchorMax = new Vector2(0.9f, 0.9f);
        checkRect.sizeDelta = Vector2.zero;
        
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = new Color(0.2f, 0.7f, 1f);
        
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = false;
        
        // Create label
        GameObject labelObj = CreateTextObject("Label", label);
        labelObj.transform.SetParent(obj.transform, false);
        
        TMP_Text labelText = labelObj.GetComponent<TMP_Text>();
        labelText.fontSize = fontSize;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(200, 30);
        
        return obj;
    }
    
    private GameObject CreateSlider(string label)
    {
        GameObject obj = new GameObject(label + " Slider");
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(uiPanelSize.x - 20, 60);
        
        // Create vertical layout
        VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperLeft;
        
        // Create label
        GameObject labelObj = CreateTextObject("Label", label);
        labelObj.transform.SetParent(obj.transform, false);
        
        TMP_Text labelText = labelObj.GetComponent<TMP_Text>();
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(uiPanelSize.x - 40, 20);
        
        // Create slider container
        GameObject sliderContainer = new GameObject("Slider Container");
        sliderContainer.transform.SetParent(obj.transform, false);
        
        RectTransform containerRect = sliderContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(uiPanelSize.x - 40, 30);
        
        // Add horizontal layout
        HorizontalLayoutGroup containerLayout = sliderContainer.AddComponent<HorizontalLayoutGroup>();
        containerLayout.spacing = 10;
        containerLayout.childForceExpandWidth = false;
        containerLayout.childAlignment = TextAnchor.MiddleLeft;
        
        // Create actual slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(sliderContainer.transform, false);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(uiPanelSize.x - 100, 20);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.5f, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.sizeDelta = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.7f, 1f);
        
        // Create handle area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);
        
        // Create handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 30);
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        
        // Set up slider properties
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.minValue = 1f;
        slider.maxValue = 5f;
        slider.value = 2f;
        
        // Create value text
        GameObject valueObj = CreateTextObject("Value", "2.0x");
        valueObj.transform.SetParent(sliderContainer.transform, false);
        
        TMP_Text valueText = valueObj.GetComponent<TMP_Text>();
        valueText.fontSize = fontSize;
        valueText.alignment = TextAlignmentOptions.Right;
        
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.sizeDelta = new Vector2(50, 30);
        
        return obj;
    }
    
    private void ToggleUIVisibility()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(!uiPanel.activeSelf);
        }
        else if (!uiInitialized)
        {
            InitializeUI();
        }
    }
    
    private void UpdateUI()
    {
        if (!uiInitialized) return;
        
        if (testingModeToggle != null)
        {
            testingModeToggle.SetIsOnWithoutNotify(enableTestingMode);
        }
        
        if (manualStepToggle != null)
        {
            manualStepToggle.SetIsOnWithoutNotify(enableManualStepMode);
        }
        
        if (speedMultiplierSlider != null)
        {
            speedMultiplierSlider.SetValueWithoutNotify(speedMultiplier);
        }
        
        if (speedValueText != null)
        {
            speedValueText.text = $"{speedMultiplier:F1}x";
        }
    }
    
    public void OnTestingModeToggled(bool isOn)
    {
        // Update testing mode
        enableTestingMode = isOn;
        testingMode = isOn;
        
        // Apply testing mode to all AI controllers
        UpdateAllAIControllers();
        
        // Log status change
        Debug.Log($"Testing Mode: {(isOn ? "ENABLED" : "DISABLED")}");
    }
    
    public void OnManualStepToggled(bool isOn)
    {
        enableManualStepMode = isOn;
        manualStepMode = isOn;
        
        // Can't have both manual step mode and pause mode
        if (isOn)
        {
            isPaused = false;
        }
        
        // Log status change
        Debug.Log($"Manual Step Mode: {(isOn ? "ENABLED" : "DISABLED")}");
    }
    
    public void OnSpeedMultiplierChanged(float value)
    {
        // Update speed multiplier
        speedMultiplier = value;
        
        // Update all UnifiedVectorAI components with new speed info
        UnifiedVectorAI[] vectorAIs = FindObjectsByType<UnifiedVectorAI>(FindObjectsSortMode.None);
        foreach (var ai in vectorAIs)
        {
            // Currently UnifiedVectorAI doesn't have a speed multiplier property
            // This would need to be added if speed control is needed
            Debug.Log($"Speed multiplier set to {value} - would affect UnifiedVectorAI if implemented");
        }
        
        // Update displayed value
        if (speedValueText != null)
        {
            speedValueText.text = $"{speedMultiplier:F1}x";
        }
        
        // Log change
        Debug.Log($"Testing Speed Multiplier set to {speedMultiplier:F1}x");
    }
    
    private void UpdateAllAIControllers()
    {
        // Find all existing UnifiedVectorAI controllers
        UnifiedVectorAI[] vectorAIs = FindObjectsByType<UnifiedVectorAI>(FindObjectsSortMode.None);
        
        // Log what we found
        Debug.Log($"Found {vectorAIs.Length} UnifiedVectorAI components to configure");
        
        if (enableTestingMode)
        {
            GameObject player1 = GameObject.FindGameObjectWithTag("Player1");
            if (player1 != null)
            {
                // Check for existing UnifiedVectorAI
                UnifiedVectorAI existingAI = player1.GetComponent<UnifiedVectorAI>();
                if (existingAI != null)
                {
                    // Configure it for testing mode
                    existingAI.debugMode = true;
                    existingAI.manualStepMode = manualStepMode;
                    Debug.Log("Configured existing UnifiedVectorAI on Player1 for testing mode");
                }
                else
                {
                    Debug.Log("Player1 needs a UnifiedVectorAI component for testing mode. Add it manually.");
                }
                
                // Disable manual input on Player1 during testing mode
                DisablePlayerInput(player1);
            }
        }
        else
        {
            // If testing mode is disabled, leave any AI components intact but re-enable player input
            GameObject player1 = GameObject.FindGameObjectWithTag("Player1");
            if (player1 != null)
            {
                // Enable manual input on Player1
                EnablePlayerInput(player1);
            }
        }
    }
    
    private void DisablePlayerInput(GameObject player)
    {
        if (player == null) return;
        
        PlayerInput input = player.GetComponent<PlayerInput>();
        if (input != null)
        {
            input.enabled = false;
            Debug.Log("Disabled manual input on player for testing mode");
        }
    }
    
    private void EnablePlayerInput(GameObject player)
    {
        if (player == null) return;
        
        PlayerInput input = player.GetComponent<PlayerInput>();
        if (input != null)
        {
            input.enabled = true;
            Debug.Log("Re-enabled manual input on player");
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event listeners
        if (testingModeToggle != null)
        {
            testingModeToggle.onValueChanged.RemoveListener(OnTestingModeToggled);
        }
        
        if (manualStepToggle != null)
        {
            manualStepToggle.onValueChanged.RemoveListener(OnManualStepToggled);
        }
        
        if (speedMultiplierSlider != null)
        {
            speedMultiplierSlider.onValueChanged.RemoveListener(OnSpeedMultiplierChanged);
        }
    }
}