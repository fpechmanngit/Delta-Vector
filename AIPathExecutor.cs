using UnityEngine;
using System.Collections.Generic;

namespace DeltaVector.AI
{
    /// <summary>
    /// Responsible for executing the chosen path as a car movement
    /// </summary>
    public class AIPathExecutor : MonoBehaviour
    {
        // Reference to components
        private PlayerMovement playerMovement;
        private MoveIndicatorManager moveIndicatorManager;
        private GameManager gameManager;
        private AITerrainAnalyzer terrainAnalyzer;
        
        // Debug state
        [SerializeField] private bool debugMode = false;
        
        // Track the last best path for validation
        private Path lastBestPath = null;
        
        /// <summary>
        /// Initialize with required components
        /// </summary>
        public void Initialize(PlayerMovement movement, MoveIndicatorManager moveManager, AITerrainAnalyzer analyzer)
        {
            playerMovement = movement;
            moveIndicatorManager = moveManager;
            terrainAnalyzer = analyzer;
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        /// <summary>
        /// Execute the best move from the selected path
        /// </summary>
        public void ExecuteBestMove(Path bestPath)
        {
            // Store the path for validation
            lastBestPath = bestPath;
            
            // If no path or empty path, use fallback
            if (bestPath == null || bestPath.Nodes.Count == 0)
            {
                Debug.LogError("No valid path found! Using fallback move system");
                ForceFindAndExecuteMove();
                return;
            }
            
            // Get the first move from the path
            PathNode firstMove = bestPath.Nodes[0];
            
            if (debugMode)
            {
                Debug.Log($"Executing best path first move to position {firstMove.Position} " +
                         $"with score {firstMove.Score:F2}");
            }
            
            // Check if this move would result in staying in the same position
            if (IsCurrentPosition(firstMove.Position))
            {
                Debug.LogWarning($"AI tried to stay in place! Current position: {playerMovement.CurrentPosition * PlayerMovement.GRID_SCALE}, " +
                                 $"Target position: {firstMove.Position}. Finding alternative move...");
                
                // Try to find an alternative move from the path
                PathNode alternativeMove = FindAlternativeMove(bestPath);
                
                if (alternativeMove != null)
                {
                    Debug.Log($"Found alternative move to {alternativeMove.Position} to prevent getting stuck");
                    ExecuteMove(alternativeMove.Position);
                    return;
                }
                else
                {
                    // If no alternative found in the path, use the fallback system
                    Debug.LogWarning("No suitable alternative in best path. Using fallback move system");
                    ForceFindAndExecuteMove(true); // Force to find a different position
                    return;
                }
            }
            
            // Validate the move is actually valid for additional safety
            if (!IsValidMove(firstMove.Position))
            {
                Debug.LogWarning($"Selected move to {firstMove.Position} appears to be invalid! Checking alternatives...");
                
                // Try again with move indicator refreshing
                RefreshMoveIndicators();
                
                // After refreshing, check again
                if (IsValidMove(firstMove.Position))
                {
                    Debug.Log("Move is now valid after refreshing move indicators.");
                    ExecuteMove(firstMove.Position);
                    return;
                }
                
                // If still not valid, try another node from the path
                if (bestPath.Nodes.Count > 1)
                {
                    PathNode alternativeMove = bestPath.Nodes[1];
                    if (IsValidMove(alternativeMove.Position) && !IsCurrentPosition(alternativeMove.Position))
                    {
                        Debug.Log($"Using alternative move to {alternativeMove.Position}");
                        ExecuteMove(alternativeMove.Position);
                        return;
                    }
                }
                
                // If still not valid, use fallback
                Debug.LogWarning("No valid alternatives in best path. Using fallback move system");
                ForceFindAndExecuteMove();
                return;
            }
            
            // Execute the move
            ExecuteMove(firstMove.Position);
        }
        
        /// <summary>
        /// Find an alternative move from the path to avoid staying in place
        /// </summary>
        private PathNode FindAlternativeMove(Path path)
        {
            // Try to find a node in the path that's not the current position
            for (int i = 1; i < path.Nodes.Count; i++)
            {
                PathNode node = path.Nodes[i];
                if (!IsCurrentPosition(node.Position) && IsValidMove(node.Position))
                {
                    return node;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if a position is the current position of the vehicle
        /// </summary>
        private bool IsCurrentPosition(Vector2Int position)
        {
            if (playerMovement == null) return false;
            
            // Get current position in grid coordinates
            Vector2Int currentGridPos = playerMovement.CurrentPosition * PlayerMovement.GRID_SCALE;
            
            // Check if same position (using a small threshold to account for floating point precision)
            return Vector2Int.Distance(position, currentGridPos) < 0.1f;
        }
        
        /// <summary>
        /// Refresh the move indicators to make sure they match the current state
        /// </summary>
        private void RefreshMoveIndicators()
        {
            if (moveIndicatorManager == null || playerMovement == null)
                return;

            // Clear existing indicators
            moveIndicatorManager.ClearIndicators();
            
            // Re-show possible moves based on current position and velocity
            Vector2Int currentPosition = playerMovement.CurrentPosition;
            Vector2Int currentVelocity = playerMovement.CurrentVelocity;
            int maxSpeed = CalculateMaxMoveDistance(currentVelocity);
            
            moveIndicatorManager.ShowPossibleMoves(currentPosition, currentVelocity, maxSpeed);
            
            // Small delay to ensure indicators are updated
            System.Threading.Thread.Sleep(10);
        }
        
        /// <summary>
        /// Execute a specific move
        /// </summary>
        private void ExecuteMove(Vector2Int position)
        {
            // Double check move validity again
            if (!IsValidMove(position))
            {
                Debug.LogWarning($"Move to {position} is not valid even at execution time. Using fallback.");
                ForceFindAndExecuteMove();
                return;
            }
            
            // Final check to ensure we're not staying in place
            if (IsCurrentPosition(position))
            {
                Debug.LogWarning($"Still trying to stay in place! Finding another move...");
                ForceFindAndExecuteMove(true);
                return;
            }

            // Clear indicators before moving
            moveIndicatorManager?.ClearIndicators();
            
            // Execute the move
            if (playerMovement != null)
            {
                playerMovement.MoveToIndicator(position);
            }
            else
            {
                Debug.LogError("Cannot execute move: playerMovement is null");
            }
            
            // End turn in game manager
            if (gameManager != null)
            {
                gameManager.EndTurn();
            }
        }
        
        /// <summary>
        /// Check if a move is valid by comparing with move indicator positions
        /// </summary>
        private bool IsValidMove(Vector2Int position)
        {
            if (moveIndicatorManager == null)
                return true; // Can't validate, assume valid
                
            List<Vector2Int> validMoves = moveIndicatorManager.GetValidMovePositions();
            
            // No valid moves available at all
            if (validMoves == null || validMoves.Count == 0)
            {
                Debug.LogWarning("No valid moves available to check against!");
                return false;
            }
            
            // Check if the position is in the list of valid moves
            foreach (Vector2Int validPos in validMoves)
            {
                // Use approximate equality to account for floating point errors
                if (Vector2Int.Distance(validPos, position) < 0.1f)
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Fallback system to ensure the AI always finds a move, even when normal pathfinding fails
        /// </summary>
        public void ForceFindAndExecuteMove(bool avoidCurrentPosition = false)
        {
            Debug.Log($"FALLBACK: Starting emergency move selection, avoidCurrentPosition: {avoidCurrentPosition}");
            
            if (playerMovement == null)
            {
                Debug.LogError("Cannot execute fallback move: playerMovement is null");
                return;
            }
            
            // First, make sure move indicators are showing
            RefreshMoveIndicators();
            
            // Check if we're on gravel and need to recover
            Vector2 currentPos = transform.position;
            bool needsRecovery = false;
            
            // Check current terrain quality to see if in emergency mode
            float terrainQuality = terrainAnalyzer.EvaluateTerrain(currentPos);
            if (terrainQuality < 0.5f) // On gravel or worse
            {
                needsRecovery = true;
                Debug.Log("FALLBACK: On poor terrain, engaging recovery mode");
            }
            
            // Find the nearest asphalt position if in recovery mode
            Vector2 recoveryTargetPos = currentPos;
            if (needsRecovery)
            {
                recoveryTargetPos = terrainAnalyzer.FindNearestAsphaltPosition(currentPos);
                Debug.Log($"FALLBACK: Emergency recovery target found at {recoveryTargetPos}");
            }
            
            // First try: use move indicator manager directly
            if (moveIndicatorManager != null)
            {
                // Check if we have any valid moves
                List<Vector2Int> validMoves = moveIndicatorManager.GetValidMovePositions();
                if (validMoves.Count > 0)
                {
                    Debug.Log($"FALLBACK: Found {validMoves.Count} valid moves from move indicator manager");
                    
                    // Filter out current position if needed
                    if (avoidCurrentPosition)
                    {
                        Vector2Int currentGridPos = playerMovement.CurrentPosition * PlayerMovement.GRID_SCALE;
                        validMoves.RemoveAll(move => Vector2Int.Distance(move, currentGridPos) < 0.1f);
                        Debug.Log($"FALLBACK: Filtered out current position, remaining moves: {validMoves.Count}");
                        
                        // If no valid moves after filtering, just keep them all (better than nothing)
                        if (validMoves.Count == 0)
                        {
                            validMoves = moveIndicatorManager.GetValidMovePositions();
                            Debug.LogWarning("FALLBACK: No moves after filtering current position, using all moves");
                        }
                    }
                    
                    // If in recovery mode, prioritize differently
                    Vector2Int bestMove;
                    if (needsRecovery)
                    {
                        bestMove = EvaluateFallbackMovesForRecovery(validMoves, recoveryTargetPos);
                    }
                    else
                    {
                        // Normal evaluation
                        bestMove = EvaluateFallbackMoves(validMoves);
                    }
                    
                    // Final check to avoid current position
                    if (avoidCurrentPosition && IsCurrentPosition(bestMove))
                    {
                        Debug.LogWarning("FALLBACK: Best move is still current position, choosing random alternative");
                        
                        // Try to find any move that isn't the current position
                        foreach (var move in validMoves)
                        {
                            if (!IsCurrentPosition(move))
                            {
                                bestMove = move;
                                Debug.Log($"FALLBACK: Selected random alternative move: {bestMove}");
                                break;
                            }
                        }
                    }
                    
                    // Execute the best move
                    moveIndicatorManager.ClearIndicators();
                    playerMovement.MoveToIndicator(bestMove);
                    
                    // End turn in game manager
                    if (gameManager != null)
                    {
                        gameManager.EndTurn();
                    }
                    
                    return;
                }
            }
            
            // Second try: manually generate all 9 possible moves
            Debug.Log("FALLBACK: No valid moves from moveIndicatorManager, generating moves manually");
            Vector2Int currPos = playerMovement.CurrentPosition;
            Vector2Int currVel = playerMovement.CurrentVelocity;
            
            // Convert to game scale for calculating base position
            Vector2Int basePosition = new Vector2Int(
                currPos.x + currVel.x,
                currPos.y + currVel.y
            );
            
            List<Vector2Int> emergencyMoves = new List<Vector2Int>();
            
            // Generate all 9 possible moves
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    // If avoiding current position and this is (0,0), skip it
                    if (avoidCurrentPosition && dx == 0 && dy == 0)
                        continue;
                        
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
                    
                    emergencyMoves.Add(precisePosition);
                }
            }
            
            // Evaluate and pick the best emergency move
            if (emergencyMoves.Count > 0)
            {
                Debug.Log($"FALLBACK: Generated {emergencyMoves.Count} emergency moves manually");
                
                // If in recovery mode, prioritize differently
                Vector2Int bestMove;
                if (needsRecovery)
                {
                    bestMove = EvaluateFallbackMovesForRecovery(emergencyMoves, recoveryTargetPos);
                }
                else
                {
                    // Normal evaluation
                    bestMove = EvaluateFallbackMoves(emergencyMoves);
                }
                
                // Execute the best emergency move
                playerMovement.MoveToIndicator(bestMove);
                
                // End turn in game manager
                if (gameManager != null)
                {
                    gameManager.EndTurn();
                }
                
                return;
            }
            
            // Last desperate attempt: Pick the direction with maximum change
            Debug.LogWarning("FALLBACK: All attempts failed! Using last resort move with forced direction change");
            
            // Generate a move with maximum direction change
            Vector2Int lastResortMove;
            
            // If velocity is zero, pick a random direction
            if (currVel.magnitude < 0.1f)
            {
                // Random direction
                int randomDir = Random.Range(0, 8);
                int dx = (randomDir % 3) - 1;
                int dy = (randomDir / 3) - 1;
                
                // Ensure we're not picking (0,0)
                if (dx == 0 && dy == 0) dx = 1;
                
                lastResortMove = new Vector2Int(
                    (currPos.x + dx) * PlayerMovement.GRID_SCALE,
                    (currPos.y + dy) * PlayerMovement.GRID_SCALE
                );
            }
            else
            {
                // Pick direction perpendicular to current velocity
                int dx = (int)-Mathf.Sign(currVel.y);
                int dy = (int)Mathf.Sign(currVel.x);
                
                if (dx == 0) dx = 1; // Ensure non-zero direction
                if (dy == 0) dy = 1;
                
                lastResortMove = new Vector2Int(
                    (currPos.x + dx) * PlayerMovement.GRID_SCALE,
                    (currPos.y + dy) * PlayerMovement.GRID_SCALE
                );
            }
            
            // Execute the last resort move
            Debug.Log($"FALLBACK: Using absolute last resort move to {lastResortMove}");
            playerMovement.MoveToIndicator(lastResortMove);
            
            // End turn in game manager
            if (gameManager != null)
            {
                gameManager.EndTurn();
            }
        }

        /// <summary>
        /// Evaluate a list of potential fallback moves and select the best one
        /// </summary>
        private Vector2Int EvaluateFallbackMoves(List<Vector2Int> moves)
        {
            // Default to first move
            Vector2Int bestMove = moves[0];
            float bestScore = -1f;
            
            // Current position and target for evaluations
            Vector2 currentPos = transform.position;
            Vector2 targetPos = GetTargetPosition();
            
            // For avoiding staying in place
            Vector2Int currentGridPos = playerMovement.CurrentPosition * PlayerMovement.GRID_SCALE;
            
            foreach (Vector2Int move in moves)
            {
                // Convert to world position
                Vector2 worldPos = new Vector2(
                    move.x / (float)PlayerMovement.GRID_SCALE,
                    move.y / (float)PlayerMovement.GRID_SCALE
                );
                
                // Calculate difference from current position to get velocity
                Vector2Int velocity = move - currentGridPos;
                
                // If this move doesn't change position, heavily penalize it
                if (velocity.magnitude < 0.1f)
                {
                    continue; // Skip this move entirely
                }
                
                // Simple scoring: distance to target + terrain quality + staying on track
                float distanceScore = EvaluateDistanceToTarget(worldPos, targetPos, currentPos);
                float terrainScore = terrainAnalyzer.EvaluateTerrain(worldPos);
                
                // Higher weight on terrain to avoid going off-track
                float score = (distanceScore * 0.7f) + (terrainScore * 2.0f);
                
                // Prefer moves that maintain some velocity
                float velocityMagnitude = velocity.magnitude / PlayerMovement.GRID_SCALE;
                if (velocityMagnitude > 0.5f)
                {
                    score += 0.5f;
                }
                
                // Check if this is better than our current best
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            
            Debug.Log($"FALLBACK: Selected best emergency move with score {bestScore:F2}");
            return bestMove;
        }

        /// <summary>
        /// Special evaluation for recovery moves when stuck on gravel
        /// </summary>
        private Vector2Int EvaluateFallbackMovesForRecovery(List<Vector2Int> moves, Vector2 recoveryTarget)
        {
            // Default to first move
            Vector2Int bestMove = moves[0];
            float bestScore = -1f;
            
            // Current position
            Vector2 currentPos = transform.position;
            
            // Current grid position for checking if move changes position
            Vector2Int currentGridPos = playerMovement.CurrentPosition * PlayerMovement.GRID_SCALE;
            
            foreach (Vector2Int move in moves)
            {
                // Convert to world position
                Vector2 worldPos = new Vector2(
                    move.x / (float)PlayerMovement.GRID_SCALE,
                    move.y / (float)PlayerMovement.GRID_SCALE
                );
                
                // Calculate velocity vector
                Vector2Int velocity = move - currentGridPos;
                
                // If this move doesn't change position, heavily penalize it
                if (velocity.magnitude < 0.1f)
                {
                    continue; // Skip this move entirely
                }
                
                Vector2 velDir = new Vector2(velocity.x, velocity.y).normalized;
                
                // RECOVERY MODE SCORING
                
                // 1. Terrain quality is highest priority (1.0 = asphalt, 0.2 = gravel)
                float terrainScore = terrainAnalyzer.EvaluateTerrain(worldPos) * 3.0f; // Triple weight
                
                // 2. Direction to recovery target
                Vector2 toRecovery = (recoveryTarget - currentPos).normalized;
                float alignmentScore = Vector2.Dot(velDir, toRecovery);
                alignmentScore = (alignmentScore + 1.0f) * 0.5f; // Convert to 0-1 range
                
                // 3. Distance improvement to recovery target
                float currentDist = Vector2.Distance(currentPos, recoveryTarget);
                float newDist = Vector2.Distance(worldPos, recoveryTarget);
                float distImprovement = (currentDist - newDist) / currentDist;
                float distScore = Mathf.Clamp01(distImprovement + 0.5f); // Convert to 0-1 range with bias
                
                // Combined score with heavy terrain bias
                float totalScore = (terrainScore * 0.6f) + (alignmentScore * 0.2f) + (distScore * 0.2f);
                
                // Check if this is better than our current best
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestMove = move;
                }
                
                Debug.Log($"RECOVERY MOVE to {worldPos}: Terrain={terrainScore:F2}, Alignment={alignmentScore:F2}, " +
                         $"Dist={distScore:F2}, Total={totalScore:F2}");
            }
            
            Debug.Log($"RECOVERY: Selected best move with score {bestScore:F2}");
            return bestMove;
        }

        /// <summary>
        /// Get a target position - either next checkpoint or finish line
        /// </summary>
        private Vector2 GetTargetPosition()
        {
            // Try to find a checkpoint manager
            CheckpointManager checkpointManager = Object.FindFirstObjectByType<CheckpointManager>();
            
            if (checkpointManager != null)
            {
                // Get next checkpoint for this player
                bool isPlayer1 = gameObject.CompareTag("Player1");
                Checkpoint nextCheckpoint = checkpointManager.GetNextCheckpointInOrder(isPlayer1);
                
                if (nextCheckpoint != null)
                {
                    return nextCheckpoint.transform.position;
                }
            }
            
            // Try to find finish line
            GameObject finishLine = GameObject.Find("StartFinishLine");
            if (finishLine != null)
            {
                return finishLine.transform.position;
            }
            
            // Fallback - use current position + velocity as target
            Vector2Int currPos = playerMovement.CurrentPosition;
            Vector2Int currVel = playerMovement.CurrentVelocity;
            
            return new Vector2(
                currPos.x + currVel.x * 2,
                currPos.y + currVel.y * 2
            );
        }

        /// <summary>
        /// Simple evaluation of how a move improves distance to target
        /// </summary>
        private float EvaluateDistanceToTarget(Vector2 movePos, Vector2 targetPos, Vector2 currentPos)
        {
            // Current distance to target
            float currentDistance = Vector2.Distance(currentPos, targetPos);
            
            // New distance to target after the move
            float newDistance = Vector2.Distance(movePos, targetPos);
            
            // Calculate improvement in distance
            if (currentDistance <= 0.01f) return 0.5f; // Already at target
            
            float improvement = (currentDistance - newDistance) / currentDistance;
            
            // Normalize to 0-1 range with a bias toward improvement
            return Mathf.Clamp01((improvement * 1.5f) + 0.5f);
        }

        /// <summary>
        /// Calculate maximum move distance based on velocity
        /// </summary>
        private int CalculateMaxMoveDistance(Vector2Int velocity)
        {
            // Default fallback is 1
            return Mathf.Max(1, Mathf.RoundToInt(velocity.magnitude / PlayerMovement.GRID_SCALE));
        }
    }
}