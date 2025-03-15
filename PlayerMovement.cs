using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public delegate void SpawnCompleteHandler();
    public event SpawnCompleteHandler OnSpawnComplete;

    public SpawnManager spawnManager;
    
    [Header("Grid Settings")]
    public const int GRID_SCALE = 10;

    [Header("Movement Settings")]
    public bool enablePreview = true;
    public bool isReplayMode = false;

    private bool isMoving = false;
    public bool IsMoving { get { return isMoving; } private set { isMoving = value; } }
    private float moveClickCooldown = 0.1f;
    private float lastMoveTime = 0f;

    private Vector2Int precisePosition;
    public Vector2Int CurrentPosition 
    { 
        get 
        {
            return new Vector2Int(
                Mathf.RoundToInt(precisePosition.x / (float)GRID_SCALE),
                Mathf.RoundToInt(precisePosition.y / (float)GRID_SCALE)
            );
        }
        private set
        {
            precisePosition = new Vector2Int(
                value.x * GRID_SCALE,
                value.y * GRID_SCALE
            );
        }
    }

    private Vector2Int preciseVelocity = Vector2Int.zero;
    public Vector2Int CurrentVelocity
    {
        get
        {
            return new Vector2Int(
                Mathf.RoundToInt(preciseVelocity.x / (float)GRID_SCALE),
                Mathf.RoundToInt(preciseVelocity.y / (float)GRID_SCALE)
            );
        }
        private set
        {
            preciseVelocity = new Vector2Int(
                value.x * GRID_SCALE,
                value.y * GRID_SCALE
            );
        }
    }

    private PlayerSpeedController speedController;
    private PlayerGroundDetector groundDetector;
    private PlayerAnimationController animationController;
    private TrailManager trailManager;
    private MoveIndicatorManager moveIndicatorManager;
    private MoveHistoryManager historyManager;
    private LapManager lapManager;
    private AudioManager audioManager;
    private GameManager gameManager;

    void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        speedController = GetComponent<PlayerSpeedController>();
        groundDetector = GetComponent<PlayerGroundDetector>();
        animationController = GetComponent<PlayerAnimationController>();
        trailManager = GetComponent<TrailManager>();
        moveIndicatorManager = GetComponent<MoveIndicatorManager>();
        audioManager = Object.FindFirstObjectByType<AudioManager>();
        gameManager = Object.FindFirstObjectByType<GameManager>();
        historyManager = GetComponent<MoveHistoryManager>();
        lapManager = Object.FindFirstObjectByType<LapManager>();
    }

    void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        lapManager = Object.FindFirstObjectByType<LapManager>();

        Vector2Int initialPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x * GRID_SCALE),
            Mathf.RoundToInt(transform.position.y * GRID_SCALE)
        );
        precisePosition = initialPos;

        transform.position = new Vector3(
            precisePosition.x / (float)GRID_SCALE,
            precisePosition.y / (float)GRID_SCALE,
            0
        );

        if (spawnManager != null && !isReplayMode)
        {
            spawnManager.ShowSpawnSelectionUI(this);
        }
    }

    public void MoveToIndicator(Vector2Int targetPosition)
    {
        if (!isReplayMode)
        {
            if (isMoving || Time.time - lastMoveTime < moveClickCooldown)
            {
                return;
            }

            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.IncrementMoveCount();
            }
        }

        isMoving = true;
        lastMoveTime = Time.time;

        try
        {
            float moveDuration = 1f;
            if (speedController != null)
            {
                float effectiveSpeed = speedController.GetEffectiveSpeed(GRID_SCALE);
                moveDuration = 1f / effectiveSpeed;
            }

            Vector2Int newVelocity = targetPosition - precisePosition;
            
            if (!isReplayMode && historyManager != null)
            {
                historyManager.AddMove(precisePosition, targetPosition, newVelocity, moveDuration);
            }
            
            preciseVelocity = newVelocity;
            precisePosition = targetPosition;

            Vector3 endPosition = new Vector3(
                precisePosition.x / (float)GRID_SCALE,
                precisePosition.y / (float)GRID_SCALE,
                0
            );

            StartCoroutine(MoveToPositionSmooth(endPosition));
        }
        catch (System.Exception e)
        {
            isMoving = false;
        }
    }

    private IEnumerator MoveToPositionSmooth(Vector3 targetPosition)
    {
        isMoving = true;
        
        if (moveIndicatorManager != null)
        {
            moveIndicatorManager.ClearIndicators();
        }

        Vector3 startPosition = transform.position;
        
        Vector3 moveDirection = (targetPosition - startPosition).normalized;
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

        // Trigger smoke effect at start position with the correct world position (not following car)
        CombinedSmokeEffect smokeFX = GetComponent<CombinedSmokeEffect>();
        if (smokeFX != null)
        {
            // Pass exact world position to ensure it stays at start position
            Vector3 exactWorldStartPos = startPosition;
            smokeFX.TriggerSmokeEffect(exactWorldStartPos, angle);
        }

        yield return StartCoroutine(animationController.AnimateMovement(startPosition, targetPosition, speedController.GetEffectiveSpeed(GRID_SCALE)));
        
        if (!isReplayMode)
        {
            groundDetector.CheckGravelStatus(CurrentPosition);
            StartNewTurn();
        }

        isMoving = false;

        if (!isReplayMode && gameManager != null)
        {
            gameManager.EndTurn();
        }
    }

    public void StartNewTurn()
    {
        if (groundDetector.IsOnGravel)
        {
            preciseVelocity = Vector2Int.zero;
        }

        if (trailManager != null)
        {
            trailManager.IncrementTurn();
        }

        GetComponent<CarSurfaceEffects>()?.EndTurn();
    }

    public void SetSpawnPoint(Vector2Int spawnPosition)
    {
        precisePosition = new Vector2Int(
            spawnPosition.x * GRID_SCALE,
            spawnPosition.y * GRID_SCALE
        );

        Vector3 worldPosition = new Vector3(
            precisePosition.x / (float)GRID_SCALE,
            precisePosition.y / (float)GRID_SCALE,
            0
        );
        
        transform.position = worldPosition;
        
        preciseVelocity = Vector2Int.zero;
        transform.rotation = Quaternion.identity;
        isMoving = false;
        lastMoveTime = 0f;

        if (trailManager != null)
        {
            trailManager.ResetTrail();
        }

        var surfaceEffects = GetComponent<CarSurfaceEffects>();
        if (surfaceEffects != null)
        {
            surfaceEffects.ClearTracks();
        }

        if (spawnManager != null && !isReplayMode)
        {
            spawnManager.ClearSpawnIndicators();
        }

        if (animationController != null)
        {
            animationController.OnSpawn(worldPosition);
        }

        if (!isReplayMode)
        {
            groundDetector.CheckGravelStatus(CurrentPosition);
            OnSpawnComplete?.Invoke();
        }
    }
}