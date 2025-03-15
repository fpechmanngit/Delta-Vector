#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Custom editor window for managing challenges in the Unity Editor
/// </summary>
public class ChallengeEditorWindow : EditorWindow
{
    private ChallengeCollection challengeCollection;
    private Vector2 scrollPosition;
    private bool showCreateNewChallenge = false;
    private ChallengeData newChallenge;
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private bool hasUnsavedChanges = false;

    [MenuItem("Delta Vector/Challenge Editor")]
    public static void ShowWindow()
    {
        GetWindow<ChallengeEditorWindow>("Challenge Editor");
    }

    private void OnEnable()
    {
        // Set up custom styles
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;
        headerStyle.margin = new RectOffset(5, 5, 10, 10);

        subHeaderStyle = new GUIStyle();
        subHeaderStyle.fontSize = 14;
        subHeaderStyle.fontStyle = FontStyle.Bold;
        subHeaderStyle.normal.textColor = Color.white;
        subHeaderStyle.margin = new RectOffset(5, 5, 5, 5);
    }

    private void OnGUI()
    {
        GUILayout.Label("Delta Vector Challenge Editor", headerStyle);

        // Collection selection
        EditorGUILayout.BeginHorizontal();
        challengeCollection = (ChallengeCollection)EditorGUILayout.ObjectField(
            "Challenge Collection:", 
            challengeCollection, 
            typeof(ChallengeCollection), 
            false
        );

        if (GUILayout.Button("Create New Collection", GUILayout.Width(150)))
        {
            CreateNewChallengeCollection();
        }
        EditorGUILayout.EndHorizontal();

        if (challengeCollection == null)
        {
            EditorGUILayout.HelpBox("Please select or create a Challenge Collection to continue.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(10);

        // Toolbox buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate All Challenges"))
        {
            bool valid = challengeCollection.ValidateAllChallenges();
            if (valid)
            {
                EditorUtility.DisplayDialog("Validation", "All challenges are valid!", "OK");
            }
        }
        
        if (GUILayout.Button("Check for Duplicates"))
        {
            bool noDuplicates = challengeCollection.CheckForDuplicateIds();
            if (noDuplicates)
            {
                EditorUtility.DisplayDialog("Duplicate Check", "No duplicate IDs found!", "OK");
            }
        }
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // Challenge list
        GUILayout.Label("Challenges", subHeaderStyle);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        bool needsRefresh = false;
        List<ChallengeData> toRemove = new List<ChallengeData>();

        for (int i = 0; i < challengeCollection.challenges.Count; i++)
        {
            var challenge = challengeCollection.challenges[i];
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            challenge.displayName = EditorGUILayout.TextField("Name:", challenge.displayName);
            if (EditorGUI.EndChangeCheck())
            {
                hasUnsavedChanges = true;
            }
            
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                toRemove.Add(challenge);
                needsRefresh = true;
                hasUnsavedChanges = true;
                continue;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            challenge.id = EditorGUILayout.TextField("ID:", challenge.id);
            challenge.description = EditorGUILayout.TextArea(challenge.description, GUILayout.Height(60));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Requirements", EditorStyles.boldLabel);
            challenge.trackId = EditorGUILayout.TextField("Track ID:", challenge.trackId);
            challenge.requiredCarId = EditorGUILayout.TextField("Required Car ID:", challenge.requiredCarId);
            challenge.targetMoves = EditorGUILayout.IntField("Target Moves:", challenge.targetMoves);
            
            if (EditorGUI.EndChangeCheck())
            {
                hasUnsavedChanges = true;
            }
            
            // Required Challenges
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Required Challenges:");
            if (GUILayout.Button("Edit Prerequisites"))
            {
                // Open a prerequisites editor
                PrerequisitesEditorWindow.ShowWindow(challenge, challengeCollection);
                hasUnsavedChanges = true;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
            
            // Cars to unlock
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Cars to Unlock:");
            if (GUILayout.Button("Edit Car Rewards"))
            {
                // Open a car rewards editor
                CarRewardsEditorWindow.ShowWindow(challenge);
                hasUnsavedChanges = true;
            }
            EditorGUILayout.EndHorizontal();
            
            // Tracks to unlock
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Tracks to Unlock:");
            if (GUILayout.Button("Edit Track Rewards"))
            {
                // Open a track rewards editor
                TrackRewardsEditorWindow.ShowWindow(challenge);
                hasUnsavedChanges = true;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.BeginChangeCheck();
            challenge.previewImage = (Sprite)EditorGUILayout.ObjectField("Preview Image:", challenge.previewImage, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                hasUnsavedChanges = true;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        // Remove any challenges marked for deletion
        foreach (var challenge in toRemove)
        {
            challengeCollection.challenges.Remove(challenge);
            hasUnsavedChanges = true;
        }

        EditorGUILayout.EndScrollView();

        // New challenge options
        EditorGUILayout.Space(10);
        showCreateNewChallenge = EditorGUILayout.Foldout(showCreateNewChallenge, "Create New Challenge");
        
        if (showCreateNewChallenge)
        {
            EditorGUILayout.BeginVertical("box");
            
            if (newChallenge == null)
            {
                newChallenge = CreateInstance<ChallengeData>();
                newChallenge.id = "Challenge" + (challengeCollection.challenges.Count + 1);
                newChallenge.displayName = "New Challenge";
                newChallenge.requiredChallenges = new string[0];
                newChallenge.carsToUnlock = new string[0];
                newChallenge.tracksToUnlock = new string[0];
            }
            
            newChallenge.displayName = EditorGUILayout.TextField("Name:", newChallenge.displayName);
            newChallenge.id = EditorGUILayout.TextField("ID:", newChallenge.id);
            
            if (GUILayout.Button("Add Challenge"))
            {
                // Create a new instance of the challenge and add it to the collection
                ChallengeData challengeToAdd = CreateInstance<ChallengeData>();
                EditorUtility.CopySerialized(newChallenge, challengeToAdd);
                
                // Save the new challenge asset
                string assetPath = AssetDatabase.GetAssetPath(challengeCollection);
                string directory = System.IO.Path.GetDirectoryName(assetPath);
                if (string.IsNullOrEmpty(directory))
                {
                    directory = "Assets";
                }
                
                string newAssetPath = System.IO.Path.Combine(directory, newChallenge.id + ".asset");
                AssetDatabase.CreateAsset(challengeToAdd, newAssetPath);
                
                // Add to collection
                challengeCollection.challenges.Add(challengeToAdd);
                hasUnsavedChanges = true;
                
                // Save everything
                EditorUtility.SetDirty(challengeCollection);
                AssetDatabase.SaveAssets();
                hasUnsavedChanges = false;
                
                // Reset
                newChallenge = null;
                showCreateNewChallenge = false;
            }
            
            EditorGUILayout.EndVertical();
        }

        // Save button at the bottom
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        
        // Shows dirty state with color
        if (hasUnsavedChanges)
        {
            GUI.color = Color.yellow;
            if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(challengeCollection);
                AssetDatabase.SaveAssets();
                hasUnsavedChanges = false;
                GUI.color = Color.white;
            }
            GUI.color = Color.white;
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button("No Changes to Save", GUILayout.Height(30));
            GUI.enabled = true;
        }
        
        EditorGUILayout.EndHorizontal();

        if (needsRefresh || GUI.changed)
        {
            hasUnsavedChanges = true;
        }
    }

    private void CreateNewChallengeCollection()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Challenge Collection",
            "ChallengeCollection",
            "asset",
            "Create a new challenge collection");
            
        if (string.IsNullOrEmpty(path))
            return;
            
        var newCollection = CreateInstance<ChallengeCollection>();
        AssetDatabase.CreateAsset(newCollection, path);
        AssetDatabase.SaveAssets();
        challengeCollection = newCollection;
    }
    
    /// <summary>
    /// Show confirmation prompt when there are unsaved changes
    /// </summary>
    private void OnLostFocus()
    {
        if (hasUnsavedChanges)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes", 
                "You have unsaved changes. Do you want to save them now?", 
                "Save", "Don't Save"))
            {
                EditorUtility.SetDirty(challengeCollection);
                AssetDatabase.SaveAssets();
                hasUnsavedChanges = false;
            }
        }
    }
}

/// <summary>
/// Helper window for editing challenge prerequisites
/// </summary>
public class PrerequisitesEditorWindow : EditorWindow
{
    private ChallengeData challenge;
    private ChallengeCollection allChallenges;
    private List<bool> prerequisiteEnabled = new List<bool>();
    private Vector2 scrollPosition;

    public static void ShowWindow(ChallengeData challenge, ChallengeCollection allChallenges)
    {
        var window = GetWindow<PrerequisitesEditorWindow>("Prerequisites");
        window.challenge = challenge;
        window.allChallenges = allChallenges;
        window.InitializeToggles();
    }

    private void InitializeToggles()
    {
        prerequisiteEnabled.Clear();
        foreach (var otherChallenge in allChallenges.challenges)
        {
            if (otherChallenge == challenge) continue;
            
            bool isRequired = challenge.requiredChallenges != null && 
                             System.Array.IndexOf(challenge.requiredChallenges, otherChallenge.id) >= 0;
            prerequisiteEnabled.Add(isRequired);
        }
    }

    private void OnGUI()
    {
        if (challenge == null || allChallenges == null)
        {
            EditorGUILayout.HelpBox("Window data not properly initialized.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Select Prerequisites for: " + challenge.displayName, EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        int index = 0;
        int validChallengeCount = 0;
        foreach (var otherChallenge in allChallenges.challenges)
        {
            if (otherChallenge == challenge) continue;
            
            validChallengeCount++;
            EditorGUILayout.BeginHorizontal();
            prerequisiteEnabled[index] = EditorGUILayout.Toggle(prerequisiteEnabled[index], GUILayout.Width(20));
            EditorGUILayout.LabelField(otherChallenge.displayName + " (" + otherChallenge.id + ")");
            EditorGUILayout.EndHorizontal();
            
            index++;
        }
        
        if (validChallengeCount == 0)
        {
            EditorGUILayout.HelpBox("No other challenges available to use as prerequisites.", MessageType.Info);
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Save Prerequisites"))
        {
            List<string> requiredIds = new List<string>();
            
            int idx = 0;
            foreach (var otherChallenge in allChallenges.challenges)
            {
                if (otherChallenge == challenge) continue;
                
                if (prerequisiteEnabled[idx])
                {
                    requiredIds.Add(otherChallenge.id);
                }
                idx++;
            }
            
            challenge.requiredChallenges = requiredIds.ToArray();
            EditorUtility.SetDirty(challenge);
            AssetDatabase.SaveAssets();
            Close();
        }
    }
}

/// <summary>
/// Helper window for editing car rewards
/// </summary>
public class CarRewardsEditorWindow : EditorWindow
{
    private ChallengeData challenge;
    private string[] availableCars;
    private bool[] carEnabled;
    private Vector2 scrollPosition;

    public static void ShowWindow(ChallengeData challenge)
    {
        var window = GetWindow<CarRewardsEditorWindow>("Car Rewards");
        window.challenge = challenge;
        window.LoadAvailableCars();
    }

    private void LoadAvailableCars()
    {
        // Load cars from Resources/Cars directory
        GameObject[] carPrefabs = Resources.LoadAll<GameObject>("Cars");
        List<string> carNames = new List<string>();
        
        if (carPrefabs != null && carPrefabs.Length > 0)
        {
            foreach (var car in carPrefabs)
            {
                carNames.Add(car.name);
            }
        }
        
        // If no cars found, use some predefined ones as fallback
        if (carNames.Count == 0)
        {
            carNames = new List<string> 
            { 
                "Grand Durin", 
                "McDarren", 
                "Gr.B Rally Car", 
                "SuperSport", 
                "Vintage Racer" 
            };
        }
        
        availableCars = carNames.ToArray();
        carEnabled = new bool[availableCars.Length];
        
        // Initialize toggles
        for (int i = 0; i < availableCars.Length; i++)
        {
            carEnabled[i] = challenge.carsToUnlock != null && 
                           System.Array.IndexOf(challenge.carsToUnlock, availableCars[i]) >= 0;
        }
    }

    private void OnGUI()
    {
        if (challenge == null || availableCars == null)
        {
            EditorGUILayout.HelpBox("Window data not properly initialized.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Select Car Rewards for: " + challenge.displayName, EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < availableCars.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            carEnabled[i] = EditorGUILayout.Toggle(carEnabled[i], GUILayout.Width(20));
            EditorGUILayout.LabelField(availableCars[i]);
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Save Car Rewards"))
        {
            List<string> selectedCars = new List<string>();
            
            for (int i = 0; i < availableCars.Length; i++)
            {
                if (carEnabled[i])
                {
                    selectedCars.Add(availableCars[i]);
                }
            }
            
            challenge.carsToUnlock = selectedCars.ToArray();
            EditorUtility.SetDirty(challenge);
            AssetDatabase.SaveAssets();
            Close();
        }
    }
}

/// <summary>
/// Helper window for editing track rewards
/// </summary>
public class TrackRewardsEditorWindow : EditorWindow
{
    private ChallengeData challenge;
    private string[] availableTracks;
    private bool[] trackEnabled;
    private Vector2 scrollPosition;

    public static void ShowWindow(ChallengeData challenge)
    {
        var window = GetWindow<TrackRewardsEditorWindow>("Track Rewards");
        window.challenge = challenge;
        window.LoadAvailableTracks();
    }

    private void LoadAvailableTracks()
    {
        // Try to get actual tracks from the build settings
        List<string> trackList = new List<string>();
        
for (int i = 0; i < UnityEditor.EditorBuildSettings.scenes.Length; i++)
{
    var scenePath = UnityEditor.EditorBuildSettings.scenes[i].path;
    string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
    
    // Skip main menu and splash screens
    if (sceneName != "MainMenu" && sceneName != "SplashScreen")
    {
        trackList.Add(sceneName);
    }
}
        
        // If no tracks found (or less than 3), use hardcoded fallback
        if (trackList.Count < 3)
        {
            availableTracks = new string[] 
            { 
                "Vector Valley", 
                "Intralaken", 
                "Intralaken Night", 
                "Mountain Pass", 
                "Desert Run" 
            };
        }
        else
        {
            availableTracks = trackList.ToArray();
        }
        
        trackEnabled = new bool[availableTracks.Length];
        
        // Initialize toggles
        for (int i = 0; i < availableTracks.Length; i++)
        {
            trackEnabled[i] = challenge.tracksToUnlock != null && 
                             System.Array.IndexOf(challenge.tracksToUnlock, availableTracks[i]) >= 0;
        }
    }

    private void OnGUI()
    {
        if (challenge == null || availableTracks == null)
        {
            EditorGUILayout.HelpBox("Window data not properly initialized.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Select Track Rewards for: " + challenge.displayName, EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < availableTracks.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            trackEnabled[i] = EditorGUILayout.Toggle(trackEnabled[i], GUILayout.Width(20));
            EditorGUILayout.LabelField(availableTracks[i]);
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Save Track Rewards"))
        {
            List<string> selectedTracks = new List<string>();
            
            for (int i = 0; i < availableTracks.Length; i++)
            {
                if (trackEnabled[i])
                {
                    selectedTracks.Add(availableTracks[i]);
                }
            }
            
            challenge.tracksToUnlock = selectedTracks.ToArray();
            EditorUtility.SetDirty(challenge);
            AssetDatabase.SaveAssets();
            Close();
        }
    }
}
#endif