using UnityEngine;

public class CombinedSmokeEffect : MonoBehaviour
{
    [Header("Smoke Particle Settings")]
    public Color[] smokeColors = new Color[] {
        new Color(0.8f, 0.8f, 0.8f, 0.6f),
        new Color(0.9f, 0.9f, 0.9f, 0.5f),
        new Color(0.7f, 0.7f, 0.7f, 0.4f),
        new Color(0.85f, 0.85f, 0.85f, 0.3f)
    };
    public float particleMinSize = 0.05f;
    public float particleMaxSize = 0.2f;
    public float baseEmissionRate = 20000f;
    public float lifetime = 1.2f;
    public float particleMinSpeed = 1f;
    public float particleMaxSpeed = 2.5f;
    public float spreadAngle = 20f;
    public float decelerationRate = 0.3f;
    public float groundOffset = 0.05f;

    [Header("Directional Wind Settings")]
    [Range(0f, 20f)]
    public float windForceStrength = 10f;
    [Range(0f, 1f)]
    public float windDirectionVariance = 0.2f;
    [Range(0f, 10f)]
    public float initialBurstResistance = 3f;
    [Range(0f, 10f)]
    public float continuousWindMultiplier = 5f;

    [Header("Positional Settings")]
    public float offsetBehindCar = 0.3f;
    public float offsetWidth = 0.8f;
    public int emissionPoints = 5;
    public float verticalOffset = -0.1f;
    public float directionMultiplier = 0.6f;
    public int maxSmokePuffs = 30;

    private GameObject[] smokePuffs;
    private int currentPuffIndex = 0;
    private PlayerMovement playerMovement;
    private PlayerGroundDetector groundDetector;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        groundDetector = GetComponent<PlayerGroundDetector>();

        if (playerMovement == null || groundDetector == null)
        {
            Debug.LogError("[CombinedSmoke] Missing required components");
            enabled = false;
            return;
        }

        CreateSmokePool();
    }

    private void CreateSmokePool()
    {
        smokePuffs = new GameObject[maxSmokePuffs];
        
        GameObject poolParent = new GameObject("SmokeEffectPool");
        poolParent.transform.SetParent(null);
        
        for (int i = 0; i < maxSmokePuffs; i++)
        {
            smokePuffs[i] = new GameObject($"SmokePuff_{i}");
            smokePuffs[i].transform.SetParent(poolParent.transform);
            
            ParticleSystem ps = smokePuffs[i].AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ConfigureParticleSystem(ps);
            
            smokePuffs[i].SetActive(false);
        }
    }

    private void ConfigureParticleSystem(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.1f;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleMinSpeed, particleMaxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(particleMinSize, particleMaxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(smokeColors[0], smokeColors[smokeColors.Length - 1]);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200000;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        
        emission.burstCount = 1;
        ParticleSystem.Burst burst = new ParticleSystem.Burst(0.0f, Mathf.RoundToInt(baseEmissionRate));
        emission.SetBurst(0, burst);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spreadAngle / 2f;
        shape.radius = 0.05f;
        shape.radiusMode = ParticleSystemShapeMultiModeValue.Random;
        shape.radiusSpread = 0.05f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.3f);
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        
        AnimationCurve windCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.05f, 0.7f),
            new Keyframe(0.3f, 1.0f),
            new Keyframe(1, 0.8f)
        );
        
        velocityOverLifetime.speedModifier = new ParticleSystem.MinMaxCurve(1f, windCurve);
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-windForceStrength, windForceStrength * windDirectionVariance);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-windForceStrength, windForceStrength * windDirectionVariance);

        var forceOverLifetime = ps.forceOverLifetime;
        forceOverLifetime.enabled = true;
        forceOverLifetime.space = ParticleSystemSimulationSpace.World;
        
        forceOverLifetime.x = new ParticleSystem.MinMaxCurve(0f);
        forceOverLifetime.y = new ParticleSystem.MinMaxCurve(0f);
        forceOverLifetime.z = new ParticleSystem.MinMaxCurve(0f);

        var inheritVelocity = ps.inheritVelocity;
        inheritVelocity.enabled = true;
        inheritVelocity.mode = ParticleSystemInheritVelocityMode.Initial;
        inheritVelocity.curve = new ParticleSystem.MinMaxCurve(initialBurstResistance);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.3f),
            new Keyframe(0.3f, 0.8f),
            new Keyframe(0.7f, 1.0f),
            new Keyframe(1.0f, 0.7f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-30f, 30f);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.1f, 0.0f),
                new GradientAlphaKey(0.7f, 0.3f),
                new GradientAlphaKey(0.5f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        
        colorOverLifetime.color = colorGradient;

        var limitVelocity = ps.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.limit = particleMaxSpeed * 5f;
        limitVelocity.dampen = 0.1f;

        var externalForces = ps.externalForces;
        externalForces.enabled = true;
        externalForces.multiplier = continuousWindMultiplier;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
        
        Texture2D particleTexture = CreateSmokeTexture();
        
        particleMaterial.mainTexture = particleTexture;
        renderer.material = particleMaterial;
        renderer.sortingOrder = -3;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        particleMaterial.SetFloat("_Mode", 2);
        particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        particleMaterial.SetInt("_ZWrite", 0);
        particleMaterial.DisableKeyword("_ALPHATEST_ON");
        particleMaterial.EnableKeyword("_ALPHABLEND_ON");
        particleMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        particleMaterial.renderQueue = 3000;
    }

    private Texture2D CreateSmokeTexture()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(size/2f, size/2f));
                float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.5f;
                
                float alpha = 0;
                if (distanceFromCenter < size/2f - noise * 5)
                {
                    alpha = 1f;
                }
                else if (distanceFromCenter < size/2f)
                {
                    alpha = 1f - (distanceFromCenter - (size/2f - noise * 5)) / (noise * 5);
                }
                
                float internalNoise = Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 0.2f + 0.8f;
                colors[y * size + x] = new Color(internalNoise, internalNoise, internalNoise, alpha * 0.8f);
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    public void TriggerSmokeEffect(Vector3 position, float angle)
    {
        if (smokePuffs == null || smokePuffs.Length == 0)
            return;

        position.y -= groundOffset;
        
        Vector2Int velocity = Vector2Int.zero;
        if (playerMovement != null)
        {
            velocity = playerMovement.CurrentVelocity;
        }
        
        Vector2 windDirection = Vector2.zero;
        if (velocity.magnitude > 0)
        {
            windDirection = new Vector2(-velocity.x, -velocity.y).normalized;
        }
        else
        {
            windDirection = new Vector2(-Mathf.Cos(angle * Mathf.Deg2Rad), -Mathf.Sin(angle * Mathf.Deg2Rad));
        }
        
        float windMagnitude = Mathf.Max(2f, velocity.magnitude / 5f);
        
        for (int i = 0; i < emissionPoints; i++)
        {
            GameObject currentPuff = smokePuffs[currentPuffIndex];
            if (currentPuff == null)
                continue;
                
            float horizontalOffset = (((float)i + 1) / (emissionPoints + 1) - 0.5f) * offsetWidth;
            float perpendicularX = -Mathf.Sin(angle * Mathf.Deg2Rad) * horizontalOffset;
            float perpendicularY = Mathf.Cos(angle * Mathf.Deg2Rad) * horizontalOffset;
            float backwardX = -Mathf.Cos(angle * Mathf.Deg2Rad) * offsetBehindCar;
            float backwardY = -Mathf.Sin(angle * Mathf.Deg2Rad) * offsetBehindCar;
            
            Vector3 emitterWorldPos = new Vector3(
                position.x + perpendicularX + backwardX,
                position.y + perpendicularY + backwardY + verticalOffset,
                position.z + 0.1f
            );
            
            currentPuff.transform.position = emitterWorldPos;
            
            float emissionAngle = angle + 180f;
            currentPuff.transform.rotation = Quaternion.Euler(0, 0, emissionAngle);
            
            ParticleSystem ps = currentPuff.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                
                var velocityModule = ps.velocityOverLifetime;
                float forceMagnitude = windForceStrength * windMagnitude;
                
                velocityModule.x = new ParticleSystem.MinMaxCurve(
                    windDirection.x * forceMagnitude * (1f - windDirectionVariance),
                    windDirection.x * forceMagnitude
                );
                
                velocityModule.y = new ParticleSystem.MinMaxCurve(
                    windDirection.y * forceMagnitude * (1f - windDirectionVariance),
                    windDirection.y * forceMagnitude
                );
                
                var forceModule = ps.forceOverLifetime;
                if (forceModule.enabled)
                {
                    float continuousForce = forceMagnitude * 0.3f;
                    forceModule.x = new ParticleSystem.MinMaxCurve(windDirection.x * continuousForce);
                    forceModule.y = new ParticleSystem.MinMaxCurve(windDirection.y * continuousForce);
                }
                
                var main = ps.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    particleMinSpeed * windMagnitude * 0.5f,
                    particleMaxSpeed * windMagnitude * 0.5f
                );
                
                currentPuff.SetActive(true);
                ps.Play();
            }
            
            currentPuffIndex = (currentPuffIndex + 1) % maxSmokePuffs;
        }
    }

    public void ClearAllParticles()
    {
        if (smokePuffs != null)
        {
            foreach (var puff in smokePuffs)
            {
                if (puff != null)
                {
                    ParticleSystem ps = puff.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Clear();
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (smokePuffs != null)
        {
            foreach (var puff in smokePuffs)
            {
                if (puff != null)
                {
                    ParticleSystem ps = puff.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                        if (renderer != null && renderer.material != null)
                        {
                            if (renderer.material.mainTexture != null)
                                Destroy(renderer.material.mainTexture);
                            Destroy(renderer.material);
                        }
                    }
                    Destroy(puff);
                }
            }
        }
        
        Transform poolParent = transform.Find("SmokeEffectPool");
        if (poolParent != null)
            Destroy(poolParent.gameObject);
    }
}