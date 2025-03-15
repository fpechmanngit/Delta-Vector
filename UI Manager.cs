using UnityEngine;

public class UIManager : MonoBehaviour
{
    private GameObject historyUI;
    private GameObject resultsUI;

    void Start()
    {
        CreateMoveHistoryUI();
    }

    private void CreateMoveHistoryUI()
    {
        historyUI = MoveHistoryUISetup.CreateHistoryUI();
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            historyUI.transform.SetParent(canvas.transform, false);
            
            // Position the history UI on the left side
            RectTransform rectTransform = historyUI.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0); // Left bottom
            rectTransform.anchorMax = new Vector2(0, 1); // Left top
            rectTransform.pivot = new Vector2(0, 0.5f);  // Left middle
            rectTransform.anchoredPosition = new Vector2(10, 0); // 10 pixels from left edge
            rectTransform.sizeDelta = new Vector2(150, 0); // Width of 150 pixels

            // Start hidden
            CanvasGroup canvasGroup = historyUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1; // Make visible
                canvasGroup.interactable = true;
            }
        }
        else
        {
            Debug.LogError("No Canvas found in scene!");
        }
    }

    // Method to toggle move history visibility
    public void ToggleMoveHistory(bool show)
    {
        if (historyUI != null)
        {
            CanvasGroup canvasGroup = historyUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1 : 0;
                canvasGroup.interactable = show;
            }
        }
    }

    // Method to handle race end
    public void OnRaceEnded()
    {
        // Hide move history when race ends
        ToggleMoveHistory(false);
    }
}