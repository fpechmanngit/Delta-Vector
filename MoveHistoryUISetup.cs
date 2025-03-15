using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoveHistoryUISetup : MonoBehaviour
{
    public static GameObject CreateHistoryUI()
    {
        GameObject panel = new GameObject("History Panel");
        panel.AddComponent<CanvasGroup>();
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panel.AddComponent<VerticalLayoutGroup>();
        
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.sizeDelta = new Vector2(200, 0);
        
        GameObject scrollView = new GameObject("History Scroll View");
        scrollView.transform.SetParent(panel.transform);
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.sizeDelta = Vector2.zero;
        
        GameObject content = new GameObject("History Content");
        content.transform.SetParent(scrollView.transform);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        
        contentLayout.padding = new RectOffset(10, 10, 10, 10);
        contentLayout.spacing = 5;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        
        return panel;
    }
}