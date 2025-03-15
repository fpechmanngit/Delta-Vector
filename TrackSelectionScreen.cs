using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TrackSelectionScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;  // The full screen background
    public GameObject trackButtonPrefab;  // Prefab for each track button
    public Transform trackButtonContainer;  // Parent object for track buttons
    
    [Header("Navigation Buttons")]
    public Button playButton;
    public Button backButton;

    [Header("Track Preview")]
    public Image trackPreviewImage;  // Large preview image
    public List<Sprite> trackPreviewSprites;  // List of track preview sprites
    
    [Header("Button Settings")]
    public Vector2 buttonImageSize = new Vector2(100, 100);  // Size for the button preview images
    public Color buttonNormalColor = Color.white;
    public Color buttonSelectedColor = new Color(0.8f, 1f, 0.8f); // Slight green tint for selected
    public Color buttonLockedColor = new Color(0.5f, 0.5f, 0.5f); // Gray for locked tracks
    
    [Header("Lock Icon")]
    public GameObject lockIconPrefab; // Prefab for the lock icon (assign in inspector)

    private List<string> availableTracks = new List<string>();
    private string selectedTrack;
    private Button selectedButton;

    private void Start()
    {
        ValidateComponents();
        LoadAvailableTracks();
        SetupNavigation();
        
        if (playButton != null)
        {
            playButton.interactable = false;
        }
    }

    private void ValidateComponents()
    {
        if (trackPreviewImage == null)
        {
            Debug.LogError("Track Preview Image is not assigned! Please assign it in the Unity Inspector.");
        }

        if (trackPreviewSprites == null || trackPreviewSprites.Count == 0)
        {
            Debug.LogError("No track preview sprites assigned! Please add preview sprites in the Unity Inspector.");
        }
        else
        {
            Debug.Log($"Found {trackPreviewSprites.Count} track preview sprites");
        }

        if (trackButtonPrefab == null)
        {
            Debug.LogError("Track Button Prefab is not assigned!");
        }
    }

    private void LoadAvailableTracks()
    {
        availableTracks.Clear();
        for (int i = 2; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            
            if (sceneName != "MainMenu")
            {
                availableTracks.Add(sceneName);
                CreateTrackButton(sceneName, availableTracks.Count - 1);
            }
        }
        Debug.Log($"Loaded {availableTracks.Count} tracks");
    }

    private void CreateTrackButton(string trackName, int index)
    {
        if (trackButtonPrefab == null || trackButtonContainer == null) return;

        GameObject buttonObj = Instantiate(trackButtonPrefab, trackButtonContainer);
        Button button = buttonObj.GetComponent<Button>();
        
        // Check if this track is unlocked
        bool isUnlocked = CareerProgress.Instance.IsTrackUnlocked(trackName);
        
        if (button != null)
        {
            // Set up the button preview image
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = buttonObj.GetComponentInChildren<Image>();
            }

            if (buttonImage != null && index < trackPreviewSprites.Count)
            {
                // Set the preview sprite
                buttonImage.sprite = trackPreviewSprites[index];
                buttonImage.preserveAspect = true;
                
                // Set the size
                RectTransform rectTransform = buttonImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = buttonImageSize;
                }
                
                // Set initial color based on unlock status
                buttonImage.color = isUnlocked ? buttonNormalColor : buttonLockedColor;
            }

            // Set up the track name text
            TMPro.TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = trackName;
                buttonText.color = isUnlocked ? Color.white : Color.gray;
            }

            // Add lock icon if track is locked
            if (!isUnlocked && lockIconPrefab != null)
            {
                GameObject lockIcon = Instantiate(lockIconPrefab, buttonObj.transform);
                RectTransform lockRect = lockIcon.GetComponent<RectTransform>();
                if (lockRect != null)
                {
                    // Position the lock icon in the center of the button
                    lockRect.anchoredPosition = Vector2.zero;
                }
            }

            // Set button interactivity based on unlock status
            button.interactable = isUnlocked;

            // Add click listener only if track is unlocked
            if (isUnlocked)
            {
                button.onClick.AddListener(() => OnTrackSelected(trackName, button, index));
            }
        }
    }

    private void OnTrackSelected(string trackName, Button clickedButton, int trackIndex)
    {
        Debug.Log($"Track selected: {trackName} at index {trackIndex}");
        
        // Update selection state
        selectedTrack = trackName;
        
        // Update previous button visual state
        if (selectedButton != null)
        {
            selectedButton.interactable = true;
            Image prevImage = selectedButton.GetComponent<Image>();
            if (prevImage != null)
            {
                prevImage.color = buttonNormalColor;
            }
        }
        
        // Update new selected button
        selectedButton = clickedButton;
        selectedButton.interactable = false;
        Image buttonImage = selectedButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = buttonSelectedColor;
        }
        
        // Update large preview image
        if (trackPreviewImage != null)
        {
            if (trackIndex < trackPreviewSprites.Count && trackPreviewSprites[trackIndex] != null)
            {
                trackPreviewImage.sprite = trackPreviewSprites[trackIndex];
                trackPreviewImage.gameObject.SetActive(true);
                Debug.Log($"Updated preview image for track: {trackName}");
            }
            else
            {
                Debug.LogError($"No preview sprite found for track {trackName} at index {trackIndex}");
                trackPreviewImage.sprite = null;
            }
        }

        if (playButton != null)
        {
            playButton.interactable = true;
        }
    }

    private void SetupNavigation()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    private void OnPlayClicked()
    {
        if (string.IsNullOrEmpty(selectedTrack)) return;

        GameInitializationManager.SelectedTrack = selectedTrack;

        if (GameInitializationManager.SelectedCar == null)
        {
            GameObject defaultCar = Resources.Load<GameObject>("Cars/DefaultCar");
            if (defaultCar != null)
            {
                GameInitializationManager.SelectedCar = defaultCar;
                Debug.Log("No car selected - using default car");
            }
            else
            {
                Debug.LogError("Default car not found in Resources/Cars/DefaultCar!");
                return;
            }
        }
        
        SceneManager.LoadScene(selectedTrack);
    }

    private void OnBackClicked()
    {
        transform.parent.Find("MainMenuScreen").gameObject.SetActive(true);
        gameObject.SetActive(false);
    }
}