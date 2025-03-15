using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class CareerConfiguration
{
    [Header("Initial Unlocks")]
    [Tooltip("Cars that will be unlocked when starting a new game")]
    public List<UnlockableCar> initialCars = new List<UnlockableCar>();

    [Tooltip("Tracks that will be unlocked when starting a new game")]
    public List<UnlockableTrack> initialTracks = new List<UnlockableTrack>();

    [Header("Game Content")]
    [Tooltip("Reference to the scriptable object containing all game content")]
    public GameContent gameContent;

    public List<string> GetInitialCarIds()
    {
        return initialCars
            .Where(car => car != null && car.IsValid())
            .Select(car => car.CarId)
            .ToList();
    }

    public List<string> GetInitialTrackIds()
    {
        return initialTracks
            .Where(track => track != null && track.IsValid())
            .Select(track => track.TrackId)
            .ToList();
    }
}

[Serializable]
public class PlayerProgressData
{
    public List<string> unlockedCars = new List<string>();
    public List<string> unlockedTracks = new List<string>();
    public List<string> completedChallenges = new List<string>();
    public Dictionary<string, TrackStats> trackStats = new Dictionary<string, TrackStats>();

    public PlayerProgressData(CareerConfiguration config)
    {
        unlockedCars = new List<string>();
        unlockedTracks = new List<string>();
    }
}

[Serializable]
public class TrackStats
{
    public int bestMoveCount = int.MaxValue;
    public float bestTime = float.MaxValue;
    public float bestMaxSpeed;
    public string bestCarUsed;
    public DateTime lastPlayed;

    public void UpdateStats(int moves, float time, float maxSpeed, string carUsed)
    {
        lastPlayed = DateTime.Now;
        if (moves < bestMoveCount)
        {
            bestMoveCount = moves;
            bestCarUsed = carUsed;
        }
        if (time < bestTime)
        {
            bestTime = time;
        }
        if (maxSpeed > bestMaxSpeed)
        {
            bestMaxSpeed = maxSpeed;
        }
    }
}

public class CareerProgress : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CareerConfiguration config;

    [Header("Debug Options")]
    [SerializeField] private bool resetProgressOnStart = false;

    private const string PROGRESS_KEY = "PlayerProgress";
    private PlayerProgressData progressData;
    private static CareerProgress instance;
    
    public static CareerProgress Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CareerProgress>();
                if (instance == null)
                {
                    GameObject go = new GameObject("CareerProgress");
                    instance = go.AddComponent<CareerProgress>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        SetupSingleton();
        ValidateConfiguration();
        InitializeProgress();
    }

    private void SetupSingleton()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void ValidateConfiguration()
    {
        if (config == null)
        {
            config = new CareerConfiguration();
        }
    }

    private void InitializeProgress()
    {
        if (resetProgressOnStart)
        {
            ResetProgress();
        }
        else
        {
            LoadProgress();
        }
    }

    private void LoadProgress()
    {
        try 
        {
            string jsonData = PlayerPrefs.GetString(PROGRESS_KEY, "");
            
            if (string.IsNullOrEmpty(jsonData))
            {
                CreateNewProgress();
                return;
            }

            progressData = JsonUtility.FromJson<PlayerProgressData>(jsonData);
            if (progressData == null)
            {
                CreateNewProgress();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading progress data: {e.Message}");
            CreateNewProgress();
        }
    }

    private void CreateNewProgress()
    {
        progressData = new PlayerProgressData(config);
        SaveProgress();
    }

    private void SaveProgress()
    {
        try
        {
            string jsonData = JsonUtility.ToJson(progressData);
            PlayerPrefs.SetString(PROGRESS_KEY, jsonData);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving progress: {e.Message}");
        }
    }

    public bool IsCarUnlocked(string carId)
    {
        return progressData.unlockedCars.Contains(carId);
    }

    public bool IsTrackUnlocked(string trackId)
    {
        return progressData.unlockedTracks.Contains(trackId);
    }

    public bool IsChallengeCompleted(string challengeId)
    {
        return progressData.completedChallenges.Contains(challengeId);
    }

    public void UnlockCar(string carId)
    {
        if (!IsCarUnlocked(carId))
        {
            progressData.unlockedCars.Add(carId);
            SaveProgress();
        }
    }

    public void UnlockTrack(string trackId)
    {
        if (!IsTrackUnlocked(trackId))
        {
            progressData.unlockedTracks.Add(trackId);
            SaveProgress();
        }
    }

    public void CompleteChallenge(string challengeId)
    {
        if (!IsChallengeCompleted(challengeId))
        {
            progressData.completedChallenges.Add(challengeId);
            SaveProgress();
        }
    }

    public List<string> GetUnlockedCars()
    {
        return new List<string>(progressData.unlockedCars);
    }

    public List<string> GetUnlockedTracks()
    {
        return new List<string>(progressData.unlockedTracks);
    }

    public List<string> GetCompletedChallenges()
    {
        return new List<string>(progressData.completedChallenges);
    }

    public void UpdateTrackStats(string trackId, int moves, float time, float maxSpeed, string carUsed)
    {
        if (!progressData.trackStats.ContainsKey(trackId))
        {
            progressData.trackStats[trackId] = new TrackStats();
        }

        progressData.trackStats[trackId].UpdateStats(moves, time, maxSpeed, carUsed);
        SaveProgress();
    }

    public TrackStats GetTrackStats(string trackId)
    {
        if (progressData.trackStats.TryGetValue(trackId, out TrackStats stats))
        {
            return stats;
        }
        return null;
    }

    public void ResetProgress()
    {
        CreateNewProgress();
    }
}