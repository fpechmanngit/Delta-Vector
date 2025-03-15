using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class RaceEventsManager : MonoBehaviour
{
    [Header("Race Start UI")]
    public GameObject countdownPanel;
    public TMP_Text countdownText;
    public float numberScale = 1.5f;
    public float numberScaleDuration = 0.5f;
    public Color[] countdownColors = new Color[] {
        new Color(1f, 0.3f, 0.3f), // Red for 3
        new Color(1f, 0.7f, 0.3f), // Orange for 2
        new Color(0.3f, 1f, 0.3f)  // Green for 1
    };
    
    [Header("GO! Effect")]
    public GameObject goPanel;
    public TMP_Text goText;
    public float goScaleDuration = 0.3f;
    public float goHoldDuration = 0.5f;
    public float goFadeDuration = 0.2f;
    
    [Header("Victory Effects")]
    public GameObject victoryPanel;
    public TMP_Text victoryText;
    public Image[] starImages;
    public float starFillDuration = 0.5f;
    public float delayBetweenStars = 0.3f;

    [Header("Failure Effects")]
    public GameObject failurePanel;      // Reference to the failure UI panel
    public TMP_Text failureText;         // Text showing the failure message
    public float failureFadeDuration = 0.3f;
    
    [Header("Audio")]
    public AudioClip countdownSound;
    public AudioClip goSound;
    public AudioClip checkpointSound;
    public AudioClip victorySound;

    [Header("Events")]
    public UnityEvent onRaceStart;
    public UnityEvent onRaceComplete;
    public UnityEvent onCheckpointReached;

    private AudioSource audioSource;
    private bool isCountingDown = false;
    private RaceParticleEffects particleEffects;

    private void Start()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("Added AudioSource component");
        }

        // Find the RaceParticleEffects component
        particleEffects = FindFirstObjectByType<RaceParticleEffects>();
        if (particleEffects == null)
        {
            Debug.LogError("RaceParticleEffects not found in scene!");
        }
        else
        {
            Debug.Log("Found RaceParticleEffects component");
        }

        // Hide all UI panels initially
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (goPanel != null) goPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (failurePanel != null) failurePanel.SetActive(false);

        // Hide star images
        if (starImages != null)
        {
            foreach (Image star in starImages)
            {
                if (star != null)
                {
                    Color c = star.color;
                    c.a = 0;
                    star.color = c;
                }
            }
        }

        Debug.Log("RaceEventsManager initialized");
    }

    public void StartRace()
    {
        if (!isCountingDown)
        {
            StartCoroutine(RaceStartSequence());
        }
    }

    private IEnumerator RaceStartSequence()
    {
        isCountingDown = true;
        Debug.Log("Starting race countdown sequence");
        
        // Show countdown panel
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
            
            // Countdown from 3 to 1
            for (int i = 3; i > 0; i--)
            {
                // Play countdown sound
                if (audioSource != null && countdownSound != null)
                {
                    audioSource.PlayOneShot(countdownSound);
                }

                // Set number and color
                countdownText.text = i.ToString();
                countdownText.color = countdownColors[3 - i];
                
                // Scale animation
                float elapsedTime = 0f;
                while (elapsedTime < numberScaleDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float scale = Mathf.Lerp(numberScale, 1f, elapsedTime / numberScaleDuration);
                    countdownText.transform.localScale = Vector3.one * scale;
                    yield return null;
                }
                
                // Reset scale for next number
                countdownText.transform.localScale = Vector3.one * numberScale;
                
                yield return new WaitForSeconds(0.5f);
            }
            
            countdownPanel.SetActive(false);
        }
        
        // Show "GO!"
        if (goPanel != null)
        {
            // Play GO sound
            if (audioSource != null && goSound != null)
            {
                audioSource.PlayOneShot(goSound);
            }

            goPanel.SetActive(true);
            
            // Scale in
            float elapsedTime = 0f;
            while (elapsedTime < goScaleDuration)
            {
                elapsedTime += Time.deltaTime;
                float scale = Mathf.Lerp(0f, 1f, elapsedTime / goScaleDuration);
                goText.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            
            // Hold
            yield return new WaitForSeconds(goHoldDuration);
            
            // Fade out
            elapsedTime = 0f;
            Color startColor = goText.color;
            while (elapsedTime < goFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / goFadeDuration);
                goText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            goPanel.SetActive(false);
        }
        
        isCountingDown = false;
        Debug.Log("Race countdown complete");
        onRaceStart?.Invoke();
    }

    public void OnCheckpointReached(Vector3 position)
    {
        Debug.Log($"RaceEventsManager: Checkpoint reached at position: {position}");
        
        // Play checkpoint sound
        if (audioSource != null && checkpointSound != null)
        {
            audioSource.PlayOneShot(checkpointSound);
            Debug.Log("Played checkpoint sound");
        }

        // Play particle effect
        if (particleEffects != null)
        {
            particleEffects.PlayCheckpointEffect(position);
            Debug.Log("Triggered checkpoint particle effect");
        }
        else
        {
            Debug.LogError("ParticleEffects is null when trying to play checkpoint effect!");
        }

        onCheckpointReached?.Invoke();
    }

    public void OnRaceComplete(int starRating)
    {
        Debug.Log($"Race completed with {starRating} stars");
        
        // Ensure failure panel is hidden
        if (failurePanel != null)
        {
            failurePanel.SetActive(false);
        }
        
        StartCoroutine(ShowVictoryEffects(starRating));
        
        // Play victory sound
        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
            Debug.Log("Played victory sound");
        }

        // Play particle effect
        if (particleEffects != null)
        {
            particleEffects.PlayVictoryEffect(transform.position);
            Debug.Log("Triggered victory particle effect");
        }
        else
        {
            Debug.LogError("ParticleEffects is null when trying to play victory effect!");
        }

        onRaceComplete?.Invoke();
    }

    public void OnRaceFailure()
    {
        Debug.Log("Race failed");
        
        // Ensure victory panel is hidden
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        
        // Play failure particle effect with a different color if needed
        if (particleEffects != null)
        {
            particleEffects.PlayFailureEffect(transform.position);
        }

        // Show the failure panel
        if (failurePanel != null)
        {
            failurePanel.SetActive(true);
            
            // Optional: Fade in the failure panel
            CanvasGroup canvasGroup = failurePanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                StartCoroutine(FadeInFailurePanel(canvasGroup));
            }
            
            // Set failure message if there's a text component
            if (failureText != null)
            {
                failureText.text = "Challenge Failed!\nTry again with fewer moves.";
            }
        }
    }

    private IEnumerator FadeInFailurePanel(CanvasGroup canvasGroup)
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;

        while (elapsedTime < failureFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / failureFadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator ShowVictoryEffects(int starRating)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            // Show stars one by one
            if (starImages != null)
            {
                for (int i = 0; i < starRating && i < starImages.Length; i++)
                {
                    StartCoroutine(FadeInStar(starImages[i]));
                    yield return new WaitForSeconds(delayBetweenStars);
                }
            }
        }
    }

    private IEnumerator FadeInStar(Image star)
    {
        float elapsedTime = 0f;
        while (elapsedTime < starFillDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / starFillDuration);
            star.color = new Color(star.color.r, star.color.g, star.color.b, alpha);
            yield return null;
        }
    }
}