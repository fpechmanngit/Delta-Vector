using UnityEngine;

[System.Serializable]
public class Challenge
{
    // Basic challenge information
    public string id;                 // Unique identifier for the challenge
    public string displayName;        // Name shown to the player
    public string description;        // Challenge description
    
    // Challenge requirements
    public string trackId;            // Which track this challenge is on
    public string requiredCarId;      // Which car must be used
    public int targetMoves;           // Maximum moves to complete the challenge
    public string[] requiredChallenges; // Challenges that must be completed first
    
    // Rewards for completing the challenge
    public string[] carsToUnlock;     // Cars unlocked by completing this challenge
    public string[] tracksToUnlock;   // Tracks unlocked by completing this challenge

    // Check if this challenge is available to attempt
    public bool IsAvailable()
    {
        // If there are no required challenges, it's always available
        if (requiredChallenges == null || requiredChallenges.Length == 0)
        {
            return true;
        }

        // Check if all required challenges are completed
        foreach (string requiredChallenge in requiredChallenges)
        {
            bool isCompleted = CareerProgress.Instance.IsChallengeCompleted(requiredChallenge);
            
            if (!isCompleted)
            {
                return false;
            }
        }

        return true;
    }

    // Complete the challenge and unlock its rewards
    public void Complete()
    {
        // First check if challenge was already completed
        if (CareerProgress.Instance.IsChallengeCompleted(id))
        {
            return;
        }

        // Mark the challenge as completed first
        CareerProgress.Instance.CompleteChallenge(id);

        // Then unlock any reward cars
        if (carsToUnlock != null && carsToUnlock.Length > 0)
        {
            foreach (string carId in carsToUnlock)
            {
                if (!CareerProgress.Instance.IsCarUnlocked(carId))
                {
                    CareerProgress.Instance.UnlockCar(carId);
                }
            }
        }

        // Then unlock any reward tracks
        if (tracksToUnlock != null && tracksToUnlock.Length > 0)
        {
            foreach (string trackId in tracksToUnlock)
            {
                if (!CareerProgress.Instance.IsTrackUnlocked(trackId))
                {
                    CareerProgress.Instance.UnlockTrack(trackId);
                }
            }
        }
    }
}