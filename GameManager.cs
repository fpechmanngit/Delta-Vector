using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool IsPlayerTurn = true;
    private UIManager uiManager;
    private bool raceEnded = false;
    private bool spawnPhaseComplete = false;
    private bool isInReplayMode = false;
    public bool IsSpawnPhaseComplete => spawnPhaseComplete;
    private RaceEventsManager raceEvents;
    private CheckpointManager checkpointManager;
    
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button skipTurnButton; // Add a reference to your skip turn button if it exists

    private GameObject player1Car;
    private GameObject player2Car;
    private bool isPlayer1Turn = true;
    public bool IsPlayer1Turn => isPlayer1Turn;
    private GameMode currentGameMode;
    private AudioManager audioManager;
    private GameUIManager gameUIManager;
    private bool raceStarted = false;
    private RaceEndEffectManager raceEndEffect;

    // Testing mode fields
    private float testingModeAutoTurnTimer = 0f;
    private const float AUTO_TURN_DELAY = 0.5f;
    private bool testingMode = false;
    private bool isPaused = false;
    private bool isProcessingTurn = false;

    private void Awake()
    {
        Debug.Log("GameManager Awake called");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (audioManager != null)
        {
            audioManager.StopAllGameSounds();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isInReplayMode)
        {
            return;
        }
    }

    public void OnReplayStart()
    {
        isInReplayMode = true;
        Time.timeScale = 1f;
    }

    public void OnReplayEnd()
    {
        isInReplayMode = false;
    }

    public void SetPlayerReferences(GameObject player1, GameObject player2)
    {
        player1Car = player1;
        player2Car = player2;
        Debug.Log($"Player references set - Player1: {(player1 != null ? player1.name : "null")}, Player2: {(player2 != null ? player2.name : "null")}");
    }

    private void Start()
    {
        Debug.Log("GameManager Start called");
        IsPlayerTurn = true;
        currentGameMode = GameInitializationManager.SelectedGameMode;
        isPlayer1Turn = true;
        
        Debug.Log($"GameManager initialized - currentGameMode: {currentGameMode}, isPlayer1Turn: {isPlayer1Turn}");
        
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }

        raceEvents = FindFirstObjectByType<RaceEventsManager>();
        audioManager = FindFirstObjectByType<AudioManager>();
        gameUIManager = FindFirstObjectByType<GameUIManager>();
        raceEndEffect = Camera.main?.GetComponent<RaceEndEffectManager>();
        checkpointManager = FindFirstObjectByType<CheckpointManager>();

        SetupUI();
    }

    private void Update()
    {
        // Log testing mode state occasionally for debugging
        if (Time.frameCount % 300 == 0) // Log every 300 frames to avoid spam
        {
            Debug.Log($"Testing Mode Status - TestingModeManager.testingMode: {TestingModeManager.testingMode}, isPaused: {isPaused}, isProcessingTurn: {isProcessingTurn}, isPlayer1Turn: {isPlayer1Turn}");
        }
        
        // Handle testing mode auto-turns if both players should be AI controlled
        if (TestingModeManager.testingMode && !isPaused && !isProcessingTurn)
        {
            // If it's Player 1's turn, check if they have an AI controller
            if (isPlayer1Turn)
            {
                GameObject player1 = GameObject.FindGameObjectWithTag("Player1");
                if (player1 != null)
                {
                    // Check if we have a UnifiedVectorAI component
                    UnifiedVectorAI ai = player1.GetComponent<UnifiedVectorAI>();
                    if (ai != null)
                    {
                        Debug.Log("Auto-starting AI turn for Player1 in testing mode");
                        ai.StartTurn();
                    }
                    else 
                    {
                        // No AI component on Player 1 - log warning
                        Debug.LogWarning("Testing mode enabled but no UnifiedVectorAI component found on Player1. " +
                                      "Add this component manually if AI behavior is desired.");
                    }
                }
            }
        }
    }

    public void InitializeGameState()
    {
        if (isInReplayMode)
        {
            return;
        }

        Debug.Log("InitializeGameState called");
        isPlayer1Turn = true;
        IsPlayerTurn = true;
        spawnPhaseComplete = true;

        if (player1Car == null)
            player1Car = GameObject.FindGameObjectWithTag("Player1");

        if (player1Car == null)
        {
            Debug.LogError("Player1Car not found!");
            return;
        }

        if (currentGameMode == GameMode.Race || currentGameMode == GameMode.PvE)
        {
            if (player2Car == null)
                player2Car = GameObject.FindGameObjectWithTag("Player2");

            if (player2Car == null)
            {
                Debug.LogError("Player2Car not found in Race or PvE mode!");
                return;
            }

            DisablePlayerMovement(player1Car);
            DisablePlayerMovement(player2Car);
            EnablePlayerMovement(player1Car, true);

            if (currentGameMode == GameMode.PvE)
            {
                // Check for UnifiedVectorAI on Player2
                var unifiedVectorAI = player2Car.GetComponent<UnifiedVectorAI>();
                
                if (unifiedVectorAI == null)
                {
                    Debug.LogWarning("PvE mode active but no UnifiedVectorAI component found on Player2. " +
                                     "Add a UnifiedVectorAI component manually if AI behavior is desired.");
                }
            }
        }
        else
        {
            DisablePlayerMovement(player1Car);
            EnablePlayerMovement(player1Car, true);
        }

        if (!raceStarted)
        {
            raceStarted = true;
            if (raceEvents != null)
            {
                raceEvents.StartRace();
            }
        }
        
        if (checkpointManager != null)
        {
            checkpointManager.UpdateAllCheckpointVisuals();
        }

        Debug.Log($"Game state initialized - currentGameMode: {currentGameMode}, isPlayer1Turn: {isPlayer1Turn}, spawnPhaseComplete: {spawnPhaseComplete}");
    }

    private void DisablePlayerMovement(GameObject playerCar)
    {
        if (playerCar == null)
        {
            Debug.LogWarning("DisablePlayerMovement: playerCar is null");
            return;
        }

        Debug.Log($"DisablePlayerMovement called for {playerCar.name}");
        
        var playerInput = playerCar.GetComponent<PlayerInput>();
        var moveIndicator = playerCar.GetComponent<MoveIndicatorManager>();

        if (playerInput != null)
        {
            playerInput.enabled = false;
            Debug.Log($"PlayerInput disabled for {playerCar.name}");
        }
        else
        {
            Debug.LogWarning($"PlayerInput component not found on {playerCar.name}");
        }

        if (moveIndicator != null)
        {
            moveIndicator.ClearIndicators();
            Debug.Log($"MoveIndicators cleared for {playerCar.name}");
        }
        else
        {
            Debug.LogWarning($"MoveIndicatorManager component not found on {playerCar.name}");
        }
    }

    private void EnablePlayerMovement(GameObject playerCar, bool showMoveIndicators)
    {
        if (playerCar == null)
        {
            Debug.LogWarning("EnablePlayerMovement: playerCar is null");
            return;
        }

        Debug.Log($"EnablePlayerMovement called for {playerCar.name}, showMoveIndicators: {showMoveIndicators}");
        
        var playerInput = playerCar.GetComponent<PlayerInput>();
        var moveIndicator = playerCar.GetComponent<MoveIndicatorManager>();
        var playerMovement = playerCar.GetComponent<PlayerMovement>();
        var speedController = playerCar.GetComponent<PlayerSpeedController>();

        if (playerInput != null)
        {
            playerInput.enabled = true;
            Debug.Log($"PlayerInput enabled for {playerCar.name}");
        }
        else
        {
            Debug.LogWarning($"PlayerInput component not found on {playerCar.name}");
        }

        if (showMoveIndicators && moveIndicator != null && playerMovement != null && speedController != null)
        {
            Debug.Log($"Showing move indicators for {playerCar.name}");
            moveIndicator.ShowPossibleMoves(
                playerMovement.CurrentPosition,
                playerMovement.CurrentVelocity,
                Mathf.RoundToInt(speedController.GetEffectiveSpeed(PlayerMovement.GRID_SCALE) / PlayerMovement.GRID_SCALE)
            );
        }
        else if (showMoveIndicators)
        {
            Debug.LogWarning($"Cannot show move indicators - missing components on {playerCar.name}");
            if (moveIndicator == null) Debug.LogWarning("MoveIndicatorManager is null");
            if (playerMovement == null) Debug.LogWarning("PlayerMovement is null");
            if (speedController == null) Debug.LogWarning("PlayerSpeedController is null");
        }
    }

    private void SetupUI()
    {
        Debug.Log("SetupUI called");
        
        if (replayButton != null)
            replayButton.onClick.AddListener(StartReplay);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartRace);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            
        // Set up skip turn button if it exists
        if (skipTurnButton != null)
        {
            Debug.Log("Skip turn button found, adding listener");
            skipTurnButton.onClick.AddListener(OnSkipTurnButtonClicked);
        }
        else
        {
            Debug.LogWarning("Skip turn button not found in inspector!");
        }

        uiManager = FindFirstObjectByType<UIManager>();
    }

    // Add a dedicated method for the skip button to call
    public void OnSkipTurnButtonClicked()
    {
        Debug.Log("Skip Turn button clicked!");
        
        if (TestingModeManager.testingMode)
        {
            Debug.Log($"Testing mode is active ({TestingModeManager.testingMode}), proceeding with turn skip");
            SwitchToNextPlayerForTesting();
        }
        else
        {
            Debug.LogWarning($"Skip Turn button clicked but TestingModeManager.testingMode is {TestingModeManager.testingMode}. Button should be hidden when testing mode is not active.");
        }
    }

    public void EndRace()
    {
        if (raceEnded || isInReplayMode) return;
        
        raceEnded = true;
        IsPlayerTurn = false;

        bool challengeCompleted = WasCurrentChallengeCompleted();

        if (raceEndEffect != null)
        {
            raceEndEffect.EnableEffect();
        }
        
        if (audioManager != null)
        {
            audioManager.StopAllGameSounds();
        }

        DisablePlayerMovement(player1Car);
        DisablePlayerMovement(player2Car);

        if (player1Car != null)
        {
            player1Car.transform.position = new Vector3(
                Mathf.Round(player1Car.transform.position.x),
                Mathf.Round(player1Car.transform.position.y),
                player1Car.transform.position.z
            );
        }
        if (player2Car != null)
        {
            player2Car.transform.position = new Vector3(
                Mathf.Round(player2Car.transform.position.x),
                Mathf.Round(player2Car.transform.position.y),
                player2Car.transform.position.z
            );
        }

        if (currentGameMode == GameMode.Challenges)
        {
            if (challengeCompleted)
            {
                if (audioManager != null)
                {
                    audioManager.PlayVictorySound();
                }
                
                if (raceEvents != null)
                {
                    int starRating = CalculateStarRating();
                    raceEvents.OnRaceComplete(starRating);
                }

                UnlockPanel unlockPanel = FindFirstObjectByType<UnlockPanel>();
                if (unlockPanel != null)
                {
                    unlockPanel.ShowUnlockPanel();
                }
            }
            else
            {
                if (audioManager != null)
                {
                    audioManager.PlayFailureMusic();
                }
                
                if (raceEvents != null)
                {
                    raceEvents.OnRaceFailure();
                }
            }
        }
        else
        {
            if (audioManager != null)
            {
                audioManager.PlayVictorySound();
            }
            
            if (raceEvents != null)
            {
                int starRating = CalculateStarRating();
                raceEvents.OnRaceComplete(starRating);
            }
        }

        if (resultsPanel != null)
        {
            Canvas resultsCanvasComponent = resultsPanel.GetComponent<Canvas>();
            if (resultsCanvasComponent != null)
            {
                resultsCanvasComponent.sortingOrder = 100;
                resultsCanvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            resultsPanel.SetActive(true);
        }

        if (uiManager != null)
        {
            uiManager.OnRaceEnded();
        }

        if (gameUIManager != null)
        {
            gameUIManager.SetGameUIVisibility(false);
        }
    }

    private bool WasCurrentChallengeCompleted()
    {
        if (currentGameMode != GameMode.Challenges)
        {
            return true;
        }

        string currentTrackId = SceneManager.GetActiveScene().name;
        
        int currentMoveCount = 0;
        if (gameUIManager != null)
        {
            currentMoveCount = gameUIManager.GetCurrentMoveCount();
        }

        ChallengeManager challengeManager = FindFirstObjectByType<ChallengeManager>();
        if (challengeManager != null)
        {
            Challenge currentChallenge = challengeManager.GetChallengeByTrackId(currentTrackId);
            if (currentChallenge != null)
            {
                bool metMoveTarget = currentMoveCount <= currentChallenge.targetMoves;
                bool usedCorrectCar = GameInitializationManager.SelectedCar.name == currentChallenge.requiredCarId;
                return metMoveTarget && usedCorrectCar;
            }
        }

        return false;
    }

    private int CalculateStarRating()
    {
        return 3;
    }

    public void StartReplay()
    {
        if (player1Car != null)
        {
            ReplayManager replayManager = player1Car.GetComponent<ReplayManager>();
            if (replayManager != null)
            {
                replayManager.StartReplay();
            }
        }
    }

    public void RestartRace()
    {
        if (isInReplayMode)
        {
            return;
        }

        raceEnded = false;
        IsPlayerTurn = true;
        isPlayer1Turn = true;
        spawnPhaseComplete = false;
        raceStarted = false;
        
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }

        if (uiManager != null)
        {
            uiManager.ToggleMoveHistory(true);
        }

        if (audioManager != null)
        {
            audioManager.StopAllGameSounds();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToMainMenu()
    {
        if (isInReplayMode)
        {
            return;
        }

        if (audioManager != null)
        {
            audioManager.StopAllGameSounds();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnCheckpointReached(Vector3 position)
    {
        if (isInReplayMode)
        {
            return;
        }

        if (raceEvents != null)
        {
            raceEvents.OnCheckpointReached(position);
        }
    }

    public void EndTurn()
    {
        Debug.Log("EndTurn called");
        
        if (raceEnded || !spawnPhaseComplete || isInReplayMode) 
        {
            Debug.LogWarning($"Cannot end turn - raceEnded: {raceEnded}, spawnPhaseComplete: {spawnPhaseComplete}, isInReplayMode: {isInReplayMode}");
            return;
        }

        var player1Movement = player1Car?.GetComponent<PlayerMovement>();
        var player2Movement = player2Car?.GetComponent<PlayerMovement>();
        if ((player1Movement != null && player1Movement.IsMoving) ||
            (player2Movement != null && player2Movement.IsMoving))
        {
            Debug.LogWarning("Cannot end turn - a player is still moving");
            return;
        }

        Debug.Log($"Processing EndTurn - currentGameMode: {currentGameMode}, isPlayer1Turn: {isPlayer1Turn}");

        if (currentGameMode == GameMode.PvE)
        {
            if (isPlayer1Turn)
            {
                isPlayer1Turn = false;
                IsPlayerTurn = false;
                Debug.Log("PvE mode: Player1's turn ended, switching to AI (Player2)");
                DisablePlayerMovement(player1Car);
                StartNextTurn();
            }
            else
            {
                isPlayer1Turn = true;
                IsPlayerTurn = true;
                Debug.Log("PvE mode: AI's turn ended, switching back to Player1");
                DisablePlayerMovement(player2Car);
                EnablePlayerMovement(player1Car, true);
            }
        }
        else if (currentGameMode == GameMode.Race)
        {
            IsPlayerTurn = false;

            if (isPlayer1Turn)
            {
                Debug.Log("Race mode: Player1's turn ended");
                DisablePlayerMovement(player1Car);
            }
            else
            {
                Debug.Log("Race mode: Player2's turn ended");
                DisablePlayerMovement(player2Car);
            }
            
            SwitchActivePlayer();
            StartNextTurn();
        }
        else
        {
            Debug.Log($"Other game mode ({currentGameMode}): resetting turn to Player1");
            DisablePlayerMovement(player1Car);
            isPlayer1Turn = true;
            IsPlayerTurn = true;
            EnablePlayerMovement(player1Car, true);
        }
        
        if (checkpointManager != null)
        {
            checkpointManager.UpdateAllCheckpointVisuals();
        }
    }

    private void SwitchActivePlayer()
    {
        isPlayer1Turn = !isPlayer1Turn;
        Debug.Log($"SwitchActivePlayer - new active player: {(isPlayer1Turn ? "Player1" : "Player2")}");
        
        DisablePlayerMovement(player1Car);
        DisablePlayerMovement(player2Car);

        if (isPlayer1Turn || currentGameMode != GameMode.PvE)
        {
            EnablePlayerMovement(isPlayer1Turn ? player1Car : player2Car, true);
        }
    }

    public void StartNextTurn()
    {
        Debug.Log("StartNextTurn called");
        
        if (raceEnded || !spawnPhaseComplete || isInReplayMode)
        {
            Debug.LogWarning($"Cannot start next turn - raceEnded: {raceEnded}, spawnPhaseComplete: {spawnPhaseComplete}, isInReplayMode: {isInReplayMode}");
            return;
        }

        IsPlayerTurn = true;
        Debug.Log($"Starting next turn - isPlayer1Turn: {isPlayer1Turn}, currentGameMode: {currentGameMode}");
        
        if (currentGameMode == GameMode.PvE)
        {
            if (!isPlayer1Turn)
            {
                GameObject aiCar = GameObject.FindGameObjectWithTag("Player2");
                if (aiCar != null)
                {
                    // Try to use UnifiedVectorAI if present
                    var vectorAI = aiCar.GetComponent<UnifiedVectorAI>();
                    if (vectorAI != null)
                    {
                        Debug.Log("PvE mode: Starting AI (Player2) turn");
                        vectorAI.StartTurn();
                    }
                    else
                    {
                        // No AI components found - log warning
                        Debug.LogWarning("No UnifiedVectorAI component found on Player2 in PvE mode. " +
                                        "Player2 won't move. Add UnifiedVectorAI component manually if needed.");
                        
                        // Skip back to player's turn
                        isPlayer1Turn = true;
                        IsPlayerTurn = true;
                        Debug.Log("PvE mode: No AI found, switching back to Player1");
                        EnablePlayerMovement(player1Car, true);
                    }
                }
            }
            else
            {
                GameObject playerCar = GameObject.FindGameObjectWithTag("Player1");
                if (playerCar != null)
                {
                    Debug.Log("PvE mode: Starting Player1 turn");
                    EnablePlayerMovement(playerCar, true);
                }
            }
        }
        else if (currentGameMode == GameMode.TimeTrial)
        {
            GameObject playerCar = GameObject.FindGameObjectWithTag("Player1");
            if (playerCar != null)
            {
                Debug.Log("TimeTrial mode: Starting Player1 turn");
                EnablePlayerMovement(playerCar, true);
            }
        }
    }

    // Method to support testing mode
    public void SwitchToNextPlayerForTesting()
    {
        Debug.Log("SwitchToNextPlayerForTesting method called");
        
        if (!spawnPhaseComplete || raceEnded) 
        {
            Debug.LogWarning($"Cannot switch player for testing - spawnPhaseComplete: {spawnPhaseComplete}, raceEnded: {raceEnded}");
            return;
        }
        
        Debug.Log($"Before switch - isPlayer1Turn: {isPlayer1Turn}, IsPlayerTurn: {IsPlayerTurn}, currentGameMode: {currentGameMode}");
        
        // Disable movement for current player
        if (isPlayer1Turn)
        {
            Debug.Log("Disabling movement for Player 1");
            DisablePlayerMovement(player1Car);
        }
        else
        {
            Debug.Log("Disabling movement for Player 2");
            DisablePlayerMovement(player2Car);
        }
        
        // Switch active player
        isPlayer1Turn = !isPlayer1Turn;
        Debug.Log($"Active player switched - new isPlayer1Turn: {isPlayer1Turn}");
        
        // For PvE mode, handle special logic
        if (currentGameMode == GameMode.PvE)
        {
            Debug.Log("Handling PvE mode logic");
            if (!isPlayer1Turn)
            {
                // Switching to AI's turn
                IsPlayerTurn = false;
                Debug.Log("Switching to AI's turn - calling StartNextTurn()");
                StartNextTurn();
            }
            else
            {
                // Switching back to player's turn
                IsPlayerTurn = true;
                Debug.Log("Switching back to player's turn - enabling Player 1 movement");
                EnablePlayerMovement(player1Car, true);
            }
        }
        else
        {
            Debug.Log($"Handling {currentGameMode} mode logic");
            // For other game modes, enable movement for the next player
            EnablePlayerMovement(isPlayer1Turn ? player1Car : player2Car, true);
            StartNextTurn();
        }
        
        // Update checkpoint visuals
        if (checkpointManager != null)
        {
            Debug.Log("Updating checkpoint visuals");
            checkpointManager.UpdateAllCheckpointVisuals();
        }
        else
        {
            Debug.LogWarning("checkpointManager is null, cannot update checkpoint visuals");
        }
        
        Debug.Log($"Testing Mode: Switched to {(isPlayer1Turn ? "Player 1" : "Player 2")}");
    }

    private void OnApplicationQuit()
    {
        if (audioManager != null)
        {
            audioManager.StopAllGameSounds();
        }
    }
}