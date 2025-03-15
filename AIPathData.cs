using UnityEngine;
using System.Collections.Generic;

namespace DeltaVector.AI
{
    /// <summary>
    /// Represents the various states of AI thinking
    /// </summary>
    public enum AIThinkingState
    {
        Idle,
        ReadyToThink,
        Thinking,
        ThinkingComplete,
        ReadyToExecute,
        Executing
    }

    /// <summary>
    /// Enum to categorize path quality for visualization
    /// </summary>
    public enum PathQuality
    {
        Unknown,
        Bad,
        Medium,
        Good,
        Best
    }

    /// <summary>
    /// Class to track a path generation task for chunked processing
    /// </summary>
    public class PathGenerationTask
    {
        public Path CurrentPath;
        public PathNode ParentNode;
        public int Depth;
        
        public PathGenerationTask(Path path, PathNode parent, int depth)
        {
            CurrentPath = path;
            ParentNode = parent;
            Depth = depth;
        }
    }

    /// <summary>
    /// Represents a node in the path tree
    /// </summary>
    public class PathNode
    {
        public Vector2Int Position { get; set; }
        public Vector2Int Velocity { get; set; }
        public float Score { get; set; }
        public Dictionary<string, float> EvaluationFactors { get; set; }
        public List<PathNode> Children { get; set; }
        
        // Fields for enhanced pruning
        public int OffTrackCount { get; set; }
        public bool IsViable { get; set; }
        public float TerrainQuality { get; set; }
        public float DistanceScore { get; set; }
        public float SpeedScore { get; set; }
        public float DirectionScore { get; set; }
        public float TrackExitRisk { get; set; }

        public PathNode(Vector2Int position, Vector2Int velocity)
        {
            Position = position;
            Velocity = velocity;
            Score = 0f;
            EvaluationFactors = new Dictionary<string, float>();
            Children = new List<PathNode>();
            
            // Initialize pruning fields
            OffTrackCount = 0;
            IsViable = true;
            TerrainQuality = 1.0f;
            DistanceScore = 0f;
            SpeedScore = 0f;
            DirectionScore = 0f;
            TrackExitRisk = 0f;
        }
    }

    /// <summary>
    /// Represents a complete path through the decision tree
    /// </summary>
    public class Path
    {
        public List<PathNode> Nodes { get; set; }
        public float TotalScore { get; set; }
        public float AverageScore { get; set; }
        public float MinNodeScore { get; set; }
        public PathQuality Quality { get; set; }
        
        // Fields for enhanced pruning
        public int OffTrackNodeCount { get; set; }
        public float AverageTerrainQuality { get; set; }
        public float TrackExitRisk { get; set; }
        public int DirectionChanges { get; set; }
        public bool HasDeadEnd { get; set; }
        public float AverageSpeed { get; set; }

        public Path()
        {
            Nodes = new List<PathNode>();
            TotalScore = 0f;
            AverageScore = 0f;
            MinNodeScore = 1f;
            Quality = PathQuality.Unknown;
            
            // Initialize pruning fields
            OffTrackNodeCount = 0;
            AverageTerrainQuality = 1.0f;
            TrackExitRisk = 0f;
            DirectionChanges = 0;
            HasDeadEnd = false;
            AverageSpeed = 0.5f; // Default to middle value
        }
    }
}