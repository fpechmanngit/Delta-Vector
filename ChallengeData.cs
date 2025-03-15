using UnityEngine;

/// <summary>
/// Scriptable Object that defines a single challenge in the game.
/// This allows for visual editing of challenges in the Unity editor.
/// </summary>
[CreateAssetMenu(fileName = "New Challenge", menuName = "Delta Vector/Challenge")]
public class ChallengeData : ScriptableObject
{
    [Header("Basic Information")]
    public string id;                    // Unique identifier for the challenge
    public string displayName;           // Name shown to the player
    [TextArea(3, 5)]
    public string description;           // Challenge description
    
    [Header("Requirements")]
    public string trackId;               // Which track this challenge is on
    public string requiredCarId;         // Which car must be used
    public int targetMoves;              // Maximum moves to complete the challenge
    public string[] requiredChallenges;  // Challenges that must be completed first
    
    [Header("Rewards")]
    public string[] carsToUnlock;        // Cars unlocked by completing this challenge
    public string[] tracksToUnlock;      // Tracks unlocked by completing this challenge
    
    [Header("Visual Settings")]
    public Sprite previewImage;          // Optional preview image for the challenge

    /// <summary>
    /// Validates the challenge data to ensure it has all required fields
    /// </summary>
    /// <returns>True if the challenge data is valid</returns>
    public bool IsValid()
    {
        // Ensure required fields are present
        if (string.IsNullOrEmpty(id) ||
            string.IsNullOrEmpty(displayName) ||
            string.IsNullOrEmpty(trackId) ||
            string.IsNullOrEmpty(requiredCarId) ||
            targetMoves <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resets this challenge to default values
    /// </summary>
    public void Reset()
    {
        id = "New_Challenge";
        displayName = "New Challenge";
        description = "Complete this challenge";
        trackId = "";
        requiredCarId = "";
        targetMoves = 100;
        requiredChallenges = new string[0];
        carsToUnlock = new string[0];
        tracksToUnlock = new string[0];
        previewImage = null;
    }
}