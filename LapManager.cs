using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LapManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text newRecordText;
    [SerializeField] private TMP_Text modeValueText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text movesCountText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button replayButton;

    [Header("Player Stats")]
    [SerializeField] private string playerName = "Player 1";

    [Header("Checkpoint Settings")]
    public bool requireCheckpoints = true;

    private bool hasFinished = false;
    private bool player1HasLeftStartLine = false;
    private bool player2HasLeftStartLine = false;
    private GameMode currentGameMode;
    private const string BEST_SCORE_KEY = "BestScore_";
    private bool raceEnded = false;
    private GameObject player1Car;
    private GameObject player2Car;
    private GameUIManager gameUIManager;
    
    private CheckpointManager checkpointManager;
    private AudioManager audioManager;
    private ChallengeManager challengeManager;
    private GameManager gameManager;
    private UIManager uiManager;
    private RaceEventsManager raceEvents;

    private void Start()
    {
        Debug.Log("LapManager started - Initializing race");
        
        InitializeReferences();
        SetupButtons();

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
            Debug.Log("Results panel hidden on start");
        }

        player1Car = GameObject.FindGameObjectWithTag("Player1");
        player2Car = GameObject.FindGameObjectWithTag("Player2");
    }

    private void InitializeReferences()
    {
        checkpointManager = FindFirstObjectByType<CheckpointManager>();
        audioManager = FindFirstObjectByType<AudioManager>();
        challengeManager = FindFirstObjectByType<ChallengeManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        raceEvents = FindFirstObjectByType<RaceEventsManager>();
        gameUIManager = FindFirstObjectByType<GameUIManager>();

        currentGameMode = GameInitializationManager.SelectedGameMode;

        if (checkpointManager == null && requireCheckpoints)
        {
            Debug.LogError("CheckpointManager not found but checkpoints are required!");
        }
        if (gameUIManager == null)
        {
            Debug.LogError("GameUIManager not found!");
        }
        if (audioManager == null)
        {
            Debug.LogError("AudioManager not found!");
        }
        if (gameManager == null)
        {
            Debug.LogError("GameManager not found!");
        }
        if (uiManager == null)
        {
            Debug.LogError("UIManager not found!");
        }
        if (raceEvents == null)
        {
            Debug.LogError("RaceEventsManager not found!");
        }
        if (currentGameMode == GameMode.Challenges && challengeManager == null)
        {
            Debug.LogError("ChallengeManager not found but we're in Challenge mode!");
        }
    }

    private void DisablePlayerMovement(GameObject playerCar)
    {
        if (playerCar == null) return;

        var playerInput = playerCar.GetComponent<PlayerInput>();
        var moveIndicator = playerCar.GetComponent<MoveIndicatorManager>();

        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        if (moveIndicator != null)
        {
            moveIndicator.ClearIndicators();
        }
    }

    private void SetupButtons()
    {
        Debug.Log("Setting up UI buttons");
        
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartRace);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (replayButton != null)
        {
            replayButton.onClick.AddListener(() => {
                Debug.Log("Replay button clicked");
                var player = GameObject.FindGameObjectWithTag("Player1");
                if (player != null)
                {
                    var replayManager = player.GetComponent<ReplayManager>();
                    if (replayManager != null)
                    {
                        replayManager.StartReplay();
                    }
                }
            });
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger entered by: {other.gameObject.name} with tag: {other.tag}");
        
        if (hasFinished)
        {
            Debug.Log("Race already finished, ignoring trigger");
            return;
        }
        
        if (other.CompareTag("Player1"))
        {
            Debug.Log($"Player1 entered finish line trigger");
            if (player1HasLeftStartLine)
            {
                if (requireCheckpoints && checkpointManager != null)
                {
                    if (!checkpointManager.AreAllCheckpointsActivated(true))
                    {
                        Debug.Log("Player1 cannot finish race - not all checkpoints activated!");
                        return;
                    }
                }
                
                Debug.Log($"Player1 has completed the race");
                CompleteRace(true);
            }
        }
        else if (other.CompareTag("Player2"))
        {
            Debug.Log($"Player2 entered finish line trigger");
            if (player2HasLeftStartLine)
            {
                if (requireCheckpoints && checkpointManager != null)
                {
                    if (!checkpointManager.AreAllCheckpointsActivated(false))
                    {
                        Debug.Log("Player2 cannot finish race - not all checkpoints activated!");
                        return;
                    }
                }
                
                Debug.Log($"Player2 has completed the race");
                CompleteRace(false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player1"))
        {
            player1HasLeftStartLine = true;
            Debug.Log($"Player1 has left start line");
        }
        else if (other.CompareTag("Player2"))
        {
            player2HasLeftStartLine = true;
            Debug.Log($"Player2 has left start line");
        }
    }

    private void CompleteRace(bool isPlayer1)
    {
        if (hasFinished) return;
        hasFinished = true;

        int currentMoveCount = 0;
        if (gameUIManager != null)
        {
            currentMoveCount = gameUIManager.GetCurrentMoveCount();
        }

        if (challengeManager != null)
        {
            string currentTrackId = SceneManager.GetActiveScene().name;
            challengeManager.CheckChallengeCompletion(currentTrackId, currentMoveCount);
            Debug.Log("Challenge completion check triggered");
        }

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
            Canvas resultsCanvas = resultsPanel.GetComponent<Canvas>();
            if (resultsCanvas != null)
            {
                resultsCanvas.sortingOrder = 100;
            }
        }

        string trackId = SceneManager.GetActiveScene().name;
        int previousBest = PlayerPrefs.GetInt(BEST_SCORE_KEY + trackId, 0);
        if (previousBest == 0 || currentMoveCount < previousBest)
        {
            PlayerPrefs.SetInt(BEST_SCORE_KEY + trackId, currentMoveCount);
            PlayerPrefs.Save();
        }

        UpdateResultsUI(currentMoveCount, previousBest);

        if (gameUIManager != null)
        {
            gameUIManager.SetGameUIVisibility(false);
        }

        if (gameManager != null)
        {
            gameManager.EndRace();
        }
    }

    private void UpdateResultsUI(int currentScore, int previousBest)
    {
        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(previousBest == 0 || currentScore < previousBest);
        }

        if (modeValueText != null)
        {
            modeValueText.text = currentGameMode == GameMode.TimeTrial ? "Time Trial" : 
                                currentGameMode == GameMode.Challenges ? "Challenge" : "Race";
        }

        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        if (movesCountText != null)
        {
            movesCountText.text = currentScore.ToString();
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = previousBest > 0 ? previousBest.ToString() : "-";
        }
    }

    public void RestartRace()
    {
        Debug.Log("Restarting race");
        
        if (gameUIManager != null)
        {
            gameUIManager.SetGameUIVisibility(true);
        }

        if (checkpointManager != null)
        {
            checkpointManager.ResetCheckpoints();
        }
        
        if (audioManager != null)
        {
            audioManager.StopAllGameSounds();
        }

        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu");
        
        if (audioManager != null)
        {
            audioManager.StopAllGameSounds();
        }

        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }

    public void SetResultsPanelVisible(bool visible)
    {
        if (resultsPanel != null)
        {
            Debug.Log($"Setting results panel visibility to: {visible}");
            resultsPanel.SetActive(visible);
        }
    }
}