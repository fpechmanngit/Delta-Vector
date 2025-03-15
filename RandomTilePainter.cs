using UnityEngine;
using UnityEngine.Tilemaps;

public class RandomTilePainter : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap targetTilemap;
    public TileBase tileToUse;

    [Header("Fill Settings")]
    [Range(0f, 1f)]
    public float fillProbability = 0.5f;
    public bool includeDiagonals = true;
    public Vector2Int gridSize = new Vector2Int(5, 5);
    public Vector2Int startPosition = new Vector2Int(0, 0);

    [Header("Debug")]
    public bool showDebugGrid = true;
    public Color debugColor = Color.yellow;

    private void OnValidate()
    {
        // Ensure grid size is always positive
        gridSize.x = Mathf.Max(1, gridSize.x);
        gridSize.y = Mathf.Max(1, gridSize.y);
    }

    public void FillRandomTiles()
    {
        if (targetTilemap == null || tileToUse == null)
        {
            Debug.LogError("Please assign Tilemap and Tile references!");
            return;
        }

        // Clear existing tiles in the area first
        ClearArea();

        // Fill random tiles
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (Random.value < fillProbability)
                {
                    Vector3Int tilePosition = new Vector3Int(
                        startPosition.x + x,
                        startPosition.y + y,
                        0
                    );
                    targetTilemap.SetTile(tilePosition, tileToUse);
                }
            }
        }

        Debug.Log($"Random tiles placed in {gridSize.x}x{gridSize.y} grid with {fillProbability * 100}% fill rate");
    }

    public void ClearArea()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3Int tilePosition = new Vector3Int(
                    startPosition.x + x,
                    startPosition.y + y,
                    0
                );
                targetTilemap.SetTile(tilePosition, null);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGrid) return;

        Gizmos.color = debugColor;

        // Draw grid outline
        Vector3 startPos = new Vector3(startPosition.x, startPosition.y, 0);
        Vector3 size = new Vector3(gridSize.x, gridSize.y, 0);

        // Draw the outer rectangle
        Gizmos.DrawWireCube(
            startPos + (size / 2f) - new Vector3(0.5f, 0.5f, 0),
            size
        );

        // Draw internal grid lines
        for (int x = 0; x <= gridSize.x; x++)
        {
            Gizmos.DrawLine(
                startPos + new Vector3(x, 0, 0),
                startPos + new Vector3(x, gridSize.y, 0)
            );
        }

        for (int y = 0; y <= gridSize.y; y++)
        {
            Gizmos.DrawLine(
                startPos + new Vector3(0, y, 0),
                startPos + new Vector3(gridSize.x, y, 0)
            );
        }
    }
}