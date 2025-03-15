using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashScreen : MonoBehaviour
{
    [Header("Animation")]
    public Animator splashAnimator;
    public float animationDuration = 3f;
    public float lastFrameHoldTime = 2f;  // How long to hold the last frame
    public float fadeOutDuration = 1f;

    [Header("Audio Settings")]
    public AudioSource introSound;
    public float audioDelay = 0f;         // Delay before starting audio
    public float audioFadeOutDuration = 1f;  // How long to fade out audio

    [Header("Optional Elements")]
    public Image fadeOverlay;
    public bool skipSplashForTesting = false;  // Set this in inspector for testing
    
    private float originalAudioVolume;
    private bool isTransitioning = false;

    private void Start()
    {
        // If testing and want to skip splash
        if (skipSplashForTesting)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        // Store original audio volume for fading
        if (introSound != null)
        {
            originalAudioVolume = introSound.volume;
        }

        // Ensure we start visible with no fade
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0, 0, 0, 0);
        }

        // Start the splash sequence immediately
        StartCoroutine(PlaySplashSequence());
    }

    private IEnumerator PlaySplashSequence()
    {
        // Start animation
        if (splashAnimator != null)
        {
            splashAnimator.Play("SplashAnimation");
            Debug.Log("Started splash animation");
        }
        else
        {
            Debug.LogWarning("No splash animator assigned");
        }
        
        // Handle audio with delay if specified
        if (introSound != null)
        {
            if (audioDelay > 0)
            {
                yield return new WaitForSeconds(audioDelay);
            }
            introSound.Play();
            Debug.Log("Started intro sound");
        }

        // Wait for main animation
        yield return new WaitForSeconds(animationDuration);
        Debug.Log("Main animation completed");

        // Hold on last frame
        yield return new WaitForSeconds(lastFrameHoldTime);
        Debug.Log("Last frame hold completed");

        // Fade out audio if it's still playing
        if (introSound != null && introSound.isPlaying)
        {
            float elapsedTime = 0;
            while (elapsedTime < audioFadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float volumeProgress = 1f - (elapsedTime / audioFadeOutDuration);
                introSound.volume = originalAudioVolume * volumeProgress;
                yield return null;
            }
            introSound.Stop();
            Debug.Log("Audio fade out completed");
        }

        // Fade out visuals
        if (fadeOverlay != null)
        {
            float elapsedTime = 0;
            Color startColor = fadeOverlay.color;
            Color targetColor = new Color(0, 0, 0, 1);

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeOutDuration;
                fadeOverlay.color = Color.Lerp(startColor, targetColor, progress);
                yield return null;
            }
            Debug.Log("Visual fade out completed");
        }

        // Prevent multiple transitions
        if (!isTransitioning)
        {
            isTransitioning = true;
            // Load main menu
            SceneManager.LoadScene("MainMenu");
            Debug.Log("Transitioning to main menu");
        }
    }

    private void Update()
    {
        // Optional: Allow skipping the splash screen with Escape or Space
        if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)) && !isTransitioning)
        {
            StopAllCoroutines();
            isTransitioning = true;
            SceneManager.LoadScene("MainMenu");
            Debug.Log("Splash screen skipped");
        }
    }

    private void OnDestroy()
    {
        // Clean up any resources if needed
        StopAllCoroutines();
        Debug.Log("SplashScreen destroyed, coroutines stopped");
    }
}