using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GarageManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject carButtonPrefab;          // Prefab for each car button
    public Transform carButtonContainer;         // Parent object for car buttons
    public Image carPreviewImage;               // Shows the selected car preview
    public TMP_Text carNameText;                // Shows the selected car name
    public TMP_Text carDescriptionText;         // Shows the selected car description
    public Button backButton;                   // Return to main menu
    public Button selectButton;                 // Confirm car selection
    
    [Header("Button Visual Settings")]
    public Color normalColor = Color.white;         // Color for available cars
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f);  // Color for locked cars
    public Color selectedColor = new Color(0.8f, 1f, 0.8f);  // Color for the currently selected car
    
    [Header("Lock Icon")]
    public GameObject lockIconPrefab;  // Prefab for the lock icon shown on locked cars

    // Internal tracking of available cars and selection state
    private List<GameObject> availableCars = new List<GameObject>();
    private GameObject selectedCar;
    private Button selectedButton;

    private void Start()
    {
        // Initialize the garage
        LoadAvailableCars();
        SetupButtons();
        
        // Initially disable select button until car is selected
        if (selectButton != null)
        {
            selectButton.interactable = false;
        }
    }

    private void LoadAvailableCars()
    {
        // Load all cars from Resources/Cars folder
        Debug.Log("Starting to load cars from Resources/Cars...");
        GameObject[] cars = Resources.LoadAll<GameObject>("Cars");
        Debug.Log($"Found {cars.Length} cars in Resources/Cars folder");
        
        availableCars.Clear();

        if (cars == null || cars.Length == 0)
        {
            Debug.LogError("No cars found in Resources/Cars folder!");
            return;
        }

        // Create buttons for each car, regardless of unlock status
        foreach (GameObject car in cars)
        {
            availableCars.Add(car);
            CreateCarButton(car);
            Debug.Log($"Added car and created button for: {car.name}");
        }

        Debug.Log($"Finished loading {availableCars.Count} cars");
    }

    private void CreateCarButton(GameObject car)
    {
        if (carButtonPrefab == null || carButtonContainer == null)
        {
            Debug.LogError("Missing car button prefab or container!");
            return;
        }

        // Instantiate the button from prefab
        GameObject buttonObj = Instantiate(carButtonPrefab, carButtonContainer);
        Button button = buttonObj.GetComponent<Button>();
        
        // Check with CareerProgress if this car is unlocked
        bool isUnlocked = CareerProgress.Instance.IsCarUnlocked(car.name);
        
        if (button != null)
        {
            // Find the Image component (might be on a child object)
            Image buttonImage = buttonObj.GetComponentInChildren<Image>();
            if (buttonImage == null)
            {
                buttonImage = buttonObj.GetComponent<Image>();
            }

            // Set up the car's visual representation
            if (buttonImage != null)
            {
                SpriteRenderer carSprite = car.GetComponent<SpriteRenderer>();
                if (carSprite != null && carSprite.sprite != null)
                {
                    // Set the sprite and adjust its appearance based on unlock status
                    buttonImage.sprite = carSprite.sprite;
                    buttonImage.preserveAspect = true;
                    buttonImage.color = isUnlocked ? normalColor : lockedColor;
                    Debug.Log($"Set sprite for car button: {car.name}, Unlocked: {isUnlocked}");
                }
                else
                {
                    Debug.LogError($"No sprite found for car: {car.name}");
                }
            }
            else
            {
                Debug.LogError($"No Image component found on button for car: {car.name}");
            }

            // Set up the car name text if it exists
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = car.name;
                buttonText.color = isUnlocked ? Color.white : Color.gray;
            }

            // Add a lock icon for locked cars
            if (!isUnlocked && lockIconPrefab != null)
            {
                GameObject lockIcon = Instantiate(lockIconPrefab, buttonObj.transform);
                RectTransform lockRect = lockIcon.GetComponent<RectTransform>();
                if (lockRect != null)
                {
                    // Center the lock icon on the button
                    lockRect.anchoredPosition = Vector2.zero;
                }
            }

            // Set button interactivity based on unlock status
            button.interactable = isUnlocked;

            // Only add click listener if the car is unlocked
            if (isUnlocked)
            {
                button.onClick.AddListener(() => OnCarSelected(car, button));
            }
        }
    }

    private void OnCarSelected(GameObject car, Button clickedButton)
    {
        // Update selection state
        selectedCar = car;
        
        // Reset the previous selected button to normal state
        if (selectedButton != null)
        {
            selectedButton.interactable = true;
            Image prevImage = selectedButton.GetComponent<Image>();
            if (prevImage != null)
            {
                prevImage.color = normalColor;
            }
        }
        
        // Update the new selected button's appearance
        selectedButton = clickedButton;
        selectedButton.interactable = false;
        Image buttonImage = selectedButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = selectedColor;
        }
        
        // Update the preview image with the selected car
        if (carPreviewImage != null)
        {
            SpriteRenderer carSprite = car.GetComponent<SpriteRenderer>();
            if (carSprite != null && carSprite.sprite != null)
            {
                carPreviewImage.sprite = carSprite.sprite;
                carPreviewImage.preserveAspect = true;
                carPreviewImage.gameObject.SetActive(true);
                Debug.Log($"Updated preview image for car: {car.name}");
            }
            else
            {
                Debug.LogError($"No sprite found for car preview: {car.name}");
            }
        }
        else
        {
            Debug.LogError("Preview image reference is missing!");
        }

        // Update the car information text
        if (carNameText != null)
        {
            carNameText.text = car.name;
        }

        if (carDescriptionText != null)
        {
            CarStats stats = car.GetComponent<CarStats>();
            if (stats != null)
            {
                carDescriptionText.text = stats.description;
            }
            else
            {
                carDescriptionText.text = "No description available";
            }
        }

        // Enable the select button now that a car is selected
        if (selectButton != null)
        {
            selectButton.interactable = true;
        }
    }

    private void SetupButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectClicked);
        }
    }

    private void OnSelectClicked()
    {
        if (selectedCar != null)
        {
            // Store selected car in GameInitializationManager
            GameInitializationManager.SelectedCar = selectedCar;
            
            // Return to main menu
            OnBackClicked();
        }
    }

    private void OnBackClicked()
    {
        // Hide garage panel and show main menu
        transform.parent.Find("MainMenuScreen").gameObject.SetActive(true);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clean up any references
        selectedCar = null;
        selectedButton = null;
        availableCars.Clear();
    }
}