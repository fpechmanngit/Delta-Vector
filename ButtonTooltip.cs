using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    [Tooltip("The text that will appear when hovering")]
    public string tooltipText = "Button Description";
    
    [Tooltip("Reference to your tooltip prefab")]
    public GameObject tooltipPrefab;

    [Header("Position Settings")]
    [Tooltip("How far above the cursor the tooltip should appear")]
    public float heightOffset = 50f;
    
    [Tooltip("How fast the tooltip moves to target position")]
    public float moveSpeed = 10f;
    
    [Tooltip("How fast the tooltip fades in")]
    public float fadeSpeed = 10f;

    // Static reference to track the currently active tooltip
    private static ButtonTooltip activeTooltip;

    // Private references
    private GameObject currentTooltip;
    private TextMeshProUGUI tooltipTextComponent;
    private CanvasGroup canvasGroup;
    private RectTransform tooltipRect;
    private Camera mainCamera;
    private Canvas parentCanvas;
    
    // State tracking
    private bool isShowingTooltip = false;
    private Vector2 targetPosition;
    private float lastToggleTime;
    private const float TOGGLE_COOLDOWN = 0.1f;

    private void Start()
    {
        // Get required references
        mainCamera = Camera.main;
        parentCanvas = GetComponentInParent<Canvas>();
        
        // Validate references
        if (mainCamera == null || parentCanvas == null)
        {
            Debug.LogError("Missing required references!");
            enabled = false;
            return;
        }

        if (tooltipPrefab == null)
        {
            Debug.LogError("Tooltip prefab not assigned!");
            enabled = false;
            return;
        }
    }

    private void ShowTooltip()
    {
        // If there's another active tooltip, hide it first
        if (activeTooltip != null && activeTooltip != this)
        {
            activeTooltip.HideTooltip();
        }

        // Set this as the active tooltip
        activeTooltip = this;

        if (currentTooltip != null) return;

        // Instantiate the tooltip prefab
        currentTooltip = Instantiate(tooltipPrefab, parentCanvas.transform);
        
        // Get required components
        tooltipTextComponent = currentTooltip.GetComponentInChildren<TextMeshProUGUI>();
        canvasGroup = currentTooltip.GetComponent<CanvasGroup>();
        tooltipRect = currentTooltip.GetComponent<RectTransform>();
        
        if (tooltipTextComponent == null || canvasGroup == null || tooltipRect == null)
        {
            Debug.LogError("Tooltip prefab missing required components!");
            Destroy(currentTooltip);
            return;
        }

        // Ensure tooltip appears above everything but ignores raycasts
        Canvas tooltipCanvas = currentTooltip.GetComponent<Canvas>();
        if (tooltipCanvas == null)
        {
            tooltipCanvas = currentTooltip.AddComponent<Canvas>();
            tooltipCanvas.overrideSorting = true;
        }
        tooltipCanvas.sortingOrder = 9999;

        // Disable all raycast targets in the tooltip
        DisableRaycastTargets(currentTooltip);

        // Make the canvas group ignore raycasts
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Setup initial state
        tooltipTextComponent.text = tooltipText;
        canvasGroup.alpha = 0;
        currentTooltip.SetActive(true);
        currentTooltip.transform.SetAsLastSibling();
        
        // Set pivot for better positioning
        tooltipRect.pivot = new Vector2(0.5f, 0);
        
        isShowingTooltip = true;
    }

    private void DisableRaycastTargets(GameObject obj)
    {
        // Disable raycast target on all Graphic components (Image, Text, etc.)
        foreach (var graphic in obj.GetComponentsInChildren<UnityEngine.UI.Graphic>())
        {
            graphic.raycastTarget = false;
        }

        // Disable any GraphicRaycasters
        foreach (var raycaster in obj.GetComponentsInChildren<GraphicRaycaster>())
        {
            raycaster.enabled = false;
        }
    }

    private void HideTooltip()
    {
        if (currentTooltip != null)
        {
            Destroy(currentTooltip);
            currentTooltip = null;
        }

        isShowingTooltip = false;

        // Clear active tooltip reference if this was the active one
        if (activeTooltip == this)
        {
            activeTooltip = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Time.unscaledTime - lastToggleTime < TOGGLE_COOLDOWN) return;
        
        lastToggleTime = Time.unscaledTime;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void Update()
    {
        if (currentTooltip == null || !isShowingTooltip) return;

        UpdateTooltipPosition();
        
        // Only fade in
        if (canvasGroup.alpha < 1)
        {
            canvasGroup.alpha = Mathf.Min(canvasGroup.alpha + Time.unscaledDeltaTime * fadeSpeed, 1);
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipRect == null || mainCamera == null) return;

        Vector2 canvasPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            Input.mousePosition,
            parentCanvas.worldCamera,
            out canvasPosition))
        {
            canvasPosition.y += heightOffset;
            targetPosition = canvasPosition;
            
            tooltipRect.anchoredPosition = Vector2.Lerp(
                tooltipRect.anchoredPosition,
                targetPosition,
                moveSpeed * Time.unscaledDeltaTime
            );
        }
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }
}