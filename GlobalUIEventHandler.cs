using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class GlobalUIEventHandler : MonoBehaviour
{
    private UIAudioHandler audioHandler;
    private EventSystem eventSystem;
    private GameObject lastHoveredButton;
    private float lastClickTime;
    private const float CLICK_COOLDOWN = 0.05f;
    private bool isEnabled = true;

    // Track currently hovered button effect
    private ButtonHoverEffect lastHoverEffect;

    public void Initialize(UIAudioHandler handler)
    {
        audioHandler = handler;
        eventSystem = EventSystem.current;
        
        // Automatically add hover effects to all buttons
        SetupAllButtonEffects();
    }

    private void SetupAllButtonEffects()
    {
        // Find all buttons in the scene
        Button[] allButtons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None); // Updated to use FindObjectsByType
        
        foreach (Button button in allButtons)
        {
            // Only add effect if it doesn't already have one
            if (button.gameObject.GetComponent<ButtonHoverEffect>() == null)
            {
                ButtonHoverEffect hoverEffect = button.gameObject.AddComponent<ButtonHoverEffect>();
                
                // Optional: Customize hover effect based on button type or location
                if (button.transform.root.name.Contains("MainMenu"))
                {
                    hoverEffect.hoverScale = 1.1f;
                    hoverEffect.useColorChange = false;
                }
                else if (button.transform.root.name.Contains("Pause"))
                {
                    hoverEffect.hoverScale = 1.05f;
                    hoverEffect.useColorChange = true;
                    hoverEffect.hoverColor = new Color(0.8f, 0.8f, 1f); // Soft blue tint
                }
            }
        }
    }

    public void OnRefresh()
    {
        lastClickTime = 0f;
        SetupAllButtonEffects(); // Re-setup effects when UI refreshes
    }

    private void Update()
    {
        if (!isEnabled || eventSystem == null || audioHandler == null) return;

        GameObject currentObject = GetCurrentlyPointedObject();

        // Handle hover state changes
        if (currentObject != lastHoveredButton)
        {
            // Reset previous hover effect
            if (lastHoverEffect != null)
            {
                lastHoverEffect.SetHovered(false);
            }

            if (currentObject != null && 
                currentObject.GetComponent<Button>() != null && 
                Time.unscaledTime > lastClickTime + CLICK_COOLDOWN)
            {
                // Play hover sound
                audioHandler.OnButtonHover(currentObject);

                // Apply hover effect
                ButtonHoverEffect hoverEffect = currentObject.GetComponent<ButtonHoverEffect>();
                if (hoverEffect != null)
                {
                    hoverEffect.SetHovered(true);
                    lastHoverEffect = hoverEffect;
                }
            }

            lastHoveredButton = currentObject;
        }

        // Handle clicks
        if (Input.GetMouseButtonDown(0))
        {
            if (currentObject != null && currentObject.GetComponent<Button>() != null)
            {
                audioHandler.OnButtonClick(currentObject);
                lastClickTime = Time.unscaledTime;
            }
        }
    }

    public void Enable()
    {
        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
        // Reset any active hover effect
        if (lastHoverEffect != null)
        {
            lastHoverEffect.SetHovered(false);
            lastHoverEffect = null;
        }
        lastHoveredButton = null;
    }

    private GameObject GetCurrentlyPointedObject()
    {
        PointerEventData eventData = new PointerEventData(eventSystem);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            Button button = result.gameObject.GetComponent<Button>();
            if (button != null && button.isActiveAndEnabled)
            {
                return result.gameObject;
            }
        }

        return null;
    }
}