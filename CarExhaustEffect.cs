using UnityEngine;

public class CarExhaustEffect : MonoBehaviour
{
    private ParticleSystem exhaustParticles;
    private PlayerAnimationController animationController;
    
    [Header("Particle Settings")]
    public Color initialColor = new Color(0.8f, 0.8f, 0.8f, 0.15f);
    public Color finalColor = new Color(0.8f, 0.8f, 0.8f, 0f);     
    public float particleSize = 0.05f;
    public float emissionRate = 30f;  // Reduced to a more manageable value
    public float lifetime = 0.5f;
    public float particleSpeed = 1f;
    public float particleSpread = 10f;
    public float minEmissionRate = 10f;  // Minimum emission rate when idle
    public float maxEmissionRate = 50f;  // Maximum emission rate when moving fast

    [Header("Exhaust Position Settings")]
    public bool useWorldSpace = false;
    [Tooltip("The X offset is along the car's sides, negative goes left")]
    public float offsetX = 0f;
    [Tooltip("The Y offset is along the car's length, negative goes to the rear")]
    public float offsetY = -0.3f;

    private GameObject exhaustObj;
    private Vector3 lastPosition;
    private float lastAngle;
    private ParticleSystem.EmissionModule emissionModule;
    private bool emissionInitialized = false;

    private void Start()
    {
        animationController = GetComponent<PlayerAnimationController>();
        if (animationController == null)
        {
            Debug.LogError("PlayerAnimationController not found!");
        }

        CreateExhaustSystem();
    }

    private void CreateExhaustSystem()
    {
        // Create a separate GameObject for the exhaust particles
        exhaustObj = new GameObject("ExhaustParticles");
        
        // Don't parent to the car initially - we'll position it manually
        exhaustObj.transform.SetParent(null);
        exhaustObj.transform.position = transform.position;
        
        exhaustParticles = exhaustObj.AddComponent<ParticleSystem>();
        
        // Create a default particle material
        var renderer = exhaustObj.GetComponent<ParticleSystemRenderer>();
        Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
        
        // Create a circular particle texture
        Texture2D particleTexture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                float alpha = Mathf.Clamp01(1f - (distanceFromCenter / 16f));
                colors[y * 32 + x] = new Color(1, 1, 1, alpha * alpha);
            }
        }
        particleTexture.SetPixels(colors);
        particleTexture.Apply();
        
        particleMaterial.mainTexture = particleTexture;
        renderer.material = particleMaterial;
        renderer.sortingOrder = 1;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Configure particle system
        var main = exhaustParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = lifetime;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.simulationSpace = useWorldSpace ? 
            ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        main.maxParticles = 5000;
        main.startColor = initialColor;

        // Configure emission - this is key for constant stream
        emissionModule = exhaustParticles.emission;
        emissionModule.rateOverTime = emissionRate;
        emissionModule.enabled = true;
        
        // Important: Set emission to constant rate
        var rateOverTime = new ParticleSystem.MinMaxCurve(emissionRate);
        rateOverTime.mode = ParticleSystemCurveMode.Constant;
        emissionModule.rateOverTime = rateOverTime;
        
        // Don't use bursts which can cause waves of particles
        emissionModule.burstCount = 0;
        
        emissionInitialized = true;

        var shape = exhaustParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = particleSpread;
        shape.radius = 0.03f;
        
        // Ensure the shape emission is set to edge for more consistent emission
        shape.radiusMode = ParticleSystemShapeMultiModeValue.Random;
        shape.radiusSpread = 0f;

        var noise = exhaustParticles.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.1f);
        noise.frequency = 1.5f;
        noise.scrollSpeed = 1f;
        noise.damping = true;

        var velocityOverLifetime = exhaustParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, new AnimationCurve(
            new Keyframe(0, -0.2f), new Keyframe(1, 0.2f)));
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, new AnimationCurve(
            new Keyframe(0, -0.2f), new Keyframe(1, 0.2f)));
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

        var colorOverLifetime = exhaustParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(initialColor, 0.0f),
                new GradientColorKey(finalColor, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(initialColor.a, 0.0f), 
                new GradientAlphaKey(finalColor.a, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = exhaustParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.7f);
        curve.AddKey(0.3f, 1f);
        curve.AddKey(1.0f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Store initial position and angle
        lastPosition = transform.position;
        lastAngle = 0f;
        
        // Make sure the particle system is playing
        if (!exhaustParticles.isPlaying)
        {
            exhaustParticles.Play(true);
        }
        
        // Initial positioning
        UpdateExhaustPosition(0f);
    }

    private void LateUpdate()
    {
        // Get the current angle from the car
        float currentAngle = 0f;
        if (animationController != null)
        {
            currentAngle = transform.rotation.eulerAngles.z;
        }
        
        // Update exhaust position to follow car
        UpdateExhaustPosition(currentAngle);
        
        // Update emission rate based on car speed (optional)
        UpdateEmissionBasedOnSpeed();
    }

    private void UpdateEmissionBasedOnSpeed()
    {
        if (!emissionInitialized) return;
        
        // Get the car's velocity - you can customize this based on your car controller
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            // Normalize speed between 0 and 1
            Vector2Int velocity = movement.CurrentVelocity;
            float speed = velocity.magnitude / 100f; // Adjust divisor based on your scale
            speed = Mathf.Clamp01(speed);
            
            // Calculate emission rate based on speed
            float currentRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, speed);
            
            // Update emission rate
            var rateOverTime = new ParticleSystem.MinMaxCurve(currentRate);
            emissionModule.rateOverTime = rateOverTime;
        }
    }

    public void UpdateExhaustPosition(float angle)
    {
        if (exhaustParticles == null || exhaustObj == null) return;

        // Get the car's current world position
        Vector3 carPosition = transform.position;
        
        // Convert the angle to radians
        float angleRad = angle * Mathf.Deg2Rad;
        
        // Calculate the offset direction vectors based on car's rotation
        float cosAngle = Mathf.Cos(angleRad);
        float sinAngle = Mathf.Sin(angleRad);
        
        // Calculate the position offset
        float offsetPosX = offsetX * cosAngle - offsetY * sinAngle;
        float offsetPosY = offsetX * sinAngle + offsetY * cosAngle;
        
        // Apply the offset to the car's position
        Vector3 exhaustWorldPos = new Vector3(
            carPosition.x + offsetPosX,
            carPosition.y + offsetPosY,
            carPosition.z
        );
        
        // Update particle system position
        exhaustObj.transform.position = exhaustWorldPos;
        
        // Update particle system rotation
        // The rotation should be 180 degrees offset from car rotation to emit backwards
        float emissionAngle = angle + 180f;
        exhaustObj.transform.rotation = Quaternion.Euler(0f, 0f, emissionAngle);
        
        // Store the last known position and angle
        lastPosition = carPosition;
        lastAngle = angle;
    }

    // Method to update particle properties at runtime
    public void UpdateParticleProperties(float size, float speed, float spread, float rate)
    {
        if (exhaustParticles == null) return;
        
        particleSize = size;
        particleSpeed = speed;
        particleSpread = spread;
        emissionRate = rate;
        
        var main = exhaustParticles.main;
        main.startSize = size;
        main.startSpeed = speed;
        
        var shape = exhaustParticles.shape;
        shape.angle = spread;
        
        var emission = exhaustParticles.emission;
        var rateOverTime = new ParticleSystem.MinMaxCurve(rate);
        emission.rateOverTime = rateOverTime;
        
        Debug.Log($"Updated exhaust properties - Size: {size}, Speed: {speed}, Spread: {spread}, Rate: {rate}");
    }
    
    // Method to update exhaust colors
    public void UpdateExhaustColors(Color initial, Color final)
    {
        if (exhaustParticles == null) return;
        
        initialColor = initial;
        finalColor = final;
        
        var main = exhaustParticles.main;
        main.startColor = initialColor;
        
        var colorOverLifetime = exhaustParticles.colorOverLifetime;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(initialColor, 0.0f),
                new GradientColorKey(finalColor, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(initialColor.a, 0.0f), 
                new GradientAlphaKey(finalColor.a, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        Debug.Log($"Updated exhaust colors - Initial: {initial}, Final: {final}");
    }

    // Force restart the particle system - use this if you notice the particles stopping
    public void RestartParticleSystem()
    {
        if (exhaustParticles != null)
        {
            exhaustParticles.Clear();
            exhaustParticles.Play();
        }
    }

    private void OnDestroy()
    {
        if (exhaustParticles != null)
        {
            var renderer = exhaustParticles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.material != null)
            {
                if (renderer.material.mainTexture != null)
                {
                    Destroy(renderer.material.mainTexture);
                }
                Destroy(renderer.material);
            }
            Destroy(exhaustObj);
        }
    }
}