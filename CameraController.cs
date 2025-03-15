using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Basic Camera Settings")]
    public float zoomSpeed = 10f;
    public float moveSpeed = 5f;
    public float minZoom = 1f;
    public float maxZoom = 20f;
    public float followSpeed = 5f;

    [Header("Speed-Based Zoom Settings")]
    public float baseZoom = 5f;
    public float speedZoomFactor = 20f;
    public float maxSpeedForZoom = 8f;
    public float speedZoomSmoothness = 5f;

    [Header("Zoom Mode")]
    [SerializeField] private bool useSpeedZoom = true;

    private bool isFreeCam = false;
    private bool hasGameStarted = false;
    private GameObject startFinishLine;
    private Transform activePlayerTransform;
    private Vector3 dragOrigin;
    private bool isDragging = false;
    private bool forceFollow = false;
    private PlayerMovement activePlayerMovement;
    private Vector3 lastPlayerBasePosition;
    private GameManager gameManager;
    private float currentSpeedZoom;
    private float currentManualZoom;

    private void Start()
    {
        InitializeComponents();
        SetupInitialCamera();
    }

    private void InitializeComponents()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        startFinishLine = GameObject.Find("StartFinishLine");
        currentSpeedZoom = baseZoom;
        currentManualZoom = baseZoom;
    }

    private void SetupInitialCamera()
    {
        if (startFinishLine != null)
        {
            Vector3 startPosition = startFinishLine.transform.position;
            transform.position = new Vector3(startPosition.x, startPosition.y, transform.position.z);
            Camera.main.orthographicSize = baseZoom;
        }
    }

    private void Update()
    {
        UpdateActivePlayer();
        HandleZoom();

        if (CheckForManualControl())
        {
            isFreeCam = true;
            forceFollow = false;
        }

        if (isFreeCam && !forceFollow)
        {
            HandleFreeCam();
        }
        else if (activePlayerTransform != null)
        {
            HandleFollowCam();
        }
    }

    private void UpdateActivePlayer()
    {
        if (gameManager != null && hasGameStarted)
        {
            GameObject currentPlayer = gameManager.IsPlayer1Turn ? 
                GameObject.FindGameObjectWithTag("Player1") : 
                GameObject.FindGameObjectWithTag("Player2");

            if (currentPlayer != null && activePlayerTransform != currentPlayer.transform)
            {
                activePlayerTransform = currentPlayer.transform;
                activePlayerMovement = currentPlayer.GetComponent<PlayerMovement>();
            }
        }
    }

    private void HandleZoom()
    {
        if (!useSpeedZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentManualZoom -= scroll * zoomSpeed;
                currentManualZoom = Mathf.Clamp(currentManualZoom, minZoom, maxZoom);
            }
            
            Camera.main.orthographicSize = currentManualZoom;
        }
        else if (activePlayerMovement != null)
        {
            Vector2Int gridVelocity = activePlayerMovement.CurrentVelocity;
            int absX = Mathf.Abs(gridVelocity.x);
            int absY = Mathf.Abs(gridVelocity.y);
            float currentSpeed = Mathf.Max(absX, absY) / (float)PlayerMovement.GRID_SCALE;
            float clampedSpeed = Mathf.Min(currentSpeed, maxSpeedForZoom);
            float targetSpeedZoom = baseZoom + (clampedSpeed * speedZoomFactor);

            currentSpeedZoom = Mathf.Lerp(currentSpeedZoom, targetSpeedZoom, Time.deltaTime * speedZoomSmoothness);
            Camera.main.orthographicSize = Mathf.Clamp(currentSpeedZoom, minZoom, maxZoom);
        }
    }

    private bool CheckForManualControl()
    {
        return Input.GetMouseButtonDown(2) || 
               Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || 
               Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
               Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
               Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);
    }

    private void HandleFreeCam()
    {
        if (Input.GetMouseButtonDown(2))
        {
            isDragging = true;
            dragOrigin = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(2))
        {
            isDragging = false;
        }

        if (isDragging && Input.GetMouseButton(2))
        {
            Vector3 difference = Camera.main.ScreenToWorldPoint(dragOrigin) - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position += difference;
            dragOrigin = Input.mousePosition;
        }

        float moveX = 0f;
        float moveY = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveY = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveY = -1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveX = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveX = 1f;

        if (moveX != 0f || moveY != 0f)
        {
            Vector3 movement = new Vector3(moveX, moveY, 0) * moveSpeed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }
    }

    private void HandleFollowCam()
    {
        if (activePlayerTransform != null && activePlayerMovement != null)
        {
            Vector2Int currentGridPos = activePlayerMovement.CurrentPosition;
            Vector3 targetPosition = new Vector3(
                currentGridPos.x,
                currentGridPos.y,
                transform.position.z
            );

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }
    }

    public void OnPlayerSpawn()
    {
        hasGameStarted = true;
        isFreeCam = false;
        forceFollow = false; // Changed from true to false
        
        currentSpeedZoom = baseZoom;
        currentManualZoom = baseZoom;
        Camera.main.orthographicSize = baseZoom;
    }

    public void OnPlayerMove()
    {
        isFreeCam = false;
        forceFollow = true;
        StartCoroutine(ResetForceFollowAfterDelay(0.5f));
    }

    private IEnumerator ResetForceFollowAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        forceFollow = false;
    }

    public void EnableFreeCamera()
    {
        forceFollow = false;
        isFreeCam = true;
    }
    
    public void SetSpeedZoom(bool enabled)
    {
        float currentZoom = Camera.main.orthographicSize;
        
        useSpeedZoom = enabled;
        
        if (enabled)
        {
            currentSpeedZoom = baseZoom;
        }
        else
        {
            currentManualZoom = currentZoom;
        }
    }
    
    public bool UseSpeedZoom 
    { 
        get { return useSpeedZoom; } 
        set { SetSpeedZoom(value); }
    }
    
    public void ResetZoom()
    {
        currentSpeedZoom = baseZoom;
        currentManualZoom = baseZoom;
        Camera.main.orthographicSize = baseZoom;
    }
}