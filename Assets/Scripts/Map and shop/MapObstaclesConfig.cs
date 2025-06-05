// MapObstaclesConfig.cs (Existing or New)
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapObstaclesConfig", menuName = "Map/Map Obstacles Config")]
public class MapObstaclesConfig : ScriptableObject
{
    [System.Serializable]
    public struct ObstacleRect
    {
        public string name;
        public Rect rect; // Define position and size of the obstacle in map coordinates
        public float padding; // Optional padding around the obstacle
    }

    public List<ObstacleRect> obstacles;
    public float markerObstacleRadius = 50f; // Radius around product markers to consider unwalkable

    // You can add methods here to visualize obstacles in editor, or use them directly in MapGrid initialization
}