// MapGrid.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapGrid", menuName = "Map/Map Grid")]
public class MapGrid : ScriptableObject
{
    [Header("Grid Dimensions")]
    public Vector2Int gridSize = new Vector2Int(50, 50); // Number of nodes in x and y
    public float nodeSize = 20f; // Size of each node in map units (e.g., pixels if your map is in pixels)

    [Header("Map Boundaries (for conversion)")]
    // These should match the MapConfig's MapWidth/MapHeight
    public float mapWidth;
    public float mapHeight;

    // The grid itself (true for walkable, false for unwalkable)
    private bool[,] _grid;

    // Node class for pathfinding
    public class Node
    {
        public Vector2Int gridPosition; // (x, y) grid coordinate
        public Vector2 worldPosition;   // Corresponding world/map coordinate
        public bool isWalkable;

        // A* specific properties
        public int gCost; // Cost from start node
        public int hCost; // Heuristic cost to end node
        public int fCost => gCost + hCost; // Total cost

        public Node parent; // For reconstructing the path

        public Node(Vector2Int gridPos, Vector2 worldPos, bool walkable)
        {
            gridPosition = gridPos;
            worldPosition = worldPos;
            isWalkable = walkable;
        }
    }

    private Node[,] _nodes;

    // Call this once to initialize the grid (e.g., in an editor script or a manager's Start)
    private void OnEnable()
    {
        // Only initialize if _nodes is null or its dimensions don't match gridSize
        // This prevents re-initialization if already set up (e.g., after a script recompile)
        if (_nodes == null || _nodes.GetLength(0) != gridSize.x || _nodes.GetLength(1) != gridSize.y)
        {
            InitializeGrid();
        }
    }

    public void InitializeGrid()
    {
        _grid = new bool[gridSize.x, gridSize.y];
        _nodes = new Node[gridSize.x, gridSize.y]; // <--- This line populates _nodes

        // Default all nodes to walkable initially
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2 worldPos = GridToWorldPosition(new Vector2Int(x, y));
                _grid[x, y] = true; // All walkable by default
                _nodes[x, y] = new Node(new Vector2Int(x, y), worldPos, true);
            }
        }
        Debug.Log($"MapGrid: Initialized grid with {gridSize.x}x{gridSize.y} nodes.");
    }

    public Node GetNode(Vector2Int gridPos)
    {
        if (gridPos.x >= 0 && gridPos.x < gridSize.x && gridPos.y >= 0 && gridPos.y < gridSize.y)
        {
            return _nodes[gridPos.x, gridPos.y];
        }
        return null;
    }

    // Convert world/map position to grid coordinates
    public Vector2Int WorldToGridPosition(Vector2 worldPos)
    {
        // Normalize world position to 0-1 range based on map dimensions
        float percentX = worldPos.x / mapWidth;
        float percentY = worldPos.y / mapHeight;

        // Clamp to ensure within bounds
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.FloorToInt(percentX * gridSize.x);
        int y = Mathf.FloorToInt(percentY * gridSize.y);

        // Clamp to prevent out-of-bounds array access for edge cases
        x = Mathf.Clamp(x, 0, gridSize.x - 1);
        y = Mathf.Clamp(y, 0, gridSize.y - 1);

        return new Vector2Int(x, y);
    }

    // Convert grid coordinates back to world/map position (center of the node)
    public Vector2 GridToWorldPosition(Vector2Int gridPos)
    {
        // Calculate center of the node in normalized 0-1 space
        float x = (gridPos.x + 0.5f) / gridSize.x;
        float y = (gridPos.y + 0.5f) / gridSize.y;

        return new Vector2(x * mapWidth, y * mapHeight);
    }

    // Mark an area as unwalkable (e.g., for obstacles)
    public void MarkUnwalkableArea(Rect obstacleRect, float padding = 0f)
    {
        Vector2 minWorld = obstacleRect.min - new Vector2(padding, padding);
        Vector2 maxWorld = obstacleRect.max + new Vector2(padding, padding);

        Vector2Int minGrid = WorldToGridPosition(minWorld);
        Vector2Int maxGrid = WorldToGridPosition(maxWorld);

        minGrid.x = Mathf.Max(0, minGrid.x);
        minGrid.y = Mathf.Max(0, minGrid.y);
        maxGrid.x = Mathf.Min(gridSize.x - 1, maxGrid.x);
        maxGrid.y = Mathf.Min(gridSize.y - 1, maxGrid.y);

        for (int x = minGrid.x; x <= maxGrid.x; x++)
        {
            for (int y = minGrid.y; y <= maxGrid.y; y++)
            {
                _grid[x, y] = false;
                _nodes[x, y].isWalkable = false;
            }
        }
        Debug.Log($"MapGrid: Marked area from {minWorld} to {maxWorld} (grid {minGrid} to {maxGrid}) as unwalkable.");
    }

    // Mark a single node as unwalkable
    public void MarkNodeUnwalkable(Vector2 worldPos, float radius = 0f)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        if (gridPos.x >= 0 && gridPos.x < gridSize.x && gridPos.y >= 0 && gridPos.y < gridSize.y)
        {
            _grid[gridPos.x, gridPos.y] = false;
            _nodes[gridPos.x, gridPos.y].isWalkable = false;

            // If a radius is provided, mark surrounding nodes as unwalkable too
            if (radius > 0)
            {
                int gridRadius = Mathf.CeilToInt(radius / nodeSize);
                for (int x = gridPos.x - gridRadius; x <= gridPos.x + gridRadius; x++)
                {
                    for (int y = gridPos.y - gridRadius; y <= gridPos.y + gridRadius; y++)
                    {
                        if (x >= 0 && x < gridSize.x && y >= 0 && y < gridSize.y)
                        {
                            Vector2 nodeWorldPos = GridToWorldPosition(new Vector2Int(x, y));
                            if (Vector2.Distance(worldPos, nodeWorldPos) <= radius + (nodeSize / 2f)) // Check if node's center is within radius
                            {
                                _grid[x, y] = false;
                                _nodes[x, y].isWalkable = false;
                            }
                        }
                    }
                }
            }
            Debug.Log($"MapGrid: Marked node at world {worldPos} (grid {gridPos}) as unwalkable (with radius {radius}).");
        }
    }

    public bool IsNodeWalkable(Vector2Int gridPos)
    {
        if (gridPos.x >= 0 && gridPos.x < gridSize.x && gridPos.y >= 0 && gridPos.y < gridSize.y)
        {
            return _grid[gridPos.x, gridPos.y];
        }
        return false; // Out of bounds is unwalkable
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        Vector2Int gridPos = node.gridPosition;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip self

                Vector2Int neighborGridPos = new Vector2Int(gridPos.x + x, gridPos.y + y);

                if (neighborGridPos.x >= 0 && neighborGridPos.x < gridSize.x &&
                    neighborGridPos.y >= 0 && neighborGridPos.y < gridSize.y)
                {
                    neighbors.Add(_nodes[neighborGridPos.x, neighborGridPos.y]);
                }
            }
        }
        return neighbors;
    }

    // Reset all nodes to walkable (useful for dynamic obstacle changes)
    public void ResetGridWalkability()
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                _grid[x, y] = true;
                _nodes[x, y].isWalkable = true;
            }
        }
        Debug.Log("MapGrid: All nodes reset to walkable.");
    }
}