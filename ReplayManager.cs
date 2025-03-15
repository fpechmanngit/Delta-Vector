using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ReplayManager : MonoBehaviour
{
    [Header("Replay Settings")]
    public float replaySpeed = 1f;
    public bool isReplaying { get; private set; } = false;

    private PlayerMovement playerMovement;
    private MoveHistoryManager historyManager;
    private Vector2Int originalPosition;
    private TrailManager trailManager;
    private CameraController cameraController;
    private GameManager gameManager;
    private UIManager uiManager;
    private string initialScene;
    private bool wasReplayRequested = false;
    private bool sceneChangeDetected = false;
    private RaceEndEffectManager raceEndEffect;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        historyManager = GetComponent<MoveHistoryManager>();
        trailManager = GetComponent<TrailManager>();
        cameraController = Camera.main?.GetComponent<CameraController>();
        gameManager = FindFirstObjectByType<GameManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        raceEndEffect = Camera.main?.GetComponent<RaceEndEffectManager>();
        initialScene = SceneManager.GetActiveScene().name;

        // Add scene change detection
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if ((wasReplayRequested || isReplaying) && scene.name != initialScene)
        {
            sceneChangeDetected = true;
            CancelReplay();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void StartReplay()
    {
        if (!isReplaying && !wasReplayRequested)
        {
            StopAllCoroutines();

            // Disable blur effect during replay
            if (raceEndEffect != null)
            {
                raceEndEffect.DisableEffect();
            }

            // Store current scene
            initialScene = SceneManager.GetActiveScene().name;
            
            // Set timescale and ensure game isn't paused
            Time.timeScale = 1f;

            // Hide results panel
            var lapManager = FindFirstObjectByType<LapManager>();
            if (lapManager != null)
            {
                lapManager.SetResultsPanelVisible(false);
            }

            // Tell GameManager we're entering replay mode
            if (gameManager != null)
            {
                gameManager.OnReplayStart();
            }

            wasReplayRequested = true;
            sceneChangeDetected = false;
            
            // Hide UI elements during replay
            if (uiManager != null)
            {
                uiManager.ToggleMoveHistory(false);
            }
        }
    }

    private void Update()
    {
        // Check for unwanted scene changes
        if ((wasReplayRequested || isReplaying) && SceneManager.GetActiveScene().name != initialScene)
        {
            CancelReplay();
            return;
        }

        // Check if replay was requested but not yet started
        if (wasReplayRequested && !isReplaying && !sceneChangeDetected)
        {
            wasReplayRequested = false;
            StartCoroutine(ReplayMoves());
        }
    }

    private void CancelReplay()
    {
        StopAllCoroutines();
        isReplaying = false;
        wasReplayRequested = false;
        
        if (playerMovement != null)
        {
            playerMovement.isReplayMode = false;
        }

        if (gameManager != null)
        {
            gameManager.OnReplayEnd();
        }

        // Re-enable blur effect
        if (raceEndEffect != null)
        {
            raceEndEffect.EnableEffect();
        }

        // Show the results panel again
        var lapManager = FindFirstObjectByType<LapManager>();
        if (lapManager != null)
        {
            lapManager.SetResultsPanelVisible(true);
        }

        // Show the unlock panel if needed
        ShowUnlockPanelIfNeeded();
    }

    private IEnumerator ReplayMoves()
    {
        isReplaying = true;
        
        if (playerMovement != null)
        {
            playerMovement.isReplayMode = true;
        }

        var moveHistory = historyManager.GetMoveHistory();
        
        if (moveHistory.Count > 0)
        {
            originalPosition = playerMovement.CurrentPosition;

            if (trailManager != null)
            {
                trailManager.ResetTrail();
            }

            if (gameManager != null)
            {
                GameManager.IsPlayerTurn = false;
            }

            // Calculate grid-scaled start position
            Vector2Int firstMoveStart = moveHistory[0].startPosition;
            Vector2Int gridStartPos = new Vector2Int(
                Mathf.RoundToInt(firstMoveStart.x / (float)PlayerMovement.GRID_SCALE),
                Mathf.RoundToInt(firstMoveStart.y / (float)PlayerMovement.GRID_SCALE)
            );
            
            if (playerMovement != null)
            {
                playerMovement.SetSpawnPoint(gridStartPos);
            }

            yield return new WaitForSeconds(0.5f);

            foreach (var move in moveHistory)
            {
                // Check for scene changes
                if (SceneManager.GetActiveScene().name != initialScene)
                {
                    CancelReplay();
                    yield break;
                }

                // Wait for any current movement to complete
                while (playerMovement != null && playerMovement.IsMoving)
                {
                    yield return null;
                }

                // Ensure replay mode is still set
                if (playerMovement != null)
                {
                    playerMovement.isReplayMode = true;
                }

                playerMovement.MoveToIndicator(move.endPosition);

                float waitTime = Mathf.Max(move.duration / replaySpeed, 0.1f);
                yield return new WaitForSeconds(waitTime);
            }
        }

        yield return new WaitForSeconds(0.5f);

        EndReplay();
    }

    private void EndReplay()
    {
        isReplaying = false;
        wasReplayRequested = false;
        
        if (playerMovement != null)
        {
            playerMovement.isReplayMode = false;
        }

        if (gameManager != null)
        {
            gameManager.OnReplayEnd();
            GameManager.IsPlayerTurn = true;
        }

        // Re-enable blur effect
        if (raceEndEffect != null)
        {
            raceEndEffect.EnableEffect();
        }

        // Show the results panel again
        var lapManager = FindFirstObjectByType<LapManager>();
        if (lapManager != null)
        {
            lapManager.SetResultsPanelVisible(true);
        }

        // Show the unlock panel if needed
        ShowUnlockPanelIfNeeded();
    }

    private void ShowUnlockPanelIfNeeded()
    {
        // Check if we're in challenge mode
        if (GameInitializationManager.SelectedGameMode == GameMode.Challenges)
        {
            UnlockPanel unlockPanel = FindFirstObjectByType<UnlockPanel>();
            if (unlockPanel != null)
            {
                // Show the unlock panel
                unlockPanel.ShowUnlockPanel();
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isReplaying = false;
        wasReplayRequested = false;
    }
}