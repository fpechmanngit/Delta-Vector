using UnityEngine;
using System.Collections.Generic;

public class TrailManager : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float maxSpeed = 5f;
    
    [Header("Color Settings")]
    public Color lowSpeedColor = Color.white;
    public Color mediumSpeedColor = Color.yellow;
    public Color highSpeedColor = Color.red;
    [Range(0f, 1f)]
    public float mediumSpeedThreshold = 0.5f;

    [Header("Trail Settings")]
    public float trailWidth = 0.2f;
    public bool fadeTrail = true;
    [Tooltip("How many turns the trail stays visible before starting to fade")]
    public int trailTurnDuration = 10;
    [Tooltip("How many additional turns it takes to fade out completely")]
    public int fadeTurnDuration = 5;

    private PlayerMovement playerMovement;
    private List<TrailPoint> trailPoints = new List<TrailPoint>();
    private Material trailMaterial;
    private int currentTurn = 0;

    private class TrailPoint
    {
        public Vector3 position;
        public Color color;
        public int turnCreated;

        public TrailPoint(Vector3 pos, Color col, int turn)
        {
            position = pos;
            color = col;
            turnCreated = turn;
        }
    }

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement component not found on the same GameObject!");
        }
        
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer not assigned to TrailManager!");
        }
        else
        {
            lineRenderer.startWidth = trailWidth;
            lineRenderer.endWidth = trailWidth;
            
            // Make sure the material supports transparency
            trailMaterial = new Material(lineRenderer.material);
            trailMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            trailMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineRenderer.material = trailMaterial;
        }
    }

    public void IncrementTurn()
    {
        currentTurn++;
        UpdateTrailFade();
    }

    private void UpdateTrailFade()
    {
        if (!fadeTrail || trailPoints.Count == 0) return;

        bool pointsRemoved = false;

        // Update trail points and remove old ones
        for (int i = trailPoints.Count - 1; i >= 0; i--)
        {
            int turnAge = currentTurn - trailPoints[i].turnCreated;
            
            // Remove points that are completely faded
            if (turnAge > trailTurnDuration + fadeTurnDuration)
            {
                trailPoints.RemoveAt(i);
                pointsRemoved = true;
                continue;
            }
            
            // Update color alpha for fading points
            if (turnAge > trailTurnDuration)
            {
                float fadeProgress = (float)(turnAge - trailTurnDuration) / fadeTurnDuration;
                Color newColor = trailPoints[i].color;
                newColor.a = 1f - Mathf.Clamp01(fadeProgress);
                trailPoints[i].color = newColor;
            }
        }

        // Update line renderer if needed
        if (pointsRemoved || fadeTrail)
        {
            UpdateLineRenderer();
        }
    }

    public void AddTrailPoint(Vector3 position)
    {
        if (lineRenderer == null || playerMovement == null) return;

        // Calculate speed based on precise grid velocity
        float speed = playerMovement.CurrentVelocity.magnitude / (float)PlayerMovement.GRID_SCALE;
        float speedRatio = Mathf.Clamp01(speed / maxSpeed);

        Color trailColor;
        if (speedRatio <= mediumSpeedThreshold)
        {
            float t = speedRatio / mediumSpeedThreshold;
            trailColor = Color.Lerp(lowSpeedColor, mediumSpeedColor, t);
        }
        else
        {
            float t = (speedRatio - mediumSpeedThreshold) / (1f - mediumSpeedThreshold);
            trailColor = Color.Lerp(mediumSpeedColor, highSpeedColor, t);
        }

        // Add new trail point with current turn number
        trailPoints.Add(new TrailPoint(position, trailColor, currentTurn));
        
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        if (trailPoints.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = trailPoints.Count;
        
        // Update both positions and colors
        Color[] colors = new Color[trailPoints.Count];
        for (int i = 0; i < trailPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, trailPoints[i].position);
            colors[i] = trailPoints[i].color;
        }

        // Set colors along the line
        lineRenderer.startColor = colors[0];
        lineRenderer.endColor = colors[colors.Length - 1];
    }

    public void ResetTrail()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            trailPoints.Clear();
            currentTurn = 0;
        }
    }
}