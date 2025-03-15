using UnityEngine;
using UnityEngine.UI;

public class SelectedCarDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public Vector2 carDisplaySize = new Vector2(100, 100);  // Size of the display area
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);  // Optional background color
    
    private Image carImage;
    private Image backgroundImage;
    private GameObject selectedCar;

    private void Start()
    {
        SetupDisplay();
        LoadDefaultCarIfNeeded();
        UpdateCarDisplay();
    }

    private void LoadDefaultCarIfNeeded()
    {
        if (GameInitializationManager.SelectedCar == null)
        {
            GameObject defaultCar = Resources.Load<GameObject>("Cars/DefaultCar");
            if (defaultCar != null)
            {
                GameInitializationManager.SelectedCar = defaultCar;
                Debug.Log("Loaded default car for display");
            }
            else
            {
                Debug.LogError("Default car not found in Resources/Cars/DefaultCar!");
            }
        }
    }

    private void SetupDisplay()
    {
        // Create background panel (optional)
        GameObject bgObj = new GameObject("DisplayBackground");
        bgObj.transform.SetParent(transform);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Create car image display
        GameObject imageObj = new GameObject("CarImage");
        imageObj.transform.SetParent(transform);
        carImage = imageObj.AddComponent<Image>();
        carImage.preserveAspect = true;

        // Setup car image rect transform
        RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = carDisplaySize;
        rectTransform.anchoredPosition = Vector2.zero;

        Debug.Log("Car display setup complete");
    }

    private void UpdateCarDisplay()
    {
        selectedCar = GameInitializationManager.SelectedCar;
        
        if (selectedCar != null && carImage != null)
        {
            // Get sprite from the selected car's SpriteRenderer
            SpriteRenderer carSprite = selectedCar.GetComponent<SpriteRenderer>();
            if (carSprite != null && carSprite.sprite != null)
            {
                carImage.sprite = carSprite.sprite;
                carImage.color = Color.white;
                Debug.Log($"Updated display with car: {selectedCar.name}");
            }
            else
            {
                Debug.LogWarning("Selected car has no sprite!");
                carImage.color = Color.clear;
            }
        }
        else
        {
            Debug.LogWarning("No car selected or display image not initialized!");
            if (carImage != null)
            {
                carImage.color = Color.clear;
            }
        }
    }

    private void OnEnable()
    {
        // Check for default car and update display whenever this object becomes active
        LoadDefaultCarIfNeeded();
        UpdateCarDisplay();
    }

    // Public method to force update (can be called from GarageManager)
    public void RefreshDisplay()
    {
        LoadDefaultCarIfNeeded();
        UpdateCarDisplay();
    }
}