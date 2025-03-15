using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class ChallengeUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject challengeButtonPrefab;
    public Transform challengeButtonContainer;    
    public Image trackPreviewImage;              
    public Image carPreviewImage;                
    public TMP_Text challengeDescriptionText;    
    public Button backButton;                    
    public Button startButton;                   
    
    [Header("Button Colors")]
    public Color availableColor = Color.white;
    public Color completedColor = Color.green;
    public Color lockedColor = Color.gray;
    public Color selectedColor = Color.yellow;   

    [Header("Layout Settings")]
    public float buttonHeight = 50f;
    public float buttonWidth = 200f;
    public float spacing = 10f;

    [Header("Lock Icon")]
    public GameObject lockIconPrefab;

    private Challenge selectedChallenge;          
    private GameObject selectedButtonObj;         

    private void Start()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        SetupGridLayout();
        LoadChallenges();
        SetupButtons();
        
        if (startButton != null)
        {
            startButton.interactable = false;
        }
    }

    private void SetupGridLayout()
    {
        GridLayoutGroup gridLayout = challengeButtonContainer.gameObject.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = challengeButtonContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        gridLayout.cellSize = new Vector2(buttonWidth, buttonHeight);
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.padding = new RectOffset(20, 20, 20, 20);
    }

    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                transform.parent.Find("MainMenuScreen").gameObject.SetActive(true);
                gameObject.SetActive(false);
            });
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(StartSelectedChallenge);
            startButton.interactable = false;
        }
    }

    private void LoadChallenges()
    {
        foreach (Transform child in challengeButtonContainer)
        {
            Destroy(child.gameObject);
        }

        var allChallenges = FindFirstObjectByType<ChallengeManager>().allChallenges;
        if (allChallenges == null)
        {
            return;
        }

        if (trackPreviewImage != null)
            trackPreviewImage.gameObject.SetActive(false);
        if (carPreviewImage != null)
            carPreviewImage.gameObject.SetActive(false);
        if (challengeDescriptionText != null)
            challengeDescriptionText.text = "Select a challenge to see details";

        foreach (var challenge in allChallenges)
        {
            bool isCompleted = CareerProgress.Instance.IsChallengeCompleted(challenge.id);
            bool isAvailable = challenge.IsAvailable();
            bool isLocked = !isAvailable && !isCompleted;

            CreateChallengeButton(challenge, isCompleted, isAvailable, isLocked);
        }
    }

    private void CreateChallengeButton(Challenge challenge, bool completed, bool available, bool locked)
    {
        GameObject buttonObj = Instantiate(challengeButtonPrefab, challengeButtonContainer);
        
        Button button = buttonObj.GetComponent<Button>();
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            // Add a completion indicator to the text if completed
            string displayText = challenge.displayName;
            if (completed)
            {
                displayText += " ✓"; // Unicode checkmark
            }
            
            buttonText.text = displayText;
            buttonText.color = locked ? Color.gray : Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.textWrappingMode = TextWrappingModes.NoWrap;
            buttonText.overflowMode = TextOverflowModes.Ellipsis;
        }

        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (completed)
                buttonImage.color = completedColor;
            else if (available)
                buttonImage.color = availableColor;
            else
                buttonImage.color = lockedColor;
        }

        if (locked && lockIconPrefab != null)
        {
            GameObject lockIcon = Instantiate(lockIconPrefab, buttonObj.transform);
            RectTransform lockRect = lockIcon.GetComponent<RectTransform>();
            if (lockRect != null)
            {
                lockRect.anchoredPosition = Vector2.zero;
                lockRect.sizeDelta = new Vector2(buttonHeight * 0.5f, buttonHeight * 0.5f);
            }
        }

        // Make button interactive only if it's available and not locked
        button.interactable = available && !locked;

        if (available && !locked)
        {
            button.onClick.AddListener(() => OnChallengeSelected(challenge, buttonObj, buttonImage));
        }
    }

    private void OnChallengeSelected(Challenge challenge, GameObject buttonObj, Image buttonImage)
    {
        if (selectedButtonObj != null && selectedButtonObj != buttonObj)
        {
            Image prevImage = selectedButtonObj.GetComponent<Image>();
            if (prevImage != null)
            {
                bool wasCompleted = CareerProgress.Instance.IsChallengeCompleted(selectedChallenge.id);
                prevImage.color = wasCompleted ? completedColor : availableColor;
            }
        }

        selectedChallenge = challenge;
        selectedButtonObj = buttonObj;
        if (buttonImage != null)
        {
            buttonImage.color = selectedColor;
        }

        UpdateSelectionDisplay(challenge, false);
        
        if (startButton != null)
        {
            startButton.interactable = true;
        }
    }

    private void StartSelectedChallenge()
    {
        if (selectedChallenge == null) return;

        GameInitializationManager.SelectedGameMode = GameMode.Challenges;
        GameInitializationManager.SelectedTrack = selectedChallenge.trackId;
        
        GameObject requiredCar = Resources.Load<GameObject>($"Cars/{selectedChallenge.requiredCarId}");
        if (requiredCar != null)
        {
            GameInitializationManager.SelectedCar = requiredCar;
        }
        else
        {
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(selectedChallenge.trackId);
    }

    private void UpdateSelectionDisplay(Challenge challenge, bool isLocked)
    {
        bool isCompleted = CareerProgress.Instance.IsChallengeCompleted(challenge.id);

        // Update track preview
        if (trackPreviewImage != null)
        {
            string trackPath = $"Previews/Tracks/{challenge.trackId}";
            Sprite trackSprite = Resources.Load<Sprite>(trackPath);
            if (trackSprite != null)
            {
                trackPreviewImage.sprite = trackSprite;
                trackPreviewImage.gameObject.SetActive(true);
            }
            else
            {
                trackPreviewImage.gameObject.SetActive(false);
            }
        }

        // Update car preview
        if (carPreviewImage != null && !isLocked)
        {
            GameObject carPrefab = Resources.Load<GameObject>($"Cars/{challenge.requiredCarId}");
            if (carPrefab != null)
            {
                SpriteRenderer carSprite = carPrefab.GetComponent<SpriteRenderer>();
                if (carSprite != null && carSprite.sprite != null)
                {
                    carPreviewImage.sprite = carSprite.sprite;
                    carPreviewImage.gameObject.SetActive(true);
                }
                else
                {
                    carPreviewImage.gameObject.SetActive(false);
                }
            }
            else
            {
                carPreviewImage.gameObject.SetActive(false);
            }
        }
        else if (carPreviewImage != null)
        {
            carPreviewImage.gameObject.SetActive(false);
        }

        // Update description
        if (challengeDescriptionText != null)
        {
            string description = "";
            
            if (isLocked)
            {
                string requiredChallenges = challenge.requiredChallenges != null && challenge.requiredChallenges.Length > 0 
                    ? string.Join(", ", challenge.requiredChallenges) 
                    : "None";
                    
                description = $"[LOCKED]\n{challenge.description}\n\n" +
                            $"Required Challenges: {requiredChallenges}\n" +
                            $"Complete the required challenges to unlock this one!";
            }
            else
            {
                description = $"{challenge.description}\n\n" +
                            $"Required Car: {challenge.requiredCarId}\n" +
                            $"Target Moves: {challenge.targetMoves}\n" +
                            $"Track: {challenge.trackId}";

                // Add completion status
                if (isCompleted)
                {
                    description += "\n\n[COMPLETED] ✓\nYou can still replay this challenge!";
                }
            }

            challengeDescriptionText.text = description;
        }
    }

    private void OnEnable()
    {
        SetupUI();
    }

    private void OnDisable()
    {
        selectedChallenge = null;
        selectedButtonObj = null;
        
        if (trackPreviewImage != null)
            trackPreviewImage.gameObject.SetActive(false);
        if (carPreviewImage != null)
            carPreviewImage.gameObject.SetActive(false);
        if (challengeDescriptionText != null)
            challengeDescriptionText.text = "";
        if (startButton != null)
            startButton.interactable = false;
    }
}