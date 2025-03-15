using UnityEngine;
using System.Collections.Generic;

namespace DeltaVector.AI
{
    /// <summary>
    /// Handles terrain analysis and track position evaluation for AI pathfinding
    /// </summary>
    public class AITerrainAnalyzer
    {
        // Reference to asphalt and gravel tilemaps
        private UnityEngine.Tilemaps.Tilemap asphaltTilemap;
        private UnityEngine.Tilemaps.Tilemap gravelTilemap;
        
        // Caches for performance
        private Dictionary<Vector2Int, float> terrainQualityCache = new Dictionary<Vector2Int, float>();
        private Dictionary<Vector2Int, bool> trackCenterCache = new Dictionary<Vector2Int, bool>();
        private Dictionary<Vector2Int, bool> exitTrackCache = new Dictionary<Vector2Int, bool>();
        
        // Target position for direction calculations
        private Vector2 targetPosition;

        // Nearest asphalt position cache
        private Dictionary<Vector2Int, Vector2> nearestAsphaltCache = new Dictionary<Vector2Int, Vector2>();
        
        /// <summary>
        /// Initialize the terrain analyzer
        /// </summary>
        public AITerrainAnalyzer(Vector2 targetPos)
        {
            targetPosition = targetPos;
            InitializeTilemaps();
        }

        /// <summary>
        /// Update the target position
        /// </summary>
        public void SetTargetPosition(Vector2 newTarget)
        {
            targetPosition = newTarget;
        }

        /// <summary>
        /// Clear all caches
        /// </summary>
        public void ClearCaches()
        {
            terrainQualityCache.Clear();
            trackCenterCache.Clear();
            exitTrackCache.Clear();
            nearestAsphaltCache.Clear();
        }

        /// <summary>
        /// Initialize tilemap references
        /// </summary>
        private void InitializeTilemaps()
        {
            GameObject asphaltObject = GameObject.FindGameObjectWithTag("Asphalt");
            GameObject gravelObject = GameObject.FindGameObjectWithTag("Gravel");
            
            if (asphaltObject != null)
                asphaltTilemap = asphaltObject.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            
            if (gravelObject != null)
                gravelTilemap = gravelObject.GetComponent<UnityEngine.Tilemaps.Tilemap>();
        }

        /// <summary>
        /// Evaluate terrain quality at a position
        /// </summary>
        public float EvaluateTerrain(Vector2 moveWorldPos)
        {
            // Check cache first for performance
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(moveWorldPos.x * 10f),
                Mathf.FloorToInt(moveWorldPos.y * 10f)
            );
            
            if (terrainQualityCache.TryGetValue(gridPos, out float cachedQuality))
            {
                return cachedQuality;
            }
            
            // Convert world position to tilemap position
            Vector3Int tilePos = new Vector3Int(
                Mathf.FloorToInt(moveWorldPos.x),
                Mathf.FloorToInt(moveWorldPos.y),
                0
            );
            
            // Check terrain type
            float quality = 0.5f; // Default
            
            if (asphaltTilemap != null && gravelTilemap != null)
            {
                bool hasAsphalt = asphaltTilemap.HasTile(tilePos);
                bool hasGravel = gravelTilemap.HasTile(tilePos);
                
                // Prioritize asphalt over gravel or no tiles
                if (hasAsphalt)
                    quality = 1.0f;  // Asphalt is present - best terrain
                else if (hasGravel)
                    quality = 0.2f;  // Gravel only - severely penalized
                else
                    quality = 0.05f;  // No tiles - extremely severe penalty
            }
            
            // Cache the result
            terrainQualityCache[gridPos] = quality;
            
            return quality;
        }

        /// <summary>
        /// Evaluate how central a position is on the track
        /// </summary>
        public float EvaluateTrackCenter(Vector2 moveWorldPos)
        {
            // If no asphalt tilemap, can't evaluate
            if (asphaltTilemap == null)
                return 0.5f;
            
            // Convert world position to tilemap position
            Vector3Int centerTilePos = new Vector3Int(
                Mathf.FloorToInt(moveWorldPos.x),
                Mathf.FloorToInt(moveWorldPos.y),
                0
            );
            
            // Check cache for this position
            Vector2Int gridPos = new Vector2Int(centerTilePos.x, centerTilePos.y);
            
            if (trackCenterCache.TryGetValue(gridPos, out bool isTrackCenter))
            {
                return isTrackCenter ? 1.0f : 0.5f;
            }
            
            // First check if on asphalt
            bool isOnAsphalt = asphaltTilemap.HasTile(centerTilePos);
            if (!isOnAsphalt)
            {
                trackCenterCache[gridPos] = false;
                return 0.0f; // Not on track at all
            }
            
            // Count asphalt tiles in surrounding area
            int asphaltCount = 0;
            int totalChecked = 0;
            int radius = 3; // Detection radius
            
            // Check surrounding tiles
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Skip center tile
                    if (x == 0 && y == 0)
                        continue;
                    
                    // Only check tiles in radius
                    float distance = Mathf.Sqrt(x*x + y*y);
                    if (distance > radius)
                        continue;
                    
                    totalChecked++;
                    
                    Vector3Int checkTilePos = new Vector3Int(
                        centerTilePos.x + x,
                        centerTilePos.y + y,
                        0
                    );
                    
                    if (asphaltTilemap.HasTile(checkTilePos))
                    {
                        asphaltCount++;
                    }
                }
            }
            
            // Calculate center score
            if (totalChecked == 0)
            {
                trackCenterCache[gridPos] = false;
                return 0.5f;
            }
                
            float centerScore = (float)asphaltCount / totalChecked;
            bool isCenterTrack = centerScore > 0.7f;
            trackCenterCache[gridPos] = isCenterTrack;
            
            return Mathf.Pow(centerScore, 0.7f); // Adjust emphasis
        }

        /// <summary>
        /// Evaluate risk of exiting the track
        /// </summary>
        public float EvaluateTrackExit(Vector2 moveWorldPos, Vector2Int velocity)
        {
            // If no asphalt tilemap, can't evaluate
            if (asphaltTilemap == null)
                return 0.5f;
                
            // Check if current position is on asphalt
            Vector3Int currentTilePos = new Vector3Int(
                Mathf.FloorToInt(moveWorldPos.x),
                Mathf.FloorToInt(moveWorldPos.y),
                0
            );
            
            bool isOnAsphalt = asphaltTilemap.HasTile(currentTilePos);
            
            // Already off track
            if (!isOnAsphalt)
                return 0.0f;
                
            // Create normalized velocity direction
            Vector2 velocityDir = new Vector2(velocity.x, velocity.y).normalized;
            
            // Create unique key for this position & velocity to check cache
            Vector2Int posKey = new Vector2Int(
                Mathf.RoundToInt(moveWorldPos.x * 10),
                Mathf.RoundToInt(moveWorldPos.y * 10)
            );
            
            Vector2Int velKey = new Vector2Int(
                Mathf.RoundToInt(velocityDir.x * 100),
                Mathf.RoundToInt(velocityDir.y * 100)
            );
            
            Vector2Int cacheKey = new Vector2Int(
                posKey.x ^ velKey.x,
                posKey.y ^ velKey.y
            );
            
            // Check cache
            if (exitTrackCache.TryGetValue(cacheKey, out bool willExitTrack))
            {
                return willExitTrack ? 0.3f : 1.0f;
            }
            
            // Look ahead a few steps in velocity direction to check for track exit
            bool foundExit = false;
            float exitScore = 1.0f;
            
            for (int steps = 1; steps <= 3; steps++)
            {
                Vector2 projectedPos = moveWorldPos + (velocityDir * steps);
                Vector3Int projectedTilePos = new Vector3Int(
                    Mathf.FloorToInt(projectedPos.x),
                    Mathf.FloorToInt(projectedPos.y),
                    0
                );
                
                // Check if this position is off track
                bool projectedOnAsphalt = asphaltTilemap.HasTile(projectedTilePos);
                
                if (!projectedOnAsphalt)
                {
                    // Calculate a risk factor based on how soon we exit
                    // Earlier exits are more dangerous
                    float riskFactor = 0.8f - (steps / 5.0f);
                    foundExit = true;
                    exitScore = 1.0f - riskFactor; // Invert so lower score means higher risk
                    break;
                }
            }
            
            // Cache the result
            exitTrackCache[cacheKey] = foundExit;
            
            return exitScore;
        }

        /// <summary>
        /// Calculate the risk of exiting the track in the next few moves
        /// </summary>
        public float CalculateTrackExitRisk(Vector2 position, Vector2Int velocity, int lookAheadSteps)
        {
            // Normalize the velocity to get direction
            Vector2 velocityDir = new Vector2(velocity.x, velocity.y).normalized;
            
            // Risk starts at 0
            float totalRisk = 0f;
            float maxRisk = 0f;
            
            // Look ahead multiple steps
            for (int step = 1; step <= lookAheadSteps; step++)
            {
                // Project position for this step
                Vector2 projectedPosition = position + (velocityDir * step);
                
                // Check terrain quality at projected position
                float terrainQuality = EvaluateTerrain(projectedPosition);
                
                // Calculate risk based on inverse terrain quality and distance
                float stepRisk = (1 - terrainQuality) / step;
                totalRisk += stepRisk;
                
                // Track maximum risk found
                if (stepRisk > maxRisk)
                {
                    maxRisk = stepRisk;
                }
                
                // If very bad terrain detected, increase risk significantly
                if (terrainQuality < 0.1f)
                {
                    totalRisk += (1.0f / step) * 2;
                }
            }
            
            // Normalize risk to 0-1 range
            return Mathf.Clamp01((totalRisk / lookAheadSteps) * 2 + maxRisk * 0.5f);
        }

        /// <summary>
        /// Get turn factor for evaluating turns and racing lines
        /// </summary>
        public float GetTurnFactor(Vector2 currentPos, Vector2 currentDir)
        {
            // If barely moving, default to moderate turn factor
            if (currentDir.magnitude < 0.1f)
                return 0.5f;
                
            currentDir.Normalize();
            
            // Direction to target
            Vector2 toTarget = targetPosition - currentPos;
            float distanceToTarget = toTarget.magnitude;
            toTarget.Normalize();
            
            // Regular turn factor based on alignment with target
            float alignment = Vector2.Dot(currentDir, toTarget);
            
            // Convert from dot product (-1 to 1) to turn factor (0 to 1)
            // 1 = perfectly aligned (straight), -1 = opposite direction (sharp turn)
            return Mathf.Clamp01((1f - alignment) * 0.4f);
        }

        /// <summary>
        /// Determine if approaching a turn by checking direction to target
        /// </summary>
        public bool IsApproachingTurn(Vector2 currentPos, Vector2 currentDir)
        {
            // If barely moving, not approaching a turn
            if (currentDir.magnitude < 0.1f)
                return false;
                
            currentDir.Normalize();
            
            // Direction to target
            Vector2 toTarget = targetPosition - currentPos;
            toTarget.Normalize();
            
            // Check direction alignment with current target
            float alignment = Vector2.Dot(currentDir, toTarget);
            
            // If alignment less than 0.5 (about 60 degrees), approaching a turn
            return alignment < 0.5f;
        }

        /// <summary>
        /// Find the nearest asphalt position from the current position
        /// ENHANCED to provide better asphalt search
        /// </summary>
        public Vector2 FindNearestAsphaltPosition(Vector2 fromPosition, float searchRadius = 8.0f)
        {
            // If no asphalt tilemap available, return original position
            if (asphaltTilemap == null) return fromPosition;
            
            // Check cache first
            Vector2Int cacheKey = new Vector2Int(
                Mathf.RoundToInt(fromPosition.x * 5f), // Lower precision cache key
                Mathf.RoundToInt(fromPosition.y * 5f)
            );
            
            if (nearestAsphaltCache.TryGetValue(cacheKey, out Vector2 cachedAsphaltPos))
            {
                return cachedAsphaltPos;
            }
            
            // Convert world position to tilemap position
            Vector3Int currentTilePos = new Vector3Int(
                Mathf.FloorToInt(fromPosition.x),
                Mathf.FloorToInt(fromPosition.y),
                0
            );
            
            // Check if already on asphalt
            if (asphaltTilemap.HasTile(currentTilePos))
            {
                // Cache and return current position
                nearestAsphaltCache[cacheKey] = fromPosition;
                return fromPosition; // Already on asphalt
            }
            
            // Search in expanding grid pattern
            Vector2 nearestAsphaltPos = fromPosition;
            float nearestDistance = float.MaxValue;
            
            // Start with smaller radius and expand if needed
            int maxSearchRadius = Mathf.CeilToInt(searchRadius);
            
            // ENHANCED: Use spiral search pattern for more efficient search
            for (int radius = 1; radius <= maxSearchRadius; radius++)
            {
                bool foundAsphalt = false;
                
                // Search in a spiral pattern for more efficiency
                for (int layer = 1; layer <= 8; layer++)
                {
                    int step = radius * layer / 8; // Divide the radius into 8 layers
                    
                    // Check each layer in a circle around the car
                    for (int angle = 0; angle < 360; angle += 360 / (8 * radius)) // More points for larger radius
                    {
                        float radians = angle * Mathf.Deg2Rad;
                        int x = Mathf.RoundToInt(step * Mathf.Cos(radians));
                        int y = Mathf.RoundToInt(step * Mathf.Sin(radians));
                        
                        Vector3Int checkTilePos = new Vector3Int(
                            currentTilePos.x + x,
                            currentTilePos.y + y,
                            0
                        );
                        
                        // Check if this position has asphalt
                        if (asphaltTilemap.HasTile(checkTilePos))
                        {
                            // Convert to world position
                            Vector2 worldPos = new Vector2(
                                checkTilePos.x + 0.5f, // Center of tile
                                checkTilePos.y + 0.5f
                            );
                            
                            // Calculate distance
                            float distance = Vector2.Distance(fromPosition, worldPos);
                            
                            // If this is closer than current nearest
                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearestAsphaltPos = worldPos;
                                foundAsphalt = true;
                            }
                        }
                    }
                }
                
                // If we found asphalt, we can stop searching
                if (foundAsphalt)
                {
                    Debug.Log($"Found nearest asphalt at distance {nearestDistance} at radius {radius}");
                    break;
                }
            }
            
            // ENHANCED: If we didn't find anything, do a more thorough search
            if (nearestDistance == float.MaxValue)
            {
                Debug.LogWarning("Couldn't find asphalt in spiral search, falling back to grid search");
                
                // Search in a grid pattern as backup
                for (int x = -maxSearchRadius; x <= maxSearchRadius; x++)
                {
                    for (int y = -maxSearchRadius; y <= maxSearchRadius; y++)
                    {
                        Vector3Int checkTilePos = new Vector3Int(
                            currentTilePos.x + x,
                            currentTilePos.y + y,
                            0
                        );
                        
                        // Check if this position has asphalt
                        if (asphaltTilemap.HasTile(checkTilePos))
                        {
                            // Convert to world position
                            Vector2 worldPos = new Vector2(
                                checkTilePos.x + 0.5f, // Center of tile
                                checkTilePos.y + 0.5f
                            );
                            
                            // Calculate distance
                            float distance = Vector2.Distance(fromPosition, worldPos);
                            
                            // If this is closer than current nearest
                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearestAsphaltPos = worldPos;
                            }
                        }
                    }
                }
            }
            
            // Cache the result
            nearestAsphaltCache[cacheKey] = nearestAsphaltPos;
            
            // If we still didn't find anything, just return the original position
            if (nearestDistance == float.MaxValue)
            {
                Debug.LogError("Failed to find any asphalt tiles in search radius!");
                return fromPosition;
            }
            
            Debug.Log($"Found nearest asphalt at {nearestAsphaltPos}, distance: {nearestDistance}");
            return nearestAsphaltPos;
        }

        /// <summary>
        /// Calculate a directional score towards nearest asphalt (for gravel recovery)
        /// ENHANCED to provide better scoring for paths that lead back to asphalt
        /// </summary>
        public float EvaluateReturnToAsphalt(Vector2 position, Vector2 velocity)
        {
            // First check if we're already on asphalt
            Vector3Int tilePos = new Vector3Int(
                Mathf.FloorToInt(position.x),
                Mathf.FloorToInt(position.y),
                0
            );
            
            if (asphaltTilemap != null && asphaltTilemap.HasTile(tilePos))
            {
                return 1.0f; // Perfect score if already on asphalt
            }
            
            // Find nearest asphalt
            Vector2 nearestAsphalt = FindNearestAsphaltPosition(position, 8.0f);
            
            // Get distance to nearest asphalt
            float distanceToAsphalt = Vector2.Distance(position, nearestAsphalt);
            
            // Direction to nearest asphalt
            Vector2 dirToAsphalt = (nearestAsphalt - position).normalized;
            
            // Check alignment of velocity with direction to asphalt
            Vector2 normalizedVelocity = velocity.normalized;
            
            // Calculate dot product (1 if perfectly aligned, -1 if opposite)
            float alignment = Vector2.Dot(normalizedVelocity, dirToAsphalt);
            
            // ENHANCED: Calculate score with stronger bias toward good alignment
            // and heavier penalty for moving away from asphalt
            float alignmentScore;
            
            if (alignment > 0) // Moving toward asphalt
            {
                // Exponential scaling to reward direct approaches more
                alignmentScore = Mathf.Pow(alignment, 0.7f); // Makes the curve steeper near 1.0
            }
            else // Moving away from asphalt
            {
                // More severe penalty for moving away from asphalt
                alignmentScore = alignment * 0.7f; // More negative
            }
            
            // Convert to 0-1 range with bias toward improvement
            float baseScore = (alignmentScore + 1.0f) * 0.5f;
            
            // ENHANCED: Add distance-based component to the score
            // Closer to asphalt should get a better score regardless of direction
            float distanceFactor = 1.0f - Mathf.Clamp01(distanceToAsphalt / 8.0f); // 8.0 units = max distance considered
            
            // Blend alignment and distance factors
            // 70% weight on alignment, 30% on distance
            float finalScore = (baseScore * 0.7f) + (distanceFactor * 0.3f);
            
            // ENHANCED: Add penalty for being very far from asphalt (too challenging to recover)
            if (distanceToAsphalt > 5.0f)
            {
                float distancePenalty = Mathf.Min(0.3f, (distanceToAsphalt - 5.0f) * 0.1f);
                finalScore = Mathf.Max(0.1f, finalScore - distancePenalty);
            }
            
            // Ensure we never return zero (to avoid division by zero issues)
            return Mathf.Max(0.01f, finalScore);
        }
    }
}