using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scriptable Object that holds a collection of challenges.
/// This serves as the main data source for challenges in the game.
/// </summary>
[CreateAssetMenu(fileName = "Challenge Collection", menuName = "Delta Vector/Challenge Collection")]
public class ChallengeCollection : ScriptableObject
{
    public List<ChallengeData> challenges = new List<ChallengeData>();
    
    /// <summary>
    /// Finds a challenge by its ID
    /// </summary>
    /// <param name="id">The challenge ID to find</param>
    /// <returns>The challenge with the matching ID, or null if not found</returns>
    public ChallengeData FindChallenge(string id)
    {
        return challenges.Find(c => c.id == id);
    }
    
    /// <summary>
    /// Finds a challenge by its track ID
    /// </summary>
    /// <param name="trackId">The track ID to find</param>
    /// <returns>The first challenge with the matching track ID, or null if not found</returns>
    public ChallengeData FindChallengeByTrackId(string trackId)
    {
        return challenges.Find(c => c.trackId == trackId);
    }
    
    /// <summary>
    /// Validates all challenges in the collection
    /// </summary>
    /// <returns>True if all challenges are valid</returns>
    public bool ValidateAllChallenges()
    {
        foreach (var challenge in challenges)
        {
            if (!challenge.IsValid())
            {
                Debug.LogError($"Challenge {challenge.id} is invalid!");
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Checks for duplicate IDs in the collection
    /// </summary>
    /// <returns>True if no duplicates are found</returns>
    public bool CheckForDuplicateIds()
    {
        HashSet<string> ids = new HashSet<string>();
        foreach (var challenge in challenges)
        {
            if (!ids.Add(challenge.id))
            {
                Debug.LogError($"Duplicate challenge ID found: {challenge.id}");
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Checks if this collection has a challenge with the specified ID
    /// </summary>
    /// <param name="challengeId">The challenge ID to check</param>
    /// <returns>True if the collection has this challenge</returns>
    public bool HasChallenge(string challengeId)
    {
        return challenges.Exists(c => c.id == challengeId);
    }
}