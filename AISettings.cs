using UnityEngine;
using System.Collections.Generic;

namespace DeltaVector.AI
{
    /// <summary>
    /// Class to store a collection of AI settings as a preset
    /// </summary>
    [System.Serializable]
    public class AIPreset
    {
        public string name;
        
        // Core settings
        public bool enableTestFeatures;
        public int pathfindingDepth;
        public bool manualStepMode;
        public bool showPathVisualization;
        public bool adaptiveRendering;
        public bool drawPathNodes;
        public bool createPathVisualizer;
        
        // Weights
        public float distanceWeight;
        public float speedWeight;
        public float terrainWeight;
        public float directionWeight;
        public float pathWeight;
        public float returnToAsphaltWeight;
        public float centerTrackWeight;
        public float trackExitPenaltyWeight;
        
        // Speed settings
        public float maxStraightSpeed;
        public float maxTurnSpeed;
        
        // Basic pruning settings
        public bool enablePathPruning;
        public int offTrackToleranceCount;
        public float minTerrainQualityThreshold;
        
        // Enhanced pruning settings
        public bool enableAggressivePruning;
        public float scorePruningThreshold;
        public float depthPruningFactor;
        public bool enableLookAheadPruning;
        public int lookAheadDistance;
        public bool pruneIneffcientMovements;
        public bool pruneExcessiveSpeedAtTurns;
        
        // Chunked processing settings
        public bool enableChunkedProcessing;
        public float targetThinkingTime;
        public float postThinkingDelay;
        public int maxPathsPerFrame;

        // Testing settings
        public bool skipPlayer1Turn;
    }

    /// <summary>
    /// Stores and manages AI configuration settings
    /// </summary>
    public class AISettings : MonoBehaviour
    {
        [Header("Global Settings")]
        [Tooltip("Master toggle for AI test features - when OFF, no test components will activate")]
        public bool enableTestFeatures = false;
        
        [Header("Core Pathfinding Settings")]
        [Tooltip("Depth of the search tree (how many moves to look ahead)")]
        [Range(1, 15)]
        public int pathfindingDepth = 5;
        
        [Header("Control Settings")]
        [Tooltip("Use manual stepping mode with spacebar")]
        public bool manualStepMode = true;
        
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
        
        [Header("Speed Settings")]
        [Range(3f, 10f)]
        public float maxStraightSpeed = 7.0f;
        [Range(2f, 5f)]
        public float maxTurnSpeed = 3.5f;
        
        [Header("Visualization Settings")]
        [Tooltip("Master toggle for showing AI path visualization")]
        public bool showPathVisualization = true;
        
        [Tooltip("Whether to automatically create AIPathVisualizer components")]
        public bool createPathVisualizer = true;
        
        [Header("Performance Settings")]
        [Tooltip("Enable adaptive rendering for better performance with many paths")]
        public bool adaptiveRendering = true;
        
        [Tooltip("Enable to draw nodes for each path (may impact performance)")]
        public bool drawPathNodes = true;
        
        [Header("Path Pruning Settings")]
        [Tooltip("Enable early pruning of obviously bad paths")]
        public bool enablePathPruning = true;
        
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
        
        [Tooltip("Prune paths with excessive speed at corners/turns")]
        public bool pruneExcessiveSpeedAtTurns = true;
        
        [Header("Chunked Processing Settings")]
        [Tooltip("Enable chunked processing to prevent framerate drops")]
        public bool enableChunkedProcessing = true;
        
        [Tooltip("Target time to spend on thinking per frame (ms)")]
        [Range(5, 50)]
        public float targetThinkingTime = 16.0f;
        
        [Tooltip("Delay after thinking completes (seconds)")]
        [Range(0, 1.0f)]
        public float postThinkingDelay = 0.1f;
        
        [Tooltip("Maximum paths to process per frame")]
        [Range(10, 10000)]
        public int maxPathsPerFrame = 200;
        
        [Header("Testing Settings")]
        [Tooltip("When enabled, Player1's turn is automatically skipped")]
        public bool skipPlayer1Turn = false;
        
        // Preset storage
        private Dictionary<string, AIPreset> savedPresets = new Dictionary<string, AIPreset>();
        
        /// <summary>
        /// Save current settings to PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetInt("AI_EnableTestFeatures", enableTestFeatures ? 1 : 0);
            PlayerPrefs.SetInt("AI_PathfindingDepth", pathfindingDepth);
            PlayerPrefs.SetInt("AI_ManualStepMode", manualStepMode ? 1 : 0);
            PlayerPrefs.SetInt("AI_ShowPathVisualization", showPathVisualization ? 1 : 0);
            PlayerPrefs.SetInt("AI_CreatePathVisualizer", createPathVisualizer ? 1 : 0);
            PlayerPrefs.SetInt("AI_AdaptiveRendering", adaptiveRendering ? 1 : 0);
            PlayerPrefs.SetInt("AI_DrawPathNodes", drawPathNodes ? 1 : 0);
            PlayerPrefs.SetInt("AI_EnablePathPruning", enablePathPruning ? 1 : 0);
            PlayerPrefs.SetInt("AI_OffTrackToleranceCount", offTrackToleranceCount);
            PlayerPrefs.SetFloat("AI_MinTerrainQualityThreshold", minTerrainQualityThreshold);
            
            // Save enhanced pruning settings
            PlayerPrefs.SetInt("AI_EnableAggressivePruning", enableAggressivePruning ? 1 : 0);
            PlayerPrefs.SetFloat("AI_ScorePruningThreshold", scorePruningThreshold);
            PlayerPrefs.SetFloat("AI_DepthPruningFactor", depthPruningFactor);
            PlayerPrefs.SetInt("AI_EnableLookAheadPruning", enableLookAheadPruning ? 1 : 0);
            PlayerPrefs.SetInt("AI_LookAheadDistance", lookAheadDistance);
            PlayerPrefs.SetInt("AI_PruneIneffcientMovements", pruneIneffcientMovements ? 1 : 0);
            PlayerPrefs.SetInt("AI_PruneExcessiveSpeedAtTurns", pruneExcessiveSpeedAtTurns ? 1 : 0);
            
            // Save chunked processing settings
            PlayerPrefs.SetInt("AI_EnableChunkedProcessing", enableChunkedProcessing ? 1 : 0);
            PlayerPrefs.SetFloat("AI_TargetThinkingTime", targetThinkingTime);
            PlayerPrefs.SetFloat("AI_PostThinkingDelay", postThinkingDelay);
            PlayerPrefs.SetInt("AI_MaxPathsPerFrame", maxPathsPerFrame);
            
            // Save weights
            PlayerPrefs.SetFloat("AI_DistanceWeight", distanceWeight);
            PlayerPrefs.SetFloat("AI_SpeedWeight", speedWeight);
            PlayerPrefs.SetFloat("AI_TerrainWeight", terrainWeight);
            PlayerPrefs.SetFloat("AI_DirectionWeight", directionWeight);
            PlayerPrefs.SetFloat("AI_PathWeight", pathWeight);
            PlayerPrefs.SetFloat("AI_ReturnToAsphaltWeight", returnToAsphaltWeight);
            PlayerPrefs.SetFloat("AI_CenterTrackWeight", centerTrackWeight);
            PlayerPrefs.SetFloat("AI_TrackExitPenaltyWeight", trackExitPenaltyWeight);
            
            // Save speed settings
            PlayerPrefs.SetFloat("AI_MaxStraightSpeed", maxStraightSpeed);
            PlayerPrefs.SetFloat("AI_MaxTurnSpeed", maxTurnSpeed);
            
            // Save testing settings
            PlayerPrefs.SetInt("AI_SkipPlayer1Turn", skipPlayer1Turn ? 1 : 0);
            
            PlayerPrefs.Save();
            Debug.Log("AI settings saved");
        }
        
        /// <summary>
        /// Load settings from PlayerPrefs
        /// </summary>
        public void LoadSettings()
        {
            // Load settings if they exist
            if (PlayerPrefs.HasKey("AI_PathfindingDepth"))
            {
                enableTestFeatures = PlayerPrefs.GetInt("AI_EnableTestFeatures", 0) == 1;
                pathfindingDepth = PlayerPrefs.GetInt("AI_PathfindingDepth", 5);
                manualStepMode = PlayerPrefs.GetInt("AI_ManualStepMode", 1) == 1;
                showPathVisualization = PlayerPrefs.GetInt("AI_ShowPathVisualization", 1) == 1;
                createPathVisualizer = PlayerPrefs.GetInt("AI_CreatePathVisualizer", 1) == 1;
                adaptiveRendering = PlayerPrefs.GetInt("AI_AdaptiveRendering", 1) == 1;
                drawPathNodes = PlayerPrefs.GetInt("AI_DrawPathNodes", 1) == 1;
                enablePathPruning = PlayerPrefs.GetInt("AI_EnablePathPruning", 1) == 1;
                offTrackToleranceCount = PlayerPrefs.GetInt("AI_OffTrackToleranceCount", 1);
                minTerrainQualityThreshold = PlayerPrefs.GetFloat("AI_MinTerrainQualityThreshold", 0.1f);
                
                // Load enhanced pruning settings
                enableAggressivePruning = PlayerPrefs.GetInt("AI_EnableAggressivePruning", 1) == 1;
                scorePruningThreshold = PlayerPrefs.GetFloat("AI_ScorePruningThreshold", 0.3f);
                depthPruningFactor = PlayerPrefs.GetFloat("AI_DepthPruningFactor", 0.5f);
                enableLookAheadPruning = PlayerPrefs.GetInt("AI_EnableLookAheadPruning", 1) == 1;
                lookAheadDistance = PlayerPrefs.GetInt("AI_LookAheadDistance", 2);
                pruneIneffcientMovements = PlayerPrefs.GetInt("AI_PruneIneffcientMovements", 1) == 1;
                pruneExcessiveSpeedAtTurns = PlayerPrefs.GetInt("AI_PruneExcessiveSpeedAtTurns", 1) == 1;
                
                // Load chunked processing settings
                enableChunkedProcessing = PlayerPrefs.GetInt("AI_EnableChunkedProcessing", 1) == 1;
                targetThinkingTime = PlayerPrefs.GetFloat("AI_TargetThinkingTime", 16.0f);
                postThinkingDelay = PlayerPrefs.GetFloat("AI_PostThinkingDelay", 0.2f);
                maxPathsPerFrame = PlayerPrefs.GetInt("AI_MaxPathsPerFrame", 200);
                
                // Load weights
                distanceWeight = PlayerPrefs.GetFloat("AI_DistanceWeight", 5f);
                speedWeight = PlayerPrefs.GetFloat("AI_SpeedWeight", 6f);
                terrainWeight = PlayerPrefs.GetFloat("AI_TerrainWeight", 10f);
                directionWeight = PlayerPrefs.GetFloat("AI_DirectionWeight", 3f);
                pathWeight = PlayerPrefs.GetFloat("AI_PathWeight", 8f);
                returnToAsphaltWeight = PlayerPrefs.GetFloat("AI_ReturnToAsphaltWeight", 12f);
                centerTrackWeight = PlayerPrefs.GetFloat("AI_CenterTrackWeight", 4f);
                trackExitPenaltyWeight = PlayerPrefs.GetFloat("AI_TrackExitPenaltyWeight", 15f);
                
                // Load speed settings
                maxStraightSpeed = PlayerPrefs.GetFloat("AI_MaxStraightSpeed", 7.0f);
                maxTurnSpeed = PlayerPrefs.GetFloat("AI_MaxTurnSpeed", 3.5f);
                
                // Load testing settings
                skipPlayer1Turn = PlayerPrefs.GetInt("AI_SkipPlayer1Turn", 0) == 1;
                
                Debug.Log("AI settings loaded from PlayerPrefs");
            }
        }

        /// <summary>
        /// Create a preset with the current settings
        /// </summary>
        public void CreatePreset(string presetName)
        {
            AIPreset preset = new AIPreset
            {
                name = presetName,
                enableTestFeatures = enableTestFeatures,
                pathfindingDepth = pathfindingDepth,
                manualStepMode = manualStepMode,
                showPathVisualization = showPathVisualization,
                createPathVisualizer = createPathVisualizer,
                adaptiveRendering = adaptiveRendering,
                drawPathNodes = drawPathNodes,
                
                // Weights
                distanceWeight = distanceWeight,
                speedWeight = speedWeight,
                terrainWeight = terrainWeight,
                directionWeight = directionWeight,
                pathWeight = pathWeight,
                returnToAsphaltWeight = returnToAsphaltWeight,
                centerTrackWeight = centerTrackWeight,
                trackExitPenaltyWeight = trackExitPenaltyWeight,
                
                // Speed settings
                maxStraightSpeed = maxStraightSpeed,
                maxTurnSpeed = maxTurnSpeed,
                
                // Basic pruning settings
                enablePathPruning = enablePathPruning,
                offTrackToleranceCount = offTrackToleranceCount,
                minTerrainQualityThreshold = minTerrainQualityThreshold,
                
                // Enhanced pruning settings
                enableAggressivePruning = enableAggressivePruning,
                scorePruningThreshold = scorePruningThreshold,
                depthPruningFactor = depthPruningFactor,
                enableLookAheadPruning = enableLookAheadPruning,
                lookAheadDistance = lookAheadDistance,
                pruneIneffcientMovements = pruneIneffcientMovements,
                pruneExcessiveSpeedAtTurns = pruneExcessiveSpeedAtTurns,
                
                // Chunked processing settings
                enableChunkedProcessing = enableChunkedProcessing,
                targetThinkingTime = targetThinkingTime,
                postThinkingDelay = postThinkingDelay,
                maxPathsPerFrame = maxPathsPerFrame,
                
                // Testing settings
                skipPlayer1Turn = skipPlayer1Turn
            };
            
            savedPresets[presetName] = preset;
            
            Debug.Log($"Created AI preset: {presetName}");
        }
        
        /// <summary>
        /// Load a preset by name
        /// </summary>
        public bool LoadPreset(string presetName)
        {
            if (savedPresets.TryGetValue(presetName, out AIPreset preset))
            {
                // Load core settings
                pathfindingDepth = preset.pathfindingDepth;
                manualStepMode = preset.manualStepMode;
                showPathVisualization = preset.showPathVisualization;
                createPathVisualizer = preset.createPathVisualizer;
                adaptiveRendering = preset.adaptiveRendering;
                drawPathNodes = preset.drawPathNodes;
                
                // Load weights
                distanceWeight = preset.distanceWeight;
                speedWeight = preset.speedWeight;
                terrainWeight = preset.terrainWeight;
                directionWeight = preset.directionWeight;
                pathWeight = preset.pathWeight;
                returnToAsphaltWeight = preset.returnToAsphaltWeight;
                centerTrackWeight = preset.centerTrackWeight;
                trackExitPenaltyWeight = preset.trackExitPenaltyWeight;
                
                // Load speed settings
                maxStraightSpeed = preset.maxStraightSpeed;
                maxTurnSpeed = preset.maxTurnSpeed;
                
                // Load basic pruning settings
                enablePathPruning = preset.enablePathPruning;
                offTrackToleranceCount = preset.offTrackToleranceCount;
                minTerrainQualityThreshold = preset.minTerrainQualityThreshold;
                
                // Load enhanced pruning settings
                enableAggressivePruning = preset.enableAggressivePruning;
                scorePruningThreshold = preset.scorePruningThreshold;
                depthPruningFactor = preset.depthPruningFactor;
                enableLookAheadPruning = preset.enableLookAheadPruning;
                lookAheadDistance = preset.lookAheadDistance;
                pruneIneffcientMovements = preset.pruneIneffcientMovements;
                pruneExcessiveSpeedAtTurns = preset.pruneExcessiveSpeedAtTurns;
                
                // Load chunked processing settings
                enableChunkedProcessing = preset.enableChunkedProcessing;
                targetThinkingTime = preset.targetThinkingTime;
                postThinkingDelay = preset.postThinkingDelay;
                maxPathsPerFrame = preset.maxPathsPerFrame;
                
                // Load testing settings
                skipPlayer1Turn = preset.skipPlayer1Turn;
                
                Debug.Log($"Loaded AI preset: {presetName}");
                return true;
            }
            else
            {
                Debug.LogWarning($"Preset not found: {presetName}");
                return false;
            }
        }
        
        /// <summary>
        /// Get all preset names
        /// </summary>
        public List<string> GetPresetNames()
        {
            return new List<string>(savedPresets.Keys);
        }
    }
}