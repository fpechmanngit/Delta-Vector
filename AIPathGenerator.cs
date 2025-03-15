using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace DeltaVector.AI
{
    /// <summary>
    /// Handles path generation for AI pathfinding decisions
    /// </summary>
    public class AIPathGenerator : MonoBehaviour
    {
        // Path generation settings
        [Header("Path Generation Settings")]
        [Tooltip("Depth of the search tree (how many moves to look ahead)")]
        [Range(1, 15)]
        public int pathfindingDepth = 3;
        
        [Header("Path Pruning Settings")]
        [Tooltip("Whether to use early pruning to discard obviously bad paths")]
        public bool enableEarlyPathPruning = true;
        
        [Tooltip("How many consecutive off-track positions before pruning a path")]
        [Range(1, 3)]
        public int offTrackToleranceCount = 1;
        
        [Tooltip("Minimum terrain quality (0-1) required to continue path (lower = stricter pruning)")]
        [Range(0.01f, 0.3f)]
        public float minTerrainQualityThreshold = 0.1f;
        
        [Header("Enhanced Pruning Settings")]
        [Tooltip("Enable more aggressive pruning for significantly better performance")]
        public bool enableAggressivePruning = true;
        
        [Tooltip("Score threshold below which paths are pruned early (higher = more aggressive)")]
        [Range(0.1f, 0.7f)]
        public float scorePruningThreshold = 0.3f;
        
        [Tooltip("Increase pruning aggressiveness with depth (0 = consistent, 1 = very aggressive at max depth)")]
        [Range(0f, 1f)]
        public float depthPruningFactor = 0.5f;
        
        [Tooltip("Look ahead pruning to detect imminent track exits")]
        public bool enableLookAheadPruning = true;
        
        [Tooltip("How far ahead to look for path problems (higher = more aggressive)")]
        [Range(1, 3)]
        public int lookAheadDistance = 2;
        
        [Tooltip("Discourage zig-zag or inefficient movements")]
        public bool pruneIneffcientMovements = true;
        
        [Tooltip("Prune paths with excessive speed at turns")]
        public bool pruneExcessiveSpeedAtTurns = true;
        
        [Header("Chunked Processing Settings")]
        [Tooltip("Enable chunked processing to prevent framerate drops")]
        public bool enableChunkedProcessing = true;
        
        [Tooltip("Maximum paths to process per frame")]
        [Range(10, 2000)]
        public int maxPathsPerChunk = 200;
        
        [Tooltip("Time to wait between processing chunks (milliseconds)")]
        [Range(1, 50)]
        public int chunkProcessingDelay = 5;
        
        [Header("Debug Settings")]
        [Tooltip("Enable verbose debugging output")]
        public bool debugVerbose = false;
        
        // Internal path generation state
        private List<Path> generatedPaths = new List<Path>();
        private PathNode rootNode;
        private int totalPathsGenerated = 0;
        private int pathsPruned = 0;
        
        // Chunked processing state
        private bool isChunkProcessingActive = false;
        private float processingProgress = 0f;
        private Queue<PathGenerationTask> pathGenerationQueue = new Queue<PathGenerationTask>();
        
        // Pruning statistics
        private int prunedByTerrain = 0;
        private int prunedByScore = 0;
        private int prunedByLookAhead = 0;
        private int prunedByInefficiency = 0;
        private int prunedBySpeed = 0;
        
        // Required components
        private AIPathEvaluator pathEvaluator;
        private AITerrainAnalyzer terrainAnalyzer;
        private MoveIndicatorManager moveIndicatorManager;
        
        // Flags for special behavior
        private bool targetingFinishLine = false;    // Flag to adjust behavior when targeting finish line
        private bool adaptivePruningEnabled = true;  // Flag to dynamically adjust pruning
        private bool emergencyPathfinding = false;   // Flag for when no paths are found
        private bool gravelInevitable = false;       // Flag for when gravel cannot be avoided
        
        // Critical fix: original pruning parameters to restore after emergency
        private bool originalEarlyPathPruning;
        private bool originalAggressivePruning;
        private float originalScorePruningThreshold;
        private int originalOffTrackToleranceCount;
        
        // NEW: Minimum number of paths to keep at top level for variety
        private const int MIN_PATHS_TO_KEEP = 3;
        
        // Critical fix: counter to ensure we keep at least some paths
        private int forcedKeepPathCounter = 0;
        
        /// <summary>
        /// Initialize with required components
        /// </summary>
        public void Initialize(AIPathEvaluator evaluator, AITerrainAnalyzer analyzer, MoveIndicatorManager moveManager)
        {
            pathEvaluator = evaluator;
            terrainAnalyzer = analyzer;
            moveIndicatorManager = moveManager;
        }

        /// <summary>
        /// Update settings from AIRulesManager
        /// </summary>
        public void UpdateSettings(AIRulesManager rulesManager)
        {
            if (rulesManager == null) return;
            
            pathfindingDepth = rulesManager.pathfindingDepth;
            
            // Basic pruning settings
            enableEarlyPathPruning = rulesManager.enablePathPruning;
            offTrackToleranceCount = rulesManager.offTrackToleranceCount;
            minTerrainQualityThreshold = rulesManager.minTerrainQualityThreshold;
            
            // Enhanced pruning settings
            enableAggressivePruning = rulesManager.enableAggressivePruning;
            scorePruningThreshold = rulesManager.scorePruningThreshold;
            depthPruningFactor = rulesManager.depthPruningFactor;
            enableLookAheadPruning = rulesManager.enableLookAheadPruning;
            lookAheadDistance = rulesManager.lookAheadDistance;
            pruneIneffcientMovements = rulesManager.pruneIneffcientMovements;
            pruneExcessiveSpeedAtTurns = rulesManager.pruneExcessiveSpeedAtTurns;
            
            // Chunked processing settings
            enableChunkedProcessing = rulesManager.enableChunkedProcessing;
            maxPathsPerChunk = rulesManager.maxPathsPerFrame;
            chunkProcessingDelay = Mathf.RoundToInt(rulesManager.targetThinkingTime / 3);
        }

        /// <summary>
        /// Generate all possible paths using chunked processing
        /// </summary>
        public IEnumerator GenerateAllPathsChunked(
            Vector2Int currentPosition, 
            Vector2Int currentVelocity, 
            Vector2 currentWorldPos,
            bool isTargetingFinishLine = false)
        {
            if (isChunkProcessingActive)
            {
                Debug.LogWarning("Chunked processing already active!");
                yield break;
            }
            
            isChunkProcessingActive = true;
            
            // Set targeting finish line flag
            targetingFinishLine = isTargetingFinishLine;
            
            // Reset state
            generatedPaths.Clear();
            totalPathsGenerated = 0;
            pathsPruned = 0;
            prunedByTerrain = 0;
            prunedByScore = 0;
            prunedByLookAhead = 0;
            prunedByInefficiency = 0;
            prunedBySpeed = 0;
            processingProgress = 0f;
            forcedKeepPathCounter = 0;
            
            // Store original pruning parameters
            originalEarlyPathPruning = enableEarlyPathPruning;
            originalAggressivePruning = enableAggressivePruning;
            originalScorePruningThreshold = scorePruningThreshold;
            originalOffTrackToleranceCount = offTrackToleranceCount;
            
            // Reset all special behavior flags
            emergencyPathfinding = false;
            gravelInevitable = false;
            adaptivePruningEnabled = true;
            
            // MODIFIED: Make pruning less aggressive for more variety in paths
            if (enableAggressivePruning)
            {
                // Slightly reduce pruning threshold to keep more viable paths
                scorePruningThreshold *= 0.9f;
            }
            
            // Log initial pruning state
            Debug.Log($"<color=yellow>PRUNING STATE: enabled={enableEarlyPathPruning}, aggressive={enableAggressivePruning}, emergency={emergencyPathfinding}</color>");
            
            // Calculate the grid-scaled position for move calculations
            Vector2Int currentPrecisePos = new Vector2Int(
                currentPosition.x * PlayerMovement.GRID_SCALE,
                currentPosition.y * PlayerMovement.GRID_SCALE
            );
            
            // Create root node (current state)
            rootNode = new PathNode(currentPrecisePos, currentVelocity);
            
            // If we don't have move indicator manager, we can't proceed
            if (moveIndicatorManager == null)
            {
                Debug.LogError("Cannot generate paths: MoveIndicatorManager is null");
                isChunkProcessingActive = false;
                yield break;
            }
            
            // Generate the move indicators for the current position
            moveIndicatorManager.ShowPossibleMoves(
                currentPosition, 
                currentVelocity, 
                CalculateMaxMoveDistance(currentVelocity)
            );
            
            // Get all valid moves from the move indicator manager
            List<Vector2Int> validInitialMoves = moveIndicatorManager.GetValidMovePositions();
            
            // Enhanced handling for no valid initial moves
            if (validInitialMoves.Count == 0)
            {
                Debug.LogWarning("No valid initial moves from move indicator manager! Generating emergency moves...");
                
                // Manually generate all 9 possible moves around the base position
                Vector2Int basePosition = new Vector2Int(
                    currentPosition.x + currentVelocity.x,
                    currentPosition.y + currentVelocity.y
                );
                
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        // Calculate adjusted position
                        Vector2Int adjustedPosition = new Vector2Int(
                            basePosition.x + dx,
                            basePosition.y + dy
                        );
                        
                        // Convert to precise grid coordinates
                        Vector2Int precisePosition = new Vector2Int(
                            adjustedPosition.x * PlayerMovement.GRID_SCALE,
                            adjustedPosition.y * PlayerMovement.GRID_SCALE
                        );
                        
                        validInitialMoves.Add(precisePosition);
                    }
                }
                
                Debug.Log($"Generated {validInitialMoves.Count} emergency initial moves");
            }
            
            // Double-check that we have initial moves
            if (validInitialMoves.Count == 0)
            {
                Debug.LogError("CRITICAL ERROR: Failed to generate any valid initial moves!");
                isChunkProcessingActive = false;
                yield break;
            }
            
            // Check current terrain quality to detect if we're already on gravel
            float currentTerrainQuality = terrainAnalyzer.EvaluateTerrain(currentWorldPos);
            bool startingOnGravel = currentTerrainQuality < 0.5f;
            
            Debug.Log($"<color=cyan>Current position terrain quality: {currentTerrainQuality}, On gravel: {startingOnGravel}</color>");
            
            if (startingOnGravel)
            {
                // We're on gravel already - use emergency recovery mode with reduced pruning
                Debug.LogWarning($"<color=orange>Currently on poor terrain (quality: {currentTerrainQuality}). Enabling emergency recovery mode.</color>");
                emergencyPathfinding = true;
                
                // Reduce pruning thresholds for emergency recovery
                offTrackToleranceCount += 1; // Allow more off-track steps
                minTerrainQualityThreshold *= 0.5f; // Halve the terrain quality threshold
                
                // GRAVEL FIX: Force speed=1 for current velocity if starting on gravel
                if (currentVelocity.magnitude > PlayerMovement.GRID_SCALE)
                {
                    // Fix: Convert Vector2Int to Vector2 before accessing normalized
                    Vector2 velAsVector2 = new Vector2(currentVelocity.x, currentVelocity.y);
                    Vector2 normalizedVel = velAsVector2.normalized;
                    
                    currentVelocity = new Vector2Int(
                        Mathf.RoundToInt(normalizedVel.x * PlayerMovement.GRID_SCALE),
                        Mathf.RoundToInt(normalizedVel.y * PlayerMovement.GRID_SCALE)
                    );
                    Debug.Log($"<color=red>GRAVEL FIX: Force reduced starting velocity to {currentVelocity} (speed=1)</color>");
                }
            }
            else
            {
                // If not on gravel, make sure emergency pathfinding is OFF
                emergencyPathfinding = false;
            }
            
            // NEW: Evaluate and pre-score initial moves to ensure best options are kept
            Dictionary<Vector2Int, float> initialMoveScores = new Dictionary<Vector2Int, float>();
            List<Vector2Int> sortedInitialMoves = new List<Vector2Int>();
            
            foreach (Vector2Int movePos in validInitialMoves)
            {
                // Calculate potential new velocity
                Vector2Int newVelocity = movePos - currentPrecisePos;
                
                // GRAVEL FIX: Adjust velocity for destination terrain
                Vector2 moveWorldPos = GridToWorldPosition(movePos);
                float terrainQuality = terrainAnalyzer.EvaluateTerrain(moveWorldPos);
                bool isOnGravel = terrainQuality < 0.5f;
                
                // Apply gravel speed limit if target position is on gravel or we're starting on gravel
                if (isOnGravel || startingOnGravel)
                {
                    // Fix: Convert Vector2Int to Vector2 before accessing normalized
                    Vector2 velAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                    Vector2 normalizedDir = velAsVector2.normalized;
                    
                    newVelocity = new Vector2Int(
                        Mathf.RoundToInt(normalizedDir.x * PlayerMovement.GRID_SCALE),
                        Mathf.RoundToInt(normalizedDir.y * PlayerMovement.GRID_SCALE)
                    );
                    
                    if (debugVerbose)
                    {
                        Debug.Log($"<color=red>GRAVEL FIX: Adjusted initial velocity for move to {movePos} to {newVelocity} (speed=1)</color>");
                    }
                }
                
                // Create a temporary node for this move
                PathNode tempNode = new PathNode(movePos, newVelocity);
                
                // Evaluate this node
                pathEvaluator.EvaluatePathNode(tempNode, 0, currentWorldPos);
                
                // Store score
                initialMoveScores[movePos] = tempNode.Score;
                sortedInitialMoves.Add(movePos);
            }
            
            // Sort initial moves by score in descending order
            sortedInitialMoves.Sort((a, b) => initialMoveScores[b].CompareTo(initialMoveScores[a]));
            
            // Check if gravel is inevitable by examining all initial moves
            bool allPathsLeadToGravel = true;
            int goodTerrainPaths = 0;
            
            foreach (Vector2Int movePos in validInitialMoves)
            {
                Vector2 moveWorldPos = GridToWorldPosition(movePos);
                float terrainQuality = terrainAnalyzer.EvaluateTerrain(moveWorldPos);
                
                if (terrainQuality >= 0.5f) // Good terrain (asphalt)
                {
                    allPathsLeadToGravel = false;
                    goodTerrainPaths++;
                }
            }
            
            Debug.Log($"<color=cyan>Gravel check: {goodTerrainPaths} good terrain paths out of {validInitialMoves.Count}</color>");
            
            // Only enable emergency mode if ALL paths lead to gravel or we're already on gravel
            if (allPathsLeadToGravel)
            {
                Debug.LogWarning($"<color=orange>EMERGENCY: All paths lead to gravel. Disabling pruning.</color>");
                gravelInevitable = true;
                emergencyPathfinding = true;
            }
            // If we have good terrain paths, make sure pruning is ON
            else if (goodTerrainPaths > 0 && currentTerrainQuality >= 0.5f) 
            {
                Debug.Log($"<color=green>Normal terrain conditions detected: {goodTerrainPaths} good terrain paths available. Pruning is ENABLED.</color>");
                emergencyPathfinding = false;
            }
            
            // NEW: Use sorted moves to prioritize best moves for processing
            foreach (Vector2Int initialMovePos in sortedInitialMoves)
            {
                // Calculate new velocity after this move
                Vector2Int newVelocity = initialMovePos - currentPrecisePos;
                
                // GRAVEL FIX: Adjust velocity for destination terrain
                Vector2 moveWorldPos = GridToWorldPosition(initialMovePos);
                float terrainQuality = terrainAnalyzer.EvaluateTerrain(moveWorldPos);
                bool isOnGravel = terrainQuality < 0.5f;
                
                // Apply gravel speed limit if target position is on gravel or we're starting on gravel
                if (isOnGravel || startingOnGravel)
                {
                    // Fix: Convert Vector2Int to Vector2 before accessing normalized
                    Vector2 velAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                    Vector2 normalizedDir = velAsVector2.normalized;
                    
                    newVelocity = new Vector2Int(
                        Mathf.RoundToInt(normalizedDir.x * PlayerMovement.GRID_SCALE),
                        Mathf.RoundToInt(normalizedDir.y * PlayerMovement.GRID_SCALE)
                    );
                    
                    Debug.Log($"<color=red>GRAVEL FIX: Enforced speed=1 for initial node at {initialMovePos}, new velocity: {newVelocity}</color>");
                }
                
                // Create a path node for this move
                PathNode initialNode = new PathNode(initialMovePos, newVelocity);
                
                // Evaluate this initial move
                pathEvaluator.EvaluatePathNode(initialNode, 0, currentWorldPos);
                
                // Add this node to root's children for visualization
                rootNode.Children.Add(initialNode);
                
                // Start a new path with this node
                Path initialPath = new Path();
                initialPath.Nodes.Add(initialNode);
                
                // Pre-calculate terrain quality for first node
                Vector2 initialWorldPos = GridToWorldPosition(initialNode.Position);
                initialNode.TerrainQuality = terrainAnalyzer.EvaluateTerrain(initialWorldPos);
                initialNode.OffTrackCount = initialNode.TerrainQuality < minTerrainQualityThreshold ? 1 : 0;
                
                // Add task to queue for chunked processing
                PathGenerationTask task = new PathGenerationTask(initialPath, initialNode, 1);
                pathGenerationQueue.Enqueue(task);
            }
            
            Debug.Log($"<color=yellow>Starting path generation with {pathGenerationQueue.Count} initial tasks. Pruning state: enabled={enableEarlyPathPruning}, emergency={emergencyPathfinding}</color>");
            
            // Start chunked processing
            int totalPathsToProcess = pathGenerationQueue.Count;
            yield return StartCoroutine(ProcessPathGenerationChunks(currentWorldPos));
            
            // CRITICAL FIX: Check if we've pruned all paths and need to regenerate some
            if (generatedPaths.Count == 0)
            {
                Debug.LogWarning("<color=red>All paths were pruned! Retrying with reduced pruning.</color>");
                
                // Reduce pruning aggressiveness but don't disable it completely
                bool wasEarlyPruningEnabled = enableEarlyPathPruning;
                bool wasAggressivePruningEnabled = enableAggressivePruning;
                float originalThreshold = scorePruningThreshold;
                
                // Reduce pruning but don't disable completely
                enableAggressivePruning = false;
                scorePruningThreshold *= 0.5f;
                offTrackToleranceCount += 1;
                
                Debug.Log($"<color=yellow>Reduced pruning: aggressive={enableAggressivePruning}, threshold={scorePruningThreshold}, tolerance={offTrackToleranceCount}</color>");
                
                // Clear queue and regenerate initial tasks
                pathGenerationQueue.Clear();
                
                // Re-add initial tasks with reduced pruning
                foreach (var initialNode in rootNode.Children)
                {
                    Path initialPath = new Path();
                    initialPath.Nodes.Add(initialNode);
                    
                    PathGenerationTask task = new PathGenerationTask(initialPath, initialNode, 1);
                    pathGenerationQueue.Enqueue(task);
                }
                
                // Process tasks again with reduced pruning
                Debug.Log("<color=yellow>Restarting pathfinding with reduced pruning settings</color>");
                yield return StartCoroutine(ProcessPathGenerationChunks(currentWorldPos));
                
                // If still no paths, now go to emergency mode
                if (generatedPaths.Count == 0)
                {
                    Debug.LogWarning("<color=red>Still no paths with reduced pruning! Switching to emergency mode.</color>");
                    
                    // Enter emergency pathfinding mode with minimal pruning
                    emergencyPathfinding = true;
                    enableEarlyPathPruning = false;
                    enableAggressivePruning = false;
                    
                    // Clear queue and regenerate initial tasks
                    pathGenerationQueue.Clear();
                    
                    // Re-add initial tasks with emergency settings
                    foreach (var initialNode in rootNode.Children)
                    {
                        Path initialPath = new Path();
                        initialPath.Nodes.Add(initialNode);
                        
                        PathGenerationTask task = new PathGenerationTask(initialPath, initialNode, 1);
                        pathGenerationQueue.Enqueue(task);
                    }
                    
                    // Process tasks again with emergency settings
                    Debug.Log("<color=red>Restarting pathfinding in emergency mode with " + pathGenerationQueue.Count + " initial tasks</color>");
                    yield return StartCoroutine(ProcessPathGenerationChunks(currentWorldPos));
                }
            }
            
            // NEW: If we have very few paths, ensure we have enough variety by processing top ones with reduced pruning
            if (generatedPaths.Count < MIN_PATHS_TO_KEEP && rootNode.Children.Count > 0)
            {
                int additionalPathsNeeded = MIN_PATHS_TO_KEEP - generatedPaths.Count;
                if (additionalPathsNeeded > 0)
                {
                    Debug.Log($"<color=yellow>Only {generatedPaths.Count} paths generated. Adding {additionalPathsNeeded} more for variety.</color>");
                    
                    // Temporarily disable pruning
                    bool originalPruning = enableEarlyPathPruning;
                    bool originalAggressive = enableAggressivePruning;
                    enableEarlyPathPruning = false;
                    enableAggressivePruning = false;
                    
                    int processedPaths = 0;
                    
                    // Process top moves with minimal pruning
                    foreach (var child in rootNode.Children)
                    {
                        if (processedPaths >= additionalPathsNeeded)
                            break;
                            
                        // Check if we already have this as a path
                        bool alreadyExists = false;
                        foreach (var existingPath in generatedPaths)
                        {
                            if (existingPath.Nodes.Count > 0 && 
                                Vector2Int.Distance(existingPath.Nodes[0].Position, child.Position) < 0.1f)
                            {
                                alreadyExists = true;
                                break;
                            }
                        }
                        
                        if (!alreadyExists)
                        {
                            // Create a special path for this move
                            Path additionalPath = new Path();
                            additionalPath.Nodes.Add(child);
                            
                            // Evaluate and add
                            pathEvaluator.EvaluatePath(additionalPath);
                            generatedPaths.Add(additionalPath);
                            processedPaths++;
                        }
                    }
                    
                    // Restore pruning settings
                    enableEarlyPathPruning = originalPruning;
                    enableAggressivePruning = originalAggressive;
                    
                    Debug.Log($"<color=green>Added {processedPaths} additional paths for variety. Now have {generatedPaths.Count}.</color>");
                }
            }
            
            // Double check: If still no paths, create at least one default path
            if (generatedPaths.Count == 0 && rootNode.Children.Count > 0)
            {
                Debug.LogError("CRITICAL: No paths generated even in emergency mode. Creating default path.");
                
                // Take the first child as a fallback
                PathNode firstChild = rootNode.Children[0];
                Path defaultPath = new Path();
                defaultPath.Nodes.Add(firstChild);
                pathEvaluator.EvaluatePath(defaultPath);
                generatedPaths.Add(defaultPath);
            }
            
            // Calculate percentage of pruned paths
            float pruningPercentage = totalPathsGenerated > 0 ? (float)pathsPruned / (pathsPruned + totalPathsGenerated) * 100 : 0;
            Debug.Log($"<color=yellow>Pruning effectiveness: {pruningPercentage:F2}% of potential paths were pruned</color>");
            
            // Don't clear indicators in manual mode - we want to see them for debugging
            if (!TestingModeManager.manualStepMode)
            {
                moveIndicatorManager.ClearIndicators();
            }
            
            // Restore original pruning settings
            enableEarlyPathPruning = originalEarlyPathPruning;
            enableAggressivePruning = originalAggressivePruning;
            scorePruningThreshold = originalScorePruningThreshold;
            offTrackToleranceCount = originalOffTrackToleranceCount;
            
            isChunkProcessingActive = false;
        }

        /// <summary>
        /// Process path generation in chunks to avoid framerate drops
        /// </summary>
        private IEnumerator ProcessPathGenerationChunks(Vector2 currentWorldPos)
        {
            // Track timing for each chunk
            System.Diagnostics.Stopwatch chunkStopwatch = new System.Diagnostics.Stopwatch();
            
            int chunkCounter = 0;
            int totalPathsTasks = pathGenerationQueue.Count;
            
            // Process tasks in chunks until queue is empty
            while (pathGenerationQueue.Count > 0)
            {
                chunkStopwatch.Reset();
                chunkStopwatch.Start();
                
                // Process a chunk of tasks
                int tasksInChunk = Mathf.Min(maxPathsPerChunk, pathGenerationQueue.Count);
                
                for (int i = 0; i < tasksInChunk; i++)
                {
                    if (pathGenerationQueue.Count == 0)
                        break;
                        
                    // Get next task from queue
                    PathGenerationTask task = pathGenerationQueue.Dequeue();
                    
                    // Process the task - this performs one level of path generation
                    ProcessPathGenerationTask(task, currentWorldPos);
                }
                
                // Update progress
                int completedTasks = totalPathsTasks - pathGenerationQueue.Count;
                processingProgress = Mathf.Clamp01((float)completedTasks / totalPathsTasks);
                
                chunkStopwatch.Stop();
                chunkCounter++;
                
                // Check if we should adapt pruning based on path generation results
                if (chunkCounter >= 5 && generatedPaths.Count < 10 && adaptivePruningEnabled && gravelInevitable)
                {
                    // We're not generating enough paths and gravel is inevitable - adapt pruning
                    adaptivePruningEnabled = false; // Prevent multiple adaptations
                    
                    Debug.Log("Few paths generated and gravel is inevitable. Adapting pruning.");
                    
                    // Reduce pruning thresholds based on current situation
                    bool reducedPruning = false;
                    
                    if (enableAggressivePruning)
                    {
                        // First, try reducing aggressive pruning
                        float originalThreshold = scorePruningThreshold;
                        scorePruningThreshold *= 0.5f; // Halve the threshold
                        reducedPruning = true;
                        Debug.Log($"Reduced score pruning threshold from {originalThreshold} to {scorePruningThreshold}");
                    }
                    
                    if (offTrackToleranceCount < 3)
                    {
                        // Increase tolerance for off-track positions
                        offTrackToleranceCount++;
                        reducedPruning = true;
                        Debug.Log($"Increased off-track tolerance to {offTrackToleranceCount}");
                    }
                    
                    // Only disable pruning entirely if we have very few paths and many were pruned
                    if (generatedPaths.Count < 5 && pathsPruned > 50 && gravelInevitable)
                    {
                        // Temporarily disable pruning
                        bool originalPruning = enableEarlyPathPruning;
                        enableEarlyPathPruning = false;
                        Debug.Log("EMERGENCY: Temporarily disabled pruning to ensure path generation through gravel");
                        
                        // Process a limited number of tasks with pruning disabled
                        for (int i = 0; i < Mathf.Min(30, pathGenerationQueue.Count); i++)
                        {
                            if (pathGenerationQueue.Count == 0)
                                break;
                            
                            ProcessPathGenerationTask(pathGenerationQueue.Dequeue(), currentWorldPos);
                        }
                        
                        // Restore original pruning setting
                        enableEarlyPathPruning = originalPruning;
                        
                        if (generatedPaths.Count > 0)
                        {
                            Debug.Log($"Generated {generatedPaths.Count} paths with pruning disabled");
                        }
                    }
                }
                
                // Low priority yield to prevent framerate drops
                yield return new WaitForSecondsRealtime(chunkProcessingDelay / 1000f);
            }
            
            // All chunks processed or stopped early
            processingProgress = 1.0f;
            
            // If we still have no paths after all processing, log an error
            if (generatedPaths.Count == 0)
            {
                Debug.LogError($"Failed to generate any paths after {chunkCounter} chunks. Pruned {pathsPruned} paths.");
            }
            else
            {
                Debug.Log($"Successfully generated {generatedPaths.Count} paths in {chunkCounter} chunks. Pruned {pathsPruned} paths.");
            }
        }

        /// <summary>
        /// Process a single path generation task
        /// </summary>
        private void ProcessPathGenerationTask(PathGenerationTask task, Vector2 currentWorldPos)
        {
            // If we've reached maximum depth, finish this path
            if (task.Depth >= pathfindingDepth)
            {
                // This path is complete, evaluate it and add to list
                pathEvaluator.EvaluatePath(task.CurrentPath);
                generatedPaths.Add(task.CurrentPath);
                totalPathsGenerated++;
                return;
            }
            
            // Check if we're on poor terrain (for recovery logic)
            Vector2 currentPosition = GridToWorldPosition(task.ParentNode.Position);
            float currentTerrainQuality = terrainAnalyzer.EvaluateTerrain(currentPosition);
            bool isOnPoorTerrain = currentTerrainQuality < 0.5f;
            
            // Generate the 9 possible moves from this position and velocity
            List<Vector2Int> nextMoves = GenerateMoveIndicatorsAt(task.ParentNode.Position, task.ParentNode.Velocity);
            
            // If no valid next moves, this path ends here
            if (nextMoves.Count == 0)
            {
                // Add this incomplete path to the list
                pathEvaluator.EvaluatePath(task.CurrentPath);
                generatedPaths.Add(task.CurrentPath);
                totalPathsGenerated++;
                return;
            }
            
            // Check if any of the next moves would lead onto gravel
            bool willHitGravel = false;
            bool hasAsphaltOptions = false;
            
            // Only perform this check if we're currently on good terrain
            if (!isOnPoorTerrain)
            {
                foreach (Vector2Int nextMove in nextMoves)
                {
                    Vector2 nextWorldPos = GridToWorldPosition(nextMove);
                    float terrainQuality = terrainAnalyzer.EvaluateTerrain(nextWorldPos);
                    
                    if (terrainQuality < 0.5f) // Less than 0.5 indicates gravel or worse
                    {
                        willHitGravel = true;
                    }
                    else
                    {
                        hasAsphaltOptions = true;
                    }
                }
                
                // Special handling when gravel is inevitable at this point in the path
                if (willHitGravel && !hasAsphaltOptions && !emergencyPathfinding && enableEarlyPathPruning)
                {
                    if (task.Depth <= 3)
                    {
                        Debug.Log($"<color=orange>Inevitable gravel detected at depth {task.Depth}. Special handling.</color>");
                    }
                    
                    // Process all next moves, but guarantee at least one path survives
                    // by selectively disabling pruning
                    for (int i = 0; i < nextMoves.Count; i++)
                    {
                        Vector2Int nextMovePos = nextMoves[i];
                        
                        // Process one path without pruning
                        if (i == 0)
                        {
                            // Save original pruning settings
                            bool originalEarlyPruning = enableEarlyPathPruning;
                            bool originalAggressivePruning = enableAggressivePruning;
                            
                            // Temporarily disable pruning for this one path
                            enableEarlyPathPruning = false;
                            enableAggressivePruning = false;
                            
                            // Process this path with pruning disabled
                            ProcessNextNode(task, nextMovePos, currentWorldPos);
                            
                            // Restore pruning settings
                            enableEarlyPathPruning = originalEarlyPruning;
                            enableAggressivePruning = originalAggressivePruning;
                        }
                        else
                        {
                            // Process other paths normally
                            ProcessNextNode(task, nextMovePos, currentWorldPos);
                        }
                    }
                    
                    // Skip regular processing
                    return;
                }
            }
            
            // Apply pruning if enabled - with special handling for recovery
            bool shouldPrune = enableEarlyPathPruning && !emergencyPathfinding;
            
            // CRITICAL FIX: Force keep one path in every major branch to prevent pruning everything
            if (shouldPrune)
            {
                // For each possible next move
                int keptPaths = 0;
                
                foreach (Vector2Int nextMovePos in nextMoves)
                {
                    // Calculate new velocity
                    Vector2Int newVelocity = nextMovePos - task.ParentNode.Position;
                    
                    // GRAVEL FIX: Check if this node would be on gravel
                    Vector2 worldPos = GridToWorldPosition(nextMovePos);
                    float terrainQuality = terrainAnalyzer.EvaluateTerrain(worldPos);
                    bool isOnGravel = terrainQuality < 0.5f;
                    
                    // If parent node was on gravel or this node will be on gravel,
                    // adjust velocity to simulate the speed limitation
                    if (isOnGravel || (task.ParentNode.TerrainQuality < 0.5f))
                    {
                        // Calculate direction of velocity
                        // Fix: Convert Vector2Int to Vector2 before accessing normalized
                        Vector2 velAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                        if (velAsVector2.magnitude > 0.01f)
                        {
                            Vector2 velDir = velAsVector2.normalized;
                            
                            // ALWAYS force magnitude to exactly 1 (in grid units) to simulate speed=1 on gravel
                            // Convert to grid scale
                            float gravelSpeed = 1.0f * PlayerMovement.GRID_SCALE;
                            
                            // Set velocity to exactly gravelSpeed in the same direction
                            newVelocity = new Vector2Int(
                                Mathf.RoundToInt(velDir.x * gravelSpeed),
                                Mathf.RoundToInt(velDir.y * gravelSpeed)
                            );
                            
                            if (debugVerbose)
                            {
                                Debug.Log($"<color=red>GRAVEL FIX: Enforced speed=1 for node at depth {task.Depth}, new velocity: {newVelocity}</color>");
                            }
                        }
                    }
                    
                    // Create node for this move
                    PathNode nextNode = new PathNode(nextMovePos, newVelocity);
                    
                    // Evaluate this node individually
                    pathEvaluator.EvaluatePathNode(nextNode, task.Depth, currentWorldPos);
                    
                    // Calculate terrain quality
                    nextNode.TerrainQuality = terrainQuality;
                    
                    // CRITICAL FIX: Check if we should prune this path
                    bool shouldPruneThisPath = ShouldPrunePath(task, nextNode, isOnPoorTerrain);
                    
                    // Force keep the best scoring node if we're at level 1 and everything would be pruned
                    if (task.Depth == 1 && keptPaths == 0 && shouldPruneThisPath)
                    {
                        // Check if this is the highest scoring node we've seen so far
                        if (nextNode.Score >= 0.3f) // Only keep reasonably good paths
                        {
                            shouldPruneThisPath = false;
                            keptPaths++;
                            
                            // CRITICAL FIX: Log that we're forcing a path to be kept
                            if (debugVerbose)
                            {
                                Debug.Log($"<color=green>FORCED KEEP: Path with score {nextNode.Score:F2} at depth {task.Depth}</color>");
                            }
                        }
                    }
                    
                    // Process or prune this path
                    if (shouldPruneThisPath)
                    {
                        // Count as pruned but don't process
                        pathsPruned++;
                    }
                    else
                    {
                        // Process this path normally
                        // Update direction changes tracking
                        if (task.Depth > 1)
                        {
                            // Fix: Convert Vector2Int to Vector2 before accessing normalized
                            Vector2 parentVelAsVector2 = new Vector2(task.ParentNode.Velocity.x, task.ParentNode.Velocity.y);
                            Vector2 newVelAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                            
                            Vector2 parentDir = parentVelAsVector2.normalized;
                            Vector2 nextDir = newVelAsVector2.normalized;
                            
                            float directionChange = Vector2.Dot(parentDir, nextDir);
                            
                            // If significant direction change detected
                            if (directionChange < 0.8f)
                            {
                                task.CurrentPath.DirectionChanges++;
                            }
                        }
                        
                        // Set off-track count either as 0 (on good terrain) or inherit + increment
                        if (nextNode.TerrainQuality < minTerrainQualityThreshold)
                        {
                            nextNode.OffTrackCount = task.ParentNode.OffTrackCount + 1;
                        }
                        else
                        {
                            nextNode.OffTrackCount = 0;
                        }
                        
                        // Look ahead for track exits
                        if (enableLookAheadPruning)
                        {
                            nextNode.TrackExitRisk = terrainAnalyzer.CalculateTrackExitRisk(worldPos, newVelocity, lookAheadDistance);
                        }
                        
                        // Add this node to parent's children for visualization
                        task.ParentNode.Children.Add(nextNode);
                        
                        // Create a new path by copying the current path and adding this move
                        Path newPath = new Path();
                        newPath.Nodes.AddRange(task.CurrentPath.Nodes);
                        newPath.Nodes.Add(nextNode);
                        
                        // Copy tracked values from current path
                        newPath.DirectionChanges = task.CurrentPath.DirectionChanges;
                        
                        // Create new task for the next depth level
                        PathGenerationTask newTask = new PathGenerationTask(newPath, nextNode, task.Depth + 1);
                        
                        // Add new task to queue
                        pathGenerationQueue.Enqueue(newTask);
                        keptPaths++;
                    }
                }
                
                // CRITICAL FIX: If we pruned all paths, force keep the best one
                if (keptPaths == 0 && nextMoves.Count > 0)
                {
                    // Find the best scoring node
                    PathNode bestNode = null;
                    float bestScore = -1f;
                    Vector2Int bestMovePos = Vector2Int.zero;
                    
                    foreach (Vector2Int nextMovePos in nextMoves)
                    {
                        // Calculate new velocity
                        Vector2Int newVelocity = nextMovePos - task.ParentNode.Position;
                        
                        // GRAVEL FIX: Check if this node would be on gravel
                        Vector2 worldPos = GridToWorldPosition(nextMovePos);
                        float terrainQuality = terrainAnalyzer.EvaluateTerrain(worldPos);
                        bool isOnGravel = terrainQuality < 0.5f;
                        
                        // Apply gravel speed limit if needed
                        if (isOnGravel || (task.ParentNode.TerrainQuality < 0.5f))
                        {
                            // Calculate direction of velocity
                            // Fix: Convert Vector2Int to Vector2 before accessing normalized
                            Vector2 velAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                            if (velAsVector2.magnitude > 0.01f)
                            {
                                Vector2 velDir = velAsVector2.normalized;
                                
                                // ALWAYS force magnitude to exactly 1 (in grid units) for gravel
                                float gravelSpeed = 1.0f * PlayerMovement.GRID_SCALE;
                                
                                // Set velocity to exactly gravelSpeed in the same direction
                                newVelocity = new Vector2Int(
                                    Mathf.RoundToInt(velDir.x * gravelSpeed),
                                    Mathf.RoundToInt(velDir.y * gravelSpeed)
                                );
                            }
                        }
                        
                        // Create node for this move
                        PathNode nextNode = new PathNode(nextMovePos, newVelocity);
                        
                        // Evaluate this node individually
                        pathEvaluator.EvaluatePathNode(nextNode, task.Depth, currentWorldPos);
                        
                        if (nextNode.Score > bestScore)
                        {
                            bestScore = nextNode.Score;
                            bestNode = nextNode;
                            bestMovePos = nextMovePos;
                        }
                    }
                    
                    if (bestNode != null)
                    {
                        // Log that we're forcing a path to be kept
                        Debug.Log($"<color=green>FORCE KEEPING BEST PATH: Score {bestScore:F2} at depth {task.Depth}</color>");
                        
                        // Process the best path
                        Vector2Int newVelocity = bestMovePos - task.ParentNode.Position;
                        
                        // GRAVEL FIX: Apply gravel speed limit if needed
                        Vector2 worldPos = GridToWorldPosition(bestMovePos);
                        float terrainQuality = terrainAnalyzer.EvaluateTerrain(worldPos);
                        bool isOnGravel = terrainQuality < 0.5f;
                        
                        if (isOnGravel || (task.ParentNode.TerrainQuality < 0.5f))
                        {
                            // Calculate direction of velocity
                            // Fix: Convert Vector2Int to Vector2 before accessing normalized
                            Vector2 velAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                            if (velAsVector2.magnitude > 0.01f)
                            {
                                Vector2 velDir = velAsVector2.normalized;
                                
                                // ALWAYS force magnitude to exactly 1 (in grid units)
                                float gravelSpeed = 1.0f * PlayerMovement.GRID_SCALE;
                                
                                // Set velocity to exactly gravelSpeed
                                newVelocity = new Vector2Int(
                                    Mathf.RoundToInt(velDir.x * gravelSpeed),
                                    Mathf.RoundToInt(velDir.y * gravelSpeed)
                                );
                                
                                Debug.Log($"<color=red>GRAVEL FIX: Force reduced velocity for best path to {newVelocity} (speed=1)</color>");
                            }
                        }
                        
                        // Update direction changes tracking
                        if (task.Depth > 1)
                        {
                            // Fix: Convert Vector2Int to Vector2 before accessing normalized
                            Vector2 parentVelAsVector2 = new Vector2(task.ParentNode.Velocity.x, task.ParentNode.Velocity.y);
                            Vector2 newVelAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                            
                            Vector2 parentDir = parentVelAsVector2.normalized;
                            Vector2 nextDir = newVelAsVector2.normalized;
                            
                            float directionChange = Vector2.Dot(parentDir, nextDir);
                            
                            // If significant direction change detected
                            if (directionChange < 0.8f)
                            {
                                task.CurrentPath.DirectionChanges++;
                            }
                        }
                        
                        // Set terrain quality
                        bestNode.TerrainQuality = terrainQuality;
                        
                        // Set off-track count either as 0 (on good terrain) or inherit + increment
                        if (bestNode.TerrainQuality < minTerrainQualityThreshold)
                        {
                            bestNode.OffTrackCount = task.ParentNode.OffTrackCount + 1;
                        }
                        else
                        {
                            bestNode.OffTrackCount = 0;
                        }
                        
                        // Look ahead for track exits
                        if (enableLookAheadPruning)
                        {
                            bestNode.TrackExitRisk = terrainAnalyzer.CalculateTrackExitRisk(worldPos, newVelocity, lookAheadDistance);
                        }
                        
                        // Add this node to parent's children for visualization
                        task.ParentNode.Children.Add(bestNode);
                        
                        // Create a new path by copying the current path and adding this move
                        Path newPath = new Path();
                        newPath.Nodes.AddRange(task.CurrentPath.Nodes);
                        newPath.Nodes.Add(bestNode);
                        
                        // Copy tracked values from current path
                        newPath.DirectionChanges = task.CurrentPath.DirectionChanges;
                        
                        // Create new task for the next depth level
                        PathGenerationTask newTask = new PathGenerationTask(newPath, bestNode, task.Depth + 1);
                        
                        // Add new task to queue
                        pathGenerationQueue.Enqueue(newTask);
                    }
                }
            }
            else
            {
                // If pruning is disabled, process all next moves normally
                foreach (Vector2Int nextMovePos in nextMoves)
                {
                    ProcessNextNode(task, nextMovePos, currentWorldPos);
                }
            }
        }
        
        /// <summary>
        /// Determines if a path should be pruned based on various criteria
        /// </summary>
        private bool ShouldPrunePath(PathGenerationTask task, PathNode node, bool isOnPoorTerrain)
        {
            // MODIFIED: Never prune first level paths with good scores
            if (task.Depth == 1 && node.Score > 0.6f)
            {
                return false; // Always keep good first level paths
            }
            
            // MODIFIED: Keep at least MIN_PATHS_TO_KEEP from the first level
            if (task.Depth == 1)
            {
                forcedKeepPathCounter++;
                if (forcedKeepPathCounter <= MIN_PATHS_TO_KEEP)
                {
                    return false; // Force keep this path
                }
            }
            
            // Calculate adaptive pruning threshold based on depth
            float adaptiveScorePruningThreshold = scorePruningThreshold;
            
            // Make pruning more aggressive as depth increases if depthPruningFactor > 0
            if (depthPruningFactor > 0)
            {
                float depthRatio = (float)task.Depth / pathfindingDepth;
                adaptiveScorePruningThreshold += (depthRatio * depthPruningFactor * (0.7f - scorePruningThreshold));
            }
            
            // Adjust pruning thresholds for finish line targeting
            if (targetingFinishLine)
            {
                // Be more lenient with pruning when targeting finish line
                adaptiveScorePruningThreshold *= 0.8f; // 20% more lenient
            }
            
            // CRITICAL FIX: Counters used to force-keep some paths
            // Every X paths, keep one regardless of score to ensure a minimum path count
            if (task.Depth == 1)
            {
                forcedKeepPathCounter++;
                if (forcedKeepPathCounter % 3 == 0)
                {
                    return false; // Force keep this path
                }
            }
            
            // 1. Terrain-based pruning - Check if this node is off-track
            if (node.TerrainQuality < minTerrainQualityThreshold)
            {
                // Inherit and increment off-track count
                int offTrackCount = task.ParentNode.OffTrackCount;
                
                // If we've exceeded tolerance, prune this path
                // BUT be more lenient if we're in recovery mode (already on poor terrain)
                int tolerance = isOnPoorTerrain ? offTrackToleranceCount + 1 : offTrackToleranceCount;
                
                if (offTrackCount >= tolerance)
                {
                    prunedByTerrain++;
                    return true;
                }
            }
            
            // 2. Score-based pruning (if aggressive pruning is enabled)
            if (enableAggressivePruning && !isOnPoorTerrain)
            {
                // Check both score and distance
                float nodeScore = node.Score;
                if (nodeScore < adaptiveScorePruningThreshold)
                {
                    // MODIFIED: More leniency for initial moves
                    if (task.Depth > 1 || nodeScore < adaptiveScorePruningThreshold * 0.6f)
                    {
                        // MODIFIED: Special case for first few paths - ensure we get some variety
                        if (task.Depth == 1 && generatedPaths.Count < MIN_PATHS_TO_KEEP)
                        {
                            // Don't prune if we don't have enough paths yet
                            return false;
                        }
                        
                        prunedByScore++;
                        return true;
                    }
                }
            }
            
            // 3. Look ahead pruning - check for imminent track exits
            // Be more lenient when in recovery mode
            if (enableLookAheadPruning)
            {
                float exitRisk = node.TrackExitRisk;
                float exitThreshold = isOnPoorTerrain ? 0.9f : 0.7f;
                
                if (exitRisk > exitThreshold)
                {
                    prunedByLookAhead++;
                    return true;
                }
            }
            
            // 4. Direction & efficiency pruning - disable when in recovery mode
            if (pruneIneffcientMovements && !isOnPoorTerrain && task.Depth > 1 && task.CurrentPath.Nodes.Count >= 2)
            {
                // Check for zigzag patterns by comparing consecutive direction changes
                if (task.CurrentPath.DirectionChanges > task.Depth / 2)
                {
                    prunedByInefficiency++;
                    return true;
                }
            }
            
            // 5. Speed-related pruning - disable when in recovery mode
            if (pruneExcessiveSpeedAtTurns && !isOnPoorTerrain)
            {
                float speedScore = node.SpeedScore;
                if (speedScore < 0.3f)
                {
                    prunedBySpeed++;
                    return true;
                }
            }
            
            // If we got here, don't prune this path
            return false;
        }
        
        /// <summary>
        /// Process a single next node for a task
        /// </summary>
        private void ProcessNextNode(PathGenerationTask task, Vector2Int nextMovePos, Vector2 currentWorldPos)
        {
            // Calculate new velocity
            Vector2Int newVelocity = nextMovePos - task.ParentNode.Position;
            
            // GRAVEL FIX: Check if this node would be on gravel
            Vector2 worldPos = GridToWorldPosition(nextMovePos);
            float terrainQuality = terrainAnalyzer.EvaluateTerrain(worldPos);
            bool isOnGravel = terrainQuality < 0.5f;
            
            // GRAVEL FIX: If parent node was on gravel or this node will be on gravel,
            // adjust velocity to ALWAYS be exactly speed=1 grid units
            if (isOnGravel || (task.ParentNode.TerrainQuality < 0.5f))
            {
                // Calculate direction of velocity
                // Fix: Convert Vector2Int to Vector2 before accessing normalized
                Vector2 velAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                if (velAsVector2.magnitude > 0.01f)
                {
                    Vector2 velDir = velAsVector2.normalized;
                    
                    // FORCE magnitude to exactly 1 (in grid units) for gravel
                    float gravelSpeed = 1.0f * PlayerMovement.GRID_SCALE;
                    
                    // Set velocity to exactly gravelSpeed in the same direction
                    newVelocity = new Vector2Int(
                        Mathf.RoundToInt(velDir.x * gravelSpeed),
                        Mathf.RoundToInt(velDir.y * gravelSpeed)
                    );
                    
                    if (debugVerbose)
                    {
                        Debug.Log($"<color=red>GRAVEL FIX: Force limited velocity for node at {nextMovePos} to {newVelocity} (speed=1)</color>");
                    }
                }
            }
            
            // Create node for this move with the potentially adjusted velocity
            PathNode nextNode = new PathNode(nextMovePos, newVelocity);
            
            // Evaluate this node individually
            pathEvaluator.EvaluatePathNode(nextNode, task.Depth, currentWorldPos);
            
            // Update direction changes tracking
            if (task.Depth > 1)
            {
                // Fix: Convert Vector2Int to Vector2 before accessing normalized
                Vector2 parentVelAsVector2 = new Vector2(task.ParentNode.Velocity.x, task.ParentNode.Velocity.y);
                Vector2 newVelAsVector2 = new Vector2(newVelocity.x, newVelocity.y);
                
                Vector2 parentDir = parentVelAsVector2.normalized;
                Vector2 nextDir = newVelAsVector2.normalized;
                
                float directionChange = Vector2.Dot(parentDir, nextDir);
                
                // If significant direction change detected
                if (directionChange < 0.8f)
                {
                    task.CurrentPath.DirectionChanges++;
                }
            }
            
            // Calculate terrain quality for this node
            nextNode.TerrainQuality = terrainQuality;
            
            // Set off-track count either as 0 (on good terrain) or inherit + increment
            if (nextNode.TerrainQuality < minTerrainQualityThreshold)
            {
                nextNode.OffTrackCount = task.ParentNode.OffTrackCount + 1;
            }
            else
            {
                nextNode.OffTrackCount = 0;
            }
            
            // Look ahead for track exits
            if (enableLookAheadPruning)
            {
                nextNode.TrackExitRisk = terrainAnalyzer.CalculateTrackExitRisk(worldPos, newVelocity, lookAheadDistance);
            }
            
            // Add this node to parent's children for visualization
            task.ParentNode.Children.Add(nextNode);
            
            // Create a new path by copying the current path and adding this move
            Path newPath = new Path();
            newPath.Nodes.AddRange(task.CurrentPath.Nodes);
            newPath.Nodes.Add(nextNode);
            
            // Copy tracked values from current path
            newPath.DirectionChanges = task.CurrentPath.DirectionChanges;
            
            // Create new task for the next depth level
            PathGenerationTask newTask = new PathGenerationTask(newPath, nextNode, task.Depth + 1);
            
            // Add new task to queue
            pathGenerationQueue.Enqueue(newTask);
        }

        /// <summary>
        /// Generate the 9 possible move positions from a given position and velocity
        /// </summary>
        private List<Vector2Int> GenerateMoveIndicatorsAt(Vector2Int position, Vector2Int velocity)
        {
            List<Vector2Int> moves = new List<Vector2Int>();
            
            // Check if current position is on gravel using world position
            Vector2 currentWorldPos = GridToWorldPosition(position);
            float currentTerrainQuality = terrainAnalyzer.EvaluateTerrain(currentWorldPos);
            bool currentlyOnGravel = currentTerrainQuality < 0.5f;
            
            // GRAVEL FIX: If currently on gravel, adjust velocity to be exactly speed=1
            if (currentlyOnGravel && velocity.magnitude > PlayerMovement.GRID_SCALE)
            {
                // Fix: Convert Vector2Int to Vector2 before accessing normalized
                Vector2 velAsVector2 = new Vector2(velocity.x, velocity.y);
                Vector2 normalizedVel = velAsVector2.normalized;
                
                velocity = new Vector2Int(
                    Mathf.RoundToInt(normalizedVel.x * PlayerMovement.GRID_SCALE),
                    Mathf.RoundToInt(normalizedVel.y * PlayerMovement.GRID_SCALE)
                );
                
                Debug.Log($"<color=red>GRAVEL FIX: Adjusted move generation velocity to {velocity} (speed=1) for position {position}</color>");
            }
            
            // Convert to game scale for calculating base position
            Vector2Int unscaledPosition = new Vector2Int(
                position.x / PlayerMovement.GRID_SCALE,
                position.y / PlayerMovement.GRID_SCALE
            );
            
            Vector2Int unscaledVelocity = new Vector2Int(
                velocity.x / PlayerMovement.GRID_SCALE,
                velocity.y / PlayerMovement.GRID_SCALE
            );
            
            // Base position is current position + current velocity
            Vector2Int basePosition = unscaledPosition + unscaledVelocity;
            
            // Generate the 9 indicators around the base position (-1, 0, 1 in each direction)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    // Calculate adjusted position
                    Vector2Int adjustedPosition = new Vector2Int(
                        basePosition.x + dx,
                        basePosition.y + dy
                    );
                    
                    // Convert to precise grid coordinates
                    Vector2Int precisePosition = new Vector2Int(
                        adjustedPosition.x * PlayerMovement.GRID_SCALE,
                        adjustedPosition.y * PlayerMovement.GRID_SCALE
                    );
                    
                    // Add to list of moves
                    moves.Add(precisePosition);
                }
            }
            
            return moves;
        }

        /// <summary>
        /// Calculate maximum move distance based on velocity
        /// </summary>
        private int CalculateMaxMoveDistance(Vector2Int velocity)
        {
            // GRAVEL FIX: Check if current position is on gravel
            // If on gravel, max move distance should be 1 exactly
            float magnitude = velocity.magnitude / PlayerMovement.GRID_SCALE;
            
            // Default fallback is 1
            return Mathf.Max(1, Mathf.RoundToInt(magnitude));
        }

        /// <summary>
        /// Convert grid position to world position
        /// </summary>
        private Vector2 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector2(
                gridPos.x / (float)PlayerMovement.GRID_SCALE,
                gridPos.y / (float)PlayerMovement.GRID_SCALE
            );
        }

        /// <summary>
        /// Get all generated paths
        /// </summary>
        public List<Path> GetGeneratedPaths()
        {
            return generatedPaths;
        }

        /// <summary>
        /// Get the root node of the path tree
        /// </summary>
        public PathNode GetRootNode()
        {
            return rootNode;
        }

        /// <summary>
        /// Get the generation progress (0-1)
        /// </summary>
        public float GetProgress()
        {
            return processingProgress;
        }

        /// <summary>
        /// Get the total number of paths generated
        /// </summary>
        public int GetTotalPathsGenerated()
        {
            return totalPathsGenerated;
        }

        /// <summary>
        /// Get the number of paths pruned
        /// </summary>
        public int GetPathsPruned()
        {
            return pathsPruned;
        }

        /// <summary>
        /// Get pruning statistics
        /// </summary>
        public int[] GetPruningStats()
        {
            return new int[]
            {
                prunedByTerrain,
                prunedByScore,
                prunedByLookAhead,
                prunedByInefficiency,
                prunedBySpeed
            };
        }
    }
}