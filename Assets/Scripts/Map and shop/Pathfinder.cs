// Pathfinder.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Pathfinder : MonoBehaviour
{
    [Header("Manager References")]
    public StoreManager storeManager;
    public ShoppingListManager shoppingListManager;
    public MapConfig mapConfig;
    public MapObstaclesConfig mapObstaclesConfig;
    public Pathfinding pathfinding; // Reference to the Pathfinding script
    public MapGrid mapGrid; // Reference to MapGrid Scriptable Object

    [Header("UI References")]
    public GameObject lineSegmentPrefab;
    public RectTransform pathLineContainer;
    public TMP_Text currentStepText;

    [Header("Path Settings")]
    public float lineThickness = 30f;
    //public Color pathColor = Color.blue;
    public float productHighlightScale = 1.2f;
    public float highlightSpeed = 5f;

    // Special points
    private ProductData entrancePoint;
    private ProductData exitPoint;

    private List<ProductData> optimizedPath = new List<ProductData>();
    private int currentStepIndex = 0;
    private List<GameObject> currentLineSegments = new List<GameObject>();
    private GameObject currentHighlightedMarker = null;

    void OnEnable()
    {
        ShoppingListManager.OnShoppingListChanged += OptimizePath;
    }

    void OnDisable()
    {
        ShoppingListManager.OnShoppingListChanged -= OptimizePath;
    }

    void Start()
    {
        if (mapConfig == null || mapConfig.uiMapRect == null)
        {
            Debug.LogError("Pathfinder: MapConfig or its uiMapRect not assigned! Please assign it in the Inspector.", this);
            return;
        }
        if (mapGrid == null)
        {
            Debug.LogError("Pathfinder: MapGrid not assigned! Please assign it in the Inspector.", this);
            return;
        }
        if (pathfinding == null)
        {
            Debug.LogError("Pathfinder: Pathfinding script not assigned! Please assign it in the Inspector.", this);
            return;
        }
        if (mapObstaclesConfig == null)
        {
            Debug.LogError("Pathfinder: MapObstaclesConfig not assigned! Please assign it in the Inspector.", this);
            return;
        }

        // --- ENTRANCE/EXIT POSITIONS (BOTTOM CENTER) ---
        // Ensure these match the dimensions used in StoreManager
        entrancePoint = new ProductData("Entrance", new Vector2(mapConfig.MapWidth / 2f, 450f), "Meta"); // Bottom center
        //exitPoint = new ProductData("Exit", new Vector2(mapConfig.MapWidth / 2f, 450f), "Meta"); // Top center (for now, based on your request, but image shows bottom)
        //exitPoint = new ProductData("Exit", new Vector2(mapConfig.MapWidth / 2f, mapConfig.MapHeight - 50f), "Meta"); // Top center (for now, based on your request, but image shows bottom)

        // If you truly want Entrance and Exit in the *exact same location* (bottom center),
        // then both points would be the same. This would mean the path goes Entrance -> items -> Exit all from one point.
        // Based on the image, the path leaves the entrance, visits items, then returns to the entrance, which then acts as the exit.
        // Let's assume for now, Entrance is bottom, Exit is implicitly the same bottom point after items are collected.
        // If you want a visual "Exit" marker at the top, keep the previous Exit definition.
        // For "Entrance and Exit be in same location: bottom center", we will make Exit the same as Entrance.
        exitPoint = new ProductData("Exit", new Vector2(mapConfig.MapWidth / 2f, 450f), "Meta"); // Same as Entrance

        // Initialize MapGrid with correct map dimensions for conversions
        mapGrid.mapWidth = mapConfig.MapWidth;
        mapGrid.mapHeight = mapConfig.MapHeight;
        Debug.LogWarning($"[Pathfinder] mapConfig.MapWidth:'{mapConfig.MapWidth}' mapConfig.MapHeight: {mapConfig.MapHeight}");
        // The MapGrid.OnEnable() should handle InitializeGrid()
        // But if you're experiencing issues, ensure it's called or manually triggered.
        // mapGrid.InitializeGrid(); // Removed as MapGrid.OnEnable should handle it.

        OptimizePath(); // Initial path calculation
    }

    void OptimizePath()
    {
        ClearPath();

        List<ShoppingListItem> itemsToFind = shoppingListManager.GetShoppingList()
            .Where(item => !item.isFound)
            .ToList();

        Debug.Log($"[Pathfinder] itemsToFind count: {itemsToFind.Count}");
        if (itemsToFind.Count == 0)
        {
            currentStepText.text = "All items found! Path complete.";
            return;
        }

        optimizedPath = new List<ProductData>();
        optimizedPath.Add(entrancePoint); // Always start from entrance

        // --- NEW: Nearest Neighbor Algorithm for Optimized Route ---
        // This is a simple greedy approach to find a "short" path, not necessarily the absolute shortest (which is NP-hard).
        List<ProductData> productsToVisit = new List<ProductData>();
        foreach (var item in itemsToFind)
        {
            ProductData product = storeManager.GetProductData(item.product.productName);
            if (product != null)
            {
                productsToVisit.Add(product);
            }
        }

        ProductData currentPoint = entrancePoint;
        while (productsToVisit.Count > 0)
        {
            ProductData nextProduct = null;
            float minDistance = float.MaxValue;

            foreach (var product in productsToVisit)
            {
                // To calculate true "path" distance, we'd need to run A* between each,
                // but that's very slow for TSP. A straight-line distance is a common heuristic here.
                // If you want more accurate "shortest" route, you'd need a more advanced TSP solver
                // that uses the A* distances as edge weights.
                float distance = Vector2.Distance(currentPoint.mapPosition, product.mapPosition);
                // For a more accurate nearest neighbor (but slower), you could get A* path length:
                // float distance = pathfinding.FindPath(currentPoint.mapPosition, product.mapPosition).Count; // Count of nodes, or sum of costs

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nextProduct = product;
                }
            }

            if (nextProduct != null)
            {
                optimizedPath.Add(nextProduct);
                productsToVisit.Remove(nextProduct);
                currentPoint = nextProduct;
            }
            else
            {
                Debug.LogWarning("[Pathfinder] No next product found in Nearest Neighbor algorithm. This shouldn't happen unless productsToVisit became empty unexpectedly.");
                break;
            }
        }

        optimizedPath.Add(exitPoint); // Always end at exit (which is now the same as entrance)

        Debug.Log("--- Optimized Path Order (Nearest Neighbor) ---");
        foreach (var product in optimizedPath)
        {
            Debug.Log($"Product: {product.productName}, Position: {product.mapPosition}");
        }
        Debug.Log("---------------------------");

        // --- Set obstacles for the pathfinding system ---
        // Get all product data to mark them as potential obstacles for the pathfinding algorithm.
        // Exclude Entrance/Exit themselves from being marked as obstacles if you want the path to pass directly through them.
        List<ProductData> allMarkersAsObstacles = storeManager.GetProductData()
            .Where(p => p.productName != "Entrance" && p.productName != "Exit")
            .ToList();
        pathfinding.SetObstacles(mapObstaclesConfig, allMarkersAsObstacles);


        currentStepIndex = 0;
        DrawPath(); // Draw the new path
        UpdateCurrentStepText(); // Update instruction
        HighlightCurrentMarker(); // Highlight the first marker
    }

    void DrawPath()
    {
        ClearLineSegments();

        if (optimizedPath.Count < 2) return;
        if (mapConfig == null || pathfinding == null) return;

        List<Vector2> fullUiPathPoints = new List<Vector2>();

        // Start with the UI position of the first product
        fullUiPathPoints.Add(mapConfig.ConvertMapPositionToAnchoredPosition(optimizedPath[0].mapPosition));

        // Iterate through the optimized path, finding A* path between each consecutive pair
        for (int i = 0; i < optimizedPath.Count - 1; i++)
        {
            ProductData startProduct = optimizedPath[i];
            ProductData endProduct = optimizedPath[i + 1];

            // Use A* pathfinding for each segment
            // The path will contain intermediate points to navigate around obstacles.
            List<Vector2> segmentMapPath = pathfinding.FindPath(startProduct.mapPosition, endProduct.mapPosition);

            // Add the intermediate path points to the overall UI path
            foreach (var mapPoint in segmentMapPath)
            {
                Vector2 uiPoint = mapConfig.ConvertMapPositionToAnchoredPosition(mapPoint);
                // Avoid adding duplicate points if the end of one segment is the start of the next
                if (fullUiPathPoints.Count == 0 || Vector2.Distance(fullUiPathPoints.Last(), uiPoint) > 0.01f)
                {
                    fullUiPathPoints.Add(uiPoint);
                }
            }
        }

        // Draw line segments between all the calculated UI points
        for (int i = 0; i < fullUiPathPoints.Count - 1; i++)
        {
            Vector2 start = fullUiPathPoints[i];
            Vector2 end = fullUiPathPoints[i + 1];

            GameObject segmentGO = Instantiate(lineSegmentPrefab, pathLineContainer);
            RectTransform segmentRect = segmentGO.GetComponent<RectTransform>();
            Image segmentImage = segmentGO.GetComponent<Image>();

            if (segmentRect != null && segmentImage != null)
            {
                //segmentImage.color = pathColor; // Apply the path color

                Vector2 diff = end - start;
                float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                float length = diff.magnitude;

                segmentRect.sizeDelta = new Vector2(length, lineThickness);
                segmentRect.anchoredPosition = start + diff / 2f;
                segmentRect.localRotation = Quaternion.Euler(0, 0, angle);

                currentLineSegments.Add(segmentGO);
            }
            else
            {
                Debug.LogError("Line Segment Prefab is missing RectTransform or Image component!");
            }
        }
        Debug.Log($"[Pathfinder] Total line segments drawn: {currentLineSegments.Count}");
    }

    void ClearPath()
    {
        ClearLineSegments();
        UnhighlightAllMarkers();
        currentStepText.text = "";
    }

    void ClearLineSegments()
    {
        foreach (var segment in currentLineSegments)
        {
            if (segment != null)
            {
                Destroy(segment);
            }
        }
        currentLineSegments.Clear();
    }

    void UpdateCurrentStepText()
    {
        if (optimizedPath.Count == 0)
        {
            currentStepText.text = "No path calculated.";
            return;
        }

        if (currentStepIndex >= optimizedPath.Count)
        {
            currentStepText.text = "Path complete!";
            return;
        }

        ProductData currentProduct = optimizedPath[currentStepIndex];
        currentStepText.text = $"Next: {currentProduct.productName}";
    }

    void HighlightCurrentMarker()
    {
        UnhighlightAllMarkers();

        if (currentStepIndex < optimizedPath.Count)
        {
            ProductData productToHighlight = optimizedPath[currentStepIndex];
            Debug.Log($"[Pathfinder] Requesting marker for product: '{productToHighlight.productName}' (from optimizedPath).");

            GameObject marker = storeManager.GetProductMarker(productToHighlight.productName);

            if (marker != null)
            {
                currentHighlightedMarker = marker;
                marker.transform.localScale = Vector3.one * productHighlightScale;
                Debug.Log($"[Pathfinder] Successfully highlighted marker for: '{productToHighlight.productName}'.");
            }
            else
            {
                Debug.LogError($"[Pathfinder] FAILED to highlight marker for product: '{productToHighlight.productName}'. GetProductMarker returned null. This might happen if the marker was never created or its name is inconsistent.");
            }
        }
    }

    void UnhighlightAllMarkers()
    {
        if (currentHighlightedMarker != null)
        {
            currentHighlightedMarker.transform.localScale = Vector3.one;
            currentHighlightedMarker = null;
        }
    }

    public void NextStep()
    {
        if (currentStepIndex < optimizedPath.Count - 1)
        {
            currentStepIndex++;
            UpdateCurrentStepText();
            HighlightCurrentMarker();
            DrawPath(); // Redraw path to current step (optional, or just update highlighted points)
        }
        else
        {
            currentStepText.text = "End of path!";
            UnhighlightAllMarkers();
            ClearPath();
        }
    }

    public void PreviousStep()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            UpdateCurrentStepText();
            HighlightCurrentMarker();
            DrawPath(); // Redraw path
        }
        else
        {
            currentStepText.text = "Already at the start.";
            HighlightCurrentMarker(); // Keep first item highlighted
        }
    }
}