using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public int checkpointNumber = 0;
    public int totalCheckpoints = 0;
    public bool isActivated = false;

    [Header("Visual Feedback")]
    public Color inactiveColor = new Color(1f, 0f, 0f, 0.3f);
    public Color activeColor = new Color(0f, 1f, 0f, 0.3f);
    
    [Header("Light Settings")]
    public bool debugMode = true;
    [ColorUsage(true, true)]
    public Color inactiveLightColor = new Color(1f, 0f, 0f, 1f);
    [ColorUsage(true, true)]
    public Color activeLightColor = new Color(0f, 1f, 0f, 1f);
    
    private SpriteRenderer spriteRenderer;
    private Light2D checkpointLight;
    private CheckpointManager checkpointManager;
    private GameManager gameManager;
    
    private bool isPlayer1Activated = false;
    private bool isPlayer2Activated = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        checkpointLight = GetComponent<Light2D>();
        
        if (debugMode)
        {
            if (checkpointLight != null)
                Debug.Log($"Checkpoint {checkpointNumber} - Light2D found and configured");
            else
                Debug.LogError($"Checkpoint {checkpointNumber} - No Light2D component found!");
        }
    }

    private void Start()
    {
        checkpointManager = FindFirstObjectByType<CheckpointManager>();
        gameManager = FindFirstObjectByType<GameManager>();

        if (checkpointManager == null)
        {
            Debug.LogError("No CheckpointManager found in scene!");
            return;
        }

        if (gameManager == null)
        {
            Debug.LogError("No GameManager found in scene!");
            return;
        }

        checkpointManager.RegisterCheckpoint(this);
        ForceUpdateLight(inactiveLightColor);
        UpdateVisuals();
        
        if (debugMode)
        {
            Debug.Log($"Checkpoint {checkpointNumber} of {totalCheckpoints} initialized");
        }
    }

    public Vector2 GetCheckpointPosition()
    {
        return transform.position;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player1"))
        {
            if (!isPlayer1Activated)
            {
                isPlayer1Activated = true;
                checkpointManager.OnCheckpointActivated(this, true);
                UpdateVisuals();

                if (gameManager != null)
                {
                    gameManager.OnCheckpointReached(transform.position);
                    if (debugMode)
                        Debug.Log($"Notified GameManager about checkpoint {checkpointNumber} activation by Player1");
                }

                if (debugMode)
                    Debug.Log($"Checkpoint {checkpointNumber} activated by Player1");
            }
        }
        else if (other.CompareTag("Player2"))
        {
            if (!isPlayer2Activated)
            {
                isPlayer2Activated = true;
                checkpointManager.OnCheckpointActivated(this, false);
                UpdateVisuals();

                if (gameManager != null)
                {
                    gameManager.OnCheckpointReached(transform.position);
                    if (debugMode)
                        Debug.Log($"Notified GameManager about checkpoint {checkpointNumber} activation by Player2");
                }

                if (debugMode)
                    Debug.Log($"Checkpoint {checkpointNumber} activated by Player2");
            }
        }
    }

    public void ResetCheckpoint()
    {
        isPlayer1Activated = false;
        isPlayer2Activated = false;
        isActivated = false;
        UpdateVisuals();
        if (debugMode)
            Debug.Log($"Checkpoint {checkpointNumber} reset");
    }

    public bool IsActivatedForPlayer(bool isPlayer1)
    {
        return isPlayer1 ? isPlayer1Activated : isPlayer2Activated;
    }

    public void UpdateVisuals()
    {
        bool showAsActivated;
        
        if (gameManager != null && gameManager.IsPlayer1Turn)
        {
            showAsActivated = isPlayer1Activated;
            isActivated = isPlayer1Activated;
        }
        else
        {
            showAsActivated = isPlayer2Activated;
            isActivated = isPlayer2Activated;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = showAsActivated ? activeColor : inactiveColor;
        }

        if (checkpointLight != null)
        {
            Color targetColor = showAsActivated ? activeLightColor : inactiveLightColor;
            ForceUpdateLight(targetColor);
            
            if (debugMode)
            {
                Debug.Log($"Checkpoint {checkpointNumber} updating visuals:");
                Debug.Log($"Is Activated for current player: {showAsActivated}");
                Debug.Log($"Target Light Color: {targetColor}");
                Debug.Log($"Current Light Color: {checkpointLight.color}");
            }
        }
    }

    private void ForceUpdateLight(Color newColor)
    {
        if (checkpointLight != null)
        {
            checkpointLight.color = newColor;
            checkpointLight.enabled = false;
            checkpointLight.enabled = true;
            
            if (debugMode)
                Debug.Log($"Force updating light color to: {newColor}");
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && checkpointLight != null)
        {
            UpdateVisuals();
        }
    }
}