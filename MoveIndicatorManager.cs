using UnityEngine;
using System.Collections.Generic;

public class MoveIndicatorManager : MonoBehaviour
{
    [Header("Indicator Settings")]
    public GameObject moveIndicatorPrefab;
    
    [Header("Indicator Sprites")]
    public Sprite moveIndicatorOffSprite;
    public Sprite moveIndicatorOnSprite;
    
    [Header("Player Colors")]
    public Color player1Color = Color.white;
    public Color player2Color = Color.white;
    
    [Header("Speed Change Colors")]
    public Color accelerationColor = Color.red;    // Speed increase - RED
    public Color maintenanceColor = Color.yellow;  // Speed same - YELLOW
    public Color brakingColor = Color.green;       // Speed decrease - GREEN

    private Queue<GameObject> indicatorPool = new Queue<GameObject>();
    private List<GameObject> activeIndicators = new List<GameObject>();
    private List<Vector2Int> validMovePositions = new List<Vector2Int>();
    
    [SerializeField] private int initialPoolSize = 20;
    private Transform indicatorParent;
    private PlayerMovement playerMovement;
    private PlayerSpeedController speedController;
    private CarStats carStats;
    private bool isPlayer1;
    private Material indicatorMaterial;

    // Track the currently hovered indicator
    private MoveIndicatorClickHandler currentHoveredIndicator;

    private const float BASE_MOVE_UNIT = 1f;

    private void Start()
    {
        InitializeComponents();
        CreateIndicatorPool();
    }

    private void InitializeComponents()
    {
        playerMovement = GetComponent<PlayerMovement>();
        carStats = GetComponent<CarStats>();
        speedController = GetComponent<PlayerSpeedController>();
        
        if (playerMovement == null || carStats == null || speedController == null || moveIndicatorPrefab == null)
            return;

        isPlayer1 = playerMovement.CompareTag("Player1");
        
        GameObject parentObj = new GameObject($"MoveIndicators_{(isPlayer1 ? "Player1" : "Player2")}");
        indicatorParent = parentObj.transform;
        
        indicatorMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    // New method to track and reset hovered indicators
    public void SetActiveHoveredIndicator(MoveIndicatorClickHandler newHoveredIndicator)
    {
        // If we already have a different indicator hovered, reset it
        if (currentHoveredIndicator != null && currentHoveredIndicator != newHoveredIndicator)
        {
            currentHoveredIndicator.ResetHoverState();
        }
        
        // Set the new currently hovered indicator
        currentHoveredIndicator = newHoveredIndicator;
    }
    
    // Reset all hover states
    private void ResetAllHoverStates()
    {
        foreach (GameObject indicator in activeIndicators)
        {
            if (indicator != null)
            {
                MoveIndicatorClickHandler handler = indicator.GetComponent<MoveIndicatorClickHandler>();
                if (handler != null)
                {
                    handler.ResetHoverState();
                }
            }
        }
        currentHoveredIndicator = null;
    }

    public void ShowPossibleMoves(Vector2Int currentPosition, Vector2Int currentVelocity, int maxSpeed)
    {
        // First ensure we clear any existing indicators to prevent buildup
        ClearIndicators();
        validMovePositions.Clear();

        bool isStationary = currentVelocity.magnitude == 0;
        
        // If car is stationary, all directions are potential acceleration
        if (isStationary)
        {
            ShowStationaryMoves(currentPosition);
            return;
        }

        // Get movement direction vector (normalized)
        Vector2 moveDirection = new Vector2(currentVelocity.x, currentVelocity.y).normalized;
        Vector2 turnDirection = new Vector2(-moveDirection.y, moveDirection.x);

        float accelerationSpread = BASE_MOVE_UNIT * (carStats.accelerationRate - 1f);
        float brakeSpread = BASE_MOVE_UNIT * (carStats.brakeRate - 1f);
        float turnSpread = BASE_MOVE_UNIT * (carStats.turnRate - 1f);

        // Calculate CURRENT speed magnitude (rounded to nearest integer)
        float currentSpeedMagnitude = Mathf.Round(currentVelocity.magnitude);

        // Calculate positions for all 9 indicators
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // Calculate base position (maintaining original grid spacing)
                Vector2 basePosition = new Vector2(
                    currentPosition.x + currentVelocity.x + dx,
                    currentPosition.y + currentVelocity.y + dy
                );

                // Calculate how much this move is accelerating or braking
                Vector2 velocityChangeNormalized = new Vector2(dx, dy);
                if (velocityChangeNormalized != Vector2.zero)
                {
                    velocityChangeNormalized.Normalize();
                }

                // Calculate relative movement vectors
                float dotProduct = Vector2.Dot(velocityChangeNormalized, moveDirection);
                float turnComponent = Vector2.Dot(velocityChangeNormalized, turnDirection);

                // Apply spreads based on movement type
                Vector2 spreadOffset = Vector2.zero;

                // Apply acceleration/braking spread
                if (dotProduct > 0.1f) // Accelerating
                {
                    spreadOffset += moveDirection * accelerationSpread;
                }
                else if (dotProduct < -0.1f) // Braking
                {
                    spreadOffset -= moveDirection * brakeSpread;
                }

                // Apply turn spread
                if (Mathf.Abs(turnComponent) > 0.1f)
                {
                    spreadOffset += turnDirection * (turnComponent * turnSpread);
                }

                // Final position with spread applied
                Vector3 finalPosition = new Vector3(
                    basePosition.x + spreadOffset.x,
                    basePosition.y + spreadOffset.y,
                    0
                );

                // Create and position indicator
                GameObject indicator = GetIndicatorFromPool();
                indicator.transform.position = finalPosition;

                var clickHandler = indicator.GetComponent<MoveIndicatorClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = indicator.AddComponent<MoveIndicatorClickHandler>();
                }

                // Store the precise position for movement
                Vector2Int precisePosition = new Vector2Int(
                    Mathf.RoundToInt(finalPosition.x * PlayerMovement.GRID_SCALE),
                    Mathf.RoundToInt(finalPosition.y * PlayerMovement.GRID_SCALE)
                );

                // Calculate NEW velocity if this move is chosen in visual grid units
                Vector2 newVelocity = new Vector2(
                    precisePosition.x / PlayerMovement.GRID_SCALE - currentPosition.x,
                    precisePosition.y / PlayerMovement.GRID_SCALE - currentPosition.y
                );

                // Calculate and round NEW speed magnitude
                float newSpeedMagnitude = Mathf.Round(newVelocity.magnitude);

                // Determine color based on speed change
                Color speedChangeColor;
                if (newSpeedMagnitude > currentSpeedMagnitude)
                {
                    // Speed increasing - RED
                    speedChangeColor = accelerationColor;
                }
                else if (newSpeedMagnitude < currentSpeedMagnitude)
                {
                    // Speed decreasing - GREEN
                    speedChangeColor = brakingColor;
                }
                else
                {
                    // Speed unchanged - YELLOW
                    speedChangeColor = maintenanceColor;
                }

                clickHandler.Initialize(playerMovement, precisePosition);
                clickHandler.SetSprites(moveIndicatorOffSprite, moveIndicatorOnSprite);
                clickHandler.SetSpeedColors(accelerationColor, maintenanceColor, brakingColor);
                
                // Apply speed color immediately
                SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // Preserve alpha of original color
                    float originalAlpha = spriteRenderer.color.a;
                    speedChangeColor.a = originalAlpha;
                    spriteRenderer.color = speedChangeColor;
                }

                activeIndicators.Add(indicator);
                validMovePositions.Add(precisePosition);
            }
        }
    }

    private void ShowStationaryMoves(Vector2Int currentPosition)
    {
        float accelerationSpread = BASE_MOVE_UNIT * (carStats.accelerationRate - 1f);

        // For stationary vehicle, current speed is 0
        float currentSpeedMagnitude = 0;

        // Calculate positions for all 9 directions (including center)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // Calculate base position (maintaining original grid spacing)
                Vector2Int basePosition = new Vector2Int(
                    currentPosition.x + dx,
                    currentPosition.y + dy
                );

                // Calculate movement direction
                Vector2 moveDirection = new Vector2(dx, dy);
                if (moveDirection != Vector2.zero)
                {
                    moveDirection.Normalize();
                }

                // Apply acceleration spread
                Vector2 spreadOffset = moveDirection * accelerationSpread;

                // Final position with spread applied
                Vector3 finalPosition = new Vector3(
                    basePosition.x + spreadOffset.x,
                    basePosition.y + spreadOffset.y,
                    0
                );

                // Create and position indicator
                GameObject indicator = GetIndicatorFromPool();
                indicator.transform.position = finalPosition;

                var clickHandler = indicator.GetComponent<MoveIndicatorClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = indicator.AddComponent<MoveIndicatorClickHandler>();
                }

                // Store the precise position for movement
                Vector2Int precisePosition = new Vector2Int(
                    Mathf.RoundToInt(finalPosition.x * PlayerMovement.GRID_SCALE),
                    Mathf.RoundToInt(finalPosition.y * PlayerMovement.GRID_SCALE)
                );

                // Calculate NEW velocity magnitude
                Vector2 newVelocity = new Vector2(
                    precisePosition.x / PlayerMovement.GRID_SCALE - currentPosition.x,
                    precisePosition.y / PlayerMovement.GRID_SCALE - currentPosition.y
                );
                float newSpeedMagnitude = Mathf.Round(newVelocity.magnitude);

                // For a stationary car:
                // Center cell (0,0) = maintain speed (remains at 0) - YELLOW
                // Any other cell = increases speed - RED
                Color speedChangeColor;
                
                if (dx == 0 && dy == 0)
                {
                    // Center position - maintain speed at 0 - YELLOW
                    speedChangeColor = maintenanceColor;
                }
                else
                {
                    // For stationary car, any movement is acceleration - RED
                    speedChangeColor = accelerationColor;
                }

                clickHandler.Initialize(playerMovement, precisePosition);
                clickHandler.SetSprites(moveIndicatorOffSprite, moveIndicatorOnSprite);
                clickHandler.SetSpeedColors(accelerationColor, maintenanceColor, brakingColor);
                
                // Apply speed color immediately
                SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // Preserve alpha of original color
                    float originalAlpha = spriteRenderer.color.a;
                    speedChangeColor.a = originalAlpha;
                    spriteRenderer.color = speedChangeColor;
                }

                activeIndicators.Add(indicator);
                validMovePositions.Add(precisePosition);
            }
        }
    }

    private void CreateIndicatorPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewIndicator();
        }
    }

    private GameObject GetIndicatorFromPool()
    {
        if (indicatorPool.Count == 0)
        {
            CreateNewIndicator();
        }
        
        GameObject indicator = indicatorPool.Dequeue();
        
        CircleCollider2D collider = indicator.GetComponent<CircleCollider2D>();
        if (collider != null)
        {
            SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                collider.radius = spriteRenderer.bounds.extents.x;
            }
        }

        indicator.SetActive(true);
        return indicator;
    }

    private void CreateNewIndicator()
    {
        GameObject indicator = Instantiate(moveIndicatorPrefab, indicatorParent);
        
        CircleCollider2D collider = indicator.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            collider = indicator.AddComponent<CircleCollider2D>();
        }

        SpriteRenderer spriteRenderer = indicator.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = moveIndicatorOffSprite;
            Color playerColor = isPlayer1 ? player1Color : player2Color;
            spriteRenderer.color = playerColor;
            
            spriteRenderer.material = new Material(indicatorMaterial);
        }

        indicator.layer = LayerMask.NameToLayer(isPlayer1 ? "Player1Indicators" : "Player2Indicators");
        
        indicator.SetActive(false);
        indicatorPool.Enqueue(indicator);
    }

    public void ClearIndicators()
    {
        // First reset all hover states
        ResetAllHoverStates();
        currentHoveredIndicator = null;
        
        // Return active indicators to the pool instead of destroying them
        foreach (GameObject indicator in activeIndicators)
        {
            if (indicator != null)
            {
                var handler = indicator.GetComponent<MoveIndicatorClickHandler>();
                if (handler != null)
                {
                    // Reset the handler state
                    handler.ResetHoverState();
                }
                
                // Return to pool
                indicator.SetActive(false);
                indicatorPool.Enqueue(indicator);
            }
        }
        
        // Clear active indicators list
        activeIndicators.Clear();
        validMovePositions.Clear();
    }

    public bool IsValidMovePosition(Vector2Int position)
    {
        return validMovePositions.Contains(position);
    }

    public List<Vector2Int> GetValidMovePositions()
    {
        return validMovePositions;
    }

    public List<GameObject> GetActiveIndicators()
    {
        return activeIndicators;
    }

    public GameObject GetIndicatorAtPosition(Vector2Int position)
    {
        return activeIndicators.Find(indicator => 
        {
            var handler = indicator.GetComponent<MoveIndicatorClickHandler>();
            if (handler != null)
            {
                // Compare the handler's target position with the given position
                return Vector2Int.Distance(
                    handler.GetTargetPosition(),
                    position
                ) < 0.1f; // Small threshold for float comparison
            }
            return false;
        });
    }

    private void OnDestroy()
    {
        foreach (GameObject indicator in activeIndicators)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        
        foreach (GameObject indicator in indicatorPool)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }

        if (indicatorParent != null)
        {
            Destroy(indicatorParent.gameObject);
        }

        if (indicatorMaterial != null)
        {
            Destroy(indicatorMaterial);
        }
    }
}