using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerGroundDetector : MonoBehaviour
{
    private PlayerSpeedController speedController;
    private bool isOnGravel = false;
    private bool[] surfaceTypeForFX = new bool[4]; // Store FX-specific surface type for each corner
    public bool IsOnGravel => isOnGravel;
    
    // New method to check if a position should use gravel FX
    public bool ShouldUseGravelFX()
    {
        // If 3 or more corners are on gravel (without asphalt), use gravel FX
        int gravelFXCount = 0;
        foreach (bool isGravel in surfaceTypeForFX)
        {
            if (isGravel) gravelFXCount++;
        }
        return gravelFXCount >= 3;
    }

    private void Awake()
    {
        speedController = GetComponent<PlayerSpeedController>();
        if (speedController == null)
        {
            Debug.LogError("PlayerSpeedController not found!");
        }
    }

    public void CheckGravelStatus(Vector2Int currentPosition)
    {
        var debugVisualizer = GetComponent<GravelDebugVisualizer>();
        GameObject asphaltObject = GameObject.FindGameObjectWithTag("Asphalt");
        GameObject gravelObject = GameObject.FindGameObjectWithTag("Gravel");
        
        Tilemap asphaltTilemap = asphaltObject?.GetComponent<Tilemap>();
        Tilemap gravelTilemap = gravelObject?.GetComponent<Tilemap>();

        // Get the actual world position
        Vector3 worldPos = transform.position;
        
        // Calculate base position using the actual position of the car
        Vector3Int basePosition = new Vector3Int(
            Mathf.FloorToInt(worldPos.x - 0.5f),
            Mathf.FloorToInt(worldPos.y - 0.5f),
            0
        );

        // Define the four tiles to check (starting from base position)
        Vector3Int[] tilesToCheck = new Vector3Int[]
        {
            basePosition,                                          // Bottom Left
            basePosition + new Vector3Int(0, 1, 0),               // Top Left
            basePosition + new Vector3Int(1, 1, 0),               // Top Right
            basePosition + new Vector3Int(1, 0, 0)                // Bottom Right
        };

        if (debugVisualizer != null)
        {
            debugVisualizer.UpdateDebugPoints(
                basePosition,
                asphaltTilemap,
                gravelTilemap
            );
        }

        int gravelSquares = 0;
        string debugTiles = "Tiles checked: ";

        // Check each tile
        for (int i = 0; i < tilesToCheck.Length; i++)
        {
            Vector3Int tilePos = tilesToCheck[i];
            bool hasAsphalt = (asphaltTilemap != null && asphaltTilemap.HasTile(tilePos));
            bool hasGravel = (gravelTilemap != null && gravelTilemap.HasTile(tilePos));
            
            // For movement purposes, treat overlapped tiles as gravel
            bool isGravelForMovement = !hasAsphalt && hasGravel;
            // For FX purposes, only count as gravel if there's no asphalt
            bool isGravelForFX = hasGravel && !hasAsphalt;

            if (isGravelForMovement)
            {
                gravelSquares++;
                debugTiles += "G";
            }
            else
            {
                debugTiles += "A";
            }

            // Store FX-specific surface info
            surfaceTypeForFX[i] = isGravelForFX;
        }

        // Car is on gravel only if 3 or 4 squares are gravel
        bool wasOnGravel = isOnGravel;
        isOnGravel = gravelSquares >= 3;
        
        // Update speed based on surface
        if (speedController != null)
        {
            if (isOnGravel)
            {
                float gravelSpeedMultiplier = speedController.minSpeed / speedController.baseSpeed;
                speedController.SetSpeedMultiplier(gravelSpeedMultiplier);
            }
            else
            {
                speedController.SetSpeedMultiplier(1f);
            }
        }

        // Detailed debug logging
        Debug.Log($"Position: {worldPos}, Base Tile: {basePosition}\n" +
                 $"{debugTiles}\n" +
                 $"Gravel squares: {gravelSquares}/4, Is On Gravel: {isOnGravel}\n" +
                 $"Changed state: {wasOnGravel != isOnGravel}");
    }
}