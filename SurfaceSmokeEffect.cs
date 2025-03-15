using UnityEngine;

public class SurfaceSmokeEffect : MonoBehaviour
{
    [Header("Common Settings")]
    public float baseEmissionRate = 1000f;
    public float lifetime = 1f;
    public float particleMinSpeed = 1f;
    public float particleMaxSpeed = 4f;
    public float spreadAngle = 30f;
    public float decelerationRate = 1f;
    public float groundOffset = 0.05f;

    [Header("Asphalt Smoke Settings")]
    public Color[] asphaltSmokeColors = new Color[] {
        new Color(0.8f, 0.8f, 0.8f, 0.6f),
        new Color(0.9f, 0.9f, 0.9f, 0.5f),
        new Color(0.7f, 0.7f, 0.7f, 0.4f),
        new Color(0.85f, 0.85f, 0.85f, 0.3f)
    };
    public float asphaltParticleMinSize = 0.05f;
    public float asphaltParticleMaxSize = 0.3f;
    
    [Header("Vacuum Effect")]
    public float vacuumEffectStrength = 1f;
    public float vacuumEffectProbability = 1f;
    public float vacuumEffectDelay = 0f;
    public float gravelVacuumMultiplier = 0f;

    [Header("Gravel Dust Settings")]
    public Color[] gravelDustColors = new Color[] {
        new Color(0.6f, 0.5f, 0.4f, 1.0f),
        new Color(0.7f, 0.6f, 0.5f, 1.0f),
        new Color(0.5f, 0.45f, 0.35f, 1.0f),
        new Color(0.65f, 0.55f, 0.45f, 1.0f)
    };
    public float gravelParticleMinSize = 0.1f;
    public float gravelParticleMaxSize = 0.2f;
    public float gravelEmissionMultiplier = 0.5f;
    public float gravelSpeedMultiplier = 1f;
    public float gravelWindEffectDivider = 3f;
    public float gravelEmissionPointsMultiplier = 1f;
    public float gravelLifetimeMultiplier = 1f;

    [Header("Directional Wind Settings")]
    public float windForceStrength = 5f;
    public float windDirectionVariance = 2f;
    public float initialBurstResistance = 10f;
    public float continuousWindMultiplier = 10f;

    [Header("Positional Settings")]
    public float offsetBehindCar = 0f;
    public float offsetWidth = 2f;
    public int emissionPoints = 2;
    public float verticalOffset = 0f;
    public int maxSmokePuffs = 5;

    private GameObject[] smokePuffs;
    private int currentPuffIndex = 0;
    private PlayerMovement playerMovement;
    private PlayerGroundDetector groundDetector;
    private bool wasOnGravel = false;
    private GameObject poolParent;
    private bool wasMoving = false;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        groundDetector = GetComponent<PlayerGroundDetector>();

        if (playerMovement == null || groundDetector == null)
        {
            enabled = false;
            return;
        }

        CreateSmokePool();
    }

    private void CreateSmokePool()
    {
        smokePuffs = new GameObject[maxSmokePuffs];
        poolParent = new GameObject("SurfaceSmokeEffectPool");
        poolParent.transform.SetParent(transform.parent);
        
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
        main.startSize = new ParticleSystem.MinMaxCurve(asphaltParticleMinSize, asphaltParticleMaxSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(asphaltSmokeColors[0], asphaltSmokeColors[asphaltSmokeColors.Length - 1]);
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
        Gradient gradient = new Gradient();
        gradient.SetKeys(
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
        colorOverLifetime.color = gradient;

        var limitVelocity = ps.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.limit = particleMaxSpeed * 5f;
        limitVelocity.dampen = 0.1f;

        var externalForces = ps.externalForces;
        externalForces.enabled = true;
        externalForces.multiplier = continuousWindMultiplier;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
        Texture2D particleTexture = CreateCircleTexture();
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

    private Texture2D CreateCircleTexture()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(size/2f, size/2f));
                float maxRadius = size / 2f;
                
                float alpha = 0;
                if (distanceFromCenter < maxRadius)
                {
                    alpha = 1.0f - (distanceFromCenter / maxRadius);
                    alpha = Mathf.Pow(alpha, 0.5f);
                }
                
                colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
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
        bool isOnGravel = groundDetector.ShouldUseGravelFX();
        int actualEmissionPoints = isOnGravel ? 
            Mathf.CeilToInt(emissionPoints * gravelEmissionPointsMultiplier) : 
            emissionPoints;

        for (int i = 0; i < actualEmissionPoints; i++)
        {
            GameObject currentPuff = smokePuffs[currentPuffIndex];
            if (currentPuff == null)
                continue;
                
            float horizontalOffset = (((float)i + 1) / (actualEmissionPoints + 1) - 0.5f) * offsetWidth;
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
                var main = ps.main;
                
                if (isOnGravel)
                {
                    main.startSize = new ParticleSystem.MinMaxCurve(gravelParticleMinSize, gravelParticleMaxSize);
                    main.startColor = new ParticleSystem.MinMaxGradient(gravelDustColors[0], gravelDustColors[gravelDustColors.Length - 1]);
                    main.startLifetime = lifetime * gravelLifetimeMultiplier;
                    
                    var emission = ps.emission;
                    ParticleSystem.Burst burst = emission.GetBurst(0);
                    burst.count = baseEmissionRate * gravelEmissionMultiplier;
                    emission.SetBurst(0, burst);

                    var colorOverLifetime = ps.colorOverLifetime;
                    Gradient gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] { 
                            new GradientColorKey(gravelDustColors[0], 0.0f),
                            new GradientColorKey(gravelDustColors[1], 0.3f),
                            new GradientColorKey(gravelDustColors[2], 0.7f),
                            new GradientColorKey(gravelDustColors[3], 1.0f)
                        },
                        new GradientAlphaKey[] { 
                            new GradientAlphaKey(1.0f, 0.0f),
                            new GradientAlphaKey(1.0f, 0.5f),
                            new GradientAlphaKey(0.7f, 0.8f),
                            new GradientAlphaKey(0.0f, 1.0f)
                        }
                    );
                    colorOverLifetime.color = gradient;
                    
                    if (gravelVacuumMultiplier > 0)
                    {
                        SetupVacuumEffect(ps, position, gravelVacuumMultiplier);
                    }
                }
                else
                {
                    main.startSize = new ParticleSystem.MinMaxCurve(asphaltParticleMinSize, asphaltParticleMaxSize);
                    main.startColor = new ParticleSystem.MinMaxGradient(asphaltSmokeColors[0], asphaltSmokeColors[asphaltSmokeColors.Length - 1]);
                    main.startLifetime = lifetime;
                    
                    var emission = ps.emission;
                    ParticleSystem.Burst burst = emission.GetBurst(0);
                    burst.count = baseEmissionRate;
                    emission.SetBurst(0, burst);
                    
                    var colorOverLifetime = ps.colorOverLifetime;
                    Gradient gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] { 
                            new GradientColorKey(asphaltSmokeColors[0], 0.0f),
                            new GradientColorKey(asphaltSmokeColors[1], 0.3f),
                            new GradientColorKey(asphaltSmokeColors[2], 0.7f),
                            new GradientColorKey(asphaltSmokeColors[3], 1.0f)
                        },
                        new GradientAlphaKey[] { 
                            new GradientAlphaKey(0.1f, 0.0f),
                            new GradientAlphaKey(0.7f, 0.3f),
                            new GradientAlphaKey(0.5f, 0.7f),
                            new GradientAlphaKey(0.0f, 1.0f)
                        }
                    );
                    colorOverLifetime.color = gradient;
                    
                    SetupVacuumEffect(ps, position, 1f);
                }
                
                var velocityModule = ps.velocityOverLifetime;
                float forceMagnitude = isOnGravel ? 
                    windForceStrength * windMagnitude / gravelWindEffectDivider : 
                    windForceStrength * windMagnitude;
                
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
                
                float speedMultiplier = isOnGravel ? gravelSpeedMultiplier : 1f;
                
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    particleMinSpeed * windMagnitude * 0.5f * speedMultiplier,
                    particleMaxSpeed * windMagnitude * 0.5f * speedMultiplier
                );
                
                currentPuff.SetActive(true);
                ps.Play();
            }
            
            currentPuffIndex = (currentPuffIndex + 1) % maxSmokePuffs;
        }
        
        wasOnGravel = isOnGravel;
    }
    
    private void SetupVacuumEffect(ParticleSystem ps, Vector3 carPosition, float multiplier)
    {
        if (multiplier <= 0f || Random.value > vacuumEffectProbability) 
            return;
        
        float effectiveVacuumStrength = vacuumEffectStrength * multiplier;
        
        AnimationCurve vacuumCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(vacuumEffectDelay, 0f),
            new Keyframe(vacuumEffectDelay + 0.2f, effectiveVacuumStrength),
            new Keyframe(1f, effectiveVacuumStrength * 2f)
        );
        
        var forceModule = ps.forceOverLifetime;
        forceModule.enabled = true;
        forceModule.space = ParticleSystemSimulationSpace.World;
        
        Vector3 directionToVacuum = (carPosition - ps.transform.position).normalized;
        forceModule.x = new ParticleSystem.MinMaxCurve(directionToVacuum.x * effectiveVacuumStrength, vacuumCurve);
        forceModule.y = new ParticleSystem.MinMaxCurve(directionToVacuum.y * effectiveVacuumStrength, vacuumCurve);
        
        var trails = ps.trails;
        if (!trails.enabled)
        {
            trails.enabled = true;
            trails.ratio = 0.3f;
            trails.lifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.1f, 0.05f);
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.worldSpace = false;
            
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.trailMaterial = renderer.material;
        }
    }

    private void Update()
    {
        if (playerMovement != null)
        {
            bool isMovingNow = playerMovement.IsMoving;
            
            if (!wasMoving && isMovingNow)
            {
                float angle = Mathf.Atan2(playerMovement.CurrentVelocity.y, playerMovement.CurrentVelocity.x) * Mathf.Rad2Deg;
                TriggerSmokeEffect(transform.position, angle);
            }
            
            wasMoving = isMovingNow;
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
        
        if (poolParent != null)
            Destroy(poolParent);
    }
}