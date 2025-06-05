// StoreManager.cs
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class StoreManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject productMarkerPrefab;
    private GameObject entranceMarker;
    private GameObject exitMarker;

    [Header("Manager References")]
    public DatabaseManager databaseManager;
    public MapConfig mapConfig;
    public RectTransform productMarkersContainer;

    // This dictionary stores references to the instantiated UI GameObjects (markers) for easy lookup.
    private Dictionary<string, GameObject> _productMarkers = new Dictionary<string, GameObject>();
    public IReadOnlyDictionary<string, GameObject> ProductMarkers => _productMarkers;

    void Start()
    {
        if (databaseManager == null)
        {
            Debug.LogError("StoreManager: DatabaseManager not assigned! Please assign it in the Inspector.", this);
            return;
        }
        if (productMarkerPrefab == null)
        {
            Debug.LogError("StoreManager: Product Marker Prefab not assigned! Please assign it in the Inspector.", this);
            return;
        }
        if (mapConfig == null || mapConfig.uiMapRect == null)
        {
            Debug.LogError("StoreManager: MapConfig or its uiMapRect not assigned! Please assign it in the Inspector.", this);
            return;
        }

        LoadProductsAndCreateMarkers();
    }

    public List<ProductData> GetProductData()
    {
        List<ProductDB> dbProducts = databaseManager.GetAllProducts();
        List<ProductData> productDataList = new List<ProductData>();
        foreach (var dbProduct in dbProducts)
        {
            productDataList.Add(new ProductData(dbProduct.ProductName, dbProduct.MapPosition, dbProduct.Category));
        }

        float mapAreaWidth = mapConfig.MapWidth;
        float mapAreaHeight = mapConfig.MapHeight;

        // --- UPDATED ENTRANCE/EXIT POSITIONS ---
        // Assuming map origin (0,0) is bottom-left. Adjust padding as needed.
        productDataList.Add(new ProductData("Entrance", new Vector2(mapAreaWidth / 2f, 450f), "Meta")); // Entrance near bottom
        productDataList.Add(new ProductData("Exit", new Vector2(mapAreaWidth / 2f, 450f), "Meta")); // Exit near top
        //productDataList.Add(new ProductData("Exit", new Vector2(mapAreaWidth / 2f, mapAreaHeight - 50f), "Meta")); // Exit near top

        return productDataList;
    }

    public ProductData GetProductData(string productName)
    {
        Debug.Log($"[StoreManager] GetProductData requested for: '{productName}'");

        float mapAreaWidth = mapConfig.MapWidth;
        float mapAreaHeight = mapConfig.MapHeight;

        // --- UPDATED ENTRANCE/EXIT POSITIONS ---
        if (productName == "Entrance")
        {
            return new ProductData("Entrance", new Vector2(mapAreaWidth / 2f, 50f), "Meta");
        }
        if (productName == "Exit")
        {
            return new ProductData("Exit", new Vector2(mapAreaWidth / 2f, mapAreaHeight - 50f), "Meta");
        }

        ProductDB dbProduct = databaseManager.GetProductByName(productName);

        if (dbProduct != null)
        {
            Debug.Log($"[StoreManager] Found DB product: '{dbProduct.ProductName}' at position {dbProduct.MapPosition}.");
            return new ProductData(dbProduct.ProductName, dbProduct.MapPosition, dbProduct.Category);
        }
        Debug.LogWarning($"[StoreManager] Product '{productName}' not found in DatabaseManager or as a meta point.");
        return null;
    }

    private void LoadProductsAndCreateMarkers()
    {
        foreach (var marker in _productMarkers.Values)
        {
            Destroy(marker);
        }
        _productMarkers.Clear();

        if (entranceMarker != null) Destroy(entranceMarker);
        if (exitMarker != null) Destroy(exitMarker);

        List<ProductDB> productsFromDb = databaseManager.GetAllProducts();

        float mapAreaWidth = mapConfig.MapWidth;
        float mapAreaHeight = mapConfig.MapHeight;

        Debug.Log($"[StoreManager] Map Area Width (from MapConfig): {mapAreaWidth}, Height: {mapAreaHeight}");
        Debug.Log($"[StoreManager] Database returned {productsFromDb.Count} products.");

        foreach (var product in productsFromDb)
        {
            Debug.Log($"[StoreManager] Creating marker for DB product: '{product.ProductName}' at map position: {product.MapPosition}");
            CreateSingleProductMarker(product.ProductName, product.MapPosition, product.Category);
        }

        // --- UPDATED ENTRANCE/EXIT POSITIONS ---
        CreateSingleProductMarker("Entrance", new Vector2(mapAreaWidth / 2f, 450f), "Meta");
        CreateSingleProductMarker("Exit", new Vector2(mapAreaWidth / 2f, 450f), "Meta");
        //CreateSingleProductMarker("Exit", new Vector2(mapAreaWidth / 2f, mapAreaHeight - 50f), "Meta");


        Debug.Log($"[StoreManager] Total markers created (including Entrance/Exit): {_productMarkers.Count}");
    }

    private void CreateSingleProductMarker(string productName, Vector2 mapPosition, string category)
    {
        GameObject marker = Instantiate(productMarkerPrefab, productMarkersContainer);
        Vector2 anchoredPos = mapConfig.ConvertMapPositionToAnchoredPosition(mapPosition);
        marker.GetComponent<RectTransform>().anchoredPosition = anchoredPos;

        TMP_Text productNameText = marker.GetComponentInChildren<TMP_Text>();
        if (productNameText != null)
        {
            productNameText.text = productName;
        }
        else
        {
            Debug.LogWarning($"Product marker prefab for {productName} is missing a TMP_Text component in its children.", marker);
        }

        if (productName == "Entrance") entranceMarker = marker;
        if (productName == "Exit") exitMarker = marker;

        if (_productMarkers.ContainsKey(productName))
        {
            Debug.LogWarning($"[StoreManager] Duplicate product name '{productName}' encountered when creating marker. Overwriting existing marker.");
            Destroy(_productMarkers[productName]); // Destroy old one if duplicate name
            _productMarkers[productName] = marker;
        }
        else
        {
            _productMarkers.Add(productName, marker);
        }
        Debug.LogWarning($"[StoreManager] Created and added marker for '{productName}' at UI anchored position: {anchoredPos}");
    }

    public GameObject GetProductMarker(string productName)
    {
        Debug.Log($"[StoreManager] GetProductMarker requested for: '{productName}'");
        if (_productMarkers.ContainsKey(productName))
        {
            Debug.Log($"[StoreManager] Found marker for '{productName}'.");
            return _productMarkers[productName];
        }
        Debug.LogWarning($"[StoreManager] Marker for '{productName}' NOT found in _productMarkers dictionary.");
        return null;
    }

    public void RefreshProductMarkers()
    {
        LoadProductsAndCreateMarkers();
    }
}