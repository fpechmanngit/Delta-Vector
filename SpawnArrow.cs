using UnityEngine;
using TMPro;

public class SpawnArrowSetup : MonoBehaviour 
{
    [Header("Text Settings")]
    public string spawnText = "Spawn Car Here!";
    public float textHeight = 1f;
    public Color textColor = Color.white;
    public float textSize = 3f;

    [Header("Arrow Animation")]
    public float bobSpeed = 2f;
    public float bobAmount = 0.2f;
    
    private TextMeshPro tmpText;
    private float initialY;

    private void Start()
    {
        initialY = transform.position.y;
        SetupText();
    }

    private void SetupText()
    {
        // Find or create the text object
        Transform textTransform = transform.Find("SpawnText");
        GameObject textObj;
        
        if (textTransform == null)
        {
            textObj = new GameObject("SpawnText");
            textObj.transform.SetParent(transform);
        }
        else
        {
            textObj = textTransform.gameObject;
        }

        // Setup Text
        tmpText = textObj.GetComponent<TextMeshPro>();
        if (tmpText == null)
        {
            tmpText = textObj.AddComponent<TextMeshPro>();
        }

        // Configure text properties
        tmpText.text = spawnText;
        tmpText.color = textColor;
        tmpText.fontSize = textSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.textWrappingMode = TextWrappingModes.NoWrap;  // Updated from enableWordWrapping

        // Position text above arrow
        textObj.transform.localPosition = new Vector3(0, textHeight, 0);
        textObj.transform.localRotation = Quaternion.identity;
        
        Debug.Log("Spawn arrow text setup completed");
    }

    private void Update()
    {
        // Bob the arrow up and down
        float newY = initialY + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Always face the camera
        if (tmpText != null)
        {
            tmpText.transform.rotation = Camera.main.transform.rotation;
        }
    }
}