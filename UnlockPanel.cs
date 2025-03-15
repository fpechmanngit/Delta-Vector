using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnlockPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject unlockPanelRoot;        // The main panel object
    public TMP_Text carUnlockText;           // Text component for showing unlocked car
    public TMP_Text trackUnlockText;         // Text component for showing unlocked track
    public Image carPreviewImage;            // Image to show the unlocked car
    public Image trackPreviewImage;          // Image to show the unlocked track
    public TMP_Text challengeDescriptionText;    // Text component for challenge description
    public TMP_Text moveCountText;           // Text to show the move count
    public Button nextChallengeButton;       // Button to go to next challenge

    [Header("Next Challenge Panel")]
    public GameObject nextChallengePanel;    // The panel containing next challenge info
    public TMP_Text nextChallengeTitleText;  // "Next challenge:" text
    public TMP_Text nextChallengeNameText;   // Name of the next challenge
    public Image nextChallengePreviewImage;  // Preview image of the next challenge
    public TMP_Text nextTrackLabelText;      // "Next Track:" label
    public TMP_Text nextTrackValueText;      // Name of the next track
    public TMP_Text nextCarLabelText;        // "Next Car:" label
    public TMP_Text nextCarValueText;        // Name of the next car
    public TMP_Text nextDescriptionLabelText; // "Description:" label
    public TMP_Text nextDescriptionValueText; // Description of the next challenge

    [Header("Animation References")]
    public TMP_Text[] textsToAnimate;
    private TMP_TextInfo[] textInfos;
    private Vector3[][] originalCharacterPositions;
    private Vector3[][] originalVertexPositions;
    private Image backgroundImage;
    private Image contentBackground;

    [Header("Animation Settings")]
    public float fadeInDuration = 0.5f;
    public float displayDuration = 3f;
    public float fadeOutDuration = 0.5f;

    private Challenge currentChallenge;          
    private Challenge nextChallenge;
    private CanvasGroup canvasGroup;
    private UnlockPanelAnimator animator;
    private GameUIManager gameUIManager;
    private AudioManager audioManager;
    private ChallengeManager challengeManager;

    private void Awake()
    {
        // Get or add required components
        if (unlockPanelRoot == null)
        {
            unlockPanelRoot = gameObject;
        }
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        animator = GetComponent<UnlockPanelAnimator>();
        if (animator == null)
        {
            animator = gameObject.AddComponent<UnlockPanelAnimator>();
        }

        challengeManager = FindFirstObjectByType<ChallengeManager>();

        // Make sure we're visible initially but with alpha 0
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Set up Next Challenge button if it exists
        if (nextChallengeButton != null)
        {
            nextChallengeButton.onClick.AddListener(OnNextChallengeClick);
            nextChallengeButton.gameObject.SetActive(false);
        }

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Initialize arrays with proper size checks
        if (textsToAnimate != null)
        {
            textInfos = new TMP_TextInfo[textsToAnimate.Length];
            originalCharacterPositions = new Vector3[textsToAnimate.Length][];
            originalVertexPositions = new Vector3[textsToAnimate.Length][];
        }

        // Create proper background setup if not present
        GameObject bgObj = new GameObject("Background Panel");
        bgObj.transform.SetParent(transform);
        bgObj.transform.SetAsFirstSibling(); // Put at back
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.localScale = Vector3.one;
        bgRect.localPosition = Vector3.zero;

        // Add Image component for glow
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        
        // Create inner panel with solid background
        GameObject contentBg = new GameObject("Content Panel");
        contentBg.transform.SetParent(bgObj.transform);
        
        RectTransform contentRect = contentBg.AddComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.sizeDelta = new Vector2(-20, -20); // Just add padding
        contentRect.localScale = Vector3.one;
        contentRect.localPosition = Vector3.zero;
        
        contentBackground = contentBg.AddComponent<Image>();
        contentBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        // Add mask to keep effects inside panel
        var mask = gameObject.GetComponent<UnityEngine.UI.Mask>();
        if (mask == null)
        {
            mask = gameObject.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;
        }

        // Add image for mask to work
        var maskImage = gameObject.GetComponent<Image>();
        if (maskImage == null)
        {
            maskImage = gameObject.AddComponent<Image>();
            maskImage.color = Color.white;
        }
    }

    public void ShowUnlockPanel()
    {
        // Get the current challenge data
        string currentTrackId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        challengeManager = challengeManager ?? FindFirstObjectByType<ChallengeManager>();
        gameUIManager = gameUIManager ?? FindFirstObjectByType<GameUIManager>();
        audioManager = audioManager ?? FindFirstObjectByType<AudioManager>();
        
        if (challengeManager != null)
        {
            currentChallenge = challengeManager.GetChallengeByTrackId(currentTrackId);
        }

        if (currentChallenge == null)
        {
            return;
        }

        // Find the next challenge
        nextChallenge = FindNextChallenge();
        bool hasNextChallenge = nextChallenge != null;

        // Update "Next Challenge" button visibility
        if (nextChallengeButton != null)
        {
            nextChallengeButton.gameObject.SetActive(hasNextChallenge);
        }

        // Update Next Challenge Panel visibility
        if (nextChallengePanel != null)
        {
            nextChallengePanel.SetActive(hasNextChallenge);
            
            if (hasNextChallenge)
            {
                UpdateNextChallengeInfo();
            }
        }

        // Force correct initial state
        gameObject.SetActive(true);
        if (unlockPanelRoot != null)
        {
            unlockPanelRoot.SetActive(true);
            transform.localScale = Vector3.one * 0.3f; // Start small
        }

        // Update UI elements
        UpdateUnlockTexts();
        UpdatePreviewImages();
        UpdateMoveCount();

        // Ensure we're visible first
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // Start animations AFTER ensuring everything is visible and set up
        if (animator != null)
        {
            animator.StartAnimation();
        }
    }

    private Challenge FindNextChallenge()
    {
        if (challengeManager == null || currentChallenge == null)
            return null;

        // Look through all challenges to find one that requires this current challenge
        foreach (var challenge in challengeManager.allChallenges)
        {
            if (challenge.requiredChallenges != null && 
                challenge.requiredChallenges.Length > 0 && 
                System.Array.IndexOf(challenge.requiredChallenges, currentChallenge.id) != -1)
            {
                // Return this as the next challenge if it's available
                if (challenge.IsAvailable())
                {
                    return challenge;
                }
            }
        }

        return null;
    }

    private void UpdateNextChallengeInfo()
    {
        if (nextChallenge == null || nextChallengePanel == null)
            return;

        // Update the name of the next challenge
        if (nextChallengeNameText != null)
        {
            nextChallengeNameText.text = nextChallenge.displayName;
        }

        // Update the track name
        if (nextTrackValueText != null)
        {
            nextTrackValueText.text = nextChallenge.trackId;
        }

        // Update the car name
        if (nextCarValueText != null)
        {
            nextCarValueText.text = nextChallenge.requiredCarId;
        }

        // Update the description
        if (nextDescriptionValueText != null)
        {
            nextDescriptionValueText.text = nextChallenge.description;
        }

        // Update preview image if available
        if (nextChallengePreviewImage != null)
        {
            // Try to load the track preview image
            string trackPath = $"Previews/Tracks/{nextChallenge.trackId}";
            Sprite trackSprite = Resources.Load<Sprite>(trackPath);
            
            if (trackSprite != null)
            {
                nextChallengePreviewImage.sprite = trackSprite;
                nextChallengePreviewImage.gameObject.SetActive(true);
            }
            else
            {
                // If track image not found, try the car sprite as fallback
                GameObject carPrefab = Resources.Load<GameObject>($"Cars/{nextChallenge.requiredCarId}");
                if (carPrefab != null)
                {
                    SpriteRenderer carSprite = carPrefab.GetComponent<SpriteRenderer>();
                    if (carSprite != null && carSprite.sprite != null)
                    {
                        nextChallengePreviewImage.sprite = carSprite.sprite;
                        nextChallengePreviewImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        nextChallengePreviewImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    nextChallengePreviewImage.gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateUnlockTexts()
    {
        if (currentChallenge.carsToUnlock != null && currentChallenge.carsToUnlock.Length > 0)
        {
            if (carUnlockText != null)
            {
                carUnlockText.text = $"New Car: {currentChallenge.carsToUnlock[0]}";
            }
        }

        if (currentChallenge.tracksToUnlock != null && currentChallenge.tracksToUnlock.Length > 0)
        {
            if (trackUnlockText != null)
            {
                trackUnlockText.text = $"New Track: {currentChallenge.tracksToUnlock[0]}";
            }
        }

        if (challengeDescriptionText != null)
        {
            string description = $"Challenge Complete!\n\n{currentChallenge.description}\n";
            description += $"Target Moves: {currentChallenge.targetMoves}";
            challengeDescriptionText.text = description;
        }
    }

    private void UpdatePreviewImages()
    {
        // Update car preview
        if (currentChallenge.carsToUnlock != null && currentChallenge.carsToUnlock.Length > 0 && carPreviewImage != null)
        {
            GameObject carPrefab = Resources.Load<GameObject>($"Cars/{currentChallenge.carsToUnlock[0]}");
            if (carPrefab != null)
            {
                SpriteRenderer carSprite = carPrefab.GetComponent<SpriteRenderer>();
                if (carSprite != null && carSprite.sprite != null)
                {
                    carPreviewImage.sprite = carSprite.sprite;
                    carPreviewImage.gameObject.SetActive(true);
                }
            }
        }

        // Update track preview
        if (currentChallenge.tracksToUnlock != null && currentChallenge.tracksToUnlock.Length > 0 && trackPreviewImage != null)
        {
            string trackPath = $"Previews/Tracks/{currentChallenge.tracksToUnlock[0]}";
            Sprite trackSprite = Resources.Load<Sprite>(trackPath);
            if (trackSprite != null)
            {
                trackPreviewImage.sprite = trackSprite;
                trackPreviewImage.gameObject.SetActive(true);
            }
        }
    }

    private void UpdateMoveCount()
    {
        if (moveCountText != null && gameUIManager != null)
        {
            int currentMoves = gameUIManager.GetCurrentMoveCount();
            moveCountText.text = $"Your Moves: {currentMoves}";
        }
    }

    private void OnNextChallengeClick()
    {
        if (nextChallenge == null || challengeManager == null)
        {
            return;
        }

        // Set up the game initialization parameters
        GameInitializationManager.SelectedGameMode = GameMode.Challenges;
        GameInitializationManager.SelectedTrack = nextChallenge.trackId;
        
        GameObject requiredCar = Resources.Load<GameObject>($"Cars/{nextChallenge.requiredCarId}");
        if (requiredCar != null)
        {
            GameInitializationManager.SelectedCar = requiredCar;
            
            // Load the next challenge's scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextChallenge.trackId);
        }
    }

    private void OnDisable()
    {
        currentChallenge = null;
        nextChallenge = null;
        
        if (trackPreviewImage != null)
            trackPreviewImage.gameObject.SetActive(false);
        if (carPreviewImage != null)
            carPreviewImage.gameObject.SetActive(false);
        if (challengeDescriptionText != null)
            challengeDescriptionText.text = "";
        if (nextChallengeButton != null)
            nextChallengeButton.gameObject.SetActive(false);
        
        // Hide the next challenge panel
        if (nextChallengePanel != null)
            nextChallengePanel.SetActive(false);
    }

    public void ClosePanel()
    {
        HidePanel();
    }

    private void HidePanel()
    {
        if (unlockPanelRoot != null)
        {
            unlockPanelRoot.SetActive(false);
        }
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void OnDestroy()
    {
        // Clean up button listener
        if (nextChallengeButton != null)
        {
            nextChallengeButton.onClick.RemoveAllListeners();
        }
    }
}