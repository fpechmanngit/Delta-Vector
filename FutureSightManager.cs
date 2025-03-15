using UnityEngine;

public class FutureSightManager : MonoBehaviour
{
    [Header("Visual Settings")]
    public GameObject futureMoveIndicatorPrefab;
    public float indicatorAlpha = 0.5f;
    public Color futureIndicatorColor = new Color(0.5f, 0.8f, 1f, 0.5f);

    [Header("Distance Limits")]
    public float maxDistanceMultiplier = 2f; // Maximum allowed distance multiplier from car to future indicator

    [Header("Debug")]
    public bool showDebugLogs = false;

    private PlayerMovement playerMovement;
    private MoveIndicatorManager moveIndicatorManager;
    private GameObject currentFutureIndicator;
    private bool isEasyMode = true;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        moveIndicatorManager = GetComponent<MoveIndicatorManager>();

        if (playerMovement == null)
            Debug.LogError("PlayerMovement component not found!");
        if (moveIndicatorManager == null)
            Debug.LogError("MoveIndicatorManager component not found!");

        isEasyMode = GameInitializationManager.SelectedDifficulty == "Easy";

        if (showDebugLogs)
        {
            Debug.Log($"FutureSightManager initialized. Easy Mode: {isEasyMode}");
        }
    }

    public void ShowFuturePosition(Vector2Int hoveredMovePosition)
    {
        if (!isEasyMode) return;

        // Get current car position
        Vector2Int carPosition = playerMovement.CurrentPosition;

        // Convert hovered position from grid scale to game scale
        Vector2Int unscaledHoverPosition = new Vector2Int(
            hoveredMovePosition.x / PlayerMovement.GRID_SCALE,
            hoveredMovePosition.y / PlayerMovement.GRID_SCALE
        );

        // Calculate relative move offset from car to hovered indicator
        Vector2Int moveOffset = unscaledHoverPosition - carPosition;

        // The future velocity will be equal to this offset
        Vector2Int futureVelocity = moveOffset;

        // Calculate future position
        Vector2Int futurePosition = unscaledHoverPosition + futureVelocity;

        // Safety check: Calculate distances
        float moveDistance = Vector2.Distance(carPosition, unscaledHoverPosition);
        float futureDistance = Vector2.Distance(carPosition, futurePosition);

        if (futureDistance > moveDistance * maxDistanceMultiplier)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"Future indicator too far! Distance: {futureDistance}, Max allowed: {moveDistance * maxDistanceMultiplier}");
            }
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log($"Car position: {carPosition}");
            Debug.Log($"Unscaled hover position: {unscaledHoverPosition} (from {hoveredMovePosition})");
            Debug.Log($"Move offset: {moveOffset}");
            Debug.Log($"Future velocity: {futureVelocity}");
            Debug.Log($"Future position: {futurePosition}");
            Debug.Log($"Move distance: {moveDistance}, Future distance: {futureDistance}");
        }

        // Show the future indicator
        ShowFutureIndicator(futurePosition);
    }

    private void ShowFutureIndicator(Vector2Int position)
    {
        // Clear any existing future indicator first
        ClearFutureIndicator();

        // Create new indicator
        if (futureMoveIndicatorPrefab != null)
        {
            Vector3 worldPosition = new Vector3(position.x, position.y, 0);
            currentFutureIndicator = Instantiate(futureMoveIndicatorPrefab, worldPosition, Quaternion.identity);

            // Make it semi-transparent and tinted
            SpriteRenderer renderer = currentFutureIndicator.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = futureIndicatorColor;
            }

            if (showDebugLogs)
            {
                Debug.Log($"Created future indicator at position {position}");
                Debug.Log($"World position: {worldPosition}");
            }
        }
    }

    public void ClearFutureIndicator()
    {
        if (currentFutureIndicator != null)
        {
            Destroy(currentFutureIndicator);
            currentFutureIndicator = null;

            if (showDebugLogs)
            {
                Debug.Log("Cleared future indicator");
            }
        }
    }

    private void OnDestroy()
    {
        ClearFutureIndicator();
    }
}