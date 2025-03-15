using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIAudioHandler : MonoBehaviour
{
    private static UIAudioHandler instance;
    private AudioManager audioManager;
    private GlobalUIEventHandler globalHandler;

    // List of buttons that should play the race start sound
    public List<string> raceStartButtonNames = new List<string>
    {
        "TimeTrialButton",
        "PVPButton",
        "StartButton",
        "PlayButton"
    };

    private void Awake()
    {
        // Setup singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Get AudioManager reference
        audioManager = FindObjectOfType<AudioManager>();

        // Create and initialize global handler
        GameObject handlerObj = new GameObject("GlobalUIEventHandler");
        handlerObj.transform.SetParent(transform);
        globalHandler = handlerObj.AddComponent<GlobalUIEventHandler>();
        globalHandler.Initialize(this);
    }

    // Called when entering a new scene or refreshing UI
    private void OnEnable()
    {
        // Re-enable global handler
        if (globalHandler != null)
        {
            globalHandler.Enable();
            globalHandler.OnRefresh();
        }
    }

    private void OnDisable()
    {
        if (globalHandler != null)
        {
            globalHandler.Disable();
        }
    }

    public void OnButtonHover(GameObject button)
    {
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null) return;
        }

        Button buttonComponent = button.GetComponent<Button>();
        if (buttonComponent != null && buttonComponent.isActiveAndEnabled && buttonComponent.interactable)
        {
            audioManager.PlayUIHoverSound();
        }
    }

    public void OnButtonClick(GameObject button)
    {
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null) return;
        }

        Button buttonComponent = button.GetComponent<Button>();
        if (buttonComponent != null && buttonComponent.interactable)
        {
            if (raceStartButtonNames.Contains(button.name))
            {
                audioManager.PlayRaceStartSound();
            }
            else
            {
                audioManager.PlayUIClickSound();
            }
        }
    }

    // Public method to refresh global handler
    public void RefreshAllButtons()
    {
        if (globalHandler != null)
        {
            globalHandler.OnRefresh();
        }
    }
}