using UnityEngine;
using System.Collections.Generic;

public class CarSurfaceEffects : MonoBehaviour
{
    [Header("Tire Tracks")]
    public float trackWidth = 0.3f;
    public float trackSpacing = 0.4f;
    public Color asphaltTrackColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color gravelTrackColor = new Color(0.7f, 0.6f, 0.5f, 0.5f);
    
    [Header("Track Conditions")]
    public float minSpeedForAsphaltTracks = 0.1f;
    public float minTurnAngleForAsphaltTracks = 5f;

    private PlayerMovement playerMovement;
    private PlayerSpeedController playerSpeedController;
    private AudioManager audioManager;
    private List<GameObject> trackSegments = new List<GameObject>();
    private bool wasOnGravel = false;
    private bool currentlyOnGravel = false;
    private Vector2 currentDirection = Vector2.right;

    private Vector3 moveStartPos;
    private Vector3 moveEndPos;
    private Color currentTrackColor;
    private bool shouldCreateTracksForMove = false;
    private LineRenderer leftTrack;
    private LineRenderer rightTrack;
    private Vector3 trackOffset;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerSpeedController = GetComponent<PlayerSpeedController>();
        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement not found on car!");
        }
        if (playerSpeedController == null)
        {
            Debug.LogError("PlayerSpeedController not found on car!");
        }
        audioManager = Object.FindFirstObjectByType<AudioManager>();

        Debug.Log("CarSurfaceEffects initialized");
    }

    private LineRenderer CreateTrackSegment(string name)
    {
        GameObject segmentObj = new GameObject(name);
        segmentObj.transform.SetParent(transform.parent);
        
        LineRenderer line = segmentObj.AddComponent<LineRenderer>();
        
        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
        line.material = lineMaterial;
        
        line.startWidth = trackWidth;
        line.endWidth = trackWidth;
        line.numCapVertices = 5;
        line.sortingOrder = -1;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.TransformZ;
        
        Debug.Log($"Created track segment {name} with width {trackWidth}, sorting order {line.sortingOrder}");
        trackSegments.Add(segmentObj);
        return line;
    }

    private bool ShouldCreateTracks(Vector2Int position, Vector2 moveDirection)
    {
        if (playerSpeedController == null || playerMovement == null)
            return false;

        if (wasOnGravel)
            return true;

        float speed = playerMovement.CurrentVelocity.magnitude / (float)PlayerMovement.GRID_SCALE;
        float angle = Vector2.Angle(currentDirection, moveDirection);

        bool speedCheck = speed >= 0.3f;
        bool angleCheck = angle >= minTurnAngleForAsphaltTracks;

        Debug.Log($"Track check - Speed: {speed}/{0.3f}, " +
                 $"Angle: {angle}°/{minTurnAngleForAsphaltTracks}, " +
                 $"Create tracks: {speedCheck && angleCheck}");

        return speedCheck && angleCheck;
    }

    public void StartTrackGeneration(Vector2Int targetPosition)
    {
        if (playerMovement == null)
        {
            Debug.LogError("StartTrackGeneration: PlayerMovement is null!");
            return;
        }

        if (leftTrack != null)
            Destroy(leftTrack.gameObject);
        if (rightTrack != null)
            Destroy(rightTrack.gameObject);
        leftTrack = null;
        rightTrack = null;

        moveStartPos = transform.position;
        moveEndPos = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        
        PlayerGroundDetector groundDetector = GetComponent<PlayerGroundDetector>();
        if (groundDetector != null)
        {
            currentlyOnGravel = groundDetector.ShouldUseGravelFX();
        }
        else
        {
            currentlyOnGravel = CheckSurface(targetPosition);
        }
        
        Vector2 moveDirection = ((Vector2)moveEndPos - (Vector2)moveStartPos).normalized;

        shouldCreateTracksForMove = wasOnGravel || ShouldCreateTracks(targetPosition, moveDirection);

        Debug.Log($"StartTrackGeneration - Should create tracks: {shouldCreateTracksForMove}, " +
                 $"Start: {moveStartPos}, End: {moveEndPos}, " +
                 $"On gravel: {currentlyOnGravel}, Was on gravel: {wasOnGravel}");

        if (shouldCreateTracksForMove)
        {
            if (wasOnGravel)
            {
                audioManager?.StartGravelSound();
            }
            else
            {
                audioManager?.PlayTireScreech();
            }

            currentTrackColor = wasOnGravel ? gravelTrackColor : asphaltTrackColor;

            Vector3 forward = (moveEndPos - moveStartPos).normalized;
            Vector3 right = Vector3.Cross(forward, Vector3.forward).normalized;
            trackOffset = right * (trackSpacing / 2f);

            leftTrack = CreateTrackSegment($"Track_Left_{Time.frameCount}");
            rightTrack = CreateTrackSegment($"Track_Right_{Time.frameCount}");

            leftTrack.startColor = currentTrackColor;
            leftTrack.endColor = currentTrackColor;
            rightTrack.startColor = currentTrackColor;
            rightTrack.endColor = currentTrackColor;

            leftTrack.SetPosition(0, moveStartPos + trackOffset);
            leftTrack.SetPosition(1, moveStartPos + trackOffset);
            rightTrack.SetPosition(0, moveStartPos - trackOffset);
            rightTrack.SetPosition(1, moveStartPos - trackOffset);

            Debug.Log($"Created tracks with color {currentTrackColor}, " +
                     $"Track offset: {trackOffset}, " +
                     $"Initial left pos: {moveStartPos + trackOffset}");
        }

        currentDirection = moveDirection;
    }

    public void UpdateTrackProgress(float progress)
    {
        if (!shouldCreateTracksForMove || leftTrack == null || rightTrack == null) 
        {
            return;
        }

        Vector3 currentEndPos = Vector3.Lerp(moveStartPos, moveEndPos, progress);
        Debug.Log($"Updating track progress: {progress}, Current End: {currentEndPos}, Move End: {moveEndPos}");

        Vector3 forward = (moveEndPos - moveStartPos).normalized;
        Vector3 right = Vector3.Cross(forward, Vector3.forward).normalized;
        Vector3 currentOffset = right * (trackSpacing / 2f);

        leftTrack.positionCount = 2;
        rightTrack.positionCount = 2;

        leftTrack.SetPosition(0, moveStartPos + currentOffset);
        leftTrack.SetPosition(1, currentEndPos + currentOffset);

        rightTrack.SetPosition(0, moveStartPos - currentOffset);
        rightTrack.SetPosition(1, currentEndPos - currentOffset);

        leftTrack.enabled = true;
        rightTrack.enabled = true;

        Debug.Log($"Track positions updated - Left track from: {moveStartPos + currentOffset} to {currentEndPos + currentOffset}");
    }

    public void FinishTrackGeneration()
    {
        if (!shouldCreateTracksForMove || leftTrack == null || rightTrack == null) return;

        leftTrack.SetPosition(0, moveStartPos + trackOffset);
        leftTrack.SetPosition(1, moveEndPos + trackOffset);
        rightTrack.SetPosition(0, moveStartPos - trackOffset);
        rightTrack.SetPosition(1, moveEndPos - trackOffset);

        leftTrack = null;
        rightTrack = null;
        shouldCreateTracksForMove = false;

        Debug.Log($"Finished track generation - Final left track: {moveStartPos + trackOffset} to {moveEndPos + trackOffset}");
    }

    private bool CheckSurface(Vector2Int position)
    {
        PlayerGroundDetector groundDetector = GetComponent<PlayerGroundDetector>();
        if (groundDetector != null)
        {
            return groundDetector.ShouldUseGravelFX();
        }

        GameObject asphaltObject = GameObject.FindGameObjectWithTag("Asphalt");
        GameObject gravelObject = GameObject.FindGameObjectWithTag("Gravel");
        
        Vector3Int gridPosition = new Vector3Int(position.x, position.y, 0);
        
        bool hasAsphalt = false;
        bool hasGravel = false;

        if (asphaltObject != null)
        {
            var asphaltTilemap = asphaltObject.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            hasAsphalt = (asphaltTilemap != null && asphaltTilemap.HasTile(gridPosition));
        }
        
        if (gravelObject != null)
        {
            var gravelTilemap = gravelObject.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            hasGravel = (gravelTilemap != null && gravelTilemap.HasTile(gridPosition));
        }

        return hasGravel && !hasAsphalt;
    }

    public void EndTurn()
    {
        wasOnGravel = currentlyOnGravel;
        if (!wasOnGravel)
        {
            audioManager?.StopGravelSound();
        }
    }

    public void ClearTracks()
    {
        foreach (var segment in trackSegments)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }
        trackSegments.Clear();
        wasOnGravel = false;
        currentlyOnGravel = false;
        currentDirection = Vector2.right;
        
        leftTrack = null;
        rightTrack = null;
        shouldCreateTracksForMove = false;
        
        Debug.Log("Cleared all track segments");
    }

    private void OnDestroy()
    {
        ClearTracks();
    }
}