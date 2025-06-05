using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OSMMapLoader : MonoBehaviour
{
    public RawImage mapImage; // Imaginea unde vom încărca harta
    public int zoom = 14; // Nivelul de zoom (între 1 și 19)
    public int zoomStep = 1; // Pasul de zoom
    public float latitude = 44.3302f; // Coordonatele orașului (ex: București)
    public float longitude = 23.7949f;
    private bool updateMap = true; // Flag pentru actualizarea hărții
    public Button zoomInButton; // Referință către butonul de zoom in
    public Button zoomOutButton; // Referință către butonul de zoom out

    private Vector2 lastMousePosition; // Poziția mouse-ului pentru drag
    private bool isDragging = false; // Flag pentru a verifica dacă se face drag
    public float dragSpeed = 0.005f; // Viteza de drag

    private void Start()
    {
        StartCoroutine(LoadOSMTile());
        zoomInButton.onClick.AddListener(ZoomIn); // Asociază butonul de zoom in cu funcția respectivă
        zoomOutButton.onClick.AddListener(ZoomOut); // Asociază butonul de zoom out cu funcția respectivă
    }

    IEnumerator LoadOSMTile()
    {
        int xTile = (int)(Mathf.Floor((longitude + 180.0f) / 360.0f * (1 << zoom)));
        int yTile = (int)(Mathf.Floor((1.0f - Mathf.Log(Mathf.Tan(latitude * Mathf.PI / 180.0f) +
                  1.0f / Mathf.Cos(latitude * Mathf.PI / 180.0f)) / Mathf.PI) / 2.0f * (1 << zoom)));

        string url = $"https://tile.openstreetmap.org/{zoom}/{xTile}/{yTile}.png";
        Debug.Log("Loading tile: " + url);

        using (WWW www = new WWW(url))
        {
            yield return www;
            mapImage.texture = www.texture;
        }
    }

    void Update()
    {
        //HandleDrag(); // Gestionarea interacțiunii de drag

        // Reîncarcă harta și repoziționează checkpoint-urile
        if (updateMap)
        {
            StartCoroutine(LoadOSMTile()); // Reîncarcă harta dacă este necesar
            updateMap = false; // Resetează flag-ul de actualizare
            //UpdateCheckpoints(); // Repoziționează checkpoint-urile
        }
        //Debug.Log($"📌 Număr coordonate la fiecare frame: {checkpointCoordinates.Count}");

    }

    void ZoomIn() { if (zoom < 22) { zoom += zoomStep; updateMap = true; } } // Crește nivelul de zoom
    void ZoomOut() { if (zoom > 1) { zoom -= zoomStep; updateMap = true; } } // Scade nivelul de zoom


    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0)) { isDragging = true; lastMousePosition = Input.mousePosition; } // Detectează începutul drag-ului
        if (Input.GetMouseButtonUp(0)) { isDragging = false; } // Detectează sfârșitul drag-ului

        if (isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition; // Calculează diferența de poziție
            longitude -= delta.x * dragSpeed * 0.5f; // Ajustează longitudinea
            latitude -= delta.y * dragSpeed * 0.5f; // Ajustează latitudinea
            lastMousePosition = Input.mousePosition; // Actualizează poziția mouse-ului
            updateMap = true; // Setează flag-ul pentru actualizare
        }
    }

}
