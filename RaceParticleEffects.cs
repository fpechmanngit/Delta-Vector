using UnityEngine;
using System.Collections;

public class RaceParticleEffects : MonoBehaviour
{
    [Header("Checkpoint Particles")]
    public Color checkpointColor = new Color(1f, 0.8f, 0.2f, 1f);
    public float checkpointDuration = 1f;
    public float checkpointSize = 0.2f;
    public float checkpointSpeed = 5f;
    public int checkpointParticleCount = 30;

    [Header("Material References")]
    public Material particleMaterial;

    [Header("Failure Particles")]
    public Color failureColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float failureParticleSize = 0.3f;
    public float failureParticleSpeed = 4f;
    public int failureParticleCount = 30;
    public float failureEffectDuration = 2f;

    [Header("Firework Particles")]
    public Color[] fireworkColors = new Color[] {
        new Color(1f, 0.2f, 0.2f, 1f),
        new Color(0.2f, 1f, 0.2f, 1f),
        new Color(0.2f, 0.2f, 1f, 1f),
        new Color(1f, 1f, 0.2f, 1f),
        new Color(1f, 0.2f, 1f, 1f)
    };
    public float fireworkDuration = 0.5f;
    public float fireworkSize = 0.3f;
    public float fireworkSpeed = 8f;
    public int fireworkParticleCount = 50;
    public int fireworkBursts = 10;
    public float burstInterval = 0.2f;
    public float victoryDuration = 5f;

    private ParticleSystem checkpointParticles;
    private ParticleSystem fireworkParticles;
    private Camera mainCamera;
    private bool isShowingVictoryEffects = false;

    private void Awake()
    {
        var otherEffects = FindObjectsByType<RaceParticleEffects>(FindObjectsSortMode.None);
        if (otherEffects.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        if (particleMaterial == null)
            return;

        mainCamera = Camera.main;

        Camera minimapCamera = FindMinimapCamera();
        if (minimapCamera != null)
            minimapCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("RaceEffects"));
    }

    private Camera FindMinimapCamera()
    {
        var cameras = FindObjectsOfType<Camera>();
        foreach (var cam in cameras)
        {
            if (cam != mainCamera)
                return cam;
        }
        return null;
    }

    private void Start()
    {
        CreateCheckpointParticles();
        CreateFireworkParticles();
    }

    private void CreateCheckpointParticles()
    {
        GameObject checkpointObj = new GameObject("CheckpointParticles");
        checkpointObj.transform.SetParent(transform);
        checkpointParticles = checkpointObj.AddComponent<ParticleSystem>();

        checkpointParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = checkpointParticles.main;
        main.duration = checkpointDuration;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = checkpointDuration;
        main.startSpeed = checkpointSpeed;
        main.startSize = checkpointSize;
        main.startColor = checkpointColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = checkpointParticleCount;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.useUnscaledTime = true;

        var emission = checkpointParticles.emission;
        emission.rateOverTime = 0;
        emission.burstCount = 1;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, checkpointParticleCount));

        var shape = checkpointParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
        shape.radiusThickness = 1f;

        var sizeOverLifetime = checkpointParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = checkpointParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(checkpointColor, 0f),
                new GradientColorKey(checkpointColor, 0.7f),
                new GradientColorKey(checkpointColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = colorGradient;

        var renderer = checkpointParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sortingOrder = -1;
        renderer.material = particleMaterial;
        
        checkpointParticles.gameObject.layer = LayerMask.NameToLayer("RaceEffects");
    }

    private void CreateFireworkParticles()
    {
        GameObject fireworkObj = new GameObject("FireworkParticles");
        fireworkObj.transform.SetParent(transform);
        fireworkParticles = fireworkObj.AddComponent<ParticleSystem>();

        fireworkParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = fireworkParticles.main;
        main.duration = fireworkDuration;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = fireworkDuration * 2f;
        main.startSpeed = fireworkSpeed;
        main.startSize = fireworkSize;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = fireworkParticleCount * fireworkBursts;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.useUnscaledTime = true;

        var startColor = new ParticleSystem.MinMaxGradient();
        startColor.mode = ParticleSystemGradientMode.RandomColor;
        Gradient colorGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[fireworkColors.Length];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        
        for (int i = 0; i < fireworkColors.Length; i++)
        {
            colorKeys[i] = new GradientColorKey(fireworkColors[i], i / (float)(fireworkColors.Length - 1));
        }
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        
        colorGradient.SetKeys(colorKeys, alphaKeys);
        startColor.gradient = colorGradient;
        main.startColor = startColor;

        var emission = fireworkParticles.emission;
        emission.rateOverTime = 0;
        emission.burstCount = 1;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, fireworkParticleCount));

        var shape = fireworkParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var sizeOverLifetime = fireworkParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = fireworkParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.7f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = fadeGradient;

        var renderer = fireworkParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sortingOrder = -1;
        renderer.material = particleMaterial;

        fireworkParticles.gameObject.layer = LayerMask.NameToLayer("RaceEffects");
    }

    public void PlayCheckpointEffect(Vector3 position)
    {
        if (checkpointParticles != null)
        {
            checkpointParticles.transform.position = position;
            checkpointParticles.Clear();
            checkpointParticles.Play();
        }
    }

    public void PlayVictoryEffect(Vector3 position)
    {
        if (!isShowingVictoryEffects)
        {
            isShowingVictoryEffects = true;
            StartCoroutine(PlayVictoryFireworks());
        }
    }

    public void PlayFailureEffect(Vector3 position)
    {
        if (fireworkParticles == null) return;

        var main = fireworkParticles.main;
        var originalStartColor = main.startColor;
        var originalStartSize = main.startSize;
        var originalStartSpeed = main.startSpeed;
        var originalDuration = main.duration;

        main.startColor = failureColor;
        main.startSize = failureParticleSize;
        main.startSpeed = failureParticleSpeed;
        main.duration = failureEffectDuration;

        fireworkParticles.transform.position = position;
        fireworkParticles.Play();

        StartCoroutine(RestoreParticleValues(main, originalStartColor, originalStartSize, 
                                         originalStartSpeed, originalDuration));
    }

    private IEnumerator RestoreParticleValues(ParticleSystem.MainModule main, 
                                          ParticleSystem.MinMaxGradient originalColor,
                                          ParticleSystem.MinMaxCurve originalSize,
                                          ParticleSystem.MinMaxCurve originalSpeed,
                                          float originalDuration)
    {
        yield return new WaitForSeconds(failureEffectDuration);

        main.startColor = originalColor;
        main.startSize = originalSize;
        main.startSpeed = originalSpeed;
        main.duration = originalDuration;
    }

    private IEnumerator PlayVictoryFireworks()
    {
        float startTime = Time.realtimeSinceStartup;
        float endTime = startTime + victoryDuration;

        float height = 2f * mainCamera.orthographicSize;
        float width = height * mainCamera.aspect;
        Vector3 cameraPos = mainCamera.transform.position;
        
        float minX = cameraPos.x - width/2 + 1f;
        float maxX = cameraPos.x + width/2 - 1f;
        float minY = cameraPos.y - height/2 + 1f;
        float maxY = cameraPos.y + height/2 - 1f;

        while (Time.realtimeSinceStartup < endTime)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(minX, maxX),
                Random.Range(minY, maxY),
                0
            );

            var main = fireworkParticles.main;
            main.startColor = fireworkColors[Random.Range(0, fireworkColors.Length)];

            fireworkParticles.transform.position = randomPos;
            fireworkParticles.Clear();
            fireworkParticles.Play();

            yield return new WaitForSecondsRealtime(burstInterval);
        }

        isShowingVictoryEffects = false;
    }

    private void OnDestroy()
    {
        if (checkpointParticles != null)
            Destroy(checkpointParticles.gameObject);
        if (fireworkParticles != null)
            Destroy(fireworkParticles.gameObject);
    }
}