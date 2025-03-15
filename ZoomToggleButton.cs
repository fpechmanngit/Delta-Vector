using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ZoomToggleButton : MonoBehaviour
{
    [Header("Settings")]
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.white;
    
    [Header("References")]
    public CameraController cameraController;
    
    private Button button;
    private TextMeshProUGUI buttonText;
    private Image buttonImage;
    
    void Start()
    {
        // Get components
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        buttonImage = GetComponent<Image>();
        
        if (button == null || buttonText == null || buttonImage == null)
        {
            Debug.LogError("Missing required components on button!");
            return;
        }
        
        // Find camera controller if not assigned
        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
            if (cameraController == null)
            {
                Debug.LogError("CameraController not found!");
                return;
            }
        }
        
        // Set up button click handler
        button.onClick.AddListener(ToggleZoomMode);
        
        // Initialize visual state based on camera controller
        UpdateVisualState();
    }
    
    void Update()
    {
        // Keep visual state updated
        UpdateVisualState();
    }
    
    public void ToggleZoomMode()
    {
        // Toggle between speed zoom and manual zoom
        cameraController.UseSpeedZoom = !cameraController.UseSpeedZoom;
        
        // Update visual appearance
        UpdateVisualState();
    }
    
    public void UpdateVisualState()
    {
        // Update button appearance based on current mode
        bool isSpeedZoom = cameraController.UseSpeedZoom;
        
        // Update button color
        if (buttonImage != null)
        {
            buttonImage.color = isSpeedZoom ? inactiveColor : activeColor;
        }
        
        // Use consistent text that indicates the button's function
        if (buttonText != null)
        {
            buttonText.text = "Switch Zoom Mode";
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(ToggleZoomMode);
        }
    }
}