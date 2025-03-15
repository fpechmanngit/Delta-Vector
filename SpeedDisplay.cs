using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SpeedDisplay : MonoBehaviour
{
    [Header("UI References")]
    public Image speedometerImage;  // Reference to the UI Image component

    [Header("Speedometer Sprites")]
    public Sprite[] speedSprites = new Sprite[10];  // Array to hold the 7 speed sprites (0-6)

    [Header("Debug")]
    public bool showDebugLogs = false;

    private PlayerMovement playerMovement;
    private int currentSpeedIndex = 0;
    private bool isInitialized = false;

    private void OnEnable()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Initialize when enabled
        InitializeSpeedDisplay();
    }

    private void OnDisable()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-initialize when a new scene is loaded
        InitializeSpeedDisplay();
    }

    private void InitializeSpeedDisplay()
    {
        // Reset initialization flag
        isInitialized = false;

        // Find the player
        FindPlayer();

        // Validate that we have the Image component
        if (speedometerImage == null)
        {
            // Try to get the Image component from this GameObject
            speedometerImage = GetComponent<Image>();
            if (speedometerImage == null)
            {
                Debug.LogError("Speedometer Image reference not set and no Image component found!");
                return;
            }
        }

        // Validate sprite array
        if (speedSprites.Length != 10)
        {
            Debug.LogError($"Expected 7 speed sprites, but found {speedSprites.Length}!");
            return;
        }

        // Set initial sprite
        UpdateSpeedDisplay(0);
        
        isInitialized = true;
        
        if (showDebugLogs)
        {
            Debug.Log("SpeedDisplay initialized successfully");
        }
    }

    private void FindPlayer()
    {
        // First try to find Player1
        GameObject player = GameObject.FindGameObjectWithTag("Player1");
        
        // If Player1 not found, try to find any active PlayerMovement component
        if (player == null)
        {
            PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
            if (players.Length > 0)
            {
                player = players[0].gameObject;
                if (showDebugLogs)
                {
                    Debug.Log("Found player through PlayerMovement component");
                }
            }
        }

        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                Debug.LogError("PlayerMovement component not found on player object!");
            }
            else if (showDebugLogs)
            {
                Debug.Log($"Found player: {player.name}");
            }
        }
        else
        {
            Debug.LogWarning("No player found in scene! Will try again later.");
        }
    }

    private void Update()
    {
        // If not initialized or no player found, try to initialize
        if (!isInitialized || playerMovement == null)
        {
            InitializeSpeedDisplay();
            return;
        }

        // Update speed display
        if (playerMovement != null)
        {
            // Get the current velocity magnitude
            Vector2Int velocity = playerMovement.CurrentVelocity;
            int speed = Mathf.Max(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y));
            
            // Clamp speed to our valid range (1-7)
            int speedIndex = Mathf.Clamp(speed, 0, 9);

            // Only update if the speed has changed
            if (speedIndex != currentSpeedIndex)
            {
                UpdateSpeedDisplay(speedIndex);
            }
        }
    }

    private void UpdateSpeedDisplay(int speedIndex)
    {
        if (speedometerImage != null && speedIndex >= 0 && speedIndex < speedSprites.Length)
        {
            speedometerImage.sprite = speedSprites[speedIndex];
            currentSpeedIndex = speedIndex;

            if (showDebugLogs)
            {
                Debug.Log($"Updated speedometer to speed {speedIndex + 1}");
            }
        }
    }

    // Helper method to set sprites at runtime if needed
    public void SetSpeedSprites(Sprite[] sprites)
    {
        if (sprites.Length != 10)
        {
            Debug.LogError($"Invalid number of sprites. Expected 7, got {sprites.Length}");
            return;
        }

        speedSprites = sprites;
        UpdateSpeedDisplay(currentSpeedIndex); // Refresh current display
    }
}