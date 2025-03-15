using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DeltaVector.AI;

/// <summary>
/// Enhanced visualization helper for UnifiedVectorAI that creates and manages visual elements
/// to represent the AI's pathing decisions in a more user-friendly way.
/// Updated to support unlimited path visualizations with performance optimizations.
/// </summary>
public class AIPathVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Create and manage LineRenderer prefabs automatically")]
    public bool autoCreateVisuals = true;
    
    [Tooltip("Prefab to use for line renderers (optional)")]
    public LineRenderer lineRendererPrefab;
    
    [Tooltip("Prefab to use for node indicators (optional)")]
    public GameObject nodeIndicatorPrefab;
    
    [Header("Visualization Colors")]
    [Tooltip("Color for the best path")]
    public Color bestPathColor = new Color(0, 1, 0, 0.8f);  // Bright green
    
    [Tooltip("Color for good paths")]
    public Color goodPathColor = new Color(0, 0.8f, 0, 0.6f);  // Green
    
    [Tooltip("Color for average paths")]
    public Color mediumPathColor = new Color(1, 0.8f, 0, 0.6f);  // Orange/Yellow
    
    [Tooltip("Color for poor paths")]
    public Color badPathColor = new Color(1, 0, 0, 0.6f);  // Red
    
    [Header("Path Settings")]
    [Tooltip("Width of line renderers")]
    public float bestPathWidth = 0.1f;
    
    [Tooltip("Width of alternative paths")]
    public float alternativePathWidth = 0.05f;
    
    [Tooltip("Size of node spheres")]
    public float nodeSphereSize = 0.2f;
    
    [Header("Performance Options")]
    [Tooltip("Use pooling for better performance")]
    public bool useObjectPooling = true;
    
    [Tooltip("Maximum size of the object pool")]
    public int maxPoolSize = 5000;
    
    [Tooltip("Draw path nodes or just lines")]
    public bool drawPathNodes = true;
    
    [Tooltip("Enable adaptive rendering for performance")]
    public bool adaptiveRendering = true;
    
    [Header("Debug UI")]
    [Tooltip("Show on-screen debug UI")]
    public bool showDebugUI = true;
    
    [Tooltip("Show detailed factor breakdown")]
    public bool showDetailedFactors = true;
    
    // Reference to the AI controller
    private UnifiedVectorAI aiController;
    
    // Parent for visualization objects
    private Transform visualsContainer;
    
    // Object pools for visualization objects
    private Queue<LineRenderer> lineRendererPool = new Queue<LineRenderer>();
    private Queue<GameObject> nodeIndicatorPool = new Queue<GameObject>();
    
    // Lists of active visualization objects
    private List<LineRenderer> activePaths = new List<LineRenderer>();
    private List<GameObject> activeNodes = new List<GameObject>();
    
    // State tracking
    private bool visualsInitialized = false;
    private bool visualsVisible = true;
    
    // UI elements
    private GameObject uiPanel;
    private Text stateText;
    private Text statsText;
    private Text factorsText;
    
    // Texture cache for UI
    private Texture2D backgroundTexture;
    
    // Performance monitoring
    private int drawnPathCount = 0;
    private int skippedPathCount = 0;
    
    void Awake()
    {
        // Get the AI controller
        aiController = GetComponent<UnifiedVectorAI>();
        if (aiController == null)
        {
            Debug.LogError("AIPathVisualizer requires a UnifiedVectorAI component on the same GameObject!");
            enabled = false;
            return;
        }
        
        // Create container for visuals
        visualsContainer = new GameObject("AI_Path_Visuals").transform;
        visualsContainer.parent = transform;
        visualsContainer.localPosition = Vector3.zero;
        
        // Initialize visuals if auto-create is enabled
        if (autoCreateVisuals)
        {
            InitializeVisuals();
        }
        
        // Create UI elements if debug UI is enabled
        if (showDebugUI)
        {
            CreateDebugUI();
        }
    }
    
    void OnEnable()
    {
        // Make sure visuals are initialized
        if (autoCreateVisuals && !visualsInitialized)
        {
            InitializeVisuals();
        }
    }
    
    void OnDisable()
    {
        // Hide visuals when disabled
        SetVisualsVisible(false);
    }
    
    void Update()
    {
        // Toggle visibility with V key
        if (Input.GetKeyDown(KeyCode.V))
        {
            visualsVisible = !visualsVisible;
            SetVisualsVisible(visualsVisible);
        }
        
        // Update visualization if AI is thinking or ready to execute
        if (aiController != null && visualsVisible)
        {
            switch (aiController.thinkingState)
            {
                case AIThinkingState.ThinkingComplete:
                case AIThinkingState.ReadyToExecute:
                    UpdatePathVisualizations();
                    break;
            }
        }
        
        // Update UI elements
        if (showDebugUI)
        {
            UpdateDebugUI();
        }
    }
    
    /// <summary>
    /// Initialize visualization objects
    /// </summary>
    private void InitializeVisuals()
    {
        if (visualsInitialized) return;
        
        // Create line renderer prefab if needed
        if (lineRendererPrefab == null)
        {
            CreateDefaultLineRendererPrefab();
        }
        
        // Create node indicator prefab if needed
        if (nodeIndicatorPrefab == null)
        {
            CreateDefaultNodeIndicatorPrefab();
        }
        
        // Pre-create some objects for the pool if using object pooling
        if (useObjectPooling)
        {
            PrePopulateObjectPools();
        }
        
        visualsInitialized = true;
    }
    
    /// <summary>
    /// Pre-populate object pools for better performance
    /// </summary>
    private void PrePopulateObjectPools()
    {
        // Calculate how many objects to pre-create
        // Don't create too many to avoid startup lag
        int initialPoolSize = Mathf.Min(500, maxPoolSize);
        
        // Create line renderers for paths
        for (int i = 0; i < initialPoolSize; i++)
        {
            LineRenderer line = CreateLineRenderer();
            ReturnLineRendererToPool(line);
        }
        
        // Create node indicators if enabled
        if (drawPathNodes)
        {
            for (int i = 0; i < initialPoolSize * 3; i++) // ~3 nodes per path
            {
                GameObject node = CreateNodeIndicator();
                ReturnNodeToPool(node);
            }
        }
        
        Debug.Log($"Pre-populated visualization pools with {initialPoolSize} lines and {(drawPathNodes ? initialPoolSize * 3 : 0)} nodes");
    }
    
    /// <summary>
    /// Create a default line renderer prefab
    /// </summary>
    private void CreateDefaultLineRendererPrefab()
    {
        GameObject lineObj = new GameObject("Line_Renderer_Prefab");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        
        // Configure the line renderer
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.positionCount = 0;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.white;
        line.endColor = Color.white;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        
        // Set as prefab
        lineRendererPrefab = line;
        
        // Hide the original
        lineObj.SetActive(false);
    }
    
    /// <summary>
    /// Create a default node indicator prefab
    /// </summary>
    private void CreateDefaultNodeIndicatorPrefab()
    {
        GameObject nodeObj = new GameObject("Node_Indicator_Prefab");
        
        // Add a sprite renderer
        SpriteRenderer sprite = nodeObj.AddComponent<SpriteRenderer>();
        sprite.sprite = CreateCircleSprite();
        sprite.color = Color.white;
        
        // Set as prefab
        nodeIndicatorPrefab = nodeObj;
        
        // Hide the original
        nodeObj.SetActive(false);
    }
    
    /// <summary>
    /// Create a circular sprite for node indicators
    /// </summary>
    private Sprite CreateCircleSprite()
    {
        // Create a simple circle texture
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        // Fill with circle
        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    colors[index] = Color.white;
                }
                else
                {
                    colors[index] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        // Create sprite from texture
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }
    
    /// <summary>
    /// Create a new line renderer instance
    /// </summary>
    private LineRenderer CreateLineRenderer()
    {
        LineRenderer line = Instantiate(lineRendererPrefab, visualsContainer);
        line.gameObject.SetActive(false);
        return line;
    }
    
    /// <summary>
    /// Create a new node indicator instance
    /// </summary>
    private GameObject CreateNodeIndicator()
    {
        GameObject node = Instantiate(nodeIndicatorPrefab, visualsContainer);
        node.SetActive(false);
        return node;
    }
    
    /// <summary>
    /// Get a line renderer from the pool or create a new one
    /// </summary>
    private LineRenderer GetLineRenderer()
    {
        if (useObjectPooling && lineRendererPool.Count > 0)
        {
            LineRenderer line = lineRendererPool.Dequeue();
            line.gameObject.SetActive(true);
            return line;
        }
        
        return CreateLineRenderer();
    }
    
    /// <summary>
    /// Get a node indicator from the pool or create a new one
    /// </summary>
    private GameObject GetNodeIndicator()
    {
        if (useObjectPooling && nodeIndicatorPool.Count > 0)
        {
            GameObject node = nodeIndicatorPool.Dequeue();
            node.SetActive(true);
            return node;
        }
        
        return CreateNodeIndicator();
    }
    
    /// <summary>
    /// Return a line renderer to the pool
    /// </summary>
    private void ReturnLineRendererToPool(LineRenderer line)
    {
        if (line == null) return;
        
        line.gameObject.SetActive(false);
        
        if (useObjectPooling && lineRendererPool.Count < maxPoolSize)
        {
            lineRendererPool.Enqueue(line);
        }
        else if (!useObjectPooling)
        {
            Destroy(line.gameObject);
        }
    }
    
    /// <summary>
    /// Return a node indicator to the pool
    /// </summary>
    private void ReturnNodeToPool(GameObject node)
    {
        if (node == null) return;
        
        node.SetActive(false);
        
        if (useObjectPooling && nodeIndicatorPool.Count < maxPoolSize)
        {
            nodeIndicatorPool.Enqueue(node);
        }
        else if (!useObjectPooling)
        {
            Destroy(node);
        }
    }
    
    /// <summary>
    /// Clear all active visualizations and return objects to pool
    /// </summary>
    private void ClearVisualizations()
    {
        // Return active path lines to pool
        foreach (var line in activePaths)
        {
            ReturnLineRendererToPool(line);
        }
        activePaths.Clear();
        
        // Return active nodes to pool
        foreach (var node in activeNodes)
        {
            ReturnNodeToPool(node);
        }
        activeNodes.Clear();
    }
    
    /// <summary>
    /// Update path visualizations based on AI data
    /// </summary>
    private void UpdatePathVisualizations()
    {
        if (aiController == null || !visualsVisible) return;
        
        // Clear existing visualizations
        ClearVisualizations();
        
        // Reset counters
        drawnPathCount = 0;
        skippedPathCount = 0;
        
        // Access the PathGenerator component to get path data
        AIPathGenerator pathGenerator = aiController.GetComponentInChildren<AIPathGenerator>();
        if (pathGenerator == null) return;
        
        // Get paths from the generator
        List<Path> generatedPaths = pathGenerator.GetGeneratedPaths();
        
        // Nothing to visualize if no paths
        if (generatedPaths == null || generatedPaths.Count == 0) return;
        
        // Find the best path (marked as PathQuality.Best)
        Path bestPath = generatedPaths.Find(p => p.Quality == PathQuality.Best);
        
        // Process the best path first
        if (bestPath != null)
        {
            // Visualize best path with special highlight
            VisualizePath(bestPath.Nodes, bestPathColor, bestPathWidth, true);
            drawnPathCount++;
            
            // Process alternative paths
            // Performance optimization
            bool useAdaptiveSampling = adaptiveRendering;
            
            // Sort remaining paths by quality
            List<Path> sortedPaths = new List<Path>(generatedPaths);
            sortedPaths.Remove(bestPath); // Remove best path, already visualized
            
            // Sort paths by score
            sortedPaths.Sort((a, b) => b.AverageScore.CompareTo(a.AverageScore));
            
            // Determine adaptive sampling rate if needed
            int samplingInterval = 1;
            int priorityPathCount = 100; // Always show at least the top 100 paths
            
            if (useAdaptiveSampling && sortedPaths.Count > 1000)
            {
                // Calculate sampling interval based on total paths
                // This ensures we don't try to render too many paths which would kill performance
                samplingInterval = Mathf.Max(1, Mathf.FloorToInt(sortedPaths.Count / 1000));
                
                Debug.Log($"Using adaptive sampling with interval {samplingInterval} for {sortedPaths.Count} paths");
            }
            
            // Process each path
            for (int i = 0; i < sortedPaths.Count; i++)
            {
                // Apply adaptive sampling if needed
                if (useAdaptiveSampling && i >= priorityPathCount && i % samplingInterval != 0)
                {
                    // Skip paths based on sampling interval, but always draw the top priority paths
                    skippedPathCount++;
                    continue;
                }
                
                Path path = sortedPaths[i];
                
                // Determine color based on quality
                Color pathColor;
                switch (path.Quality)
                {
                    case PathQuality.Good:
                        pathColor = goodPathColor;
                        break;
                    case PathQuality.Medium:
                        pathColor = mediumPathColor;
                        break;
                    case PathQuality.Bad:
                    default:
                        pathColor = badPathColor;
                        break;
                }
                
                // Visualize the path
                VisualizePath(path.Nodes, pathColor, alternativePathWidth, false);
                drawnPathCount++;
            }
        }
    }
    
    /// <summary>
    /// Visualize a path with the specified color and width
    /// </summary>
    private void VisualizePath(List<PathNode> nodes, Color color, float width, bool isBestPath)
    {
        if (nodes == null || nodes.Count < 1)
            return;
            
        // Get a line renderer
        LineRenderer line = GetLineRenderer();
        if (line == null) return;
        
        // Configure line
        line.startColor = color;
        line.endColor = color;
        line.startWidth = width;
        line.endWidth = width;
        line.gameObject.SetActive(true);
        
        // Track in active list
        activePaths.Add(line);
        
        // List to store points
        List<Vector3> points = new List<Vector3>();
        
        // Start from current position
        points.Add(transform.position);
        
        // Process each node
        int nodeIndex = 0;
        foreach (var node in nodes)
        {
            // Get position and convert to world space
            Vector2Int positionInt = node.Position;
            Vector3 worldPos = new Vector3(
                positionInt.x / (float)PlayerMovement.GRID_SCALE,
                positionInt.y / (float)PlayerMovement.GRID_SCALE,
                0
            );
            
            // Add to points list
            points.Add(worldPos);
            
            // Create node indicators if enabled
            if (drawPathNodes)
            {
                // For non-best paths, we might skip some nodes for performance
                bool drawNode = isBestPath || nodeIndex < 3 || nodeIndex == points.Count - 1;
                
                if (drawNode)
                {
                    GameObject nodeIndicator = GetNodeIndicator();
                    if (nodeIndicator != null)
                    {
                        // Position the node
                        nodeIndicator.transform.position = worldPos;
                        nodeIndicator.transform.localScale = Vector3.one * (nodeSphereSize + (nodeIndex == 0 ? 0.05f : 0));
                        
                        // Set color
                        SpriteRenderer nodeSprite = nodeIndicator.GetComponent<SpriteRenderer>();
                        if (nodeSprite != null)
                        {
                            // Vary opacity by node index and importance
                            float alpha;
                            if (isBestPath)
                            {
                                alpha = nodeIndex == 0 ? 1.0f : 0.8f; // Best path nodes are more visible
                            }
                            else
                            {
                                alpha = Mathf.Lerp(0.6f, 0.3f, nodeIndex / 5f); // Fade out nodes on alternative paths
                            }
                            
                            nodeSprite.color = new Color(color.r, color.g, color.b, alpha);
                        }
                        
                        // Add to active nodes list
                        activeNodes.Add(nodeIndicator);
                    }
                }
            }
            
            nodeIndex++;
        }
        
        // Set line points
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }
    
    /// <summary>
    /// Set visuals visibility
    /// </summary>
    private void SetVisualsVisible(bool visible)
    {
        if (visualsContainer != null)
        {
            visualsContainer.gameObject.SetActive(visible);
        }
        
        if (uiPanel != null)
        {
            uiPanel.SetActive(visible && showDebugUI);
        }
    }
    
    /// <summary>
    /// Create debug UI elements
    /// </summary>
    private void CreateDebugUI()
    {
        // Find canvas or create one
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("AI_Debug_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create UI panel
        uiPanel = new GameObject("AI_Debug_Panel");
        uiPanel.transform.SetParent(canvas.transform, false);
        
        // Set up panel
        RectTransform panelRect = uiPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(10, 10);
        panelRect.sizeDelta = new Vector2(300, 400);
        
        // Add vertical layout
        VerticalLayoutGroup layout = uiPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10;
        
        // Create state text
        GameObject stateTextObj = new GameObject("State_Text");
        stateTextObj.transform.SetParent(uiPanel.transform, false);
        
        stateText = stateTextObj.AddComponent<Text>();
        stateText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stateText.fontSize = 18;
        stateText.color = Color.cyan;
        stateText.text = "AI State: Idle";
        
        // Create stats text
        GameObject statsTextObj = new GameObject("Stats_Text");
        statsTextObj.transform.SetParent(uiPanel.transform, false);
        
        statsText = statsTextObj.AddComponent<Text>();
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.fontSize = 16;
        statsText.color = Color.white;
        statsText.text = "Waiting for AI...";
        
        // Create factors text
        GameObject factorsTextObj = new GameObject("Factors_Text");
        factorsTextObj.transform.SetParent(uiPanel.transform, false);
        
        factorsText = factorsTextObj.AddComponent<Text>();
        factorsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        factorsText.fontSize = 14;
        factorsText.color = Color.white;
        factorsText.text = "Factors will appear here...";
        factorsText.alignment = TextAnchor.UpperLeft;
        
        // Set up rect transforms
        RectTransform stateTextRect = stateText.rectTransform;
        stateTextRect.sizeDelta = new Vector2(280, 30);
        
        RectTransform statsTextRect = statsText.rectTransform;
        statsTextRect.sizeDelta = new Vector2(280, 80);
        
        RectTransform factorsTextRect = factorsText.rectTransform;
        factorsTextRect.sizeDelta = new Vector2(280, 260);
        
        // Add background images
        Image panelBg = uiPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.7f);
    }
    
    /// <summary>
    /// Update debug UI with current AI information
    /// </summary>
    private void UpdateDebugUI()
    {
        if (aiController == null) return;
        
        // Update state text
        if (stateText != null)
        {
            string state = aiController.thinkingState.ToString();
            Color stateColor;
            
            // Set color based on state
            switch (aiController.thinkingState)
            {
                case AIThinkingState.Idle:
                    stateColor = Color.white;
                    break;
                case AIThinkingState.ReadyToThink:
                    stateColor = Color.cyan;
                    break;
                case AIThinkingState.Thinking:
                    stateColor = Color.yellow;
                    break;
                case AIThinkingState.ThinkingComplete:
                case AIThinkingState.ReadyToExecute:
                    stateColor = Color.green;
                    break;
                case AIThinkingState.Executing:
                    stateColor = Color.red;
                    break;
                default:
                    stateColor = Color.white;
                    break;
            }
            
            stateText.text = $"AI State: {state}";
            stateText.color = stateColor;
        }
        
        // Update stats text
        if (statsText != null)
        {
            // Get stats from AI and path generator
            AIPathGenerator pathGenerator = aiController.GetComponentInChildren<AIPathGenerator>();
            if (pathGenerator != null)
            {
                // Get basic stats
                int totalPaths = pathGenerator.GetTotalPathsGenerated();
                int depth = pathGenerator.pathfindingDepth;
                float bestScore = aiController.BestPathScore;
                
                // Add visualization stats
                string pathStats = $"Generated Paths: {totalPaths}\n" +
                                $"Pathfinding Depth: {depth}\n" +
                                $"Best Path Score: {bestScore:P1}\n" +
                                $"Visualized Paths: {drawnPathCount}";
                
                // Add performance stats if applicable
                if (skippedPathCount > 0)
                {
                    pathStats += $"\nSkipped Paths: {skippedPathCount} (optimization)";
                }
                
                // Add adaptive sampling info if active
                if (adaptiveRendering && totalPaths > 1000)
                {
                    pathStats += $"\nAdaptive Sampling Active";
                }
                
                // Update text
                statsText.text = pathStats;
            }
            else
            {
                statsText.text = "Path generator not found";
            }
        }
        
        // Update factors text if showing detailed factors
        if (factorsText != null && showDetailedFactors)
        {
            UpdateFactorsText();
        }
    }
    
    /// <summary>
    /// Update factors text with details from best move
    /// </summary>
    private void UpdateFactorsText()
    {
        // Access the best path from path generator
        AIPathGenerator pathGenerator = aiController.GetComponentInChildren<AIPathGenerator>();
        if (pathGenerator == null) return;
        
        List<Path> paths = pathGenerator.GetGeneratedPaths();
        Path bestPath = paths.Find(p => p.Quality == PathQuality.Best);
        
        if (bestPath != null && bestPath.Nodes.Count > 0)
        {
            PathNode firstNode = bestPath.Nodes[0];
            
            if (firstNode.EvaluationFactors != null && firstNode.EvaluationFactors.Count > 0)
            {
                string factorsText = "Best Move Factors:\n";
                
                // Add important factors
                if (firstNode.EvaluationFactors.ContainsKey("Distance"))
                    factorsText += $"• Distance: {firstNode.EvaluationFactors["Distance"]:F2}\n";
                    
                if (firstNode.EvaluationFactors.ContainsKey("Speed"))
                    factorsText += $"• Speed: {firstNode.EvaluationFactors["Speed"]:F2}\n";
                    
                if (firstNode.EvaluationFactors.ContainsKey("Terrain"))
                    factorsText += $"• Terrain: {firstNode.EvaluationFactors["Terrain"]:F2}\n";
                    
                if (firstNode.EvaluationFactors.ContainsKey("Direction"))
                    factorsText += $"• Direction: {firstNode.EvaluationFactors["Direction"]:F2}\n";
                    
                if (firstNode.EvaluationFactors.ContainsKey("ExitRisk"))
                    factorsText += $"• Exit Risk: {firstNode.EvaluationFactors["ExitRisk"]:F2}\n";
                    
                if (firstNode.EvaluationFactors.ContainsKey("Center"))
                    factorsText += $"• Track Center: {firstNode.EvaluationFactors["Center"]:F2}\n";
                    
                this.factorsText.text = factorsText;
            }
        }
    }
    
    /// <summary>
    /// Draw additional gizmos for path visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!visualsVisible || !Application.isPlaying || aiController == null) return;
        
        // Draw line to target
        DrawTargetLine();
    }
    
    /// <summary>
    /// Draw a line to the current target
    /// </summary>
    private void DrawTargetLine()
    {
        // Find next checkpoint target
        CheckpointManager checkpointManager = FindFirstObjectByType<CheckpointManager>();
        if (checkpointManager == null) return;
        
        bool isPlayer1 = gameObject.CompareTag("Player1");
        Checkpoint targetCheckpoint = checkpointManager.GetNextCheckpointInOrder(isPlayer1);
        
        if (targetCheckpoint != null)
        {
            Vector3 targetPos = targetCheckpoint.transform.position;
            
            // Draw line to checkpoint
            Gizmos.color = new Color(1f, 0.7f, 0f, 0.8f); // Orange-yellow
            Gizmos.DrawLine(transform.position, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.3f);
        }
        else
        {
            // Try to find finish line if no checkpoint
            GameObject finishLine = GameObject.Find("StartFinishLine");
            if (finishLine != null)
            {
                Vector3 targetPos = finishLine.transform.position;
                
                // Draw line to finish line
                Gizmos.color = new Color(1f, 0.7f, 0f, 0.8f); // Orange-yellow
                Gizmos.DrawLine(transform.position, targetPos);
                Gizmos.DrawWireSphere(targetPos, 0.3f);
            }
        }
    }
}