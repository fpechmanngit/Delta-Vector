using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInput : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private MoveIndicatorManager moveIndicatorManager;
    private ReplayManager replayManager;
    private HoverLineVisualizer hoverLine;
    private Camera mainCamera;
    private AudioManager audioManager;
    private GameManager gameManager;
    
    private const float inputBufferTime = 0.1f;
    private float lastClickTime;
    private Vector2 lastClickPosition;
    
    private bool isUIOpen = false;
    private const float minClickDistance = 0.1f;
    private Vector2Int lastHoveredPosition = Vector2Int.zero;
    private bool isPlayer1;
    private int playerLayer;

    // Track the currently selected indicator position
    private Vector2Int? selectedMovePosition = null;
    private KeyCode? lastPressedKey = null;

    // Track current game mode
    private GameMode currentGameMode;

    // Define the numpad key mappings to indicator indices in clockwise order starting from North
    private readonly Dictionary<KeyCode, int> keyToIndicatorIndex = new Dictionary<KeyCode, int>
    {
        { KeyCode.Keypad1, 0 },    // North
        { KeyCode.Keypad4, 1 },    // Northeast
        { KeyCode.Keypad7, 2 },    // East
        { KeyCode.Keypad2, 3 },    // Southeast
        { KeyCode.Keypad5, 4 },    // South
        { KeyCode.Keypad8, 5 },    // Southwest
        { KeyCode.Keypad3, 6 },    // West
        { KeyCode.Keypad6, 7 },    // Northwest
        { KeyCode.Keypad9, 8 }     // Center
    };

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        moveIndicatorManager = GetComponent<MoveIndicatorManager>();
        replayManager = GetComponent<ReplayManager>();
        hoverLine = GetComponent<HoverLineVisualizer>();
        mainCamera = Camera.main;
        audioManager = FindFirstObjectByType<AudioManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        
        // Get current game mode
        currentGameMode = GameInitializationManager.SelectedGameMode;
        
        isPlayer1 = CompareTag("Player1");
        playerLayer = LayerMask.NameToLayer(isPlayer1 ? "Player1Indicators" : "Player2Indicators");
        
        if (mainCamera == null)
            Debug.LogError("Main Camera not found!");
        
        if (moveIndicatorManager == null)
            Debug.LogError("MoveIndicatorManager not found!");

        if (hoverLine == null)
            Debug.LogError("HoverLineVisualizer not found!");

        if (gameManager == null)
            Debug.LogError("GameManager not found!");

        ClearAllVisuals();
    }

    private bool IsMyTurn()
    {
        if (gameManager == null) return false;
        // In Challenge mode, it's always the player's turn
        if (currentGameMode == GameMode.Challenges) return true;
        return (isPlayer1 && gameManager.IsPlayer1Turn) || (!isPlayer1 && !gameManager.IsPlayer1Turn);
    }

    void Update()
    {
        if (!enabled) return;
        if (replayManager != null && replayManager.isReplaying) return;
        if (gameManager != null && !gameManager.IsSpawnPhaseComplete) return;
        if (!IsMyTurn())
        {
            ClearAllVisuals();
            return;
        }
        if (!isUIOpen)
        {
            HandleInput();
        }
    }

    private bool CanUseMouse()
    {
        // In Time Trial or Challenge mode, mouse input is always allowed
        if (currentGameMode == GameMode.TimeTrial || currentGameMode == GameMode.Challenges) 
            return true;
        
        // In Race mode, only Player 1 can use mouse
        return currentGameMode == GameMode.Race && isPlayer1;
    }

    private bool CanUseKeyboard()
    {
        // In Time Trial or Challenge mode, keyboard input is always allowed
        if (currentGameMode == GameMode.TimeTrial || currentGameMode == GameMode.Challenges) 
            return true;
        
        // In Race mode, only Player 2 can use keyboard
        return currentGameMode == GameMode.Race && !isPlayer1;
    }

private void HandleInput()
{
    // Skip if it's AI's turn in PvE mode
    if (GameInitializationManager.SelectedGameMode == GameMode.PvE && !GameManager.IsPlayerTurn)
    {
        return;
    }

        bool canUseMouse = CanUseMouse();
        bool canUseKeyboard = CanUseKeyboard();

        // Handle mouse input if allowed
        if (canUseMouse)
        {
            // Handle mouse clicks
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelMove();
                audioManager?.PlayHoverSound();
            }

            // Only process hover for mouse-enabled players
            if (!selectedMovePosition.HasValue)
            {
                UpdateHoverVisualization();
            }
        }
        // For players who can't use mouse, ensure hover visuals are cleared
        else
        {
            ClearHoverVisuals();
        }

        // Handle keyboard input if allowed
        if (canUseKeyboard)
        {
            HandleKeyboardInput();
        }
    }

    private void HandleKeyboardInput()
    {
        foreach (var keyMapping in keyToIndicatorIndex)
        {
            if (Input.GetKeyDown(keyMapping.Key))
            {
                ProcessKeyPress(keyMapping.Key);
                break;
            }
        }
    }

    private void ProcessKeyPress(KeyCode pressedKey)
    {
        // Block input if player is moving
        if (playerMovement != null && playerMovement.IsMoving)
        {
            return;
        }

        // Get all valid move positions
        List<Vector2Int> validMoves = moveIndicatorManager.GetValidMovePositions();
        
        // If there are no valid moves, return
        if (validMoves.Count == 0) return;

        // Get the index for this key
        int indicatorIndex = keyToIndicatorIndex[pressedKey];
        
        // If the index is out of range for our valid moves, return
        if (indicatorIndex >= validMoves.Count) return;

        // Get the target position from the valid moves list
        Vector2Int targetPos = validMoves[indicatorIndex];

        // Clear previous indicator highlights if different key
        if (lastPressedKey.HasValue && lastPressedKey != pressedKey)
        {
            foreach (var indicator in moveIndicatorManager.GetActiveIndicators())
            {
                var handler = indicator.GetComponent<MoveIndicatorClickHandler>();
                if (handler != null)
                {
                    handler.HideHoverEffects();
                }
            }
        }

        // Get the indicator for this position
        GameObject targetIndicator = moveIndicatorManager.GetIndicatorAtPosition(targetPos);
        MoveIndicatorClickHandler clickHandler = targetIndicator?.GetComponent<MoveIndicatorClickHandler>();

        // If we're pressing the same key as before and selected
        if (lastPressedKey == pressedKey && selectedMovePosition.HasValue)
        {
            if (clickHandler != null)
            {
                clickHandler.ExecuteMove();
            }
            // Clear selection
            selectedMovePosition = null;
            lastPressedKey = null;
        }
        // If we're pressing a new key
        else
        {
            // Show preview and effects
            selectedMovePosition = targetPos;
            lastPressedKey = pressedKey;
            
            if (clickHandler != null)
            {
                clickHandler.ShowHoverEffects();
            }
        }
    }

    private void UpdateHoverVisualization()
    {
        // Early return if player can't use mouse
        if (!CanUseMouse()) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        int layerMask = 1 << playerLayer;
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction, Mathf.Infinity, layerMask);

        if (hits.Length == 0)
        {
            ClearHoverVisuals();
            return;
        }

        // Sort hits by distance to get the closest one
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Get the closest indicator
        GameObject hitIndicator = hits[0].collider.gameObject;
        MoveIndicatorClickHandler clickHandler = hitIndicator.GetComponent<MoveIndicatorClickHandler>();

        if (clickHandler != null)
        {
            Vector2Int targetPosition = clickHandler.GetTargetPosition();
            
            if (targetPosition != lastHoveredPosition)
            {
                // Clear previous hover effects
                ClearHoverVisuals();

                // Show new hover effects
                clickHandler.ShowHoverEffects();
                lastHoveredPosition = targetPosition;
            }
        }
        else
        {
            ClearHoverVisuals();
        }
    }

    private void HandleClick()
    {
        // Early return if player can't use mouse
        if (!CanUseMouse()) return;
        if (!IsMyTurn()) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        int layerMask = 1 << playerLayer;
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction, Mathf.Infinity, layerMask);

        if (hits.Length == 0) return;

        // Sort hits by distance to get the closest one
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Get the closest indicator
        GameObject hitIndicator = hits[0].collider.gameObject;
        MoveIndicatorClickHandler clickHandler = hitIndicator.GetComponent<MoveIndicatorClickHandler>();

        if (clickHandler != null)
        {
            Vector2Int targetPosition = clickHandler.GetTargetPosition();

            if (Time.time - lastClickTime < inputBufferTime)
            {
                Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                float clickDistance = Vector2.Distance(mousePosition, lastClickPosition);
                if (clickDistance < minClickDistance)
                {
                    return;
                }
            }

            lastClickTime = Time.time;
            lastClickPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            // Clear any keyboard selection when using mouse
            selectedMovePosition = null;
            lastPressedKey = null;

            if (IsValidMovePosition(targetPosition))
            {
                MakeMove(targetPosition);
            }
            else
            {
                audioManager?.PlayHoverSound();
            }
        }
    }

    private void MakeMove(Vector2Int gridPosition)
    {
        if (!IsMyTurn()) return;
        playerMovement.MoveToIndicator(gridPosition);
        audioManager?.PlayRevSound();
        ClearAllVisuals();
    }

    private void ClearHoverVisuals()
    {
        if (hoverLine != null)
        {
            hoverLine.HideLine();
            lastHoveredPosition = Vector2Int.zero;
        }
    }

    private void ClearAllVisuals()
    {
        if (hoverLine != null)
        {
            hoverLine.HideLine();
            lastHoveredPosition = Vector2Int.zero;
        }
        if (moveIndicatorManager != null)
            moveIndicatorManager.ClearIndicators();
        
        selectedMovePosition = null;
        lastPressedKey = null;
    }

    private void CancelMove()
    {
        ClearAllVisuals();
        
        // Recreate move indicators after canceling
        if (moveIndicatorManager != null && playerMovement != null)
        {
            moveIndicatorManager.ShowPossibleMoves(
                playerMovement.CurrentPosition,
                playerMovement.CurrentVelocity,
                Mathf.RoundToInt(playerMovement.GetComponent<PlayerSpeedController>().GetEffectiveSpeed(PlayerMovement.GRID_SCALE) / PlayerMovement.GRID_SCALE)
            );
        }
    }

    private bool IsValidMovePosition(Vector2Int position)
    {
        if (replayManager != null && replayManager.isReplaying)
            return true;

        if (!IsMyTurn())
            return false;

        List<Vector2Int> validMoves = moveIndicatorManager.GetValidMovePositions();
        return validMoves.Contains(position);
    }

    public void SetUIState(bool isOpen)
    {
        isUIOpen = isOpen;
        if (isOpen)
        {
            ClearAllVisuals();
        }
    }

    private void OnDisable()
    {
        ClearAllVisuals();
    }
}