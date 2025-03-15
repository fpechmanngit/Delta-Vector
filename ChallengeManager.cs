using UnityEngine;
using System.Collections.Generic;

public class ChallengeManager : MonoBehaviour
{
    // Singleton setup for easy access from other scripts
    private static ChallengeManager instance;
    public static ChallengeManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ChallengeManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ChallengeManager");
                    instance = go.AddComponent<ChallengeManager>();
                }
            }
            return instance;
        }
    }

    [Header("Challenge Configuration")]
    public ChallengeCollection challengeCollection;

    // List of all challenges in the game
    public List<Challenge> allChallenges = new List<Challenge>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Create all challenges
        InitializeChallenges();
    }

    private void InitializeChallenges()
    {
        allChallenges.Clear(); // Clear existing challenges before initializing

        // If no challenge collection is assigned, use hardcoded fallback
        if (challengeCollection == null || challengeCollection.challenges.Count == 0)
        {
            Debug.LogWarning("No ChallengeCollection assigned or it's empty. Using hardcoded challenges instead.");
            InitializeHardcodedChallenges();
            return;
        }

        // Create challenges from scriptable objects
        foreach (var challengeData in challengeCollection.challenges)
        {
            Challenge challenge = new Challenge
            {
                id = challengeData.id,
                displayName = challengeData.displayName,
                description = challengeData.description,
                trackId = challengeData.trackId,
                requiredCarId = challengeData.requiredCarId,
                targetMoves = challengeData.targetMoves,
                requiredChallenges = challengeData.requiredChallenges,
                carsToUnlock = challengeData.carsToUnlock,
                tracksToUnlock = challengeData.tracksToUnlock
            };
            
            allChallenges.Add(challenge);
        }
        
        Debug.Log($"Initialized {allChallenges.Count} challenges from challenge collection");
    }

    // Keep the original method for backward compatibility
    private void InitializeHardcodedChallenges()
    {
        
        Debug.Log("Initialized hardcoded challenges as fallback");
    }

    public List<Challenge> GetAvailableChallenges()
    {
        List<Challenge> available = new List<Challenge>();
        foreach (var challenge in allChallenges)
        {
            if (challenge.IsAvailable() && !CareerProgress.Instance.IsChallengeCompleted(challenge.id))
            {
                available.Add(challenge);
            }
        }
        return available;
    }

    public List<Challenge> GetCompletedChallenges()
    {
        List<Challenge> completed = new List<Challenge>();
        foreach (var challenge in allChallenges)
        {
            if (CareerProgress.Instance.IsChallengeCompleted(challenge.id))
            {
                completed.Add(challenge);
            }
        }
        return completed;
    }

    public void CheckChallengeCompletion(string trackId, int moveCount)
    {
        if (GameInitializationManager.SelectedGameMode != GameMode.Challenges)
        {
            return;
        }

        bool foundMatchingChallenge = false;
        bool completedAnyChallenge = false;

        foreach (var challenge in allChallenges)
        {
            if (challenge.trackId == trackId)
            {
                foundMatchingChallenge = true;

                if (!challenge.IsAvailable())
                {
                    Debug.Log($"Challenge {challenge.id} is not available yet");
                    continue;
                }

                if (CareerProgress.Instance.IsChallengeCompleted(challenge.id))
                {
                    Debug.Log($"Challenge {challenge.id} is already completed");
                    continue;
                }
                
                // Check if the correct car was used
                if (GameInitializationManager.SelectedCar.name != challenge.requiredCarId)
                {
                    Debug.Log($"Challenge {challenge.id} requires car {challenge.requiredCarId} but {GameInitializationManager.SelectedCar.name} was used");
                    continue;
                }
                
                if (moveCount <= challenge.targetMoves)
                {
                    Debug.Log($"Challenge {challenge.id} completed with {moveCount} moves (target: {challenge.targetMoves})");
                    challenge.Complete();
                    completedAnyChallenge = true;
                }
                else
                {
                    Debug.Log($"Challenge {challenge.id} not completed: {moveCount} moves > {challenge.targetMoves} target");
                }
            }
        }
        
        if (!foundMatchingChallenge)
        {
            Debug.Log($"No challenge found for track: {trackId}");
        }
    }

    public Challenge GetChallenge(string challengeId)
    {
        return allChallenges.Find(c => c.id == challengeId);
    }

    public Challenge GetChallengeByTrackId(string trackId)
    {
        foreach (var challenge in allChallenges)
        {
            if (challenge.trackId == trackId)
            {
                return challenge;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Reloads challenges from the challenge collection
    /// </summary>
    public void ReloadChallenges()
    {
        InitializeChallenges();
    }
}