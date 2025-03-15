using UnityEngine;
using System.Collections.Generic;

public class MoveHistoryManager : MonoBehaviour
{
    [System.Serializable]
    public class MoveRecord
    {
        public Vector2Int startPosition;
        public Vector2Int endPosition;
        public float timestamp;      // When this move was made
        public float duration;       // How long the move took
        public Vector2Int velocity;  // The velocity at this point

        public MoveRecord(Vector2Int start, Vector2Int end, Vector2Int vel, float moveTime)
        {
            startPosition = start;
            endPosition = end;
            velocity = vel;
            timestamp = Time.time;
            duration = moveTime;
        }

        public override string ToString()
        {
            return $"Move from {startPosition} to {endPosition} at {timestamp}, duration: {duration}s, velocity: {velocity}";
        }
    }

    private List<MoveRecord> moveHistory = new List<MoveRecord>();
    public int maxHistorySize = 100;

    // Store total race time
    private float raceStartTime;
    private float raceEndTime;

    private void Start()
    {
        // Record when the race starts
        raceStartTime = Time.time;
    }

    public void AddMove(Vector2Int start, Vector2Int end, Vector2Int velocity, float moveDuration)
    {
        MoveRecord record = new MoveRecord(start, end, velocity, moveDuration);
        moveHistory.Add(record);
        Debug.Log($"Added move to history: {record}");

        if (moveHistory.Count > maxHistorySize)
        {
            moveHistory.RemoveAt(0);
            Debug.Log("Oldest move removed from history");
        }
    }

    public List<MoveRecord> GetMoveHistory()
    {
        Debug.Log($"Retrieving move history. Total moves: {moveHistory.Count}");
        foreach (var move in moveHistory)
        {
            Debug.Log($"Recorded move: {move}");
        }
        return new List<MoveRecord>(moveHistory);
    }

    public void EndRace()
    {
        raceEndTime = Time.time;
        Debug.Log($"Race completed in {raceEndTime - raceStartTime} seconds");
    }

    public float GetTotalRaceTime()
    {
        return raceEndTime - raceStartTime;
    }

    public void ClearHistory()
    {
        moveHistory.Clear();
        Debug.Log("Move history cleared");
    }
}