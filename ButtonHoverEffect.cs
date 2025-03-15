using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Static settings that apply to all buttons
    [Header("Hover Effect Settings")]
    public float hoverScale = 1.1f;
    public float scaleSpeed = 10f;

    // Optional: Customizable hover color
    public Color hoverColor = Color.white;
    public Color normalColor = Color.white;
    public bool useColorChange = false;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float scaleVelocity;

    // Store original color components
    private Image buttonImage;
    private Color originalImageColor;
    private TMPro.TMP_Text buttonText;
    private Color originalTextColor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        targetScale = originalScale;

        // Get image and text components if they exist
        buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            originalImageColor = buttonImage.color;
        }

        buttonText = GetComponentInChildren<TMPro.TMP_Text>();
        if (buttonText == null)
        {
            buttonText = GetComponent<TMPro.TMP_Text>();
        }
        
        if (buttonText != null)
        {
            originalTextColor = buttonText.color;
        }
    }

    private void OnEnable()
    {
        // Reset state when enabled
        ResetState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only apply effect if the button is interactable
        Button button = GetComponent<Button>();
        if (button == null || button.interactable)
        {
            // Scale up
            targetScale = originalScale * hoverScale;

            // Optional color change
            if (useColorChange)
            {
                if (buttonImage != null)
                {
                    buttonImage.color = hoverColor;
                }
                
                if (buttonText != null)
                {
                    buttonText.color = hoverColor;
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset to original state
        targetScale = originalScale;

        // Reset color
        if (useColorChange)
        {
            if (buttonImage != null)
            {
                buttonImage.color = originalImageColor;
            }
            
            if (buttonText != null)
            {
                buttonText.color = originalTextColor;
            }
        }
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || !enabled) return;

        // Smoothly update scale
        float currentScaleX = rectTransform.localScale.x;
        float targetScaleX = targetScale.x;
        float newScale = Mathf.SmoothDamp(
            currentScaleX, 
            targetScaleX, 
            ref scaleVelocity, 
            1f / scaleSpeed
        );
        rectTransform.localScale = Vector3.one * newScale;
    }

    public void ResetState()
    {
        targetScale = originalScale;
        scaleVelocity = 0f;
        rectTransform.localScale = originalScale;

        // Reset color if changed
        if (useColorChange)
        {
            if (buttonImage != null)
            {
                buttonImage.color = originalImageColor;
            }
            
            if (buttonText != null)
            {
                buttonText.color = originalTextColor;
            }
        }
    }

    // Added method to match the GlobalUIEventHandler call
    public void SetHovered(bool isHovered)
    {
        if (isHovered)
        {
            targetScale = originalScale * hoverScale;

            // Optional color change
            if (useColorChange)
            {
                if (buttonImage != null)
                {
                    buttonImage.color = hoverColor;
                }
                
                if (buttonText != null)
                {
                    buttonText.color = hoverColor;
                }
            }
        }
        else
        {
            targetScale = originalScale;

            // Reset color
            if (useColorChange)
            {
                if (buttonImage != null)
                {
                    buttonImage.color = originalImageColor;
                }
                
                if (buttonText != null)
                {
                    buttonText.color = originalTextColor;
                }
            }
        }
    }
}