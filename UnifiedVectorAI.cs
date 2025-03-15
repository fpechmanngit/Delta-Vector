using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DeltaVector.AI;

/// <summary>
/// Main AI controller that coordinates all the AI subsystems
/// Refactored to use specialized components for each major function
/// </summary>
public class UnifiedVectorAI : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Show debug logs and visualization")]
    public bool debugMode = false;

    [Header("Manual Stepping Control")]
    [Tooltip("Use manual stepping mode with spacebar")]
    public bool manualStepMode = true;
    
    [Tooltip("Key to advance AI thinking and execution")]
    public KeyCode stepKey = KeyCode.Space;

    [Header("Pathfinding Settings")]
    [Tooltip("Depth of the search tree (how many moves to look ahead)")]
    [Range(1, 15)]
    public int pathfindingDepth = 5;

    [Header("Evaluation Weights")]
    [Range(0f, 15f)]
    public float distanceWeight = 5f;
    [Range(0f, 15f)]
    public float speedWeight = 6f;
    [Range(0f, 15f)]
    public float terrainWeight = 10f;
    [Range(0f, 15f)]
    public float directionWeight = 3f;
    [Range(0f, 15f)]
    public float pathWeight = 8f;
    [Range(0f, 15f)]
    public float returnToAsphaltWeight = 12f;
    [Range(0f, 15f)]
    public float centerTrackWeight = 4f;
    [Range(0f, 15f)]
    public float trackExitPenaltyWeight = 15f;
    [Range(0f, 15f)]
    public float finishLineWeight = 12f; // NEW: Added finish line weight

    [Header("Path Pruning Settings")]
    [Tooltip("Enable early pruning of obviously bad paths")]
    public bool enableEarlyPathPruning = true;
    
    [Tooltip("How many consecutive off-track positions before pruning a path")]
    [Range(1, 3)]
    public int offTrackToleranceCount = 1;
    
    [Tooltip("Minimum terrain quality (0-1) required to continue path (lower = stricter pruning)")]
    [Range(0.01f, 0.3f)]
    public float minTerrainQualityThreshold = 0.1f;

    [Header("Visualization")]
    [Tooltip("Show visual representation of the AI's decision process")]
    public bool showPathVisualization = true;
    
    [Tooltip("Color for the best path")]
    public Color bestPathColor = new Color(0, 1, 0, 0.8f);
    
    [Tooltip("Color for alternative good paths")]
    public Color goodPathColor = new Color(0, 0.8f, 0, 0.6f);
    
    [Tooltip("Color for average paths")]
    public Color mediumPathColor = new Color(1, 0.8f, 0, 0.6f);
    
    [Tooltip("Color for poor paths")]
    public Color badPathColor = new Color(1, 0, 0, 0.6f);
    
    // AI subsystems
    private AIPathGenerator pathGenerator;
    private AIPathEvaluator pathEvaluator;
    private AITerrainAnalyzer terrainAnalyzer;
    private AIPathExecutor pathExecutor;
    
    // Component references
    private PlayerMovement playerMovement;
    private MoveIndicatorManager moveIndicatorManager;
    private GameManager gameManager;
    private CheckpointManager checkpointManager;
    private PlayerGroundDetector groundDetector; // Added reference to ground detector
    
    // Target tracking
    private Checkpoint targetCheckpoint;
    private Vector2 targetPosition;
    private bool allCheckpointsReached = false; // NEW: Track if all checkpoints are reached
    
    // Path state
    private Path bestPath;
    
    // AI state
    public AIThinkingState thinkingState { get; private set; } = AIThinkingState.Idle;
    private bool isProcessingTurn = false;
    private bool stepKeyPressed = false;
    
    // Added variables to track visualization vs execution
    private bool isPathVisualizationValid = false;
    private Path visualizedPath = null;
    
    // Public properties for accessing state
    public bool IsProcessingTurn => isProcessingTurn;
    public bool IsThinking => thinkingState == AIThinkingState.Thinking;
    public bool IsReadyToExecute => thinkingState == AIThinkingState.ReadyToExecute;
    public int TotalPathsGenerated => pathGenerator != null ? pathGenerator.GetTotalPathsGenerated() : 0;
    public int PathfindingDepth => pathGenerator != null ? pathGenerator.pathfindingDepth : 0;
    public float BestPathScore => bestPath?.AverageScore ?? 0f;
    public float ProcessingProgress => pathGenerator != null ? pathGenerator.GetProgress() : 0f;

    private void Awake()
    {
        // Initialize essential components
        InitializeComponents();
        
        // Apply global settings from AI Rules Manager if available
        ApplyGlobalSettings();
    }

    private void Start()
    {
        // Initialize AI subsystems
        InitializeAISubsystems();
    }

    private void Update()
    {
        // Handle manual stepping control
        if (manualStepMode)
        {
            if (Input.GetKeyDown(stepKey))
            {
                if (debugMode)
                    Debug.Log("Space key detected by UnifiedVectorAI");
                    
                stepKeyPressed = true;
            }

            // Process step when key is pressed
            if (stepKeyPressed)
            {
                if (debugMode)
                    Debug.Log("Processing step in state: " + thinkingState);
                    
                ProcessStep();
                stepKeyPressed = false;
            }
        }
    }

    /// <summary>
    /// Initialize component references
    /// </summary>
    private void InitializeComponents()
    {
        // Get required components from this GameObject
        playerMovement = GetComponent<PlayerMovement>();
        moveIndicatorManager = GetComponent<MoveIndicatorManager>();
        groundDetector = GetComponent<PlayerGroundDetector>(); // Get the ground detector
        
        // Find managers in the scene
        gameManager = FindFirstObjectByType<GameManager>();
        checkpointManager = FindFirstObjectByType<CheckpointManager>();

        // Validate essential components
        if (playerMovement == null)
            Debug.LogError("UnifiedVectorAI: Missing PlayerMovement component!");
        if (moveIndicatorManager == null)
            Debug.LogError("UnifiedVectorAI: Missing MoveIndicatorManager component!");
        if (groundDetector == null)
            Debug.LogWarning("UnifiedVectorAI: Missing PlayerGroundDetector component! Gravel detection will be less effective.");
    }

    /// <summary>
    /// Initialize AI subsystems
    /// </summary>
    private void InitializeAISubsystems()
    {
        // Create and initialize terrain analyzer
        terrainAnalyzer = new AITerrainAnalyzer(transform.position);
        
        // Create and initialize path evaluator
        pathEvaluator = new AIPathEvaluator(terrainAnalyzer, transform.position);
        
        // Create path generator
        GameObject generatorObj = new GameObject("AI_Path_Generator");
        generatorObj.transform.parent = transform;
        pathGenerator = generatorObj.AddComponent<AIPathGenerator>();
        pathGenerator.Initialize(pathEvaluator, terrainAnalyzer, moveIndicatorManager);
        
        // Create path executor
        pathExecutor = gameObject.AddComponent<AIPathExecutor>();
        pathExecutor.Initialize(playerMovement, moveIndicatorManager, terrainAnalyzer);
        
        // Apply settings from rules manager
        AIRulesManager rulesManager = AIRulesManager.Instance;
        if (rulesManager != null)
        {
            pathGenerator.UpdateSettings(rulesManager);
            
            // Update path evaluator with weights from rules manager
            float[] weights = new float[]
            {
                rulesManager.distanceWeight,
                rulesManager.speedWeight,
                rulesManager.terrainWeight,
                rulesManager.directionWeight,
                rulesManager.pathWeight,
                rulesManager.returnToAsphaltWeight,
                rulesManager.centerTrackWeight,
                rulesManager.trackExitPenaltyWeight,
                finishLineWeight // NEW: Added finish line weight
            };
            
            float[] speedSettings = new float[]
            {
                rulesManager.maxStraightSpeed,
                rulesManager.maxTurnSpeed
            };
            
            pathEvaluator.UpdateSettings(weights, speedSettings);
        }
        else
        {
            // Update path generator with local settings
            pathGenerator.pathfindingDepth = pathfindingDepth;
            pathGenerator.enableEarlyPathPruning = enableEarlyPathPruning;
            pathGenerator.offTrackToleranceCount = offTrackToleranceCount;
            pathGenerator.minTerrainQualityThreshold = minTerrainQualityThreshold;
            
            // Update path evaluator with local weights
            float[] weights = new float[]
            {
                distanceWeight,
                speedWeight,
                terrainWeight,
                directionWeight,
                pathWeight,
                returnToAsphaltWeight,
                centerTrackWeight,
                trackExitPenaltyWeight,
                finishLineWeight // NEW: Added finish line weight
            };
            
            float[] speedSettings = new float[]
            {
                7.0f, // maxStraightSpeed default
                3.5f  // maxTurnSpeed default
            };
            
            pathEvaluator.UpdateSettings(weights, speedSettings);
        }
    }

    /// <summary>
    /// Apply global settings from AIRulesManager
    /// </summary>
    private void ApplyGlobalSettings()
    {
        AIRulesManager rulesManager = AIRulesManager.Instance;
        if (rulesManager != null)
        {
            manualStepMode = rulesManager.manualStepMode;
            showPathVisualization = rulesManager.showPathVisualization;
            pathfindingDepth = rulesManager.pathfindingDepth;
            
            // Apply weights
            distanceWeight = rulesManager.distanceWeight;
            speedWeight = rulesManager.speedWeight;
            terrainWeight = rulesManager.terrainWeight;
            directionWeight = rulesManager.directionWeight;
            pathWeight = rulesManager.pathWeight;
            returnToAsphaltWeight = rulesManager.returnToAsphaltWeight;
            centerTrackWeight = rulesManager.centerTrackWeight;
            trackExitPenaltyWeight = rulesManager.trackExitPenaltyWeight;
            
            // Apply pruning settings
            enableEarlyPathPruning = rulesManager.enablePathPruning;
            offTrackToleranceCount = rulesManager.offTrackToleranceCount;
            minTerrainQualityThreshold = rulesManager.minTerrainQualityThreshold;
        }
    }

    /// <summary>
    /// Check if it's this AI's turn to move
    /// </summary>
    private bool IsMyTurn()
    {
        if (gameManager == null)
            return false;
            
        // In TimeTrial or Challenges mode, it's always our turn
        GameMode currentGameMode = GameInitializationManager.SelectedGameMode;
        if (currentGameMode == GameMode.TimeTrial || currentGameMode == GameMode.Challenges)
            return true;
            
        // Check if it's actually our turn in other modes
        bool isPlayer1 = gameObject.CompareTag("Player1");
        
        // In PvE mode, Player1 is human, Player2 is AI
        if (currentGameMode == GameMode.PvE)
        {
            // If we're Player1 (human) in PvE, we shouldn't act as AI
            if (isPlayer1)
                return false;
                
            // If we're Player2 (AI) in PvE, we should only act when it's not Player1's turn
            return !gameManager.IsPlayer1Turn;
        }
        
        // In Race mode, we check whose turn it is
        return (isPlayer1 && gameManager.IsPlayer1Turn) || (!isPlayer1 && !gameManager.IsPlayer1Turn);
    }

    /// <summary>
    /// Main entry point - called by GameManager to start AI's turn
    /// </summary>
    public void StartTurn()
    {
        // Check if we're already processing a turn
        if (isProcessingTurn)
        {
            Debug.Log($"AI skipping turn for {gameObject.name}: Already processing");
            return;
        }
        
        // Check if it's actually our turn to move
        if (!IsMyTurn())
        {
            Debug.Log($"AI skipping turn for {gameObject.name}: Not my turn");
            return;
        }
        
        Debug.Log($"Unified Vector AI starting turn for {gameObject.name}");
        isProcessingTurn = true;
        
        // Reset visualization state
        isPathVisualizationValid = false;
        visualizedPath = null;
        bestPath = null;  // Important! Reset best path at turn start
        
        // Find the next target checkpoint
        UpdateTargetCheckpoint();
        
        // Update terrain analyzer and path evaluator with new target
        terrainAnalyzer.SetTargetPosition(targetPosition);
        pathEvaluator.SetTarget(targetPosition, targetCheckpoint);
        
        // Inform the path evaluator of the current terrain quality
        // ENHANCED GRAVEL HANDLING: Pass current position for gravel detection
        pathEvaluator.SetCurrentPositionTerrainQuality(transform.position);
        
        // NEW: Tell path evaluator if we're targeting finish line
        pathEvaluator.SetTargetingFinishLine(allCheckpointsReached);
        
        // Reset caches
        terrainAnalyzer.ClearCaches();
        
        // Set the state to ReadyToThink
        thinkingState = AIThinkingState.ReadyToThink;
        Debug.Log("AI is ready to think. Press Space to start thinking.");
        
        if (manualStepMode)
        {
            // In manual mode, we wait for spacebar input
            Debug.Log("Manual step mode active - press Space to start AI thinking.");
        }
        else
        {
            // In auto mode, process turn immediately
            StartCoroutine(ProcessTurn());
        }
    }

    /// <summary>
    /// Process a single step of the AI's thinking or execution based on current state
    /// </summary>
    public void ProcessStep()
    {
        // Check if it's actually our turn
        if (!IsMyTurn())
        {
            Debug.LogWarning($"AI cannot process step: Not my turn");
            return;
        }
        
        switch (thinkingState)
        {
            case AIThinkingState.ReadyToThink:
                // Start thinking process
                Debug.Log("Starting AI thinking process - generating paths");
                StartCoroutine(ProcessTurn());
                break;
                
            case AIThinkingState.ReadyToExecute:
                // Execute the best move
                Debug.Log("Executing best move");
                ExecuteBestMove();
                break;
                
            default:
                Debug.Log($"AI is in state {thinkingState} - waiting for the current operation to complete.");
                break;
        }
    }

    /// <summary>
    /// Process the AI's turn using the tree-based pathfinding approach
    /// </summary>
    private IEnumerator ProcessTurn()
    {
        // Double-check it's actually our turn
        if (!IsMyTurn())
        {
            Debug.LogWarning($"AI aborting turn for {gameObject.name}: Not my turn");
            isProcessingTurn = false;
            thinkingState = AIThinkingState.Idle;
            yield break;
        }
        
        Debug.Log("Beginning vector pathfinding calculation...");
        
        // Get current position and velocity
        Vector2Int currentPosition = playerMovement.CurrentPosition;
        Vector2Int currentVelocity = playerMovement.CurrentVelocity;
        Vector2 currentWorldPos = transform.position;
        
        // ENHANCED GRAVEL HANDLING: Check if we're on gravel and update path evaluator
        // Check current terrain
        if (groundDetector != null)
        {
            bool isOnGravel = groundDetector.IsOnGravel;
            Debug.Log($"Ground detector reports car is on gravel: {isOnGravel}");
        }
        
        // Make sure path evaluator knows current position terrain quality
        pathEvaluator.SetCurrentPositionTerrainQuality(currentWorldPos);
        
        // Start the timer for performance measurement
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        
        // Generate all possible paths up to the configured depth
        thinkingState = AIThinkingState.Thinking;
        
        // Reset visualized path
        visualizedPath = null;
        bestPath = null; // Also reset best path before generating new paths
        isPathVisualizationValid = false;
        
        // Generate paths using the path generator
        yield return StartCoroutine(pathGenerator.GenerateAllPathsChunked(
            currentPosition, 
            currentVelocity, 
            currentWorldPos,
            allCheckpointsReached // NEW: Pass whether all checkpoints are reached
        ));
        
        // Stop the timer
        stopwatch.Stop();
        float timeTaken = stopwatch.ElapsedMilliseconds / 1000f;
        
        // Get the generated paths
        List<Path> generatedPaths = pathGenerator.GetGeneratedPaths();
        if (generatedPaths.Count == 0)
        {
            Debug.LogError("Critical error: No paths were generated!");
            
            // Handle gracefully by entering execution state anyway
            thinkingState = AIThinkingState.ReadyToExecute;
            // Ensure best path is null to trigger fallback mechanism
            bestPath = null;
            visualizedPath = null;
            isPathVisualizationValid = false;
            
            if (!manualStepMode)
            {
                yield return new WaitForSeconds(0.2f);
                ExecuteBestMove();
            }
            
            yield break;
        }
        
        // Select the best path based on evaluation
        bestPath = pathEvaluator.SelectBestPath(generatedPaths, pathGenerator.enableAggressivePruning);
        thinkingState = AIThinkingState.ThinkingComplete;
        
        // Update visualization state - critical for ensuring we execute what we visualize
        visualizedPath = bestPath;  // Store the visualized path
        isPathVisualizationValid = true;
        
        // Get pruning statistics
        int[] pruningStats = pathGenerator.GetPruningStats();
        
        // Log detailed results
        Debug.Log($"Pathfinding complete: Generated {pathGenerator.GetTotalPathsGenerated()} paths, " +
                 $"pruned {pathGenerator.GetPathsPruned()} " +
                 $"({pruningStats[0]} terrain, {pruningStats[1]} score, {pruningStats[2]} lookahead, " +
                 $"{pruningStats[3]} inefficient, {pruningStats[4]} speed), " +
                 $"evaluated {generatedPaths.Count} in {timeTaken:F3} seconds. " +
                 $"Targeting: {(allCheckpointsReached ? "FINISH LINE" : "CHECKPOINT")}");
        
        if (bestPath != null)
        {
            Debug.Log($"Best path quality: {bestPath.Quality}, score: {bestPath.AverageScore:F2}, " +
                     $"nodes: {bestPath.Nodes.Count}, terrain: {bestPath.AverageTerrainQuality:F2}");
            
            if (bestPath.Nodes.Count > 0)
            {
                PathNode firstMove = bestPath.Nodes[0];
                Debug.Log($"First move position: {firstMove.Position}, score: {firstMove.Score:F2}");
            }
        }
        else
        {
            Debug.LogError("No best path was selected!");
        }
        
        // In manual mode, wait for next step input
        if (manualStepMode)
        {
            Debug.Log("AI thinking complete. Press Space to execute the move.");
            thinkingState = AIThinkingState.ReadyToExecute;
            yield break;
        }
        
        // Auto mode - execute move after a short delay
        yield return new WaitForSeconds(0.2f);
        
        // Execute the move
        ExecuteBestMove();
    }

    /// <summary>
    /// Execute the best move from the selected path
    /// </summary>
    private void ExecuteBestMove()
    {
        // Final check that it's still our turn
        if (!IsMyTurn())
        {
            Debug.LogWarning($"AI cannot execute move: Not my turn");
            isProcessingTurn = false;
            thinkingState = AIThinkingState.Idle;
            return;
        }
        
        // Change state to executing before we pass the path to the executor
        thinkingState = AIThinkingState.Executing;
        
        // Check if the visualized path is still valid and use it for consistency
        if (isPathVisualizationValid && visualizedPath != null)
        {
            // Use the visualized path to ensure consistency with what the player sees
            bestPath = visualizedPath;
        }
        else if (!isPathVisualizationValid || bestPath == null)
        {
            Debug.LogWarning("Best path is not valid or is null! This may cause AI to move differently than visualized.");
            // We'll continue anyway and let the path executor handle it
        }
        
        // Log the final decision
        if (bestPath != null && bestPath.Nodes.Count > 0)
        {
            PathNode firstMove = bestPath.Nodes[0];
            Debug.Log($"FINAL DECISION: AI will move to {firstMove.Position} with score {firstMove.Score:F2}");
        }
        else
        {
            Debug.LogWarning("FINAL DECISION: No valid path available, will use fallback mechanism");
        }
        
        // Execute the chosen path using the path executor
        pathExecutor.ExecuteBestMove(bestPath);
        
        // Reset visualization
        isPathVisualizationValid = false;
        visualizedPath = null;
        
        // End turn processing
        isProcessingTurn = false;
        thinkingState = AIThinkingState.Idle;
    }

    /// <summary>
    /// Identify the target checkpoint to aim for
    /// </summary>
    private void UpdateTargetCheckpoint()
    {
        // Exit if no checkpoint manager exists
        if (checkpointManager == null) return;
        
        // Get the next checkpoint in sequence for this AI
        bool isPlayer1 = gameObject.CompareTag("Player1");
        targetCheckpoint = checkpointManager.GetNextCheckpointInOrder(isPlayer1);
        
        // NEW: Reset allCheckpointsReached
        allCheckpointsReached = false;
        
        // If no checkpoint returned, target the finish line
        if (targetCheckpoint == null)
        {
            // Set flag that we're targeting finish line
            allCheckpointsReached = true;
            
            // Try to find the finish line (first by name, then by tag as fallback)
            GameObject finishLine = GameObject.Find("StartFinishLine");
            if (finishLine == null)
            {
                // Try by tag as fallback
                finishLine = GameObject.FindGameObjectWithTag("FinishLine");
            }
            
            if (finishLine != null)
            {
                targetPosition = finishLine.transform.position;
                
                if (debugMode)
                {
                    Debug.Log($"All checkpoints complete. Targeting finish line at {targetPosition}");
                }
                
                // Check if we should blend with the last checkpoint for a better racing line
                Checkpoint[] allCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
                if (allCheckpoints != null && allCheckpoints.Length > 0)
                {
                    // Find checkpoint with highest number (the last one)
                    Checkpoint lastCheckpoint = null;
                    int highestNumber = -1;
                    
                    foreach (var cp in allCheckpoints)
                    {
                        if (cp.checkpointNumber > highestNumber && 
                            cp.IsActivatedForPlayer(isPlayer1))
                        {
                            highestNumber = cp.checkpointNumber;
                            lastCheckpoint = cp;
                        }
                    }
                    
                    // If we found the last checkpoint, check distance for blending
                    if (lastCheckpoint != null)
                    {
                        Vector2 lastCheckpointPos = lastCheckpoint.transform.position;
                        float distanceToLastCheckpoint = Vector2.Distance(transform.position, lastCheckpointPos);
                        
                        // If we're in blending range
                        if (distanceToLastCheckpoint < 5.0f)
                        {
                            // Create a blended target position for smoother approach to finish
                            float blendFactor = Mathf.Clamp01(1.0f - (distanceToLastCheckpoint / 5.0f)) * 0.5f;
                            targetPosition = Vector2.Lerp(targetPosition, lastCheckpointPos, blendFactor);
                            
                            if (debugMode)
                            {
                                Debug.Log($"Blending finish line with last checkpoint. Blend factor: {blendFactor:F2}");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("UnifiedVectorAI: Could not find any target (checkpoint or finish line)!");
                // Fallback target position - straight ahead
                targetPosition = new Vector2(
                    playerMovement.CurrentPosition.x + playerMovement.CurrentVelocity.x * 2,
                    playerMovement.CurrentPosition.y + playerMovement.CurrentVelocity.y * 2
                );
            }
        }
        else
        {
            targetPosition = targetCheckpoint.transform.position;
            
            // Check if we're close to the current checkpoint and should consider the next one
            float distanceToTarget = Vector2.Distance(transform.position, targetPosition);
            if (distanceToTarget < 3.0f)
            {
                // Look ahead to next checkpoint to influence our path
                Checkpoint nextCheckpoint = LookAheadToNextCheckpoint();
                if (nextCheckpoint != null)
                {
                    // Create a blended target position that considers both checkpoints
                    Vector2 nextPosition = nextCheckpoint.transform.position;
                    float blendFactor = Mathf.Clamp01(1.0f - (distanceToTarget / 3.0f));
                    
                    // Adjust target position to be a weighted average (racing line)
                    targetPosition = Vector2.Lerp(targetPosition, nextPosition, blendFactor * 0.6f);
                    
                    if (debugMode)
                    {
                        Debug.Log($"Blending checkpoint target with next checkpoint. Blend factor: {blendFactor:F2}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get the next checkpoint after the current target
    /// </summary>
    private Checkpoint LookAheadToNextCheckpoint()
    {
        if (checkpointManager == null || targetCheckpoint == null) 
            return null;
            
        // Get all checkpoints
        var allCheckpoints = checkpointManager.GetAllCheckpoints();
        if (allCheckpoints == null || allCheckpoints.Count == 0)
            return null;
            
        // Find current checkpoint's index
        int currentIndex = allCheckpoints.IndexOf(targetCheckpoint);
        if (currentIndex < 0)
            return null;
            
        // Get next checkpoint, or return null if this is the last one
        if (currentIndex < allCheckpoints.Count - 1)
            return allCheckpoints[currentIndex + 1];
            
        return null;
    }

    /// <summary>
    /// Draw debug gizmos for path visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showPathVisualization)
            return;
            
        // Draw line to target checkpoint
        if (targetCheckpoint != null)
        {
            Gizmos.color = new Color(1f, 0.7f, 0f, 0.8f); // Orange-yellow
            Vector3 from = transform.position;
            Vector3 to = targetCheckpoint.transform.position;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawSphere(to, 0.2f);
        }
        else if (allCheckpointsReached) // NEW: Draw line to finish when targeting it
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f); // Green-cyan for finish line
            Vector3 from = transform.position;
            Vector3 to = new Vector3(targetPosition.x, targetPosition.y, 0);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawSphere(to, 0.3f); // Larger sphere for finish
        }
            
        // Only draw paths if thinking is complete or executing
        if (thinkingState != AIThinkingState.ThinkingComplete && 
            thinkingState != AIThinkingState.ReadyToExecute)
            return;
        
        // Check if visualization is valid
        if (isPathVisualizationValid && visualizedPath != null && visualizedPath.Nodes.Count > 0)
        {
            // Draw the visualized path
            DrawPath(visualizedPath, bestPathColor, 0.07f);
            
            // Draw the first node with a clearer visual to show the next move
            if (visualizedPath.Nodes.Count > 0)
            {
                PathNode firstNode = visualizedPath.Nodes[0];
                Vector3 firstNodePos = new Vector3(
                    firstNode.Position.x / (float)PlayerMovement.GRID_SCALE,
                    firstNode.Position.y / (float)PlayerMovement.GRID_SCALE,
                    0
                );
                
                // Draw a larger sphere for the first move
                Gizmos.color = new Color(0f, 1f, 0f, 1f); // Bright green
                Gizmos.DrawSphere(firstNodePos, 0.15f);
                
                // Draw a line from current position to first move
                Gizmos.DrawLine(transform.position, firstNodePos);
            }
        }
        else if (bestPath != null && bestPath.Nodes.Count > 0)
        {
            // Draw the best path
            DrawPath(bestPath, bestPathColor, 0.07f);
        }
    }

    /// <summary>
    /// Draw a path using Gizmos
    /// </summary>
    private void DrawPath(Path path, Color color, float lineWidth)
    {
        if (path == null || path.Nodes.Count < 1)
            return;
            
        Gizmos.color = color;
        
        // Start position
        Vector3 startPos = transform.position;
        
        // Draw from current position to first node
        Vector3 firstNodePos = new Vector3(
            path.Nodes[0].Position.x / (float)PlayerMovement.GRID_SCALE,
            path.Nodes[0].Position.y / (float)PlayerMovement.GRID_SCALE,
            0
        );
        
        Gizmos.DrawLine(startPos, firstNodePos);
        Gizmos.DrawSphere(firstNodePos, lineWidth * 2f);
        
        // Draw connections between nodes
        for (int i = 0; i < path.Nodes.Count - 1; i++)
        {
            Vector3 from = new Vector3(
                path.Nodes[i].Position.x / (float)PlayerMovement.GRID_SCALE,
                path.Nodes[i].Position.y / (float)PlayerMovement.GRID_SCALE,
                0
            );
            
            Vector3 to = new Vector3(
                path.Nodes[i+1].Position.x / (float)PlayerMovement.GRID_SCALE,
                path.Nodes[i+1].Position.y / (float)PlayerMovement.GRID_SCALE,
                0
            );
            
            Gizmos.DrawLine(from, to);
            Gizmos.DrawSphere(to, lineWidth * 2f);
        }
    }
}