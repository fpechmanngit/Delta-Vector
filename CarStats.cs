using UnityEngine;

public class CarStats : MonoBehaviour
{
    [Header("Car Information")]
    public string carName;
    [TextArea(3, 5)]
    public string description;
    
    [Header("Performance Stats")]
    public float baseSpeed = 5f;
    public float acceleration = 1f;
    public float handling = 1f;
    
    [Header("Movement Rates")]
    [Tooltip("Multiplier for forward movement spread (default: 1)")]
    [Range(0.5f, 2f)]
    public float accelerationRate = 1f;
    
    [Tooltip("Multiplier for braking movement spread (default: 1)")]
    [Range(0.5f, 2f)]
    public float brakeRate = 1f;
    
    [Tooltip("Multiplier for turning movement spread (default: 1)")]
    [Range(0.5f, 2f)]
    public float turnRate = 1f;

    [Header("Unlocking")]
    public bool isUnlocked = true;
    public string unlockCondition = "";
}