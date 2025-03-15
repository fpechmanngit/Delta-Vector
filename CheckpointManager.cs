using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    [Header("Debug")]
    public bool showDebugMessages = true;

    private List<Checkpoint> checkpoints = new List<Checkpoint>();
    private int totalCheckpoints = 0;
    private int activatedCheckpointsPlayer1 = 0;
    private int activatedCheckpointsPlayer2 = 0;
    private GameManager gameManager;
    private int currentCheckpointIndexPlayer1 = 0;
    private int currentCheckpointIndexPlayer2 = 0;

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("No GameManager found in scene!");
        }
        CollectCheckpoints();
        SortCheckpointsByNumber();
        
        if (showDebugMessages)
        {
            Debug.Log($"CheckpointManager initialized with {checkpoints.Count} checkpoints");
            foreach (var cp in checkpoints)
            {
                Debug.Log($"Checkpoint #{cp.checkpointNumber} at position {cp.transform.position}");
            }
        }
    }
    
    // Actively look for checkpoints in the scene rather than waiting for registration
    private void CollectCheckpoints()
    {
        Checkpoint[] sceneCheckpoints = FindObjectsOfType<Checkpoint>();
        foreach (var checkpoint in sceneCheckpoints)
        {
            if (!checkpoints.Contains(checkpoint))
            {
                checkpoints.Add(checkpoint);
                totalCheckpoints++;
                
                if (showDebugMessages)
                {
                    Debug.Log($"Collected checkpoint #{checkpoint.checkpointNumber} at {checkpoint.transform.position}");
                }
            }
        }
    }

    public void RegisterCheckpoint(Checkpoint checkpoint)
    {
        if (!checkpoints.Contains(checkpoint))
        {
            checkpoints.Add(checkpoint);
            totalCheckpoints++;
            SortCheckpointsByNumber();
            
            if (showDebugMessages)
            {
                Debug.Log($"Registered checkpoint #{checkpoint.checkpointNumber} at {checkpoint.transform.position}");
            }
        }
    }

    private void SortCheckpointsByNumber()
    {
        if (checkpoints.Count > 1)
        {
            checkpoints.Sort((a, b) => a.checkpointNumber.CompareTo(b.checkpointNumber));
            
            if (showDebugMessages)
            {
                Debug.Log("Checkpoints sorted by number");
                for (int i = 0; i < checkpoints.Count; i++)
                {
                    Debug.Log($"Checkpoint at index {i}: #{checkpoints[i].checkpointNumber}");
                }
            }
        }
    }

    public void OnCheckpointActivated(Checkpoint checkpoint, bool isPlayer1)
    {
        if (isPlayer1)
        {
            activatedCheckpointsPlayer1++;
            if (checkpoint.checkpointNumber == currentCheckpointIndexPlayer1)
            {
                currentCheckpointIndexPlayer1++;
            }
        }
        else
        {
            activatedCheckpointsPlayer2++;
            if (checkpoint.checkpointNumber == currentCheckpointIndexPlayer2)
            {
                currentCheckpointIndexPlayer2++;
            }
        }
            
        if (showDebugMessages)
        {
            Debug.Log($"Checkpoint {checkpoint.checkpointNumber} activated for {(isPlayer1 ? "Player1" : "Player2")}. " +
                      $"Total: {(isPlayer1 ? activatedCheckpointsPlayer1 : activatedCheckpointsPlayer2)}/{totalCheckpoints}");
            Debug.Log($"Current checkpoint index for {(isPlayer1 ? "Player1" : "Player2")}: " +
                      $"{(isPlayer1 ? currentCheckpointIndexPlayer1 : currentCheckpointIndexPlayer2)}");
        }
    }

    public bool AreAllCheckpointsActivated(bool isPlayer1)
    {
        if (isPlayer1)
            return activatedCheckpointsPlayer1 >= totalCheckpoints && totalCheckpoints > 0;
        else
            return activatedCheckpointsPlayer2 >= totalCheckpoints && totalCheckpoints > 0;
    }

    public void ResetCheckpoints()
    {
        activatedCheckpointsPlayer1 = 0;
        activatedCheckpointsPlayer2 = 0;
        currentCheckpointIndexPlayer1 = 0;
        currentCheckpointIndexPlayer2 = 0;
        foreach (var checkpoint in checkpoints)
        {
            checkpoint.ResetCheckpoint();
        }
        
        if (showDebugMessages)
        {
            Debug.Log("All checkpoints reset");
        }
    }

    public void UpdateAllCheckpointVisuals()
    {
        foreach (var checkpoint in checkpoints)
        {
            checkpoint.UpdateVisuals();
        }
    }

    private void OnDestroy()
    {
        checkpoints.Clear();
    }

    private void Update()
    {
        if (gameManager != null && gameManager.IsSpawnPhaseComplete && totalCheckpoints > 0)
        {
            UpdateAllCheckpointVisuals();
        }
    }
    
    public Checkpoint GetNextCheckpointInOrder(bool isPlayer1)
    {
        // Get the next index for the player
        int targetIndex = isPlayer1 ? currentCheckpointIndexPlayer1 : currentCheckpointIndexPlayer2;
        
        // Make sure checkpoint list is updated
        if (checkpoints.Count == 0)
        {
            CollectCheckpoints();
            SortCheckpointsByNumber();
        }
        
        // Validate the index against available checkpoints
        if (targetIndex < 0)
        {
            targetIndex = 0;
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"Looking for next checkpoint for {(isPlayer1 ? "Player1" : "Player2")} at index {targetIndex}. Total checkpoints: {checkpoints.Count}");
        }
        
        // Look for the checkpoint with this index
        if (targetIndex < checkpoints.Count)
        {
            Checkpoint nextCheckpoint = checkpoints[targetIndex];
            
            if (showDebugMessages)
            {
                Debug.Log($"Next checkpoint for {(isPlayer1 ? "Player1" : "Player2")} is #{nextCheckpoint.checkpointNumber} at {nextCheckpoint.transform.position}");
            }
            
            return nextCheckpoint;
        }
        
        // If we've gone through all checkpoints, return null (will target finish line)
        if (showDebugMessages)
        {
            Debug.Log($"No more checkpoints for {(isPlayer1 ? "Player1" : "Player2")}");
        }
        
        return null;
    }
    
    public List<Checkpoint> GetAllCheckpoints()
    {
        // First ensure checkpoints are collected and sorted
        if (checkpoints.Count == 0)
        {
            CollectCheckpoints();
            SortCheckpointsByNumber();
        }
        
        // Return a copy of the checkpoints list
        return new List<Checkpoint>(checkpoints);
    }
}