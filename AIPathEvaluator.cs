using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DeltaVector.AI
{
    /// <summary>
    /// Responsible for evaluating and scoring paths for AI decision making
    /// </summary>
    public class AIPathEvaluator
    {
        // Evaluation weight settings
        private float distanceWeight = 5f;
        private float speedWeight = 6f;
        private float terrainWeight = 10f;
        private float directionWeight = 3f;
        private float pathWeight = 8f;
        private float returnToAsphaltWeight = 12f;
        private float centerTrackWeight = 4f;
        private float trackExitPenaltyWeight = 15f;
        private float finishLineWeight = 12f; // Finish line specific weight
        
        // Speed settings
        private float maxStraightSpeed = 7.0f;
        private float maxTurnSpeed = 3.5f;
        
        // Reference to terrain analyzer
        private AITerrainAnalyzer terrainAnalyzer;
        
        // Target position for evaluations
        private Vector2 targetPosition;
        
        // Maximum possible score for normalization
        private float maxPossibleScore;
        
        // Reference to checkpoints
        private CheckpointManager checkpointManager;
        private Checkpoint targetCheckpoint;
        
        // Finish line targeting flag
        private bool targetingFinishLine = false;

        // Current position terrain quality - used to detect if on gravel
        private float currentPositionTerrainQuality = 1.0f;
        private bool isOnGravel = false;
        
        /// <summary>
        /// Initialize the path evaluator with weights
        /// </summary>
        public AIPathEvaluator(AITerrainAnalyzer analyzer, Vector2 target)
        {
            terrainAnalyzer = analyzer;
            targetPosition = target;
            
            // Calculate max possible score for normalization
            maxPossibleScore = distanceWeight + speedWeight + terrainWeight + directionWeight +
                              pathWeight + returnToAsphaltWeight + centerTrackWeight + trackExitPenaltyWeight + finishLineWeight;
                              
            // Find checkpoint manager
            checkpointManager = Object.FindFirstObjectByType<CheckpointManager>();
        }

        /// <summary>
        /// Update the evaluator's settings from AIRulesManager
        /// </summary>
        public void UpdateSettings(float[] weights, float[] speedSettings)
        {
            if (weights.Length >= 9) // Updated to handle finish line weight too
            {
                distanceWeight = weights[0];
                speedWeight = weights[1];
                terrainWeight = weights[2];
                directionWeight = weights[3];
                pathWeight = weights[4];
                returnToAsphaltWeight = weights[5];
                centerTrackWeight = weights[6];
                trackExitPenaltyWeight = weights[7];
                finishLineWeight = weights[8]; // Finish line weight
            }
            
            if (speedSettings.Length >= 2)
            {
                maxStraightSpeed = speedSettings[0];
                maxTurnSpeed = speedSettings[1];
            }
            
            // Recalculate max possible score
            maxPossibleScore = distanceWeight + speedWeight + terrainWeight + directionWeight +
                              pathWeight + returnToAsphaltWeight + centerTrackWeight + 
                              trackExitPenaltyWeight + finishLineWeight;
        }

        /// <summary>
        /// Update target position and checkpoint
        /// </summary>
        public void SetTarget(Vector2 target, Checkpoint checkpoint)
        {
            targetPosition = target;
            targetCheckpoint = checkpoint;
            
            // Update terrain analyzer's target
            terrainAnalyzer.SetTargetPosition(target);
            
            Debug.Log($"AIPathEvaluator target set: {target}, checkpoint: {(checkpoint != null ? checkpoint.name : "FINISH LINE")}");
        }
        
        /// <summary>
        /// Set whether we're currently targeting the finish line
        /// </summary>
        public void SetTargetingFinishLine(bool isTargetingFinishLine)
        {
            targetingFinishLine = isTargetingFinishLine;
            
            if (targetingFinishLine)
            {
                Debug.Log("AIPathEvaluator: Now targeting finish line");
            }
        }

        /// <summary>
        /// Set the current terrain quality at car's position - crucial for gravel detection
        /// </summary>
        public void SetCurrentPositionTerrainQuality(Vector2 currentPos)
        {
            currentPositionTerrainQuality = terrainAnalyzer.EvaluateTerrain(currentPos);
            isOnGravel = currentPositionTerrainQuality < 0.5f; // Less than 0.5 indicates gravel or worse
            
            Debug.Log($"Current position terrain quality: {currentPositionTerrainQuality}, Car is on gravel: {isOnGravel}");
        }
        
        /// <summary>
        /// Evaluate an individual path node using the evaluation factors
        /// </summary>
        public void EvaluatePathNode(PathNode node, int depth, Vector2 currentPos)
        {
            // World position for this node
            Vector2 worldPos = GridToWorldPosition(node.Position);
            
            // Factors dictionary for detailed breakdown
            Dictionary<string, float> factors = new Dictionary<string, float>();
            
            // Update current position terrain quality
            SetCurrentPositionTerrainQuality(currentPos);
            
            // 1. Distance evaluation - getting closer to target
            float distScore = EvaluateDistance(worldPos, currentPos);
            factors["Distance"] = distScore;
            node.DistanceScore = distScore;
            
            // 2. Speed evaluation - maintaining appropriate speed
            float speedScore = EvaluateSpeed(node.Position, node.Velocity, currentPos);
            factors["Speed"] = speedScore;
            node.SpeedScore = speedScore;
            
            // 3. Terrain evaluation - preferring better terrain
            float terrainScore = terrainAnalyzer.EvaluateTerrain(worldPos);
            factors["Terrain"] = terrainScore;
            node.TerrainQuality = terrainScore;
            
            // 4. Direction evaluation - facing toward target
            float dirScore = EvaluateDirection(node.Velocity);
            factors["Direction"] = dirScore;
            node.DirectionScore = dirScore;
            
            // 5. Track center preference - prefer middle of track
            float centerScore = terrainAnalyzer.EvaluateTrackCenter(worldPos);
            factors["Center"] = centerScore;
            
            // 6. Track exit risk - heavily penalize moves that might exit track
            float exitRiskScore = terrainAnalyzer.EvaluateTrackExit(worldPos, node.Velocity);
            factors["ExitRisk"] = exitRiskScore;
            node.TrackExitRisk = 1.0f - exitRiskScore; // Invert so higher is more risky
            
            // 7. Future positioning score for next checkpoint
            float futureScore = 0.5f; // Default neutral value
            if (checkpointManager != null && targetCheckpoint != null)
            {
                futureScore = EvaluateFuturePositioning(worldPos, currentPos);
                factors["FuturePosition"] = futureScore;
            }
            
            // 8. Return to asphalt evaluation - critical when on gravel
            float returnToAsphaltScore = 0.5f; // Default neutral value
            
            // IMPROVED: Check current terrain at our current position to detect if we're on gravel
            // If we're on poor terrain (gravel or worse), evaluate return to asphalt
            if (isOnGravel)
            {
                returnToAsphaltScore = terrainAnalyzer.EvaluateReturnToAsphalt(worldPos, node.Velocity);
                factors["ReturnToAsphalt"] = returnToAsphaltScore;
                
                // Log that we're evaluating return to asphalt because we're on gravel
                Debug.Log($"Car is on gravel! ReturnToAsphalt score: {returnToAsphaltScore}, " +
                         $"From terrain quality: {currentPositionTerrainQuality}");
            }
            
            // 9. Finish line targeting score - only used when targeting finish line
            float finishLineScore = 0.5f; // Default neutral value
            if (targetingFinishLine)
            {
                // When targeting finish line, use more aggressive distance evaluation
                finishLineScore = EvaluateDirectApproachToTarget(worldPos, currentPos, node.Velocity);
                factors["FinishLine"] = finishLineScore;
            }
            
            // Calculate weighted score with variable weighting based on context
            float totalScore;
            
            // ENHANCED GRAVEL HANDLING: If on gravel, heavily prioritize getting back to asphalt
            if (isOnGravel)
            {
                // Emergency recovery mode - focus almost exclusively on getting back to asphalt
                totalScore = 
                    (returnToAsphaltScore * returnToAsphaltWeight * 3.0f) + // TRIPLED weight for return to asphalt
                    (terrainScore * terrainWeight * 2.0f) +                 // Doubled terrain weight 
                    (distScore * distanceWeight * 0.25f) +                  // Reduced distance importance even further
                    (dirScore * directionWeight * 0.2f) +                   // Reduced direction importance even further
                    (exitRiskScore * trackExitPenaltyWeight * 0.5f) +       // Keep some exit penalty but reduced
                    (speedScore * speedWeight * 0.1f) +                     // Almost ignore speed (it's fixed to 1 anyway)
                    (centerScore * centerTrackWeight * 0.1f);               // Minimal center track importance
                    
                // Even in recovery, if targeting finish line, keep some weight on it
                if (targetingFinishLine)
                {
                    totalScore += (finishLineScore * finishLineWeight * 0.3f); // Very low weight during gravel recovery
                }
                
                // Add a massive bonus for nodes on asphalt to encourage getting off gravel
                if (terrainScore > 0.9f) // High score means asphalt
                {
                    totalScore += returnToAsphaltWeight * 2.0f; // Fixed bonus for any asphalt node
                    Debug.Log("Applied massive bonus for asphalt node while car is on gravel");
                }
            }
            else if (targetingFinishLine)
            {
                // Special scoring when targeting finish line
                totalScore = 
                    (finishLineScore * finishLineWeight * 1.5f) +            // Heavily weight finish line approach
                    (distScore * distanceWeight * 1.5f) +                    // Boost distance importance
                    (dirScore * directionWeight * 1.5f) +                    // Boost direction importance 
                    (terrainScore * terrainWeight) +                         // Keep terrain weight normal
                    (speedScore * speedWeight * 0.6f) +                      // Reduce speed importance
                    (exitRiskScore * trackExitPenaltyWeight) +               // Keep exit penalty the same
                    (centerScore * centerTrackWeight * 0.5f) +               // Reduce center track importance
                    (returnToAsphaltScore * returnToAsphaltWeight * 0.5f);   // Reduce return to asphalt weight
            }
            else
            {
                // Normal checkpoint mode - balance all factors
                totalScore = 
                    (distScore * distanceWeight * 1.5f) +                    // Boost distance to checkpoint
                    (dirScore * directionWeight * 1.3f) +                    // Boost direction importance
                    (speedScore * speedWeight) +
                    (terrainScore * terrainWeight) +
                    (centerScore * centerTrackWeight) +
                    (exitRiskScore * trackExitPenaltyWeight) +
                    (returnToAsphaltScore * returnToAsphaltWeight * 0.5f) +  // Use half weight when not needed
                    (futureScore * pathWeight * 0.5f);                       // Using half of the path weight
            }
            
            // Normalize based on total possible weight (use a higher denominator for emergency mode)
            float normalizedScore;
            if (isOnGravel)
            {
                // Use a different normalization factor for gravel emergency mode
                float emergencyMaxScore = returnToAsphaltWeight * 3.0f + 
                                         terrainWeight * 2.0f + 
                                         distanceWeight * 0.25f + 
                                         directionWeight * 0.2f + 
                                         trackExitPenaltyWeight * 0.5f + 
                                         speedWeight * 0.1f + 
                                         centerTrackWeight * 0.1f;
                                         
                // Include finish line weight if targeting it
                if (targetingFinishLine)
                {
                    emergencyMaxScore += finishLineWeight * 0.3f;
                }
                
                // Add the asphalt bonus to normalization factor if needed
                emergencyMaxScore += returnToAsphaltWeight * 2.0f;
                                         
                normalizedScore = totalScore / emergencyMaxScore;
            }
            else if (targetingFinishLine)
            {
                // Special normalization for finish line targeting
                float finishLineMaxScore = finishLineWeight * 1.5f +
                                          distanceWeight * 1.5f +
                                          directionWeight * 1.5f +
                                          terrainWeight +
                                          speedWeight * 0.6f +
                                          trackExitPenaltyWeight +
                                          centerTrackWeight * 0.5f +
                                          returnToAsphaltWeight * 0.5f;
                                          
                normalizedScore = totalScore / finishLineMaxScore;
            }
            else
            {
                // Normal checkpoint normalization with boosted distance and direction
                float checkpointMaxScore = distanceWeight * 1.5f +
                                          directionWeight * 1.3f +
                                          speedWeight +
                                          terrainWeight +
                                          centerTrackWeight +
                                          trackExitPenaltyWeight +
                                          returnToAsphaltWeight * 0.5f +
                                          pathWeight * 0.5f;
                
                normalizedScore = totalScore / checkpointMaxScore;
            }
            
            // Apply a depth bonus/penalty
            // Earlier good moves are slightly preferred over later ones with equal score
            float depthFactor = 1.0f - (0.05f * depth);
            normalizedScore *= depthFactor;
            
            // Store the score and factors in the node
            node.Score = normalizedScore;
            node.EvaluationFactors = factors;
        }

        /// <summary>
        /// Evaluate a complete path based on its nodes
        /// </summary>
        public void EvaluatePath(Path path)
        {
            // If the path is empty, give it a zero score
            if (path.Nodes.Count == 0)
            {
                path.TotalScore = 0f;
                path.AverageScore = 0f;
                path.Quality = PathQuality.Bad;
                return;
            }
            
            // Sum up all node scores
            float totalScore = 0f;
            float minScore = 1f;
            float totalTerrainQuality = 0f;
            int offTrackNodes = 0;
            float exitRiskMax = 0f;
            float totalSpeedScore = 0f;
            float totalDistanceScore = 0f;
            float totalDirectionScore = 0f;
            float totalReturnToAsphaltScore = 0f; // Track return to asphalt score
            int asphaltNodes = 0; // Count nodes that are on asphalt
            
            foreach (var node in path.Nodes)
            {
                totalScore += node.Score;
                
                // Track the minimum score in this path
                if (node.Score < minScore)
                {
                    minScore = node.Score;
                }
                
                // Track terrain quality 
                totalTerrainQuality += node.TerrainQuality;
                if (node.TerrainQuality < 0.1f) // Using 0.1 as threshold
                {
                    offTrackNodes++;
                }
                
                // Count asphalt nodes (TerrainQuality > 0.9)
                if (node.TerrainQuality > 0.9f)
                {
                    asphaltNodes++;
                }
                
                // Track maximum exit risk
                if (node.TrackExitRisk > exitRiskMax)
                {
                    exitRiskMax = node.TrackExitRisk;
                }
                
                // Track speed scores
                if (node.EvaluationFactors.TryGetValue("Speed", out float speedScore))
                {
                    totalSpeedScore += speedScore;
                }
                
                // Track distance and direction scores
                totalDistanceScore += node.DistanceScore;
                totalDirectionScore += node.DirectionScore;
                
                // Track return to asphalt score if available
                if (node.EvaluationFactors.TryGetValue("ReturnToAsphalt", out float returnScore))
                {
                    totalReturnToAsphaltScore += returnScore;
                }
            }
            
            // Calculate average scores
            float averageScore = totalScore / path.Nodes.Count;
            float averageTerrainQuality = totalTerrainQuality / path.Nodes.Count;
            float averageSpeedScore = totalSpeedScore / path.Nodes.Count;
            float averageDistanceScore = totalDistanceScore / path.Nodes.Count;
            float averageDirectionScore = totalDirectionScore / path.Nodes.Count;
            float averageReturnToAsphaltScore = path.Nodes.Count > 0 ? totalReturnToAsphaltScore / path.Nodes.Count : 0;
            
            // Store scores in the path
            path.TotalScore = totalScore;
            path.AverageScore = averageScore;
            path.MinNodeScore = minScore;
            path.AverageTerrainQuality = averageTerrainQuality;
            path.OffTrackNodeCount = offTrackNodes;
            path.TrackExitRisk = exitRiskMax;
            path.AverageSpeed = averageSpeedScore;
            
            // ENHANCED GRAVEL HANDLING: Apply bonuses for paths that lead to asphalt when on gravel
            if (isOnGravel)
            {
                // Check if this path reaches asphalt at any point
                if (asphaltNodes > 0)
                {
                    // Apply bonus based on how quickly the path reaches asphalt
                    float asphaltBonus = 0.0f;
                    
                    // Find the first node that's on asphalt
                    for (int i = 0; i < path.Nodes.Count; i++)
                    {
                        if (path.Nodes[i].TerrainQuality > 0.9f)
                        {
                            // Earlier asphalt nodes get a bigger bonus
                            asphaltBonus = 0.3f * (1.0f - (float)i / path.Nodes.Count);
                            
                            Debug.Log($"Path reaches asphalt at node {i}/{path.Nodes.Count}, bonus: {asphaltBonus}");
                            break;
                        }
                    }
                    
                    // Apply the bonus to the path score
                    path.AverageScore += asphaltBonus;
                    path.TotalScore += asphaltBonus * path.Nodes.Count;
                    
                    // Mark paths that reach asphalt as good quality at minimum
                    if (path.Quality == PathQuality.Bad || path.Quality == PathQuality.Unknown)
                    {
                        path.Quality = PathQuality.Medium;
                    }
                    
                    Debug.Log($"Applied asphalt bonus to path. New score: {path.AverageScore}");
                }
                
                // If the path has a good return to asphalt score, boost it further
                if (averageReturnToAsphaltScore > 0.7f)
                {
                    float returnBonus = (averageReturnToAsphaltScore - 0.7f) * 0.25f;
                    path.AverageScore += returnBonus;
                    path.TotalScore += returnBonus * path.Nodes.Count;
                    
                    Debug.Log($"Applied additional return to asphalt bonus: {returnBonus}");
                }
            }
            // Give finish line paths a special bonus if their distance score is good
            else if (targetingFinishLine && path.Nodes.Count > 0)
            {
                // Get terminal node's distance score
                if (path.Nodes[path.Nodes.Count - 1].EvaluationFactors.TryGetValue("FinishLine", out float finishScore) && 
                    finishScore > 0.7f)
                {
                    // Apply a bonus to the path's average score based on terminal finish line score
                    float bonus = (finishScore - 0.7f) * 0.3f; // Up to 0.09 bonus
                    path.AverageScore += bonus;
                    
                    // Also apply bonus to the total score
                    path.TotalScore += bonus * path.Nodes.Count;
                }
            }
            // Special bonuses for checkpoint targeting
            else if (!targetingFinishLine && path.Nodes.Count > 0)
            {
                // Strong bonus for paths with consistently good distance and direction scores
                if (averageDistanceScore > 0.7f && averageDirectionScore > 0.7f)
                {
                    float bonus = 0.15f; // Substantial bonus for well-directed paths
                    path.AverageScore += bonus;
                    path.TotalScore += bonus * path.Nodes.Count;
                }
                
                // Extra bonus for terminal nodes with particularly good distance
                float terminalDistanceScore = path.Nodes[path.Nodes.Count - 1].DistanceScore;
                if (terminalDistanceScore > 0.8f)
                {
                    float bonus = 0.1f; // Bonus for ending close to target
                    path.AverageScore += bonus;
                    path.TotalScore += bonus * path.Nodes.Count;
                }
            }
            
            // Check for dead end (path that likely leads off track)
            if (exitRiskMax > 0.8f || offTrackNodes > path.Nodes.Count / 2)
            {
                path.HasDeadEnd = true;
            }
            
            // Determine path quality for visualization
            if (averageScore >= 0.8f)
            {
                path.Quality = PathQuality.Good;
            }
            else if (averageScore >= 0.5f)
            {
                path.Quality = PathQuality.Medium;
            }
            else
            {
                path.Quality = PathQuality.Bad;
            }
        }

        /// <summary>
        /// Select the best path from all generated paths
        /// </summary>
        public Path SelectBestPath(List<Path> generatedPaths, bool enableAggressivePruning)
        {
            if (generatedPaths.Count == 0)
            {
                Debug.LogWarning("No paths generated to select from!");
                return null;
            }
            
            // First, filter out paths with high risk/dead ends if we have enough options
            List<Path> viablePaths = generatedPaths;
            
            // ENHANCED GRAVEL HANDLING: Special filtering for gravel/asphalt cases
            if (isOnGravel && generatedPaths.Count > 3)
            {
                // When on gravel, prioritize paths that reach asphalt quickly
                List<Path> asphaltPaths = generatedPaths
                    .Where(p => p.Nodes.Count > 0 && 
                           p.Nodes.Any(n => n.TerrainQuality > 0.9f)) // Any node is on asphalt
                    .ToList();
                
                if (asphaltPaths.Count > 0)
                {
                    Debug.Log($"Found {asphaltPaths.Count} paths that reach asphalt. Using these for selection.");
                    viablePaths = asphaltPaths;
                }
                else
                {
                    // No paths reach asphalt directly, so find paths with good ReturnToAsphalt scores
                    List<Path> recoveryPaths = generatedPaths
                        .Where(p => p.Nodes.Count > 0 && 
                               p.Nodes[0].EvaluationFactors.TryGetValue("ReturnToAsphalt", out float score) && 
                               score > 0.6f)
                        .ToList();
                        
                    if (recoveryPaths.Count > 0)
                    {
                        Debug.Log($"No direct asphalt paths. Using {recoveryPaths.Count} recovery paths with good scores.");
                        viablePaths = recoveryPaths;
                    }
                }
            }
            // Use a different filtering approach when targeting the finish line
            else if (targetingFinishLine && generatedPaths.Count > 5)
            {
                // When targeting finish, prioritize paths with good distance scores
                List<Path> finishTargetingPaths = generatedPaths
                    .Where(p => p.Nodes.Count > 0 && 
                           p.Nodes[p.Nodes.Count - 1].DistanceScore > 0.7f &&
                           p.AverageTerrainQuality > 0.6f) // Still need decent terrain
                    .ToList();
                
                // If we have any good finish-targeting paths, use those
                if (finishTargetingPaths.Count > 0)
                {
                    Debug.Log($"Using {finishTargetingPaths.Count} specialized finish line targeting paths");
                    viablePaths = finishTargetingPaths;
                }
                else if (generatedPaths.Count > 10 && enableAggressivePruning)
                {
                    // Otherwise use normal filtering
                    viablePaths = generatedPaths
                        .Where(p => !p.HasDeadEnd && p.TrackExitRisk < 0.85f)
                        .ToList();
                }
            }
            // Regular checkpoint targeting - prioritize direction toward checkpoint
            else if (!targetingFinishLine && generatedPaths.Count > 5)
            {
                List<Path> checkpointTargetingPaths = generatedPaths
                    .Where(p => p.Nodes.Count > 0 && 
                           p.Nodes[0].DistanceScore > 0.6f && // First node must move toward checkpoint
                           p.Nodes[0].DirectionScore > 0.6f && // First node must face toward checkpoint
                           p.AverageTerrainQuality > 0.7f)    // Need good terrain
                    .ToList();
                    
                if (checkpointTargetingPaths.Count > 0)
                {
                    Debug.Log($"Using {checkpointTargetingPaths.Count} specialized checkpoint targeting paths");
                    viablePaths = checkpointTargetingPaths;
                }
                else if (generatedPaths.Count > 10 && enableAggressivePruning)
                {
                    viablePaths = generatedPaths
                        .Where(p => !p.HasDeadEnd && p.TrackExitRisk < 0.85f)
                        .ToList();
                }
            }
            else if (generatedPaths.Count > 10 && enableAggressivePruning)
            {
                viablePaths = generatedPaths
                    .Where(p => !p.HasDeadEnd && p.TrackExitRisk < 0.85f)
                    .ToList();
            }
                    
            // If we filtered too aggressively and have no viable paths, fall back
            if (viablePaths.Count == 0)
            {
                Debug.Log("Path filter was too aggressive, falling back to all generated paths");
                viablePaths = generatedPaths;
            }
            
            // Safety check to make sure we have at least one path
            if (viablePaths.Count == 0)
            {
                Debug.LogError("Critical error: No viable paths available after filtering!");
                return null;
            }
            
            // Sort paths differently based on current surface and targeting
            List<Path> sortedPaths;
            
            if (isOnGravel)
            {
                // ENHANCED GRAVEL HANDLING: When on gravel, prioritize paths that lead to asphalt
                sortedPaths = viablePaths
                    .OrderByDescending(p => 
                        // Check if any node in the path is on asphalt (terrain quality > 0.9)
                        p.Nodes.Any(n => n.TerrainQuality > 0.9f) ? 1.0f : 0.0f)
                    .ThenByDescending(p => 
                        // Then consider the first node's return to asphalt score
                        p.Nodes.Count > 0 && 
                        p.Nodes[0].EvaluationFactors.TryGetValue("ReturnToAsphalt", out float score) ? 
                        score : 0.0f)
                    .ThenByDescending(p => p.AverageScore) // Then use average score
                    .ThenByDescending(p => p.AverageTerrainQuality) // Prefer paths that have better terrain
                    .ToList();
                
                Debug.Log("Sorting paths with gravel recovery priority");
            }
            else if (targetingFinishLine)
            {
                // When targeting finish line, prioritize distance and direction heavily
                sortedPaths = viablePaths
                    .OrderByDescending(p => 
                        p.AverageScore + 
                        (p.Nodes.Count > 0 ? p.Nodes[p.Nodes.Count - 1].DistanceScore * 0.5f : 0) +
                        (p.Nodes.Count > 0 ? p.Nodes[p.Nodes.Count - 1].DirectionScore * 0.3f : 0) -
                        (p.TrackExitRisk * 0.5f))
                    .ThenByDescending(p => p.AverageTerrainQuality) // Prefer paths that stay on track
                    .ToList();
                
                Debug.Log("Sorting paths with finish line targeting priority");
            }
            else
            {
                // Enhanced sorting for checkpoints - strongly prioritize first move direction and distance
                sortedPaths = viablePaths
                    .OrderByDescending(p => 
                        p.AverageScore + 
                        (p.Nodes.Count > 0 ? p.Nodes[0].DirectionScore * 0.7f : 0) + // Heavily weight initial direction
                        (p.Nodes.Count > 0 ? p.Nodes[0].DistanceScore * 0.5f : 0) +  // Weight initial distance improvement
                        (p.AverageTerrainQuality * 0.3f) - 
                        (p.TrackExitRisk * 0.5f) + 
                        (p.AverageSpeed * 0.2f))
                    .ThenByDescending(p => p.MinNodeScore)
                    .ToList();
                
                Debug.Log("Sorting paths with checkpoint targeting priority");
            }
            
            // Select the best path
            Path bestPath = sortedPaths[0];
            bestPath.Quality = PathQuality.Best;  // Mark as the selected best path
            
            // Log details about the best path
            var bestNode = bestPath.Nodes.Count > 0 ? bestPath.Nodes[0] : null;
            string scoreDetails = "";
            
            if (bestNode != null && bestNode.EvaluationFactors != null)
            {
                scoreDetails = "Scores: ";
                foreach (var kvp in bestNode.EvaluationFactors)
                {
                    scoreDetails += $"{kvp.Key}={kvp.Value:F2}, ";
                }
            }
            
            Debug.Log($"Selected best path with avg score: {bestPath.AverageScore:F2}, " +
                     $"min node score: {bestPath.MinNodeScore:F2}, " +
                     $"quality: {bestPath.Quality}, " +
                     $"terrain: {bestPath.AverageTerrainQuality:F2}, " +
                     $"exit risk: {bestPath.TrackExitRisk:F2}, " +
                     $"speed: {bestPath.AverageSpeed:F2}, " +
                     $"length: {bestPath.Nodes.Count}, " +
                     $"targeting finish: {(targetingFinishLine ? "YES" : "NO")}, " +
                     $"on gravel: {(isOnGravel ? "YES" : "NO")}. " +
                     scoreDetails);
                     
            return bestPath;
        }

        /// <summary>
        /// Evaluate how a position improves distance to target
        /// </summary>
        private float EvaluateDistance(Vector2 moveWorldPos, Vector2 currentWorldPos)
        {
            // Current distance to target
            float currentDistance = Vector2.Distance(currentWorldPos, targetPosition);
            
            // New distance to target after the move
            float newDistance = Vector2.Distance(moveWorldPos, targetPosition);
            
            // Calculate improvement in distance
            float improvement = (currentDistance - newDistance) / currentDistance;
            
            // ENHANCED GRAVEL HANDLING: Even stronger bias toward improvement when on gravel
            if (isOnGravel)
            {
                // Check if this move is toward the nearest asphalt
                Vector2 nearestAsphalt = terrainAnalyzer.FindNearestAsphaltPosition(currentWorldPos);
                float distToAsphalt = Vector2.Distance(currentWorldPos, nearestAsphalt);
                float newDistToAsphalt = Vector2.Distance(moveWorldPos, nearestAsphalt);
                float asphaltImprovement = (distToAsphalt - newDistToAsphalt) / distToAsphalt;
                
                // Blend between asphalt direction and target direction
                // Heavily weight asphalt direction when on gravel
                return Mathf.Clamp01((asphaltImprovement * 2.5f + improvement * 0.5f) / 3.0f + 0.5f);
            }
            // Enhanced scoring - stronger bias toward improvement for checkpoint targeting
            else if (!targetingFinishLine)
            {
                return Mathf.Clamp01((improvement * 2.0f) + 0.5f); // Stronger bias for checkpoints
            }
            else
            {
                // Default bias for finish line targeting
                return Mathf.Clamp01((improvement * 1.5f) + 0.5f);
            }
        }

        /// <summary>
        /// Evaluate a direct approach to the target - specialized for finish line
        /// </summary>
        private float EvaluateDirectApproachToTarget(Vector2 moveWorldPos, Vector2 currentWorldPos, Vector2Int velocity)
        {
            // Get normalized direction vector from move position to target
            Vector2 toTarget = (targetPosition - moveWorldPos).normalized;
            
            // Get normalized velocity vector
            Vector2 velocityDir = new Vector2(velocity.x, velocity.y).normalized;
            
            // 1. Alignment between velocity and target direction
            float alignment = Vector2.Dot(velocityDir, toTarget);
            
            // Convert from [-1, 1] to [0, 1] range
            float alignmentScore = (alignment + 1f) * 0.5f;
            
            // 2. Distance improvement from current position
            float currentDistance = Vector2.Distance(currentWorldPos, targetPosition);
            float newDistance = Vector2.Distance(moveWorldPos, targetPosition);
            float distImprovement = (currentDistance - newDistance) / currentDistance;
            
            // Clamp and bias towards improvement
            float distanceScore = Mathf.Clamp01((distImprovement * 1.8f) + 0.5f);
            
            // 3. Calculate straight-line path quality
            // Check terrain along the direct line to the target
            float pathQuality = CalculatePathQualityToFinish(moveWorldPos);
            
            // Combine scores with emphasis on alignment for finish line approach
            return (alignmentScore * 0.5f) + (distanceScore * 0.3f) + (pathQuality * 0.2f);
        }

        /// <summary>
        /// Calculate terrain quality along path to finish line
        /// </summary>
        private float CalculatePathQualityToFinish(Vector2 fromPosition)
        {
            // Sample points along the path to the finish
            Vector2 toTarget = targetPosition - fromPosition;
            float distance = toTarget.magnitude;
            Vector2 direction = toTarget.normalized;
            
            // Don't sample too many points
            int sampleCount = Mathf.Min(5, Mathf.FloorToInt(distance));
            if (sampleCount <= 0) return 1.0f; // Already at target
            
            float stepSize = distance / sampleCount;
            
            // Check terrain at each sample point
            float totalQuality = 0f;
            int pointsChecked = 0;
            
            for (int i = 1; i <= sampleCount; i++)
            {
                Vector2 samplePos = fromPosition + (direction * stepSize * i);
                float terrainQuality = terrainAnalyzer.EvaluateTerrain(samplePos);
                
                // Weight closer points more heavily
                float weight = 1.0f - ((float)i / sampleCount) * 0.5f;
                totalQuality += terrainQuality * weight;
                pointsChecked += 1;
            }
            
            // Return average terrain quality
            return pointsChecked > 0 ? totalQuality / pointsChecked : 0.5f;
        }

        /// <summary>
        /// Evaluate future positioning relative to next checkpoint
        /// </summary>
        private float EvaluateFuturePositioning(Vector2 finalPosition, Vector2 currentPos)
        {
            // Try to look ahead to the next checkpoint
            Checkpoint nextCheckpoint = LookAheadToNextCheckpoint();
            if (nextCheckpoint == null)
                return 0.5f;
            
            // Get position of next checkpoint
            Vector2 nextCheckpointPos = nextCheckpoint.transform.position;
            
            // Calculate current distance to next checkpoint
            float currentDistanceToNext = Vector2.Distance(currentPos, nextCheckpointPos);
            
            // Calculate distance from final position to next checkpoint
            float finalDistanceToNext = Vector2.Distance(finalPosition, nextCheckpointPos);
            
            // Calculate improvement factor
            float improvement = (currentDistanceToNext - finalDistanceToNext) / currentDistanceToNext;
            
            // Normalize to 0-1 range
            return Mathf.Clamp01((improvement * 1.5f) + 0.5f);
        }

        /// <summary>
        /// Evaluate how a move affects speed
        /// </summary>
        private float EvaluateSpeed(Vector2Int movePos, Vector2Int newVelocity, Vector2 currentPos)
        {
            // Convert to world position to check terrain
            Vector2 moveWorldPos = GridToWorldPosition(movePos);
            
            // Calculate speed from velocity (used in both cases)
            float newSpeed = newVelocity.magnitude / PlayerMovement.GRID_SCALE;
            
            // ENHANCED GRAVEL HANDLING: Check if current position is on gravel (not just destination)
            bool currentlyOnGravel = isOnGravel;
            
            // Check if this move's destination is on gravel
            float terrainQuality = terrainAnalyzer.EvaluateTerrain(moveWorldPos);
            bool moveToGravel = terrainQuality < 0.5f; // Less than 0.5 indicates gravel or worse
            
            // If currently on gravel OR moving onto gravel, speed is locked to 1
            if (currentlyOnGravel || moveToGravel)
            {
                // Speed must be 1 exactly when on gravel
                float gravelSpeed = 1.0f;
                
                if (newSpeed > gravelSpeed + 0.1f) // Adding small tolerance
                {
                    // Severely penalize paths that try to go faster than 1 on gravel
                    // This is physically impossible since the game will cap it
                    return 0.1f;
                }
                else if (newSpeed < gravelSpeed - 0.1f)
                {
                    // Penalize going slower than necessary on gravel
                    // We want to maintain speed=1 exactly for efficiency
                    return 0.3f;
                }
                else
                {
                    // Proper speed=1 for gravel
                    return 0.9f; // Near perfect score for proper gravel speed
                }
            }
            
            // Normal speed evaluation for non-gravel terrain
            
            // Use turn factor to determine ideal speed
            Vector2 moveDir = new Vector2(newVelocity.x, newVelocity.y);
            if (moveDir.magnitude > 0.01f)
                moveDir.Normalize();
                
            float turnFactor = terrainAnalyzer.GetTurnFactor(currentPos, moveDir);
            
            // Adjust turn factor when targeting finish line to allow higher speeds
            if (targetingFinishLine)
            {
                // When targeting finish line, we can accept higher speeds
                turnFactor *= 0.8f; // Reduce turn factor by 20%
            }
            
            // Make a more aggressive blend for target speed in corners
            float targetSpeed = Mathf.Lerp(maxStraightSpeed, maxTurnSpeed, turnFactor * 0.7f);
            
            // Score based on how close to target speed
            float difference = Mathf.Abs(newSpeed - targetSpeed);
            float baseScore = Mathf.Clamp01(1.0f - (difference / targetSpeed));
            
            // Bonus for higher speeds when not in a sharp turn
            if (turnFactor < 0.5f && newSpeed > targetSpeed)
            {
                float speedBonus = Mathf.Min(0.25f, (newSpeed - targetSpeed) * 0.08f);
                return Mathf.Clamp01(baseScore + speedBonus);
            }
            
            // Even in turns, give a small bonus for maintaining momentum
            if (turnFactor >= 0.5f && newSpeed >= 0.8f * targetSpeed)
            {
                float momentumBonus = 0.1f;
                return Mathf.Clamp01(baseScore + momentumBonus);
            }
            
            return baseScore;
        }

        /// <summary>
        /// Evaluate direction alignment with target
        /// </summary>
        private float EvaluateDirection(Vector2Int velocity)
        {
            // Direction to target from current position
            Vector2 toTarget = targetPosition - Vector2.zero; // We're evaluating relative to origin
            if (toTarget.magnitude < 0.01f)
                return 1.0f; // Already at target
            
            toTarget.Normalize();
            
            // Direction of velocity
            Vector2 moveDir = new Vector2(velocity.x, velocity.y);
            if (moveDir.magnitude < 0.01f)
                return 0.5f; // Not moving
                
            moveDir.Normalize();
            
            // Calculate alignment between move and target direction
            float dotProduct = Vector2.Dot(moveDir, toTarget);
            
            // ENHANCED GRAVEL HANDLING: When on gravel, direction is more weighted toward nearest asphalt
            if (isOnGravel)
            {
                // Find direction to nearest asphalt
                Vector2 nearestAsphalt = terrainAnalyzer.FindNearestAsphaltPosition(Vector2.zero);
                Vector2 toAsphalt = (nearestAsphalt - Vector2.zero).normalized;
                
                // Calculate alignment with asphalt direction
                float asphaltAlignment = Vector2.Dot(moveDir, toAsphalt);
                
                // Blend between asphalt alignment and target alignment
                // When on gravel, we care more about getting to asphalt than the target
                dotProduct = asphaltAlignment * 0.8f + dotProduct * 0.2f;
                
                // Log that we're calculating direction with asphalt in mind
                Debug.Log($"On gravel, direction score blends target and asphalt alignment: {dotProduct}");
            }
            // Apply different scaling based on targeting mode
            else if (targetingFinishLine)
            {
                // When targeting finish line, increase penalty for non-alignment
                if (dotProduct < 0.3f)
                {
                    // Add extra penalty for very poor alignment when targeting finish line
                    dotProduct *= 0.8f;
                }
            }
            else
            {
                // For checkpoints, apply even stronger penalties for wrong direction
                if (dotProduct < 0.0f)
                {
                    // Severe penalty for moving away from checkpoint
                    dotProduct *= 1.5f;
                }
            }
            
            // Normalize to 0-1 range
            return (dotProduct + 1.0f) * 0.5f;
        }

        /// <summary>
        /// Get the next checkpoint after the current target
        /// </summary>
        private Checkpoint LookAheadToNextCheckpoint()
        {
            if (checkpointManager == null || targetCheckpoint == null) 
                return null;
            
            // Get all checkpoints
            List<Checkpoint> allCheckpoints = checkpointManager.GetAllCheckpoints();
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
        /// Convert grid position to world position
        /// </summary>
        private Vector2 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector2(
                gridPos.x / (float)PlayerMovement.GRID_SCALE,
                gridPos.y / (float)PlayerMovement.GRID_SCALE
            );
        }
    }
}