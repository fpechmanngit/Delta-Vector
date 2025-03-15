using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Settings")]
    public string tutorialKey = "HasSeenTutorial";  // PlayerPrefs key to track if tutorial was seen
    public float slideTransitionTime = 0.5f;        // Time for fade transitions
    
    [Header("Debug Options")]
    public bool showTutorialAgain = false;          // Debug option to force tutorial once
    public bool alwaysShowTutorial = false;         // Debug option to always show tutorial

    [Header("UI References")]
    public GameObject tutorialPanel;                // The main panel containing tutorial elements
    public Image slideImage;                        // Image component to display tutorial slides
    public Button nextButton;                       // Button to advance to next slide
    public Button skipButton;                       // Optional button to skip tutorial
    public TMP_Text pageIndicatorText;             // Optional text to show current slide number
    public Toggle doNotShowToggle;                 // Checkbox for "Do not show again"

    [Header("Slides")]
    public List<Sprite> tutorialSlides;            // List of tutorial image slides
    
    // Private variables
    private int currentSlideIndex = 0;
    private bool isTutorialActive = false;
    private CanvasGroup canvasGroup;
    private GameManager gameManager;
    private ChallengeManager challengeManager;

    private void Start()
    {
        // Get required components
        gameManager = FindFirstObjectByType<GameManager>();
        challengeManager = FindFirstObjectByType<ChallengeManager>();
        canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
        }

        // Set up button listeners
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipTutorial);
        }

        // Initially hide tutorial
        tutorialPanel.SetActive(false);

        // Check if we should show tutorial
        if (ShouldShowTutorial())
        {
            StartTutorial();
        }
    }

    private bool ShouldShowTutorial()
    {
        // Check if this is the Training Grounds challenge
        Challenge currentChallenge = challengeManager?.GetChallenge("Challenge1");
        if (currentChallenge == null || currentChallenge.id != "Challenge1")
        {
            return false;
        }

        // Always show if debug option is enabled
        if (alwaysShowTutorial)
        {
            return true;
        }

        // Show tutorial if one-time debug option is enabled or if player hasn't chosen to hide it
        return showTutorialAgain || PlayerPrefs.GetInt(tutorialKey, 0) == 0;
    }

    public void StartTutorial()
    {
        if (tutorialSlides == null || tutorialSlides.Count == 0)
        {
            Debug.LogError("No tutorial slides assigned!");
            return;
        }

        // Pause the game while tutorial is active
        if (gameManager != null)
        {
            Time.timeScale = 0f;
        }

        // Show tutorial panel
        tutorialPanel.SetActive(true);
        isTutorialActive = true;
        currentSlideIndex = 0;

        // Ensure tutorial panel is on top
        Canvas canvas = tutorialPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 1000; // Very high number to ensure it's on top
        }

        // Make sure the CanvasGroup blocks raycasts
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        // Show first slide
        UpdateSlideDisplay();

        // Start fade in animation
        StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, slideTransitionTime));

        Debug.Log("Tutorial started");
    }

    private void UpdateSlideDisplay()
    {
        if (slideImage != null && currentSlideIndex < tutorialSlides.Count)
        {
            slideImage.sprite = tutorialSlides[currentSlideIndex];
        }

        // Update page indicator if present
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentSlideIndex + 1}/{tutorialSlides.Count}";
        }

        // Update next button text on last slide if needed
        if (nextButton != null && nextButton.GetComponentInChildren<TMP_Text>() != null)
        {
            nextButton.GetComponentInChildren<TMP_Text>().text = 
                (currentSlideIndex == tutorialSlides.Count - 1) ? "Start Game" : "Next";
        }
    }

    private void OnNextButtonClicked()
    {
        if (!isTutorialActive) return;

        if (currentSlideIndex < tutorialSlides.Count - 1)
        {
            // Show next slide
            StartCoroutine(TransitionToNextSlide());
        }
        else
        {
            // End tutorial
            CompleteTutorial();
        }
    }

    private IEnumerator TransitionToNextSlide()
    {
        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, slideTransitionTime));

        // Change slide
        currentSlideIndex++;
        UpdateSlideDisplay();

        // Fade in
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, slideTransitionTime));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        group.alpha = end;
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
    }

    private void CompleteTutorial()
    {
        // Only save the "do not show" preference if the toggle is checked
        if (doNotShowToggle != null && doNotShowToggle.isOn)
        {
            PlayerPrefs.SetInt(tutorialKey, 1);
            PlayerPrefs.Save();
            Debug.Log("Tutorial preference saved - will not show again");
        }

        // Hide tutorial panel
        StartCoroutine(EndTutorial());
    }

    private IEnumerator EndTutorial()
    {
        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, slideTransitionTime));

        // Make sure game state is properly reset
        Time.timeScale = 1f;
        GameManager.IsPlayerTurn = true;

        // Hide panel
        tutorialPanel.SetActive(false);
        isTutorialActive = false;

        Debug.Log("Tutorial completed - Game resumed with timeScale: " + Time.timeScale);
    }

    // Public method to reset tutorial state
    public void ResetTutorialState()
    {
        // Store all the challenge completion data
        var completedChallenges = CareerProgress.Instance.GetCompletedChallenges();
        var unlockedCars = CareerProgress.Instance.GetUnlockedCars();
        var unlockedTracks = CareerProgress.Instance.GetUnlockedTracks();

        // Delete only the tutorial key
        PlayerPrefs.DeleteKey(tutorialKey);
        PlayerPrefs.Save();

        // Re-save all the progress data
        foreach (var challengeId in completedChallenges)
        {
            CareerProgress.Instance.CompleteChallenge(challengeId);
        }

        foreach (var carId in unlockedCars)
        {
            CareerProgress.Instance.UnlockCar(carId);
        }

        foreach (var trackId in unlockedTracks)
        {
            CareerProgress.Instance.UnlockTrack(trackId);
        }

        Debug.Log("Tutorial state reset - will show on next start (game progress preserved)");
    }

    private void OnDestroy()
    {
        // Clean up button listeners
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
        }
    }
}