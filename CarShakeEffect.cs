using UnityEngine;

public class CarShakeEffect : MonoBehaviour
{
    [Header("Idle Engine Vibration")]
    [Range(0f, 0.001f)]
    public float idleAmplitude = 0.0001f;
    [Range(10f, 120f)]
    public float idleFrequency = 30f;
    
    [Header("Acceleration Shake Settings")]
    [Range(0f, 0.004f)]
    public float accelerationAmplitude = 0.0008f;
    [Range(0.1f, 2f)]
    public float accelerationDuration = 0.15f;
    [Range(30f, 200f)]
    public float accelerationFrequency = 50f;
    
    private Vector3 basePosition;
    private float accelerationTimer = 0f;
    private bool isAccelerating = false;
    private PlayerMovement playerMovement;
    private float time = 0f;
    private Vector3 currentOffset = Vector3.zero;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement component not found!");
        }
    }

    private void LateUpdate()
    {
        // Only apply effects if we're not in replay mode
        if (playerMovement != null && !playerMovement.isReplayMode)
        {
            // Update the timer
            time += Time.deltaTime;

            // Store the current position before applying shake
            basePosition = transform.position;

            // Calculate engine vibration
            Vector3 idleOffset = CalculateEngineVibration();

            // Calculate acceleration shake if active
            Vector3 accelerationOffset = CalculateAccelerationShake();

            // Combine effects
            currentOffset = idleOffset + accelerationOffset;

            // Apply the offset
            transform.position = basePosition + currentOffset;
        }
    }

    private Vector3 CalculateEngineVibration()
    {
        // Create a high-frequency, subtle vibration using multiple frequencies
        float primaryVibration = Mathf.Sin(time * idleFrequency) * idleAmplitude;
        float secondaryVibration = Mathf.Sin(time * (idleFrequency * 1.3f)) * (idleAmplitude * 0.7f);
        float tertiaryVibration = Mathf.Sin(time * (idleFrequency * 0.8f)) * (idleAmplitude * 0.4f);
        
        // Combine vibrations with slightly more vertical movement
        float verticalOffset = (primaryVibration + secondaryVibration + tertiaryVibration) * 1.2f;
        float horizontalOffset = (primaryVibration + secondaryVibration) * 0.8f;
        
        return new Vector3(horizontalOffset, verticalOffset, 0f);
    }

    private Vector3 CalculateAccelerationShake()
    {
        if (isAccelerating)
        {
            accelerationTimer += Time.deltaTime;
            if (accelerationTimer >= accelerationDuration)
            {
                isAccelerating = false;
                accelerationTimer = 0f;
                return Vector3.zero;
            }

            // Calculate a decreasing amplitude based on the timer with a stronger initial punch
            float progress = accelerationTimer / accelerationDuration;
            float currentAmplitude = accelerationAmplitude * (1f - Mathf.Pow(progress, 2));  // Squared for stronger initial effect

            // Create a higher frequency shake for acceleration
            float xOffset = Mathf.Sin(time * accelerationFrequency) * currentAmplitude;
            float yOffset = Mathf.Sin(time * (accelerationFrequency * 1.2f)) * (currentAmplitude * 1.2f);  // Slightly stronger vertical

            // Add some randomness to the acceleration shake
            xOffset += Mathf.Sin(time * (accelerationFrequency * 0.7f)) * (currentAmplitude * 0.5f);
            yOffset += Mathf.Sin(time * (accelerationFrequency * 0.9f)) * (currentAmplitude * 0.5f);

            return new Vector3(xOffset, yOffset, 0f);
        }

        return Vector3.zero;
    }

    public void TriggerAccelerationShake()
    {
        isAccelerating = true;
        accelerationTimer = 0f;
        Debug.Log("Acceleration shake triggered");
    }

    private void OnDisable()
    {
        // Reset position when disabled
        if (transform != null)
        {
            transform.position = basePosition;
        }
    }
}