using UnityEngine;
using SQLite; // Core SQLite-NET functionality
using System.IO; // For Path.Combine to handle file paths
using System.Collections.Generic; // For List<T>
using System.Linq; // For LINQ methods like .Count() and .FirstOrDefault()

public class DatabaseManager : MonoBehaviour
{
    private SQLiteConnection _connection; // The main connection object to the database
    private string _databasePath; // The full path to your database file

    // Awake is called when the script instance is being loaded, even if the GameObject is disabled.
    // It's a good place for initialization that doesn't depend on other GameObjects.
    void Awake()
    {
        // Determine the path where your database file will be stored.
        // Application.persistentDataPath is ideal for mobile/desktop as it's a persistent, user-specific directory.
        // For development in Editor, Application.dataPath can also work but persistentDataPath is generally safer.
        _databasePath = Path.Combine(Application.persistentDataPath, "StoreProducts.db");
        Debug.Log($"[DatabaseManager] Database path: {_databasePath}"); // Log the path for debugging

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            // Create a new connection to the SQLite database file.
            // If the file doesn't exist, SQLite will create it.
            _connection = new SQLiteConnection(_databasePath);
            Debug.Log("[DatabaseManager] Database opened successfully.");

            // Create the 'ProductDB' table.
            // If the table already exists, this method does nothing.
            _connection.CreateTable<ProductDB>();
            Debug.Log("[DatabaseManager] ProductDB table ensured (created if not exists).");

            // --- Optional: Populate initial data if the database is newly created and empty ---
            // This prevents adding duplicate data every time the app starts.
            if (_connection.Table<ProductDB>().Count() == 0) // Check if the table has any rows
            {
                Debug.Log("[DatabaseManager] Database is empty, populating initial data.");
                PopulateInitialData();
            }
        }
        catch (SQLiteException ex)
        {
            // Catch any SQLite-specific errors during initialization (e.g., file access issues)
            Debug.LogError($"[DatabaseManager] SQLite error during initialization: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            // Catch any other general exceptions
            Debug.LogError($"[DatabaseManager] General error during database initialization: {ex.Message}");
        }
    }

    private void PopulateInitialData()
    {
        // Create a list of ProductDB objects with your hardcoded data
        // IMPORTANT: Adjust X and Y positions based on YOUR map's dimensions
        // (e.g., if your MapBackground is 750 width x 523 height, X will be 0-750, Y will be 0-523)
        var products = new List<ProductDB>
        {
            new ProductDB("Milk", new Vector2(100, 300), "Dairy"),
            new ProductDB("Bread", new Vector2(100, 150), "Bakery"),
            new ProductDB("Apples", new Vector2(400, 250), "Produce"),
            new ProductDB("Banana", new Vector2(500, 600), "Produce"),
            new ProductDB("Bananeeee", new Vector2(500, 650), "Produce"),
            new ProductDB("Cereal", new Vector2(150, 150), "Breakfast"),
            new ProductDB("Cheese", new Vector2(100, 400), "Dairy"),
            new ProductDB("Yogurt", new Vector2(180, 450), "Dairy"),
            new ProductDB("Juice", new Vector2(500, 450), "Beverages"),
            new ProductDB("Pasta", new Vector2(400, 200), "Dry Goods"),
            new ProductDB("Sauce", new Vector2(450, 250), "Dry Goods"),
            new ProductDB("Chips", new Vector2(650, 100), "Snacks")
        };

        // Insert all products into the database in one go
        _connection.InsertAll(products);
        Debug.Log($"[DatabaseManager] {products.Count} initial products added to the database.");
    }

    // --- Public Methods for CRUD Operations (Create, Read, Update, Delete) ---

    public void AddProduct(ProductDB product)
    {
        _connection.Insert(product); // Inserts a new row into the table
        Debug.Log($"[DatabaseManager] Added product: {product.ProductName}");
    }

    public List<ProductDB> GetAllProducts()
    {
        return _connection.Table<ProductDB>().ToList(); // Retrieves all rows from the table as a List
    }

    public ProductDB GetProductByName(string name)
    {
        // Queries the table where ProductName matches the given name and returns the first match (or null)
        return _connection.Table<ProductDB>().Where(p => p.ProductName == name).FirstOrDefault();
    }

    public void UpdateProduct(ProductDB product)
    {
        // Updates an existing row. 'product' must have its PrimaryKey (Id) set for this to work.
        _connection.Update(product);
        Debug.Log($"[DatabaseManager] Updated product: {product.ProductName}");
    }

    public void DeleteProduct(ProductDB product)
    {
        // Deletes a row. 'product' must have its PrimaryKey (Id) set for this to work.
        _connection.Delete(product);
        Debug.Log($"[DatabaseManager] Deleted product: {product.ProductName}");
    }

    // OnDestroy is called when the GameObject is destroyed or the application quits.
    // Crucial for closing the database connection to prevent locking issues.
    void OnDestroy()
    {
        if (_connection != null)
        {
            _connection.Close(); // Close the database connection
            Debug.Log("[DatabaseManager] Database connection closed.");
        }
    }
}