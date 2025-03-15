using UnityEngine;

public class SpawnIndicator : MonoBehaviour
{
    // Core references
    private PlayerMovement player;
    private Vector2Int position;
    private bool hasSpawned = false;
    private AudioManager audioManager;

    // Sprite and color handling
    private SpriteRenderer spriteRenderer;
    private Color normalColor = Color.white;      // Default white for normal state
    private Color hoverColor = Color.red;         // Bright red for hover state

    void Awake()
    {
        // Get and set up our sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"[SpawnIndicator] No SpriteRenderer found on {gameObject.name}!");
            return;
        }

        // Set initial appearance
        spriteRenderer.sortingOrder = 1000;   // Make sure it's visible
        spriteRenderer.color = normalColor;    // Start with normal color
    }

    public void Initialize(PlayerMovement player, Vector2Int position)
    {
        this.player = player;
        this.position = position;
        hasSpawned = false;
        
        // Get audio manager for hover and click sounds
        audioManager = Object.FindFirstObjectByType<AudioManager>();
    }

    public void SetSprite(Sprite sprite)
    {
        // Set the single sprite we'll use
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    private void OnMouseEnter()
    {
        if (hasSpawned || player == null) return;

        // Play hover sound
        if (audioManager != null)
        {
            audioManager.PlayHoverSound();
        }

        // Change to hover color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hoverColor;
            Debug.Log($"[SpawnIndicator] Changed to hover color on {gameObject.name}");
        }
    }

    private void OnMouseExit()
    {
        if (hasSpawned || player == null) return;

        // Change back to normal color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
            Debug.Log($"[SpawnIndicator] Changed back to normal color on {gameObject.name}");
        }
    }

    private void OnMouseDown()
    {
        if (hasSpawned || player == null) return;

        hasSpawned = true;
        
        // Play selection sound
        if (audioManager != null)
        {
            audioManager.PlayRevSound();
        }

        // Set player spawn point
        player.SetSpawnPoint(position);

        // Clean up all spawn indicators
        var indicators = Object.FindObjectsByType<SpawnIndicator>(FindObjectsSortMode.None);
        foreach (var indicator in indicators)
        {
            Destroy(indicator.gameObject);
        }
    }
}