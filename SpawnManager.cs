using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject spawnIndicatorPrefab;
    public int numberOfSpawnPoints = 5;
    public float spawnSpacing = 1f;

    [Header("Indicator Sprite")]
    public Sprite indicatorSprite;    // Now we only need one sprite

    private List<GameObject> spawnIndicators = new List<GameObject>();

    public void ShowSpawnSelectionUI(PlayerMovement player)
    {
        Debug.Log("[SPAWN_FLOW] ShowSpawnSelectionUI called for " + player.gameObject.name);
        
        // Clear any existing indicators
        ClearSpawnIndicators();

        // Find start/finish line
        GameObject startFinishLine = GameObject.Find("StartFinishLine");
        if (startFinishLine == null)
        {
            Debug.LogError("[SPAWN_FLOW] Start/Finish Line not found!");
            return;
        }

        BoxCollider2D collider = startFinishLine.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            Debug.LogError("[SPAWN_FLOW] Start/Finish Line missing BoxCollider2D!");
            return;
        }

        Debug.Log($"[SPAWN_FLOW] Found StartFinishLine at position: {startFinishLine.transform.position}");
        Debug.Log($"[SPAWN_FLOW] Collider offset: {collider.offset}");

        // Calculate spawn positions
        Vector2 center = collider.offset + (Vector2)startFinishLine.transform.position;
        float middleY = center.y;
        float x = center.x;

        Debug.Log($"[SPAWN_FLOW] Center position for spawn points: {center}");

        // Create spawn indicators
        for (int i = 0; i < numberOfSpawnPoints; i++)
        {
            // Calculate alternating positions above and below middle
            float offset = ((i + 1) / 2) * spawnSpacing * (i % 2 == 0 ? 1 : -1);
            float y = middleY + offset;
            Vector2 spawnPosition = new Vector2(x, Mathf.Round(y));

            Debug.Log($"[SPAWN_FLOW] Creating spawn indicator {i} at position: {spawnPosition}");

            // Create indicator
            GameObject indicator = Instantiate(spawnIndicatorPrefab, spawnPosition, Quaternion.identity);
            
            // Get and set up the SpawnIndicator component
            SpawnIndicator spawnIndicatorScript = indicator.GetComponent<SpawnIndicator>();
            if (spawnIndicatorScript != null)
            {
                // Initialize with player and position
                spawnIndicatorScript.Initialize(player, Vector2Int.RoundToInt(spawnPosition));
                
                // Set the sprite
                spawnIndicatorScript.SetSprite(indicatorSprite);
                Debug.Log($"[SPAWN_FLOW] Initialized spawn indicator {i} for {player.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[SPAWN_FLOW] SpawnIndicator component missing on spawn indicator {i}!");
            }

            // Add to our list for cleanup
            spawnIndicators.Add(indicator);
        }

        Debug.Log($"[SPAWN_FLOW] Created {spawnIndicators.Count} spawn indicators");
    }

    public void ClearSpawnIndicators()
    {
        Debug.Log("[SPAWN_FLOW] Clearing spawn indicators");
        foreach (GameObject indicator in spawnIndicators)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
        }
        spawnIndicators.Clear();
        Debug.Log("[SPAWN_FLOW] All spawn indicators cleared");
    }
}