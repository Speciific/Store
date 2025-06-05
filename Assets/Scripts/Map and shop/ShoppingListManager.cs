//ShoppingListManager
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using UnityEngine.UI; // Required for Button

public class ShoppingListManager : MonoBehaviour
{
    public static event System.Action OnShoppingListChanged;

    [Header("UI References")]
    public TMPro.TMP_InputField inputField;
    public Transform contentPanel; // Parent for shopping list items (ScrollView/Viewport/Content)
    public GameObject shoppingListItemPrefab; // Prefab for each item in the list

    // NEW: Reference to the container for suggestion buttons
    public RectTransform suggestionButtonsContainer;
    // NEW: Reference to the prefab for each suggestion button
    public GameObject suggestionButtonPrefab;

    [Header("Manager References")]
    public StoreManager storeManager;
    public Pathfinder pathfinder;

    private List<ShoppingListItem> currentShoppingList = new List<ShoppingListItem>();
    private List<ProductData> _allAvailableProducts; // Cache of all products from StoreManager for suggestions

    // NEW: Keep track of instantiated suggestion buttons to clear them
    private List<GameObject> activeSuggestionButtons = new List<GameObject>();

    void Start()
    {
        // Basic null checks
        if (storeManager == null) { Debug.LogError("ShoppingListManager: StoreManager not assigned!", this); return; }
        if (inputField == null) { Debug.LogError("ShoppingListManager: Input Field not assigned!", this); return; }
        // NEW: Check for new references
        if (suggestionButtonsContainer == null) { Debug.LogError("ShoppingListManager: Suggestion Buttons Container not assigned!", this); return; }
        if (suggestionButtonPrefab == null) { Debug.LogError("ShoppingListManager: Suggestion Button Prefab not assigned!", this); return; }
        if (contentPanel == null) { Debug.LogError("ShoppingListManager: Content Panel not assigned!", this); return; }
        if (shoppingListItemPrefab == null) { Debug.LogError("ShoppingListManager: Shopping List Item Prefab not assigned!", this); return; }


        _allAvailableProducts = storeManager.GetProductData();

        inputField.onValueChanged.AddListener(OnSearchInputChanged);
        inputField.onEndEdit.AddListener(OnInputEndEdit);

        // Hide the suggestion container initially
        suggestionButtonsContainer.gameObject.SetActive(false);
    }

    void OnSearchInputChanged(string input)
    {
        ClearSuggestionButtons(); // Always clear old buttons when input changes

        if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
        {
            suggestionButtonsContainer.gameObject.SetActive(false);
            return;
        }

        var filteredProducts = _allAvailableProducts
            .Where(p => p.productName.ToLower().Contains(input.ToLower()))
            .Select(p => p.productName)
            .ToList();

        if (filteredProducts.Count > 0)
        {
            foreach (string productName in filteredProducts)
            {
                // Instantiate a new button for each suggestion
                GameObject suggestionButtonGO = Instantiate(suggestionButtonPrefab, suggestionButtonsContainer);
                activeSuggestionButtons.Add(suggestionButtonGO); // Add to our list to manage

                // Get the TextMeshPro text component on the button
                // It might be a child of the button, so using GetComponentInChildren
                TMP_Text buttonText = suggestionButtonGO.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = productName;
                }
                else
                {
                    Debug.LogWarning("SuggestionButtonPrefab is missing a TMP_Text component in its children.", suggestionButtonGO);
                }

                // Get the Button component and add a listener
                Button button = suggestionButtonGO.GetComponent<Button>();
                if (button != null)
                {
                    // Use a lambda expression to capture the current productName
                    button.onClick.AddListener(() => OnSuggestionButtonClicked(productName));
                }
                else
                {
                    Debug.LogWarning("SuggestionButtonPrefab is missing a Button component.", suggestionButtonGO);
                }
            }
            suggestionButtonsContainer.gameObject.SetActive(true); // Show the container
        }
        else
        {
            suggestionButtonsContainer.gameObject.SetActive(false); // Hide if no matches
        }
    }

    // NEW: Method called when a suggestion button is clicked
    void OnSuggestionButtonClicked(string productName)
    {
        AddProductToShoppingList(productName);
        inputField.text = ""; // Clear input after adding
        ClearSuggestionButtons(); // Clear and hide suggestion buttons
        suggestionButtonsContainer.gameObject.SetActive(false); // Hide the container
    }

    void OnInputEndEdit(string input)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            AddProductToShoppingList(input);
            inputField.text = "";
            ClearSuggestionButtons(); // Clear and hide suggestion buttons
            suggestionButtonsContainer.gameObject.SetActive(false);
        }
    }

    // NEW: Method to clear all active suggestion buttons
    void ClearSuggestionButtons()
    {
        foreach (GameObject buttonGO in activeSuggestionButtons)
        {
            Destroy(buttonGO);
        }
        activeSuggestionButtons.Clear();
    }

    void AddProductToShoppingList(string productName)
    {
        ProductData productToAdd = storeManager.GetProductData(productName);

        if (productToAdd != null)
        {
            // Check if product already exists to prevent duplicates
            if (!currentShoppingList.Any(item => item.product.productName.Equals(productName, System.StringComparison.OrdinalIgnoreCase)))
            {
                ShoppingListItem newItem = new ShoppingListItem
                {
                    product = productToAdd,
                    isFound = false
                };
                currentShoppingList.Add(newItem);
                InstantiateShoppingListItemUI(newItem);
                Debug.Log($"[ShoppingListManager] Added '{productName}' to shopping list.");
                OnShoppingListChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[ShoppingListManager] '{productName}' is already in the shopping list.");
            }
        }
        else
        {
            Debug.LogWarning($"[ShoppingListManager] Product '{productName}' not found in available products.", this);
        }
    }

    private void InstantiateShoppingListItemUI(ShoppingListItem item)
    {
        GameObject itemGO = Instantiate(shoppingListItemPrefab, contentPanel);
        item.uiInstance = itemGO;

        TMP_Text nameText = itemGO.transform.Find("ProductNameText")?.GetComponent<TMP_Text>();
        Toggle foundToggle = itemGO.transform.Find("FoundToggle")?.GetComponent<Toggle>();
        Button removeButton = itemGO.transform.Find("RemoveButton")?.GetComponent<Button>();

        if (nameText != null) nameText.text = item.product.productName;
        if (foundToggle != null)
        {
            foundToggle.isOn = item.isFound;
            foundToggle.onValueChanged.AddListener((isOn) => OnItemFoundToggle(item, isOn));
        }
        else { Debug.LogWarning($"Shopping list item prefab for {item.product.productName} is missing 'FoundToggle' component.", itemGO); }

        if (removeButton != null)
        {
            removeButton.onClick.AddListener(() => RemoveItemFromShoppingList(item));
        }
        else { Debug.LogWarning($"Shopping list item prefab for {item.product.productName} is missing 'RemoveButton' component.", itemGO); }
    }

    void OnItemFoundToggle(ShoppingListItem item, bool isFound)
    {
        item.isFound = isFound;
        Debug.Log($"[ShoppingListManager] '{item.product.productName}' marked as {(isFound ? "found" : "not found")}.");
        OnShoppingListChanged?.Invoke();
    }

    void RemoveItemFromShoppingList(ShoppingListItem itemToRemove)
    {
        if (itemToRemove.uiInstance != null)
        {
            Destroy(itemToRemove.uiInstance);
        }
        currentShoppingList.Remove(itemToRemove);

        Debug.Log($"[ShoppingListManager] Removed '{itemToRemove.product.productName}' from shopping list.");
        OnShoppingListChanged?.Invoke();
    }

    public List<ShoppingListItem> GetShoppingList()
    {
        return currentShoppingList;
    }
}

// Ensure ShoppingListItem is defined correctly, perhaps with a reference to the UI GameObject
[System.Serializable]
public class ShoppingListItem
{
    public ProductData product;
    public bool isFound;
    public GameObject uiInstance; // Reference to the instantiated UI GameObject
}