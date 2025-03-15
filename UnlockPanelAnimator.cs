using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class UnlockPanelAnimator : MonoBehaviour
{
    [Header("Gradient Effect")]
    public bool useGradientEffect = true;
    [Range(0f, 2f)]
    public float gradientSpeed = 0.5f;
    public Color[] gradientColors = new Color[] {
        new Color(0.5f, 0.9f, 1f, 0.9f),    // Bright Blue
        new Color(0.9f, 0.5f, 1f, 0.9f),    // Bright Purple
        new Color(1f, 0.5f, 0.9f, 0.9f),    // Bright Pink
        new Color(0.5f, 1f, 0.9f, 0.9f)     // Bright Cyan
    };

    [Header("Glitter Effect")]
    public bool useGlitterEffect = true;
    [Range(100, 50000)]
    public int particleCount = 2000;
    [Range(0.01f, 1f)]
    public float particleSize = 0.1f;
    [Range(1f, 100f)]
    public float particleSpeed = 20f;
    public Color particleColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("Scale Animation")]
    [Range(0.1f, 2f)]
    public float scaleTime = 0.5f;
    [Range(0.1f, 1f)]
    public float startScale = 0.3f;
    [Range(1f, 1.2f)]
    public float bounceOvershoot = 1.1f;

    private float animationTime;
    private bool isAnimating = false;
    private Image panelImage;
    private ParticleSystem glitterParticles;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera canvasCamera;

    private void Awake()
    {
        // Get our components
        panelImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        
        if (parentCanvas != null)
        {
            canvasCamera = parentCanvas.worldCamera;
        }

        // Setup mask for the panel
        var mask = gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        if (useGlitterEffect)
        {
            SetupGlitterParticles();
        }
    }

    private void SetupGlitterParticles()
    {
        // Create particle system as child of this panel
        GameObject particleObj = new GameObject("GlitterParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        particleObj.transform.localRotation = Quaternion.identity;
        particleObj.transform.localScale = Vector3.one;

        // Setup particle system
        glitterParticles = particleObj.AddComponent<ParticleSystem>();
        var mainModule = glitterParticles.main;
        mainModule.playOnAwake = true;
        mainModule.duration = 1f;
        mainModule.loop = true;
        mainModule.startLifetime = 0.5f;
        mainModule.startSpeed = particleSpeed;
        mainModule.startSize = particleSize;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        mainModule.maxParticles = particleCount;
        mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Emission setup - increased to fill larger area
        var emission = glitterParticles.emission;
        emission.rateOverTime = particleCount * 4; // Increased emission rate

        // Shape setup to match panel size exactly
        var shape = glitterParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        // Get exact panel size in world units
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float width = Vector3.Distance(corners[0], corners[3]);
        float height = Vector3.Distance(corners[0], corners[1]);
        shape.scale = new Vector3(width, height, 1); // Using full width/height
        shape.randomDirectionAmount = 1.0f; // Increased for better spread
        shape.randomPositionAmount = 1.0f; // Full random position within shape

        // Add velocity limits to keep particles contained
        var limitVelocity = glitterParticles.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.limit = particleSpeed;
        limitVelocity.dampen = 0.5f;

        // Setup collision
        var collision = glitterParticles.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.bounce = 0.5f;
        collision.lifetimeLoss = 0.2f;

        // Color and fade setup
        var colorOverLifetime = glitterParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(particleColor, 0.0f),
                new GradientColorKey(particleColor, 0.7f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;

        // Setup renderer for camera space
        var renderer = glitterParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 2;
        Material particleMaterial = new Material(Shader.Find("UI/Default"));
        renderer.material = particleMaterial;
        renderer.sortingLayerID = parentCanvas.sortingLayerID;
        renderer.sortingOrder = panelImage.canvas.sortingOrder + 1;

        // Add a Canvas to ensure proper rendering in screen space camera
        Canvas particleCanvas = particleObj.AddComponent<Canvas>();
        particleCanvas.overrideSorting = true;
        particleCanvas.sortingOrder = panelImage.canvas.sortingOrder + 1;
        particleCanvas.worldCamera = canvasCamera;

        glitterParticles.Play();
    }

    public void StartAnimation()
    {
        StopAllCoroutines();
        isAnimating = true;
        animationTime = 0f;
        
        // Start scale animation
        rectTransform.localScale = Vector3.one * startScale;
        StartCoroutine(ScaleAnimation());

        if (glitterParticles != null)
        {
            glitterParticles.Clear();
            glitterParticles.Play();
        }
    }

    private IEnumerator ScaleAnimation()
    {
        float elapsed = 0f;
        while (elapsed < scaleTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / scaleTime;
            
            // Bounce easing
            t = Mathf.Sin(t * Mathf.PI * (0.2f + t * 0.3f)) * Mathf.Pow(1f - t, 2f) * bounceOvershoot + t;
            
            rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, t);
            yield return null;
        }
        rectTransform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!isAnimating) return;

        animationTime += Time.unscaledDeltaTime;

        if (useGradientEffect && panelImage != null)
        {
            UpdateBackgroundEffect();
        }

        // Update particle bounds if panel size changes
        if (glitterParticles != null)
        {
            UpdateParticleBounds();
        }
    }

    private void UpdateParticleBounds()
    {
        // Get current panel bounds in world space
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float width = Vector3.Distance(corners[0], corners[3]);
        float height = Vector3.Distance(corners[0], corners[1]);

        var shape = glitterParticles.shape;
        shape.scale = new Vector3(width, height, 1); // Using full panel dimensions
    }

    private void UpdateBackgroundEffect()
    {
        try
        {
            float normalizedTime = (animationTime * gradientSpeed) % gradientColors.Length;
            int currentIndex = Mathf.FloorToInt(normalizedTime);
            float lerpAmount = normalizedTime - currentIndex;
            
            Color currentColor = gradientColors[currentIndex];
            Color nextColor = gradientColors[(currentIndex + 1) % gradientColors.Length];
            
            Color lerpedColor = Color.Lerp(currentColor, nextColor, lerpAmount);
            lerpedColor.a = 0.95f;
            
            panelImage.color = lerpedColor;
        }
        catch (System.Exception)
        {
            // Silently catch any exceptions
        }
    }

    private void OnDisable()
    {
        isAnimating = false;

        if (glitterParticles != null)
        {
            glitterParticles.Stop();
        }
    }

    private void OnDestroy()
    {
        if (glitterParticles != null)
        {
            var renderer = glitterParticles.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.material != null)
            {
                Destroy(renderer.material);
            }
            Destroy(glitterParticles.gameObject);
        }
    }
}