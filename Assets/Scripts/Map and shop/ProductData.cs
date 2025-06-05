using UnityEngine; // Required for Vector2

[System.Serializable] // Makes this class visible in the Inspector for debugging if needed
public class ProductData
{
    public string productName;
    public Vector2 mapPosition;
    public string category;

    public ProductData(string name, Vector2 pos, string cat)
    {
        productName = name;
        mapPosition = pos;
        category = cat;
    }

    // You might want an override for ToString() for easier debugging
    public override string ToString()
    {
        return $"Product: {productName}, Pos: {mapPosition}, Category: {category}";
    }
}