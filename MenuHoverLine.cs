using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuHoverLine : MonoBehaviour
{
    [Header("Line Settings")]
    public Color lineColor = Color.red;
    public float lineWidth = 0.1f;

    [Header("Animation")]
    [Range(0.5f, 1f)]
    public float minAlpha = 0.7f;
    [Range(0.8f, 1f)]
    public float maxAlpha = 1f;
    public float pulseSpeed = 3f;

    private GameObject lineObject;
    private LineRenderer lineRenderer;
    private float currentAlpha = 1f;
    private bool isShowingLine = false;

    private void Start()
    {
        InitializeLine();
        SetupButtonListeners();
    }

    private void InitializeLine()
    {
        // Create line object as child of this manager
        lineObject = new GameObject("MenuHoverLine");
        lineObject.transform.SetParent(transform);
        lineObject.transform.localPosition = Vector3.zero; // Set at manager's position
        
        // Setup line renderer
        lineRenderer = lineObject.AddComponent<LineRenderer>();
        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.renderQueue = 3000; // Ensure it renders on top
        
        lineRenderer.material = lineMaterial;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.sortingOrder = 5000;
        lineRenderer.useWorldSpace = true;
        
        // Initially hide the line
        HideLine();
        
        Debug.Log("Menu hover line initialized at position: " + transform.position);
    }

    private void SetupButtonListeners()
    {
        // Find all buttons in the scene
        Button[] buttons = FindObjectsOfType<Button>();
        
        foreach (Button button in buttons)
        {
            // Add event trigger if it doesn't exist
            EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = button.gameObject.AddComponent<EventTrigger>();
            }

            // Setup enter event
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { OnButtonHover(button); });
            eventTrigger.triggers.Add(enterEntry);

            // Setup exit event
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { HideLine(); });
            eventTrigger.triggers.Add(exitEntry);
        }
    }

    private void OnButtonHover(Button button)
    {
        if (lineRenderer != null)
        {
            // Use the manager's position as start point
            Vector3 startPos = transform.position;
            
            // Get button's world position
            Vector3 buttonWorldPos = button.transform.position;
            
            // Show line from manager to button
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, buttonWorldPos);
            
            // Enable line and start pulsing
            lineRenderer.enabled = true;
            isShowingLine = true;
            
            UpdateLineColor();
            
            Debug.Log($"Drawing line from {startPos} to {buttonWorldPos}");
        }
    }

    private void HideLine()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            isShowingLine = false;
        }
    }

    private void Update()
    {
        if (isShowingLine)
        {
            // Update alpha with sin wave for pulsing effect
            currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            UpdateLineColor();
        }
    }

    private void UpdateLineColor()
    {
        if (lineRenderer != null)
        {
            Color colorWithAlpha = lineColor;
            colorWithAlpha.a = currentAlpha;
            lineRenderer.startColor = colorWithAlpha;
            lineRenderer.endColor = colorWithAlpha;
        }
    }

    private void OnDestroy()
    {
        if (lineObject != null)
        {
            Destroy(lineObject);
        }
    }
}