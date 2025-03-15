using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class GameUIManager : MonoBehaviour
{
    [Header("Speed Display")]
    public GameObject speedDisplayPanel;
    public TMP_Text speedText;
    public string speedFormat = "Speed: {0}";

    [Header("Move Counter")]
    public GameObject moveCountPanel;
    public TMP_Text moveCountText;
    public string moveCountFormat = "Moves: {0}";
    private int currentMoveCount = 0;

    [Header("Challenge Target")]
    public GameObject targetMovesPanel;
    public TMP_Text targetMovesText;
    public string targetMovesFormat = "Target: {0}";

    [Header("Minimap Settings")]
    public RawImage minimapDisplay;
    public Camera minimapCamera;
    public GameObject minimapPanel;
    public float minimapSize = 15f;
    public float minimapFollowSpeed = 5f;
    public Vector2 minimapOffset = Vector2.zero;
    public float minimapHideSpeed = 5f;
    private bool isMinimapHovered = false;
    private float targetMinimapAlpha = 1f;
    private CanvasGroup minimapCanvasGroup;
    private CanvasGroup minimapPanelCanvasGroup;

    [Header("Minimap Zoom")]
    public float[] zoomLevels = new float[] { 5f, 20f, 40f, 60f };
    private int currentZoomIndex = 0;
    public Button zoomInButton;
    public Button zoomOutButton;

    [Header("Pause Menu Settings")]
    public Button pauseButton;
    public GameObject pauseMenuPanel;
    public Image pauseMenuBackground;
    
    [Header("Pause Menu Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button mainMenuButton;
    public Button optionsButton;
    
    [Header("Pause Menu Animation")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.2f;
    public float menuScaleInDuration = 0.3f;
    private bool isPaused = false;

    private PlayerMovement activePlayer;
    private LapManager lapManager;
    private GameManager gameManager;
    private ChallengeManager challengeManager;
    private RenderTexture minimapRenderTexture;
    private CanvasGroup pauseMenuCanvasGroup;

    public delegate void MoveCountChangedHandler(int newCount);
    public event MoveCountChangedHandler OnMoveCountChanged;

    private static GameUIManager instance;
    public static GameUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameUIManager>();
            }
            return instance;
        }
    }

    public int GetCurrentMoveCount()
    {
        return currentMoveCount;
    }

    private void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        InitializeComponents();
        SetupUI();
        SetupMinimapCamera();
        SetupZoomButtonListeners();
        SetupPauseMenu();
        SetupMinimapHoverDetection();
        
        InvokeRepeating(nameof(UpdateUI), 0.1f, 0.1f);
    }

    private void InitializeComponents()
    {
        lapManager = FindFirstObjectByType<LapManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        challengeManager = FindFirstObjectByType<ChallengeManager>();
        
        ValidateMinimapComponents();
        UpdateTargetMovesVisibility();
    }

    private void UpdateTargetMovesVisibility()
    {
        bool isInChallengeMode = (GameInitializationManager.SelectedGameMode == GameMode.Challenges);
        
        if (targetMovesPanel != null)
        {
            targetMovesPanel.SetActive(isInChallengeMode);
            
            if (isInChallengeMode && targetMovesText != null && challengeManager != null)
            {
                string currentTrackId = SceneManager.GetActiveScene().name;
                Challenge currentChallenge = challengeManager.GetChallengeByTrackId(currentTrackId);
                
                if (currentChallenge != null)
                {
                    targetMovesText.text = string.Format(targetMovesFormat, currentChallenge.targetMoves);
                }
                else
                {
                    targetMovesText.text = string.Format(targetMovesFormat, "?");
                }
            }
        }
    }

    public void SetGameUIVisibility(bool visible)
    {
        if (speedDisplayPanel != null)
        {
            speedDisplayPanel.SetActive(visible);
        }

        if (moveCountPanel != null)
        {
            moveCountPanel.SetActive(visible);
        }
        
        if (targetMovesPanel != null && GameInitializationManager.SelectedGameMode == GameMode.Challenges)
        {
            targetMovesPanel.SetActive(visible);
        }

        if (minimapPanel != null)
        {
            minimapPanel.SetActive(visible);
        }

        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(visible);
        }
    }

    private void SetupUI()
    {
        if (moveCountText != null)
        {
            moveCountText.text = string.Format(moveCountFormat, currentMoveCount);
        }
        
        UpdateTargetMovesVisibility();
    }

    private void ValidateMinimapComponents()
    {
        if (minimapCamera == null || minimapDisplay == null)
        {
            return;
        }

        if (minimapRenderTexture != null)
        {
            minimapRenderTexture.Release();
        }

        minimapRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
        minimapRenderTexture.antiAliasing = 1;
        minimapRenderTexture.filterMode = FilterMode.Bilinear;

        minimapCamera.targetTexture = minimapRenderTexture;
        minimapDisplay.texture = minimapRenderTexture;

        GameObject minimapObj = minimapDisplay.gameObject;
        minimapCanvasGroup = minimapObj.GetComponent<CanvasGroup>();
        if (minimapCanvasGroup == null)
        {
            minimapCanvasGroup = minimapObj.AddComponent<CanvasGroup>();
        }
        minimapCanvasGroup.alpha = 1f;
    }

    private void SetupMinimapCamera()
    {
        if (minimapCamera == null) return;

        minimapCamera.transform.position = new Vector3(0, 0, -10);
        minimapCamera.transform.rotation = Quaternion.identity;
        minimapCamera.orthographic = true;
        
        if (zoomLevels != null && zoomLevels.Length > 0)
        {
            currentZoomIndex = zoomLevels.Length / 2;
            minimapSize = zoomLevels[currentZoomIndex];
            minimapCamera.orthographicSize = minimapSize;
        }
        else
        {
            minimapCamera.orthographicSize = minimapSize;
        }
        
        minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        minimapCamera.depth = -1;
        minimapCamera.enabled = true;
    }

    private void SetupZoomButtonListeners()
    {
        if (zoomInButton != null)
        {
            zoomInButton.onClick.AddListener(() => ChangeMinimapZoom(-1));
        }

        if (zoomOutButton != null)
        {
            zoomOutButton.onClick.AddListener(() => ChangeMinimapZoom(1));
        }
    }

    public void ChangeMinimapZoom(float zoomDelta)
    {
        if (minimapCamera == null || zoomLevels.Length == 0) return;
        
        if (zoomDelta < 0 && currentZoomIndex < zoomLevels.Length - 1)
        {
            currentZoomIndex++;
        }
        else if (zoomDelta > 0 && currentZoomIndex > 0)
        {
            currentZoomIndex--;
        }
        
        float newSize = zoomLevels[currentZoomIndex];
        minimapCamera.orthographicSize = newSize;
        minimapSize = newSize;
    }

    private void SetupMinimapHoverDetection()
    {
        if (minimapDisplay != null)
        {
            EventTrigger eventTrigger = minimapDisplay.gameObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = minimapDisplay.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => { OnMinimapHoverEnter(); });
            eventTrigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => { OnMinimapHoverExit(); });
            eventTrigger.triggers.Add(exitEntry);
        }
    }

    private void OnMinimapHoverEnter()
    {
        isMinimapHovered = true;
        targetMinimapAlpha = 0f;
    }

    private void OnMinimapHoverExit()
    {
        isMinimapHovered = false;
        targetMinimapAlpha = 1f;
    }

    private void Update()
    {
        if (minimapCanvasGroup != null)
        {
            minimapCanvasGroup.alpha = Mathf.Lerp(
                minimapCanvasGroup.alpha, 
                targetMinimapAlpha, 
                Time.deltaTime * minimapHideSpeed
            );
        }
    }

    private void UpdateUI()
    {
        if (activePlayer == null)
        {
            UpdateActivePlayer();
            return;
        }

        UpdateSpeedDisplay();
        UpdateMinimapCamera();
    }

    private void UpdateActivePlayer()
    {
        GameObject playerObj = GameManager.IsPlayerTurn ? 
            GameObject.FindGameObjectWithTag("Player1") : 
            GameObject.FindGameObjectWithTag("Player2");

        if (playerObj != null)
        {
            activePlayer = playerObj.GetComponent<PlayerMovement>();
        }
    }

    private void UpdateSpeedDisplay()
    {
        if (speedText != null && activePlayer != null)
        {
            Vector2Int velocity = activePlayer.CurrentVelocity;
            int absX = Mathf.Abs(velocity.x);
            int absY = Mathf.Abs(velocity.y);
            int currentSpeed = Mathf.Max(absX, absY);
            speedText.text = string.Format(speedFormat, currentSpeed);
        }
    }

    private void UpdateMinimapCamera()
    {
        if (minimapCamera == null || activePlayer == null) return;

        Vector3 targetPos = activePlayer.transform.position;
        targetPos.x += minimapOffset.x;
        targetPos.y += minimapOffset.y;
        targetPos.z = -10;

        minimapCamera.transform.position = Vector3.Lerp(
            minimapCamera.transform.position,
            targetPos,
            Time.deltaTime * minimapFollowSpeed
        );
    }

    public void IncrementMoveCount()
    {
        currentMoveCount++;
        
        if (moveCountText != null)
        {
            moveCountText.text = string.Format(moveCountFormat, currentMoveCount);
        }

        OnMoveCountChanged?.Invoke(currentMoveCount);
    }

    public void ResetMoveCount()
    {
        currentMoveCount = 0;
        if (moveCountText != null)
        {
            moveCountText.text = string.Format(moveCountFormat, currentMoveCount);
        }
        OnMoveCountChanged?.Invoke(currentMoveCount);
    }

    private void OnDestroy()
    {
        if (minimapRenderTexture != null)
        {
            minimapRenderTexture.Release();
            Destroy(minimapRenderTexture);
        }
        
        if (zoomInButton != null)
        {
            zoomInButton.onClick.RemoveAllListeners();
        }
        
        if (zoomOutButton != null)
        {
            zoomOutButton.onClick.RemoveAllListeners();
        }
    }

    #region Pause Menu Methods
    private void SetupPauseMenu()
    {
        pauseMenuCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
        if (pauseMenuCanvasGroup == null)
        {
            pauseMenuCanvasGroup = pauseMenuPanel.AddComponent<CanvasGroup>();
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OpenOptions);
        }

        pauseMenuPanel.SetActive(false);
        if (pauseMenuBackground != null)
        {
            Color bgColor = pauseMenuBackground.color;
            bgColor.a = 0f;
            pauseMenuBackground.color = bgColor;
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            StartCoroutine(HidePauseMenu());
        }
        else
        {
            StartCoroutine(ShowPauseMenu());
        }
    }

    private IEnumerator ShowPauseMenu()
    {
        isPaused = true;
        Time.timeScale = 0f;

        pauseMenuPanel.SetActive(true);

        if (pauseMenuBackground != null)
        {
            Color bgColor = pauseMenuBackground.color;
            float elapsedTime = 0f;
            float startAlpha = bgColor.a;

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = elapsedTime / fadeInDuration;
                
                bgColor.a = Mathf.Lerp(startAlpha, 0.8f, normalizedTime);
                pauseMenuBackground.color = bgColor;
                
                if (pauseMenuCanvasGroup != null)
                {
                    pauseMenuCanvasGroup.alpha = normalizedTime;
                }
                
                yield return null;
            }
        }
    }

    private IEnumerator HidePauseMenu()
    {
        float elapsedTime = 0f;

        if (pauseMenuBackground != null)
        {
            Color bgColor = pauseMenuBackground.color;
            float startAlpha = bgColor.a;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = elapsedTime / fadeOutDuration;
                
                bgColor.a = Mathf.Lerp(startAlpha, 0f, normalizedTime);
                pauseMenuBackground.color = bgColor;
                
                if (pauseMenuCanvasGroup != null)
                {
                    pauseMenuCanvasGroup.alpha = 1 - normalizedTime;
                }
                
                yield return null;
            }
        }

        pauseMenuPanel.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
    }

    public void ResumeGame()
    {
        StartCoroutine(HidePauseMenu());
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void OpenOptions()
    {
    }
    #endregion
}