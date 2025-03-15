using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameInitializationManager : MonoBehaviour
{
    // Static variables that persist between scenes
    public static GameObject SelectedCar;
    public static string SelectedTrack;
    public static string SelectedDifficulty;
    public static GameMode SelectedGameMode;

    // Configuration for the second player's appearance
    public Color player2Color = Color.blue;
    
    // References to player objects
    private GameObject player1Car;
    private GameObject player2Car;
    
    // State tracking for spawn completion
    private bool player1HasSpawned = false;
    private bool player2HasSpawned = false;
    
    // Reference to the game manager
    private GameManager gameManager;
    
    // Flag to track if we're in replay mode
    private bool isReplayMode = false;
    private bool isSpawnSequenceRunning = false;
    private string currentScene;

    private void Awake()
    {
        currentScene = SceneManager.GetActiveScene().name;
        ValidateOrCreateTags();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isReplayMode)
        {
            return;
        }
        currentScene = scene.name;
        
        if (isSpawnSequenceRunning)
        {
            isSpawnSequenceRunning = false;
            StopAllCoroutines();
        }
    }

    private void ValidateOrCreateTags()
    {
        // Validation method kept but log messages removed
    }

    private bool TagExists(string tagName)
    {
        try
        {
            return true;
        }
        catch
        {
            return false;
        }
    }

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        if (SelectedCar != null)
        {
            StartCoroutine(SpawnSequence());
        }
    }

    private IEnumerator SpawnSequence()
    {
        isSpawnSequenceRunning = true;

        // Check for replay mode
        if (GameObject.FindObjectsOfType<ReplayManager>().Length > 0)
        {
            isSpawnSequenceRunning = false;
            yield break;
        }

        SpawnPlayer1Car();

        float timeout = 10f;
        float elapsed = 0f;

        while (!player1HasSpawned && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            
            // Check if we're now in replay mode
            if (player1Car != null && player1Car.GetComponent<ReplayManager>()?.isReplaying == true)
            {
                isSpawnSequenceRunning = false;
                yield break;
            }
            
            yield return null;
        }

        if (elapsed >= timeout)
        {
            // If we're in replay mode, this is expected
            if (player1Car != null && player1Car.GetComponent<ReplayManager>()?.isReplaying == true)
            {
                isSpawnSequenceRunning = false;
                yield break;
            }

            // Not in replay mode but no spawn - reset the scene
            if (!isReplayMode)
            {
                isSpawnSequenceRunning = false;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                yield break;
            }
        }
        
        if (SelectedGameMode == GameMode.Race || SelectedGameMode == GameMode.PvE)
        {
            SpawnPlayer2Car();
            
            elapsed = 0f;
            while (!player2HasSpawned && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                
                // Check if we're now in replay mode
                if (player1Car != null && player1Car.GetComponent<ReplayManager>()?.isReplaying == true)
                {
                    isSpawnSequenceRunning = false;
                    yield break;
                }
                
                yield return null;
            }

            if (elapsed >= timeout && !isReplayMode)
            {
                isSpawnSequenceRunning = false;
                yield break;
            }
            
            gameManager.SetPlayerReferences(player1Car, player2Car);
            yield return new WaitForSeconds(0.1f);
            InitializeGameStateAfterSpawn();
        }
        else
        {
            gameManager.SetPlayerReferences(player1Car, null);
            yield return new WaitForSeconds(0.1f);
            InitializeGameStateAfterSpawn();
        }

        isSpawnSequenceRunning = false;
    }

    private void InitializeGameStateAfterSpawn()
    {
        if (SceneManager.GetActiveScene().name != currentScene)
        {
            return;
        }

        if (isReplayMode)
        {
            return;
        }

        DisableAllPlayerInputs();
        
        gameManager.InitializeGameState();
    }

    private void DisableAllPlayerInputs()
    {
        if (player1Car != null)
        {
            var input1 = player1Car.GetComponent<PlayerInput>();
            if (input1 != null)
            {
                input1.enabled = false;
            }
        }

        if (player2Car != null)
        {
            var input2 = player2Car.GetComponent<PlayerInput>();
            if (input2 != null)
            {
                input2.enabled = false;
            }
        }
    }

    private void SpawnPlayer1Car()
    {
        player1Car = Instantiate(SelectedCar, Vector3.zero, Quaternion.identity);
        player1Car.name = "Player1Car";
        player1Car.tag = "Player1";
        
        SetupCarComponents(player1Car, true);

        PlayerInput playerInput = player1Car.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }
        
        MoveIndicatorManager moveIndicator = player1Car.GetComponent<MoveIndicatorManager>();
        if (moveIndicator != null)
        {
            moveIndicator.ClearIndicators();
        }
    }

    private void SpawnPlayer2Car()
    {
        player2Car = Instantiate(SelectedCar, Vector3.zero, Quaternion.identity);
        player2Car.name = "Player2Car";
        player2Car.tag = "Player2";

        SpriteRenderer spriteRenderer = player2Car.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = player2Color;
        }

        // Get AIRulesManager to check settings - but don't auto-create it
        AIRulesManager rulesManager = FindFirstObjectByType<AIRulesManager>();
        bool testFeaturesEnabled = rulesManager != null && rulesManager.enableTestFeatures;
        bool allowVisualizerCreation = rulesManager != null && rulesManager.createPathVisualizer;

        if (SelectedGameMode == GameMode.PvE)
        {
            // Check if UnifiedVectorAI already exists
            UnifiedVectorAI existingVectorAI = player2Car.GetComponent<UnifiedVectorAI>();
            if (existingVectorAI == null)
            {
                // If you want AI functionality in PvE mode, you must manually add the UnifiedVectorAI
                // component to the game object in the main menu scene
                Debug.Log("PvE mode active but no UnifiedVectorAI component found on Player2. " +
                          "Add this component manually if AI behavior is desired.");
            }
            else
            {
                // If the component exists, just configure it with settings
                if (rulesManager != null)
                {
                    existingVectorAI.pathfindingDepth = rulesManager.pathfindingDepth;
                    existingVectorAI.manualStepMode = rulesManager.manualStepMode;
                    existingVectorAI.showPathVisualization = rulesManager.showPathVisualization;
                }
                
                Debug.Log("Using existing UnifiedVectorAI on Player2 for PvE mode");
            }
            
            // Do NOT add AITestEnabler automatically
            // Previously: if (testFeaturesEnabled) { player2Car.AddComponent<AITestEnabler>(); }
            
            // Only handle path visualizer if it already exists
            AIPathVisualizer existingVisualizer = player2Car.GetComponent<AIPathVisualizer>();
            if (existingVisualizer != null && !allowVisualizerCreation)
            {
                Destroy(existingVisualizer);
                Debug.Log("Removed AIPathVisualizer from Player2 (disabled by global settings)");
            }
        }

        SetupCarComponents(player2Car, false);

        PlayerInput playerInput = player2Car.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }
        
        MoveIndicatorManager moveIndicator = player2Car.GetComponent<MoveIndicatorManager>();
        if (moveIndicator != null)
        {
            moveIndicator.ClearIndicators();
        }
    }

    private void SetupCarComponents(GameObject car, bool isPlayer1)
    {
        ReplayManager replayManager = car.GetComponent<ReplayManager>();
        isReplayMode = replayManager != null && replayManager.isReplaying;
        
        PlayerMovement playerMovement = car.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            SpawnManager spawnManager = Object.FindFirstObjectByType<SpawnManager>();
            if (spawnManager != null)
            {
                playerMovement.spawnManager = spawnManager;
                
                if (isPlayer1)
                {
                    playerMovement.OnSpawnComplete += () => 
                    {
                        if (!isReplayMode)
                        {
                            player1HasSpawned = true;
                        }
                    };
                }
                else
                {
                    playerMovement.OnSpawnComplete += () => 
                    {
                        if (!isReplayMode)
                        {
                            player2HasSpawned = true;
                        }
                    };
                }
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        player1Car = null;
        player2Car = null;
        gameManager = null;
    }
}