using UnityEngine;
using UnityEngine.Tilemaps;

public class GravelDebugVisualizer : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private Vector3Int[] checkPoints;
    private bool[] isGravelPoints;
    private LineRenderer[] debugLines;
    private GameObject[] debugSpheres;
    
    [Header("Visual Settings")]
    public Color gravelColor = Color.red;
    public Color nonGravelColor = Color.green;
    public float pointSize = 0.2f;
    public bool showDebug = true;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        checkPoints = new Vector3Int[4]; // Only 4 diagonal points
        isGravelPoints = new bool[4];
        
        CreateDebugLines();
        CreateDebugSpheres();
    }

    private void CreateDebugLines()
    {
        debugLines = new LineRenderer[4]; // Lines connecting diagonal points
        
        for (int i = 0; i < 4; i++)
        {
            GameObject lineObj = new GameObject($"DebugLine_{i}");
            lineObj.transform.SetParent(transform);
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.positionCount = 2;
            line.sortingOrder = 100;
            
            debugLines[i] = line;
        }
    }

    private void CreateDebugSpheres()
    {
        debugSpheres = new GameObject[4];
        
        for (int i = 0; i < 4; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"DebugSphere_{i}";
            sphere.transform.SetParent(transform);
            sphere.transform.localScale = Vector3.one * pointSize;
            
            Renderer sphereRenderer = sphere.GetComponent<Renderer>();
            sphereRenderer.material = new Material(Shader.Find("Sprites/Default"));
            sphereRenderer.sortingOrder = 100;

            Destroy(sphere.GetComponent<Collider>());
            
            debugSpheres[i] = sphere;
        }
    }

    public void UpdateDebugPoints(Vector3Int centerPosition, Tilemap asphaltTilemap, Tilemap gravelTilemap)
    {
        if (!showDebug)
        {
            HideDebugVisualization();
            return;
        }

        // Define the four diagonal points
        Vector2Int[] offsets = new Vector2Int[]
        {
            new Vector2Int(-1, 1),  // Top Left
            new Vector2Int(1, 1),   // Top Right
            new Vector2Int(-1, -1), // Bottom Left
            new Vector2Int(1, -1)   // Bottom Right
        };

        int gravelPointCount = 0;

        for (int i = 0; i < 4; i++)
        {
            checkPoints[i] = centerPosition + new Vector3Int(offsets[i].x, offsets[i].y, 0);
            isGravelPoints[i] = IsPointOnGravel(checkPoints[i], asphaltTilemap, gravelTilemap);
            
            if (isGravelPoints[i])
            {
                gravelPointCount++;
            }
        }

        UpdateDebugLines();
        UpdateDebugSpheres();

        Debug.Log($"Gravel points: {gravelPointCount}/4, Is On Gravel: {gravelPointCount >= 3}");
    }

private bool IsPointOnGravel(Vector3Int point, Tilemap asphaltTilemap, Tilemap gravelTilemap)
{
    int gravelCount = 0;
    
    // Check a 3x3 grid centered on the point
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            Vector3Int checkPoint = point + new Vector3Int(x, y, 0);
            bool hasAsphalt = (asphaltTilemap != null && asphaltTilemap.HasTile(checkPoint));
            bool hasGravel = (gravelTilemap != null && gravelTilemap.HasTile(checkPoint));
            
            if (!hasAsphalt && hasGravel)
            {
                gravelCount++;
            }
        }
    }

    // Return true if majority of 9 tiles are gravel (5 or more)
    return gravelCount >= 5;
}

    private void UpdateDebugLines()
    {
        if (debugLines == null) return;

        // Update the cross pattern
        for (int i = 0; i < 4; i++)
        {
            LineRenderer line = debugLines[i];
            Vector3 start = transform.position;
            Vector3 end = new Vector3(checkPoints[i].x, checkPoints[i].y, 0);
            
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startColor = Color.yellow;
            line.endColor = Color.yellow;
        }
    }

    private void UpdateDebugSpheres()
    {
        for (int i = 0; i < checkPoints.Length; i++)
        {
            GameObject sphere = debugSpheres[i];
            if (sphere != null)
            {
                Vector3 worldPos = new Vector3(checkPoints[i].x, checkPoints[i].y, 0);
                sphere.transform.position = worldPos;
                
                Renderer sphereRenderer = sphere.GetComponent<Renderer>();
                sphereRenderer.material.color = isGravelPoints[i] ? gravelColor : nonGravelColor;
                
                sphere.SetActive(true);
            }
        }
    }

    private void HideDebugVisualization()
    {
        if (debugLines != null)
        {
            foreach (LineRenderer line in debugLines)
            {
                if (line != null)
                {
                    line.enabled = false;
                }
            }
        }

        if (debugSpheres != null)
        {
            foreach (GameObject sphere in debugSpheres)
            {
                if (sphere != null)
                {
                    sphere.SetActive(false);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (debugLines != null)
        {
            foreach (LineRenderer line in debugLines)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }
        }

        if (debugSpheres != null)
        {
            foreach (GameObject sphere in debugSpheres)
            {
                if (sphere != null)
                {
                    Destroy(sphere);
                }
            }
        }
    }
}