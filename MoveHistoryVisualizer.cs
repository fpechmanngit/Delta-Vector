using UnityEngine;

public class MoveHistoryVisualizer : MonoBehaviour
{
    private MoveHistoryManager historyManager;

    private void Awake()
    {
        historyManager = GetComponent<MoveHistoryManager>();
        if (historyManager == null)
        {
            historyManager = gameObject.AddComponent<MoveHistoryManager>();
        }
    }

    public void UpdateVisualization()
    {
        var history = historyManager.GetMoveHistory();
        // Any future visualization logic can go here if needed
    }
}