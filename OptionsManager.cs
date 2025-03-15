using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class OptionsManager : MonoBehaviour
{
    [Header("Panel References")]
    public GameObject optionsPanel;
    public GameObject audioPanel;
    public GameObject visualPanel;
    public GameObject controlsPanel;
    public GameObject gameplayPanel;

    [Header("Audio Settings")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider uiVolumeSlider;
    public TMP_Text masterVolumeText;
    public TMP_Text musicVolumeText;
    public TMP_Text sfxVolumeText;
    public TMP_Text uiVolumeText;

    [Header("Navigation Buttons")]
    public Button audioButton;
    public Button visualButton;
    public Button controlsButton;
    public Button gameplayButton;
    public Button backButton;
    public Button applyButton;

    private AudioManager audioManager;
    private Dictionary<string, float> pendingAudioChanges = new Dictionary<string, float>();
    private Dictionary<string, float> currentAudioLevels = new Dictionary<string, float>();

    private void Awake()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogError("AudioManager not found!");
        }

        // Initialize dictionaries
        pendingAudioChanges["master"] = 1f;
        pendingAudioChanges["music"] = 1f;
        pendingAudioChanges["sfx"] = 1f;
        pendingAudioChanges["ui"] = 1f;

        currentAudioLevels["master"] = 1f;
        currentAudioLevels["music"] = 1f;
        currentAudioLevels["sfx"] = 1f;
        currentAudioLevels["ui"] = 1f;
    }

    private void Start()
    {
        SetupUI();
        LoadSettings();
        HideAllPanels();
    }

    private void SetupUI()
    {
        // Setup audio sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener((value) => {
                UpdateVolumeText(masterVolumeText, value);
                pendingAudioChanges["master"] = value;
                PreviewAudioSettings();
            });
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener((value) => {
                UpdateVolumeText(musicVolumeText, value);
                pendingAudioChanges["music"] = value;
                PreviewAudioSettings();
            });
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.AddListener((value) => {
                UpdateVolumeText(sfxVolumeText, value);
                pendingAudioChanges["sfx"] = value;
                PreviewAudioSettings();
            });
        }

        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.onValueChanged.AddListener((value) => {
                UpdateVolumeText(uiVolumeText, value);
                pendingAudioChanges["ui"] = value;
                PreviewAudioSettings();
            });
        }

        // Setup navigation buttons
        if (audioButton != null)
            audioButton.onClick.AddListener(() => ShowPanel(audioPanel));

        if (visualButton != null)
            visualButton.onClick.AddListener(() => ShowPanel(visualPanel));

        if (controlsButton != null)
            controlsButton.onClick.AddListener(() => ShowPanel(controlsPanel));

        if (gameplayButton != null)
            gameplayButton.onClick.AddListener(() => ShowPanel(gameplayPanel));

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButton);

        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);

        Debug.Log("OptionsManager UI setup complete");
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        HideAllPanels();
        panel.SetActive(true);
    }

    private void HideAllPanels()
    {
        if (audioPanel != null) audioPanel.SetActive(false);
        if (visualPanel != null) visualPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
    }

    private void UpdateVolumeText(TMP_Text text, float value)
    {
        if (text != null)
        {
            text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void PreviewAudioSettings()
    {
        if (audioManager != null)
        {
            // Preview the master volume setting
            audioManager.SetMasterVolume(pendingAudioChanges["master"]);
            
            // You can add more specific volume controls here as you implement them
            // For example:
            // audioManager.SetMusicVolume(pendingAudioChanges["music"]);
            // audioManager.SetSFXVolume(pendingAudioChanges["sfx"]);
            // audioManager.SetUIVolume(pendingAudioChanges["ui"]);
        }
    }

    private void LoadSettings()
    {
        // Load saved volumes
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);

        // Update sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = masterVolume;
            UpdateVolumeText(masterVolumeText, masterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = musicVolume;
            UpdateVolumeText(musicVolumeText, musicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfxVolume;
            UpdateVolumeText(sfxVolumeText, sfxVolume);
        }

        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.value = uiVolume;
            UpdateVolumeText(uiVolumeText, uiVolume);
        }

        // Update current levels
        currentAudioLevels["master"] = masterVolume;
        currentAudioLevels["music"] = musicVolume;
        currentAudioLevels["sfx"] = sfxVolume;
        currentAudioLevels["ui"] = uiVolume;

        // Apply loaded settings
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(masterVolume);
            // Add other volume controls as implemented
        }

        Debug.Log("Settings loaded successfully");
    }

    private void SaveSettings()
    {
        // Save audio settings
        PlayerPrefs.SetFloat("MasterVolume", pendingAudioChanges["master"]);
        PlayerPrefs.SetFloat("MusicVolume", pendingAudioChanges["music"]);
        PlayerPrefs.SetFloat("SFXVolume", pendingAudioChanges["sfx"]);
        PlayerPrefs.SetFloat("UIVolume", pendingAudioChanges["ui"]);

        // Update current levels
        currentAudioLevels = new Dictionary<string, float>(pendingAudioChanges);

        PlayerPrefs.Save();
        Debug.Log("Settings saved successfully");
    }

    private void ApplySettings()
    {
        SaveSettings();
        
        // Apply volumes
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(pendingAudioChanges["master"]);
            // Add other volume controls as implemented
        }

        Debug.Log("Settings applied successfully");
    }

    private void OnBackButton()
    {
        // Revert to previous settings if not applied
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(currentAudioLevels["master"]);
            // Add other volume controls as implemented
        }

        // Reset pending changes
        pendingAudioChanges = new Dictionary<string, float>(currentAudioLevels);

        // Reset UI
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = currentAudioLevels["master"];
            UpdateVolumeText(masterVolumeText, currentAudioLevels["master"]);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = currentAudioLevels["music"];
            UpdateVolumeText(musicVolumeText, currentAudioLevels["music"]);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = currentAudioLevels["sfx"];
            UpdateVolumeText(sfxVolumeText, currentAudioLevels["sfx"]);
        }

        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.value = currentAudioLevels["ui"];
            UpdateVolumeText(uiVolumeText, currentAudioLevels["ui"]);
        }

        // Hide options panel
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }

        Debug.Log("Settings reverted to previous values");
    }

    public void ShowOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
            ShowPanel(audioPanel); // Show audio panel by default
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
        
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        
        if (uiVolumeSlider != null)
            uiVolumeSlider.onValueChanged.RemoveAllListeners();
        
        if (audioButton != null)
            audioButton.onClick.RemoveAllListeners();
        
        if (visualButton != null)
            visualButton.onClick.RemoveAllListeners();
        
        if (controlsButton != null)
            controlsButton.onClick.RemoveAllListeners();
        
        if (gameplayButton != null)
            gameplayButton.onClick.RemoveAllListeners();
        
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();
        
        if (applyButton != null)
            applyButton.onClick.RemoveAllListeners();
    }
}