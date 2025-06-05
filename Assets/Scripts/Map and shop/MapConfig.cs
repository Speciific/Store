// MapConfig.cs
using UnityEngine;

public class MapConfig : MonoBehaviour
{
    [Header("UI Map RectTransform (for visual representation)")]
    // Reference to the UI RectTransform that visually represents this map
    public RectTransform uiMapRect;

    // Public properties to get the actual calculated dimensions
    // These will be updated correctly in OnEnable/Start
    public float MapWidth => uiMapRect != null ? uiMapRect.rect.width : 0f;
    public float MapHeight => uiMapRect != null ? uiMapRect.rect.height : 0f;

    // Call this method in Start/OnEnable of other scripts to get positions
    public Vector2 ConvertMapPositionToAnchoredPosition(Vector2 mapPosition)
    {
        if (uiMapRect == null)
        {
            Debug.LogError("uiMapRect not assigned in MapConfig!");
            return Vector2.zero;
        }
        // Assuming mapPosition is (0,0) at bottom-left of the conceptual map
        // and uiMapRect's pivot is (0.5, 0.5) (center)
        return new Vector2(mapPosition.x - (MapWidth / 2f), mapPosition.y - (MapHeight / 2f));
    }

    public Vector2 ConvertAnchoredPositionToMapPosition(Vector2 anchoredPosition)
    {
        if (uiMapRect == null)
        {
            Debug.LogError("uiMapRect not assigned in MapConfig!");
            return Vector2.zero;
        }
        return new Vector2(anchoredPosition.x + (MapWidth / 2f), anchoredPosition.y + (MapHeight / 2f));
    }

    // Optional: Force a layout update if you need dimensions immediately and are sure it's the root of the layout
    // This is rarely needed if you get dimensions in Start() or OnEnable()
    // public void ForceLayoutUpdate()
    // {
    //     LayoutRebuilder.ForceRebuildLayoutImmediate(uiMapRect);
    // }
}