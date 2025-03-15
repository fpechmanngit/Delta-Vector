using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
   [Header("Button References")]
   public Button timeTrialButton;
   public Button pvpButton; 
   public Button pveButton;
   public Button garageButton;
   public Button challengesButton;
   public Button easyButton;
   public Button hardButton;
   public Button exitButton;
   public Button optionsButton;

   [Header("Button Sprites")]
   public Sprite timeTrialNormal;
   public Sprite pvpNormal;
   public Sprite pveNormal;
   public Sprite garageNormal;
   public Sprite challengesNormal;
   public Sprite easyNormal;
   public Sprite hardNormal;
   public Sprite exitNormal;
   public Sprite optionsNormal;

   public Sprite timeTrialHighlight;
   public Sprite pvpHighlight;
   public Sprite pveHighlight;
   public Sprite garageHighlight;
   public Sprite challengesHighlight;
   public Sprite easyHighlight;
   public Sprite hardHighlight;
   public Sprite exitHighlight;
   public Sprite optionsHighlight;

   [Header("Lock Settings")]
   public Color lockedColor = new Color(0.5f, 0.5f, 0.5f);
   public GameObject lockIconPrefab;

   [Header("Panels")]
   public GameObject trackSelectionPanel;  
   public GameObject garagePanel;         
   public GameObject challengesPanel;     
   public GameObject mainMenuPanel;       
   public GameObject optionsPanel;

   private bool isEasySelected = true;
   private UIAudioHandler audioHandler;
   private GlobalUIEventHandler globalHandler;

   private void Awake()
   {
       HideAllPanels();
       if (mainMenuPanel != null)
       {
           mainMenuPanel.SetActive(true);
       }

       if (GetComponent<RectTransform>() == null)
       {
           gameObject.AddComponent<RectTransform>();
           Debug.Log("Added missing RectTransform to MainMenuManager");
       }
   }

   private void Start()
   {
       InitializeButtons();
       SetInitialDifficulty();
       
       if (GameInitializationManager.SelectedCar == null)
       {
           LoadDefaultCar();
       }

       audioHandler = FindObjectOfType<UIAudioHandler>();
       if (audioHandler == null)
       {
           Debug.LogError("UIAudioHandler not found in scene!");
       }

       globalHandler = FindObjectOfType<GlobalUIEventHandler>();
       if (globalHandler == null && audioHandler != null)
       {
           GameObject handlerObj = new GameObject("GlobalUIEventHandler");
           globalHandler = handlerObj.AddComponent<GlobalUIEventHandler>();
           globalHandler.Initialize(audioHandler);
       }
       
       CheckButtonUnlockStatus();

       HideAllPanels();
       if (mainMenuPanel != null)
       {
           mainMenuPanel.SetActive(true);
       }

       RefreshAllHandlers();
   }

   private void HideAllPanels()
   {
       if (trackSelectionPanel != null) trackSelectionPanel.SetActive(false);
       if (garagePanel != null) garagePanel.SetActive(false);
       if (challengesPanel != null) challengesPanel.SetActive(false);
       if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
       if (optionsPanel != null) optionsPanel.SetActive(false);
   }

   private void OnEnable()
   {
       HideAllPanels();
       if (mainMenuPanel != null)
       {
           mainMenuPanel.SetActive(true);
       }
       RefreshAllHandlers();
   }

   private void RefreshAllHandlers()
   {
       StartCoroutine(DelayedRefresh());
   }

   private IEnumerator DelayedRefresh()
   {
       yield return null;

       if (audioHandler == null)
       {
           audioHandler = FindObjectOfType<UIAudioHandler>();
       }

       if (globalHandler == null)
       {
           globalHandler = FindObjectOfType<GlobalUIEventHandler>();
       }

       if (audioHandler == null)
       {
           GameObject audioObj = new GameObject("UIAudioHandler");
           audioHandler = audioObj.AddComponent<UIAudioHandler>();
           DontDestroyOnLoad(audioObj);
           Debug.Log("Created new UIAudioHandler");
       }

       if (globalHandler == null && audioHandler != null)
       {
           GameObject handlerObj = new GameObject("GlobalUIEventHandler");
           handlerObj.transform.SetParent(audioHandler.transform);
           globalHandler = handlerObj.AddComponent<GlobalUIEventHandler>();
           globalHandler.Initialize(audioHandler);
           Debug.Log("Created new GlobalUIEventHandler");
       }

       if (audioHandler != null)
       {
           audioHandler.enabled = false;
           if (globalHandler != null)
           {
               globalHandler.enabled = false;
           }
           yield return null;

           audioHandler.enabled = true;
           if (globalHandler != null)
           {
               globalHandler.enabled = true;
               globalHandler.Initialize(audioHandler);
           }
           yield return null;
       }

       Button[] allButtons = FindObjectsOfType<Button>();
       foreach (Button button in allButtons)
       {
           if (button != null && button.gameObject.activeInHierarchy)
           {
               ButtonHoverEffect hoverEffect = button.GetComponent<ButtonHoverEffect>();
               if (hoverEffect == null)
               {
                   hoverEffect = button.gameObject.AddComponent<ButtonHoverEffect>();
               }
               hoverEffect.ResetState();
           }
       }

       if (globalHandler != null)
       {
           globalHandler.OnRefresh();
       }

       if (audioHandler != null)
       {
           audioHandler.RefreshAllButtons();
       }

       Debug.Log($"Completed refresh of {allButtons.Length} buttons");
   }

   private void InitializeButtons()
   {
       if (timeTrialButton != null)
       {
           SetupButtonSprites(timeTrialButton, timeTrialNormal, timeTrialHighlight);
           timeTrialButton.onClick.AddListener(() => OnTimeTrialClicked());
       }

       if (pvpButton != null)
       {
           SetupButtonSprites(pvpButton, pvpNormal, pvpHighlight);
           pvpButton.onClick.AddListener(() => OnPVPClicked());
       }

       if (pveButton != null)
       {
           SetupButtonSprites(pveButton, pveNormal, pveHighlight);
           pveButton.onClick.AddListener(() => OnPVEClicked());
       }

       if (garageButton != null)
       {
           SetupButtonSprites(garageButton, garageNormal, garageHighlight);
           garageButton.onClick.AddListener(() => OnGarageClicked());
       }

       if (challengesButton != null)
       {
           SetupButtonSprites(challengesButton, challengesNormal, challengesHighlight);
           challengesButton.onClick.AddListener(() => OnChallengesClicked());
       }

       if (easyButton != null)
       {
           SetupButtonSprites(easyButton, easyNormal, easyHighlight);
           easyButton.onClick.AddListener(() => OnDifficultySelected(true));
       }

       if (hardButton != null)
       {
           SetupButtonSprites(hardButton, hardNormal, hardHighlight);
           hardButton.onClick.AddListener(() => OnDifficultySelected(false));
       }

       if (exitButton != null)
       {
           SetupButtonSprites(exitButton, exitNormal, exitHighlight);
           exitButton.onClick.AddListener(() => QuitGame());
       }

       if (optionsButton != null)
       {
           SetupButtonSprites(optionsButton, optionsNormal, optionsHighlight);
           optionsButton.onClick.AddListener(() => OnOptionsClicked());
       }
   }

   private void CheckButtonUnlockStatus()
   {
       var completedChallenges = CareerProgress.Instance.GetCompletedChallenges();
       bool hasCompletedAnyChallenge = completedChallenges.Count > 0;

       if (timeTrialButton != null)
       {
           SetupLockedButton(timeTrialButton, !hasCompletedAnyChallenge);
       }

       if (pvpButton != null)
       {
           SetupLockedButton(pvpButton, !hasCompletedAnyChallenge);
       }

       if (pveButton != null)
       {
           SetupLockedButton(pveButton, !hasCompletedAnyChallenge);
       }

       RefreshAllHandlers();
   }

   private void SetupLockedButton(Button button, bool isLocked)
   {
       button.interactable = !isLocked;

       Image buttonImage = button.GetComponent<Image>();
       if (buttonImage != null)
       {
           buttonImage.color = isLocked ? lockedColor : Color.white;
       }

       Transform lockIcon = button.transform.Find("LockIcon");
       
       if (isLocked)
       {
           if (lockIcon == null && lockIconPrefab != null)
           {
               GameObject lockObj = Instantiate(lockIconPrefab, button.transform);
               lockObj.name = "LockIcon";
               
               RectTransform lockRect = lockObj.GetComponent<RectTransform>();
               if (lockRect != null)
               {
                   lockRect.anchoredPosition = Vector2.zero;
               }
           }
       }
       else
       {
           if (lockIcon != null)
           {
               Destroy(lockIcon.gameObject);
           }
       }
   }

   private void SetupButtonSprites(Button button, Sprite normal, Sprite highlighted)
   {
       Image buttonImage = button.GetComponent<Image>();
       if (buttonImage != null)
       {
           buttonImage.sprite = normal;
           
           SpriteState spriteState = new SpriteState();
           spriteState.highlightedSprite = highlighted;
           spriteState.pressedSprite = highlighted;
           button.spriteState = spriteState;
       }
   }

   private void SetInitialDifficulty()
   {
       OnDifficultySelected(true);
   }

   private void LoadDefaultCar()
   {
       GameObject defaultCar = Resources.Load<GameObject>("Cars/DefaultCar");
       if (defaultCar != null)
       {
           GameInitializationManager.SelectedCar = defaultCar;
           Debug.Log("Default car loaded");
       }
       else
       {
           Debug.LogError("Default car not found in Resources/Cars/DefaultCar!");
       }
   }

   private void OnTimeTrialClicked()
   {
       HideAllPanels();
       if (trackSelectionPanel != null)
       {
           trackSelectionPanel.SetActive(true);
           GameInitializationManager.SelectedGameMode = GameMode.TimeTrial;
           Debug.Log("Time Trial mode selected");
           RefreshAllHandlers();
       }
   }

   private void OnPVPClicked()
   {
       HideAllPanels();
       if (trackSelectionPanel != null)
       {
           trackSelectionPanel.SetActive(true);
           GameInitializationManager.SelectedGameMode = GameMode.Race;
           Debug.Log("PVP mode selected");
           RefreshAllHandlers();
       }
   }

   private void OnPVEClicked()
   {
       HideAllPanels();
       if (trackSelectionPanel != null)
       {
           trackSelectionPanel.SetActive(true);
           GameInitializationManager.SelectedGameMode = GameMode.PvE;
           Debug.Log("PvE mode selected");
           RefreshAllHandlers();
       }
   }

   private void OnGarageClicked()
   {
       HideAllPanels();
       if (garagePanel != null)
       {
           garagePanel.SetActive(true);
           Debug.Log("Garage panel opened");
           RefreshAllHandlers();
       }
   }

   private void OnChallengesClicked()
   {
       HideAllPanels();
       if (challengesPanel != null)
       {
           challengesPanel.SetActive(true);
           GameInitializationManager.SelectedGameMode = GameMode.Challenges;
           Debug.Log("Entered Challenges mode");
           RefreshAllHandlers();
       }
   }

   private void OnOptionsClicked()
   {
       var optionsManager = FindObjectOfType<OptionsManager>();
       if (optionsManager != null)
       {
           optionsManager.ShowOptions();
           Debug.Log("Options menu opened");
           RefreshAllHandlers();
       }
       else
       {
           Debug.LogError("OptionsManager not found in scene!");
       }
   }

   private void OnDifficultySelected(bool isEasy)
   {
       isEasySelected = isEasy;
       
       if (easyButton != null)
       {
           easyButton.GetComponent<Image>().sprite = isEasy ? easyHighlight : easyNormal;
       }
       if (hardButton != null)
       {
           hardButton.GetComponent<Image>().sprite = !isEasy ? hardHighlight : hardNormal;
       }

       GameInitializationManager.SelectedDifficulty = isEasy ? "Easy" : "Hard";
       RefreshAllHandlers();
   }

   private void QuitGame()
   {
       Debug.Log("Quitting game...");
       #if UNITY_EDITOR
           UnityEditor.EditorApplication.isPlaying = false;
       #else
           Application.Quit();
       #endif
   }

   public void ReturnToMainMenu()
   {
       HideAllPanels();
       if (mainMenuPanel != null)
       {
           mainMenuPanel.SetActive(true);
       }
       RefreshAllHandlers();
       Debug.Log("Returned to main menu");
   }

   private void OnDestroy()
   {
       if (timeTrialButton != null) timeTrialButton.onClick.RemoveAllListeners();
       if (pvpButton != null) pvpButton.onClick.RemoveAllListeners();
       if (pveButton != null) pveButton.onClick.RemoveAllListeners();
       if (garageButton != null) garageButton.onClick.RemoveAllListeners();
       if (challengesButton != null) challengesButton.onClick.RemoveAllListeners();
       if (easyButton != null) easyButton.onClick.RemoveAllListeners();
       if (hardButton != null) hardButton.onClick.RemoveAllListeners();
       if (exitButton != null) exitButton.onClick.RemoveAllListeners();
       if (optionsButton != null) optionsButton.onClick.RemoveAllListeners();
   }
}