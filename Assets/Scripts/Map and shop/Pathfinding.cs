// Pathfinding.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For .Min() on F_Cost

public class Pathfinding : MonoBehaviour
{
    public MapGrid mapGrid; // Assign your MapGrid Scriptable Object here

    void Start()
    {
        if (mapGrid == null)
        {
            Debug.LogError("Pathfinding: MapGrid not assigned! Please assign it in the Inspector.", this);
            return;
        }
        // Initialize the grid (can also be done in a dedicated MapInitializer script)
        mapGrid.InitializeGrid();
    }

    // This method will be called to mark obstacles after the mapGrid is initialized
    public void SetObstacles(MapObstaclesConfig obstaclesConfig, List<ProductData> allProductData)
    {
        if (mapGrid == null) return;

        mapGrid.ResetGridWalkability(); // Clear previous obstacles

        // Mark static obstacles from MapObstaclesConfig
        foreach (var obstacle in obstaclesConfig.obstacles)
        {
            mapGrid.MarkUnwalkableArea(obstacle.rect, obstacle.padding);
        }

        // Mark product markers as obstacles (excluding entrance/exit if needed, or if they are in shopping list)
        foreach (var product in allProductData)
        {
            // IMPORTANT: You might NOT want to mark Entrance/Exit as unwalkable if you want the path to pass *through* their exact spot.
            // Adjust this condition based on your design. For "going around", it's better to mark them as obstacles.
            // If you want the path to end *exactly* on them, maybe don't mark them as obstacles.
            // For now, let's mark all for demonstration, and the pathfinding will find the closest walkable node.
            // If we don't want path to go through specific product markers, mark them as unwalkable.
            // A more robust approach would be to only mark them unwalkable if they are NOT the start/end of the current path segment.
            if (product.productName != "Entrance" && product.productName != "Exit") // Don't mark start/end nodes as unwalkable
            {
                mapGrid.MarkNodeUnwalkable(product.mapPosition, obstaclesConfig.markerObstacleRadius);
            }
        }
        Debug.Log("Pathfinding: Obstacles updated in MapGrid.");
    }


    /// <summary>
    /// Finds the shortest path between two world/map positions using A* algorithm.
    /// </summary>
    /// <param name="startWorldPos">The starting map position.</param>
    /// <param name="targetWorldPos">The target map position.</param>
    /// <returns>A list of world/map positions representing the path, or an empty list if no path is found.</returns>
    public List<Vector2> FindPath(Vector2 startWorldPos, Vector2 targetWorldPos)
    {
        if (mapGrid == null)
        {
            Debug.LogError("Pathfinding: MapGrid is null. Cannot find path.", this);
            return new List<Vector2>();
        }

        MapGrid.Node startNode = mapGrid.GetNode(mapGrid.WorldToGridPosition(startWorldPos));
        MapGrid.Node targetNode = mapGrid.GetNode(mapGrid.WorldToGridPosition(targetWorldPos));

        if (startNode == null || targetNode == null)
        {
            Debug.LogError($"Pathfinding: Start or target node is null. Start: {startWorldPos}, Target: {targetWorldPos}");
            return new List<Vector2>();
        }
        // Find a walkable node near the start if the start node itself is unwalkable
        if (!startNode.isWalkable)
        {
            startNode = FindNearestWalkableNode(startNode);
            if (startNode == null)
            {
                Debug.LogWarning($"Pathfinding: Start position {startWorldPos} is unwalkable and no nearby walkable node found.");
                return new List<Vector2>();
            }
        }

        // Find a walkable node near the target if the target node itself is unwalkable
        if (!targetNode.isWalkable)
        {
            targetNode = FindNearestWalkableNode(targetNode);
            if (targetNode == null)
            {
                Debug.LogWarning($"Pathfinding: Target position {targetWorldPos} is unwalkable and no nearby walkable node found.");
                return new List<Vector2>();
            }
        }


        // Open list: Nodes to be evaluated
        List<MapGrid.Node> openSet = new List<MapGrid.Node>();
        // Closed list: Nodes already evaluated
        HashSet<MapGrid.Node> closedSet = new HashSet<MapGrid.Node>();

        openSet.Add(startNode);

        // Reset costs for all nodes before starting a new pathfind
        for (int x = 0; x < mapGrid.gridSize.x; x++)
        {
            for (int y = 0; y < mapGrid.gridSize.y; y++)
            {
                mapGrid.GetNode(new Vector2Int(x, y)).gCost = int.MaxValue;
                mapGrid.GetNode(new Vector2Int(x, y)).hCost = 0;
                mapGrid.GetNode(new Vector2Int(x, y)).parent = null;
            }
        }

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);

        while (openSet.Count > 0)
        {
            MapGrid.Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return ReconstructPath(startNode, targetNode); // Path found
            }

            foreach (MapGrid.Node neighbor in mapGrid.GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost)
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // If no path is found
        Debug.LogWarning($"Pathfinding: No path found from {startWorldPos} to {targetWorldPos}.");
        return new List<Vector2>();
    }

    private List<Vector2> ReconstructPath(MapGrid.Node startNode, MapGrid.Node endNode)
    {
        List<Vector2> path = new List<Vector2>();
        MapGrid.Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPosition);
            if (currentNode.parent == null)
            {
                Debug.LogError("Pathfinding: Path reconstruction failed. A node has no parent before reaching start node.");
                return new List<Vector2>();
            }
            currentNode = currentNode.parent;
        }
        path.Add(startNode.worldPosition); // Add the start node
        path.Reverse(); // Path is built backwards, so reverse it
        return path;
    }

    private int GetDistance(MapGrid.Node nodeA, MapGrid.Node nodeB)
    {
        int distX = Mathf.Abs(nodeA.gridPosition.x - nodeB.gridPosition.x);
        int distY = Mathf.Abs(nodeA.gridPosition.y - nodeB.gridPosition.y);

        // For diagonal movement cost
        if (distX > distY)
            return 14 * distY + 10 * (distX - distY); // 14 for diagonal, 10 for straight
        return 14 * distX + 10 * (distY - distX);
    }

    // Helper to find a nearby walkable node if start/end point is unwalkable
    private MapGrid.Node FindNearestWalkableNode(MapGrid.Node originNode, int searchRadius = 5)
    {
        Queue<MapGrid.Node> queue = new Queue<MapGrid.Node>();
        HashSet<MapGrid.Node> visited = new HashSet<MapGrid.Node>();

        queue.Enqueue(originNode);
        visited.Add(originNode);

        MapGrid.Node closestWalkable = null;
        float minDistance = float.MaxValue;

        while (queue.Count > 0)
        {
            MapGrid.Node currentNode = queue.Dequeue();

            if (currentNode.isWalkable)
            {
                float dist = Vector2.Distance(originNode.worldPosition, currentNode.worldPosition);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestWalkable = currentNode;
                }
            }

            foreach (MapGrid.Node neighbor in mapGrid.GetNeighbors(currentNode))
            {
                if (!visited.Contains(neighbor) && (Mathf.Abs(neighbor.gridPosition.x - originNode.gridPosition.x) <= searchRadius &&
                                                    Mathf.Abs(neighbor.gridPosition.y - originNode.gridPosition.y) <= searchRadius))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return closestWalkable;
    }

    // In Pathfinding.cs (or MapGrid.cs)
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (mapGrid == null || mapGrid.gridSize.x == 0 || mapGrid.gridSize.y == 0) return;

        // Draw grid lines
        Gizmos.color = Color.grey;
        for (int x = 0; x <= mapGrid.gridSize.x; x++)
        {
            Vector2 start = mapGrid.GridToWorldPosition(new Vector2Int(x, 0));
            Vector2 end = mapGrid.GridToWorldPosition(new Vector2Int(x, mapGrid.gridSize.y - 1));
            // Need to convert to actual Unity world space if your UI is in Screen Space Overlay.
            // For Gizmos, if your map is placed in world space, this would work directly.
            // If it's UI, this requires more advanced Gizmo drawing or running the app.
            // For simplicity, let's assume map coordinates can be somewhat visualized.
        }

        // Draw unwalkable nodes
        if (mapGrid != null && mapGrid.GetNode(Vector2Int.zero) != null) // Check if grid is initialized
        {
            for (int x = 0; x < mapGrid.gridSize.x; x++)
            {
                for (int y = 0; y < mapGrid.gridSize.y; y++)
                {
                    MapGrid.Node node = mapGrid.GetNode(new Vector2Int(x, y));
                    if (node != null && !node.isWalkable)
                    {
                        Gizmos.color = Color.red;
                        Vector2 worldPos = node.worldPosition;
                        // You might need to adjust this to match your actual UI canvas world space
                        // For a quick visual, just draw squares.
                        Gizmos.DrawCube(new Vector3(worldPos.x, worldPos.y, 0), new Vector3(mapGrid.nodeSize, mapGrid.nodeSize, 0));
                    }
                }
            }
        }
    }
#endif
}
