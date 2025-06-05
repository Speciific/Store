using SQLite; // This using statement is crucial for SQLite-NET attributes and types
using UnityEngine; // For Vector2 (though we convert it for storage)

// This class defines the schema for our 'ProductDB' table in the SQLite database
public class ProductDB
{
    // [PrimaryKey] makes 'Id' the unique identifier for each row.
    // [AutoIncrement] tells SQLite to automatically assign an increasing ID when new rows are added.
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // [Indexed] creates an index on this column, making lookups by ProductName much faster.
    public string ProductName { get; set; }

    // SQLite doesn't directly support Unity's Vector2.
    // We store it as a string (e.g., "100.0,300.0") and convert it in code.
    public string MapPositionString { get; set; }

    public string Category { get; set; }

    // [Ignore] tells SQLite-NET to NOT store this property in the database.
    // This is a "transient" property, useful for converting MapPositionString to Vector2 and vice-versa
    // directly within your code without cluttering the database schema.
    [Ignore]
    public Vector2 MapPosition
    {
        get
        {
            if (string.IsNullOrEmpty(MapPositionString))
            {
                return Vector2.zero; // Default to zero if string is empty or null
            }
            // Split the string "x,y" into parts and try to parse them
            string[] coords = MapPositionString.Split(',');
            if (coords.Length == 2 && float.TryParse(coords[0], out float x) && float.TryParse(coords[1], out float y))
            {
                return new Vector2(x, y); // Return the parsed Vector2
            }
            return Vector2.zero; // Return zero if parsing fails
        }
        set
        {
            // When MapPosition is set in code, convert it to a string for database storage
            MapPositionString = $"{value.x},{value.y}";
        }
    }

    // IMPORTANT: A parameterless constructor is often required by ORMs like SQLite-NET
    // so it can create instances of your class when reading from the database.
    public ProductDB() { }

    // Optional: A convenient constructor for creating new ProductDB objects in code
    public ProductDB(string name, Vector2 mapPos, string category)
    {
        ProductName = name;
        MapPosition = mapPos; // This will use the 'set' accessor of the [Ignore]d property
        Category = category;
    }
}