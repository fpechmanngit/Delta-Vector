using UnityEngine;
using System.Collections;

public class MoveIndicatorClickHandler : MonoBehaviour
{
    // Core component references
    private PlayerMovement player;
    private MoveIndicatorManager moveIndicatorManager;
    private AudioManager audioManager;
    private GameManager gameManager;
    private HoverLineVisualizer hoverLine;
    private FutureSightManager futureSightManager;  // Reference for future sight
    private PlayerSpeedController speedController;  // Added for speed calculations
    
    // State tracking
    private Vector2Int targetPosition;
    private bool isPlayer1;
    private bool isHovering = false;  // Added to track hover state
    
    // Public property to check hover state
    public bool IsHovered => isHovering;

    // Sprite switching components
    private SpriteRenderer spriteRenderer;
    private Sprite offSprite;  // Normal state sprite
    private Sprite onSprite;   // Hover state sprite

    // Animation settings
    [Header("Hover Animation")]
    private float hoverScaleMultiplier = 1.5f;
    private float hoverAnimationDuration = 0.1f;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;

    [Header("Fade Animation")]
    private float fadeInDuration = 0.2f;
    private float fadeOutDuration = 0.2f;
    private Coroutine fadeCoroutine;
    
    // Speed change colors
    [Header("Speed Change Colors")]
    private Color accelerationColor = Color.red;     // Speed increase
    private Color maintenanceColor = Color.yellow;   // Speed same
    private Color brakingColor = Color.green;        // Speed decrease
    private Color currentColor;                      // Current active color
    private Color originalColor;                     // Store original color to revert to after hover
    
    // Public getter for target position
    public Vector2Int GetTargetPosition() 
    {
        return targetPosition;
    }

    private void Start()
    {
        originalScale = transform.localScale;

        // Start with fully transparent
        if (spriteRenderer != null)
        {
            Color startColor = spriteRenderer.color;
            currentColor = startColor;  // Store the current color
            originalColor = startColor; // Also store as original color
            startColor.a = 0f;
            spriteRenderer.color = startColor;
            
            // Fade in when created
            StartFadeAnimation(true);
        }
    }

    public void Initialize(PlayerMovement player, Vector2Int position)
    {
        // Store core references
        this.player = player;
        this.targetPosition = position;
        this.moveIndicatorManager = player.GetComponent<MoveIndicatorManager>();
        this.audioManager = Object.FindFirstObjectByType<AudioManager>();
        this.gameManager = Object.FindFirstObjectByType<GameManager>();
        this.hoverLine = player.GetComponent<HoverLineVisualizer>();
        this.futureSightManager = player.GetComponent<FutureSightManager>();
        this.isPlayer1 = player.CompareTag("Player1");
        this.speedController = player.GetComponent<PlayerSpeedController>();

        // Get and store sprite renderer reference
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            currentColor = spriteRenderer.color;  // Store current color
            originalColor = spriteRenderer.color; // Also store as original color
        }

        // Set up the correct layer for player-specific interaction
        if (GameInitializationManager.SelectedGameMode == GameMode.Challenges)
        {
            gameObject.layer = LayerMask.NameToLayer("Player1Indicators");
        }
        else
        {
            if (isPlayer1)
            {
                gameObject.layer = LayerMask.NameToLayer("Player1Indicators");
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer("Player2Indicators");
            }
        }
    }

    public void SetSprites(Sprite offSprite, Sprite onSprite)
    {
        this.offSprite = offSprite;
        this.onSprite = onSprite;
        
        // Set initial sprite to off state
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = offSprite;
        }
    }
    
    public void SetSpeedColors(Color accelerationColor, Color maintenanceColor, Color brakingColor)
    {
        this.accelerationColor = accelerationColor;
        this.maintenanceColor = maintenanceColor;
        this.brakingColor = brakingColor;
    }

    private bool IsCorrectPlayerTurn()
    {
        if (gameManager == null) return false;
        
        if (GameInitializationManager.SelectedGameMode == GameMode.Challenges)
            return true;
            
        bool isTurn = (isPlayer1 && gameManager.IsPlayer1Turn) || (!isPlayer1 && !gameManager.IsPlayer1Turn);
        return isTurn;
    }

    private void OnMouseEnter()
    {
        if (player == null || !IsCorrectPlayerTurn()) return;

        // Tell the manager to reset all other indicators first
        if (moveIndicatorManager != null)
        {
            moveIndicatorManager.SetActiveHoveredIndicator(this);
        }
        
        // Set our hover state
        isHovering = true;
        ShowHoverEffects();
    }

    private void OnMouseExit()
    {
        if (!isHovering) return;
        
        isHovering = false;
        HideHoverEffects();
    }

    public void ResetHoverState()
    {
        if (!isHovering) return;
        
        isHovering = false;
        HideHoverEffects();
    }

    public void ShowHoverEffects()
    {
        audioManager?.PlayHoverSound();
            
        // Store current color before changing it for the hover effect
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Switch to hover state sprite but keep current color
        if (spriteRenderer != null && onSprite != null)
        {
            spriteRenderer.sprite = onSprite;
            
            // Apply highlight effect but keep the same color
            // Just make it slightly brighter to indicate hover
            Color hoverColor = originalColor;
            hoverColor = Color.Lerp(hoverColor, Color.white, 0.2f); // Brighten slightly
            spriteRenderer.color = hoverColor;
        }

        // Start hover scale animation
        StartScaleAnimation(true);

        // Show hover line
        if (hoverLine != null)
        {
            hoverLine.ShowLine(targetPosition);
        }

        // Show future sight prediction if in easy mode
        if (futureSightManager != null)
        {
            futureSightManager.ShowFuturePosition(targetPosition);
        }
    }

    public void HideHoverEffects()
    {
        // Switch back to normal state sprite and restore original color
        if (spriteRenderer != null && offSprite != null)
        {
            spriteRenderer.sprite = offSprite;
            spriteRenderer.color = originalColor; // Restore the exact original color
        }

        // Return to original scale
        StartScaleAnimation(false);

        // Hide hover line
        if (hoverLine != null)
        {
            hoverLine.HideLine();
        }

        // Clear future sight prediction
        if (futureSightManager != null)
        {
            futureSightManager.ClearFutureIndicator();
        }
    }

    private void StartScaleAnimation(bool scaleUp)
    {
        // Check if GameObject is active before starting coroutine
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleAnimation(scaleUp));
    }

    private IEnumerator ScaleAnimation(bool scaleUp)
    {
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = scaleUp ? originalScale * hoverScaleMultiplier : originalScale;

        while (elapsedTime < hoverAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / hoverAnimationDuration;
            
            // Use smooth step for more natural animation
            t = t * t * (3f - 2f * t);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
        scaleCoroutine = null;
    }

    private void StartFadeAnimation(bool fadeIn)
    {
        // Check if GameObject is active before starting coroutine
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeAnimation(fadeIn));
    }

    private IEnumerator FadeAnimation(bool fadeIn)
    {
        if (spriteRenderer == null) yield break;

        float elapsedTime = 0f;
        float duration = fadeIn ? fadeInDuration : fadeOutDuration;
        float startAlpha = spriteRenderer.color.a;
        float targetAlpha = fadeIn ? 1f : 0f;
        Color color = spriteRenderer.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            spriteRenderer.color = color;
            
            yield return null;
        }

        color.a = targetAlpha;
        spriteRenderer.color = color;
        fadeCoroutine = null;

        // If fading out, destroy the object when done
        if (!fadeIn && targetAlpha == 0)
        {
            Destroy(gameObject);
        }
    }

    public void FadeOutAndDestroy()
    {
        StartFadeAnimation(false);
    }

    private void OnMouseDown()
    {
        if (player == null) return;

        if (!IsCorrectPlayerTurn()) return;

        ExecuteMove();
    }

    public void ExecuteMove()
    {
        if (player.IsMoving) return; // Don't execute if already moving
        
        audioManager?.PlayRevSound();
        ScreenShakeManager.Instance.ShakeScreen();
        player.MoveToIndicator(targetPosition);
        gameManager.EndTurn();
    }

    private void OnDisable()
    {
        // Clean up any running coroutines
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        
        isHovering = false;
        
        // Don't call HideHoverEffects() which tries to start new coroutines
        // Just clean up references directly
        if (hoverLine != null)
        {
            hoverLine.HideLine();
        }
        
        if (futureSightManager != null)
        {
            futureSightManager.ClearFutureIndicator();
        }
    }
}