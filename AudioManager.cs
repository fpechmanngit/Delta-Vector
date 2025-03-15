using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    // Music Settings
    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip[] trackMusicList;
    public AudioClip victoryMusic;
    public AudioClip failureMusic;
    [Range(0f, 1f)] public float menuMusicVolume = 0.5f, trackMusicVolume = 0.5f;
    [Range(0f, 1f)] public float victoryMusicVolume = 0.5f;
    [Range(0f, 1f)] public float failureMusicVolume = 0.5f;
    public float fadeInDuration = 1f, fadeOutDuration = 1f;

    // Vehicle Sound Settings
    [Header("Vehicle")]
    public AudioClip idleSound, revSound, tireScreechSound, gravelSound;
    [Range(0f, 1f)] public float idleSoundVolume = 0.3f, revSoundVolume = 0.5f, 
                                 tireScreechVolume = 0.4f, gravelSoundVolume = 0.4f;

    // UI Sound Settings
    [Header("UI")]
    public AudioClip uiHoverSound, uiClickSound, raceStartSound, moveIndicatorHoverSound;
    [Range(0f, 1f)] public float uiHoverVolume = 0.3f, uiClickVolume = 0.4f, 
                                 raceStartVolume = 0.5f, moveIndicatorHoverVolume = 0.3f;

    [Header("Master")] [Range(0f, 1f)] public float masterVolume = 1f;

    // Private variables
    private static AudioManager instance;
    private bool isFirstLoad = true, isInitialized, isMusicTransitioning;
    private float currentSpeedRatio;
    private Coroutine currentMusicFadeCoroutine;
    private AudioSource menuMusicSource, trackMusicSource, idleSource, revSource, 
                       tireScreechSource, gravelSource, uiSource, victoryMusicSource, failureMusicSource;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneChanged;
        isInitialized = true;

        if (isFirstLoad)
        {
            isFirstLoad = false;
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene == "MainMenu") StartMenuMusic(); else StartRandomTrackMusic();
        }
    }

    private void SetupAudioSources()
    {
        menuMusicSource = CreateAudioSource("MenuMusic", menuMusic, menuMusicVolume, true);
        trackMusicSource = CreateAudioSource("TrackMusic", null, trackMusicVolume, true);
        idleSource = CreateAudioSource("IdleSound", idleSound, idleSoundVolume, true);
        revSource = CreateAudioSource("RevSound", revSound, revSoundVolume, false);
        tireScreechSource = CreateAudioSource("TireScreech", tireScreechSound, tireScreechVolume, false);
        gravelSource = CreateAudioSource("GravelSound", gravelSound, gravelSoundVolume, true);
        uiSource = CreateAudioSource("UISound", null, 1f, false);
        victoryMusicSource = CreateAudioSource("VictoryMusic", victoryMusic, victoryMusicVolume, false);
        failureMusicSource = CreateAudioSource("FailureMusic", failureMusic, failureMusicVolume, false);
    }

    private AudioSource CreateAudioSource(string name, AudioClip clip, float volume, bool loop)
    {
        var obj = new GameObject(name);
        obj.transform.parent = transform;
        var source = obj.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume * masterVolume;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private void OnSceneChanged(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StartCoroutine(HandleSceneChange(scene.name));
    }

    private IEnumerator HandleSceneChange(string sceneName)
    {
        if (!isInitialized) yield break;
        yield return new WaitForSeconds(0.1f);

        if (currentMusicFadeCoroutine != null)
        {
            StopCoroutine(currentMusicFadeCoroutine);
            currentMusicFadeCoroutine = null;
        }

        if (sceneName == "MainMenu")
        {
            StopAllGameSounds(); // Ensure all game sounds are stopped when returning to menu
            if (trackMusicSource != null) yield return StartCoroutine(StopMusicSource(trackMusicSource));
            if (victoryMusicSource != null) victoryMusicSource.Stop();
            if (failureMusicSource != null) failureMusicSource.Stop();
            StartMenuMusic();
        }
        else
        {
            if (menuMusicSource != null) yield return StartCoroutine(StopMusicSource(menuMusicSource));
            if (victoryMusicSource != null) victoryMusicSource.Stop();
            if (failureMusicSource != null) failureMusicSource.Stop();
            StartRandomTrackMusic();
        }
    }

    public void StartMenuMusic()
    {
        if (!isInitialized || menuMusicSource == null || menuMusic == null) return;
        if (menuMusicSource.isPlaying && menuMusicSource.clip == menuMusic && 
            Mathf.Approximately(menuMusicSource.volume, menuMusicVolume * masterVolume)) return;

        if (currentMusicFadeCoroutine != null) StopCoroutine(currentMusicFadeCoroutine);
        if (trackMusicSource != null && trackMusicSource.isPlaying) StartCoroutine(StopMusicSource(trackMusicSource));
        if (menuMusicSource.isPlaying && menuMusicSource.clip != menuMusic) menuMusicSource.Stop();

        menuMusicSource.clip = menuMusic;
        menuMusicSource.volume = 0f;
        if (!menuMusicSource.isPlaying) menuMusicSource.Play();
        currentMusicFadeCoroutine = StartCoroutine(FadeMusicSource(menuMusicSource, menuMusicVolume * masterVolume));
    }

    public void StartRandomTrackMusic()
    {
        if (!isInitialized || trackMusicSource == null || trackMusicList == null || trackMusicList.Length == 0) return;

        if (currentMusicFadeCoroutine != null) StopCoroutine(currentMusicFadeCoroutine);
        if (menuMusicSource != null && menuMusicSource.isPlaying) StartCoroutine(StopMusicSource(menuMusicSource));

        var selectedTrack = trackMusicList[Random.Range(0, trackMusicList.Length)];
        if (trackMusicSource.isPlaying && trackMusicSource.clip != selectedTrack) trackMusicSource.Stop();

        trackMusicSource.clip = selectedTrack;
        trackMusicSource.volume = 0f;
        if (!trackMusicSource.isPlaying) trackMusicSource.Play();
        currentMusicFadeCoroutine = StartCoroutine(FadeMusicSource(trackMusicSource, trackMusicVolume * masterVolume));
    }

    public void PlayVictorySound()
    {
        // First stop all other sounds including track music
        StopAllGameSounds();
        
        // Ensure track music is fully stopped
        if (trackMusicSource != null)
        {
            trackMusicSource.Stop();
            trackMusicSource.clip = null;
        }
        
        // Play victory music with a small delay to ensure clean transition
        if (victoryMusicSource != null && victoryMusic != null)
        {
            victoryMusicSource.clip = victoryMusic;
            victoryMusicSource.volume = victoryMusicVolume * masterVolume;
            victoryMusicSource.Play();
        }
    }

    public void PlayFailureMusic()
    {
        // First stop all other sounds including track music
        StopAllGameSounds();
        
        // Ensure track music is fully stopped
        if (trackMusicSource != null)
        {
            trackMusicSource.Stop();
            trackMusicSource.clip = null;
        }
        
        // Play failure music with a small delay to ensure clean transition
        if (failureMusicSource != null && failureMusic != null)
        {
            failureMusicSource.clip = failureMusic;
            failureMusicSource.volume = failureMusicVolume * masterVolume;
            failureMusicSource.Play();
        }
    }

    private IEnumerator FadeMusicSource(AudioSource source, float targetVolume)
    {
        if (source == null || source.clip == null) yield break;

        isMusicTransitioning = true;
        float startVolume = source.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeInDuration);
            yield return null;
        }

        source.volume = targetVolume;
        isMusicTransitioning = false;
        currentMusicFadeCoroutine = null;
    }

    private IEnumerator StopMusicSource(AudioSource source)
    {
        if (source == null || !source.isPlaying) yield break;

        float startVolume = source.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration && source.isPlaying)
        {
            elapsedTime += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeOutDuration);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
        source.clip = null;
        yield return null;
    }

    // Vehicle Sound Methods
    public void StartIdleSound() { if (idleSource != null && !idleSource.isPlaying) idleSource.Play(); }
    public void StopIdleSound() { if (idleSource != null) idleSource.Stop(); }
    public void PlayRevSound() { if (revSource != null) { revSource.pitch = Random.Range(0.9f, 1.1f); revSource.Play(); } }
    public void PlayTireScreech() { if (tireScreechSource != null) { tireScreechSource.pitch = Random.Range(0.9f, 1.1f); tireScreechSource.Play(); } }
    public void StartGravelSound() { if (gravelSource != null && !gravelSource.isPlaying) gravelSource.Play(); }
    public void StopGravelSound() { if (gravelSource != null) gravelSource.Stop(); }

    // UI Sound Methods
    public void PlayHoverSound() => PlayMoveIndicatorHoverSound();
    public void PlayMoveIndicatorHoverSound() { if (uiSource != null && moveIndicatorHoverSound != null) PlayUISound(moveIndicatorHoverSound, moveIndicatorHoverVolume); }
    public void PlayUIHoverSound() { if (uiSource != null && uiHoverSound != null) PlayUISound(uiHoverSound, uiHoverVolume); }
    public void PlayUIClickSound() { if (uiSource != null && uiClickSound != null) PlayUISound(uiClickSound, uiClickVolume); }
    public void PlayRaceStartSound() { if (uiSource != null && raceStartSound != null) PlayUISound(raceStartSound, raceStartVolume); }
    private void PlayUISound(AudioClip clip, float volume) => uiSource.PlayOneShot(clip, volume * masterVolume);

    // Properties
    public float CurrentSpeedRatio
    {
        get => currentSpeedRatio;
        set
        {
            currentSpeedRatio = Mathf.Clamp01(value);
            if (idleSource != null)
            {
                idleSource.volume = Mathf.Lerp(0.3f, idleSoundVolume, currentSpeedRatio) * masterVolume;
                idleSource.pitch = Mathf.Lerp(0.8f, 1.2f, currentSpeedRatio);
            }
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (menuMusicSource != null) menuMusicSource.volume = menuMusicVolume * masterVolume;
        if (trackMusicSource != null) trackMusicSource.volume = trackMusicVolume * masterVolume;
        if (idleSource != null) idleSource.volume = idleSoundVolume * masterVolume;
        if (revSource != null) revSource.volume = revSoundVolume * masterVolume;
        if (tireScreechSource != null) tireScreechSource.volume = tireScreechVolume * masterVolume;
        if (gravelSource != null) gravelSource.volume = gravelSoundVolume * masterVolume;
        if (victoryMusicSource != null) victoryMusicSource.volume = victoryMusicVolume * masterVolume;
        if (failureMusicSource != null) failureMusicSource.volume = failureMusicVolume * masterVolume;
    }

    public void StopAllGameSounds()
    {
        StopIdleSound();
        if (revSource != null) revSource.Stop();
        if (tireScreechSource != null) tireScreechSource.Stop();
        if (gravelSource != null) gravelSource.Stop();
        if (victoryMusicSource != null) victoryMusicSource.Stop();
        if (failureMusicSource != null) failureMusicSource.Stop();
        if (trackMusicSource != null)
        {
            trackMusicSource.Stop();
            trackMusicSource.clip = null;
        }
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneChanged;
        StopAllCoroutines();
        StopAllGameSounds();

        var sources = new AudioSource[] { 
            menuMusicSource, trackMusicSource, idleSource, 
            revSource, tireScreechSource, gravelSource, uiSource,
            victoryMusicSource, failureMusicSource 
        };
        foreach (var source in sources)
            if (source != null) Destroy(source.gameObject);
    }
}