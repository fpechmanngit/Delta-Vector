using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This attribute makes our classes show up in the Unity Inspector
[System.Serializable]
public class TrackReference
{
    // The scene asset reference that will show up in the Unity Inspector
    // We use Object type because Scene assets can only be referenced in the Editor
    [SerializeField] private Object sceneAsset;
    
    // The actual path to the scene asset in the project
    [SerializeField] private string scenePath;

    // This will be called automatically by Unity when values change in the Inspector
    public void OnValidate()
    {
        #if UNITY_EDITOR
        if (sceneAsset != null)
        {
            // Get the actual path of the scene asset in the project
            scenePath = AssetDatabase.GetAssetPath(sceneAsset);
        }
        #endif
    }

    // Property to get just the scene name without path or extension
    public string SceneName
    {
        get
        {
            if (string.IsNullOrEmpty(scenePath)) return "";
            // Extract just the filename without extension (e.g., "Level1" from "Assets/Scenes/Level1.unity")
            return System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }
    }

    // Helper method to check if this reference actually points to a valid scene
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(scenePath) && sceneAsset != null;
    }
}

// Class that represents a car that can be unlocked in the game
[System.Serializable]
public class UnlockableCar
{
    // Reference to the actual car prefab asset
    [Tooltip("Drag the car prefab here")]
    public GameObject carPrefab;

    // Property to get the car's unique identifier (using the prefab name)
    public string CarId => carPrefab != null ? carPrefab.name : "";

    // Helper method to check if this is a valid car reference
    public bool IsValid()
    {
        return carPrefab != null;
    }

    // Optional: Add more car-specific properties
    [Tooltip("Brief description of the car")]
    public string description;
    
    [Tooltip("Display name shown to the player")]
    public string displayName;
}

// Class that represents a track that can be unlocked in the game
[System.Serializable]
public class UnlockableTrack
{
    // Reference to the track scene
    [Tooltip("Drag the track scene here")]
    public TrackReference trackScene;

    // Property to get the track's unique identifier (using the scene name)
    public string TrackId => trackScene != null ? trackScene.SceneName : "";

    // Helper method to check if this is a valid track reference
    public bool IsValid()
    {
        return trackScene != null && trackScene.IsValid();
    }

    // Optional: Add more track-specific properties
    [Tooltip("Brief description of the track")]
    public string description;
    
    [Tooltip("Display name shown to the player")]
    public string displayName;
    
    [Tooltip("Preview image of the track")]
    public Sprite previewImage;
}

// Optional: Helper class to store all available content in the game
[CreateAssetMenu(fileName = "GameContent", menuName = "DeltaVector/Game Content")]
public class GameContent : ScriptableObject
{
    [Header("Available Cars")]
    [Tooltip("All car prefabs that can be unlocked in the game")]
    public UnlockableCar[] allCars;

    [Header("Available Tracks")]
    [Tooltip("All track scenes that can be unlocked in the game")]
    public UnlockableTrack[] allTracks;

    // Helper methods to find content by ID
    public UnlockableCar FindCar(string carId)
    {
        if (string.IsNullOrEmpty(carId)) return null;
        return System.Array.Find(allCars, car => car.CarId == carId);
    }

    public UnlockableTrack FindTrack(string trackId)
    {
        if (string.IsNullOrEmpty(trackId)) return null;
        return System.Array.Find(allTracks, track => track.TrackId == trackId);
    }

    // Validation method to check for duplicate IDs
    public void ValidateContent()
    {
        // Check for duplicate car IDs
        var carIds = new System.Collections.Generic.HashSet<string>();
        foreach (var car in allCars)
        {
            if (car.IsValid())
            {
                if (!carIds.Add(car.CarId))
                {
                    Debug.LogError($"Duplicate car ID found: {car.CarId}");
                }
            }
        }

        // Check for duplicate track IDs
        var trackIds = new System.Collections.Generic.HashSet<string>();
        foreach (var track in allTracks)
        {
            if (track.IsValid())
            {
                if (!trackIds.Add(track.TrackId))
                {
                    Debug.LogError($"Duplicate track ID found: {track.TrackId}");
                }
            }
        }
    }

    // Called by Unity when values change in the Inspector
    private void OnValidate()
    {
        ValidateContent();
    }
}