using System.Collections; // Importă namespace-ul pentru utilizarea coroutinelor
using System.Collections.Generic; // Importă namespace-ul pentru liste generice
using UnityEngine; // Importă biblioteca Unity
using UnityEngine.UI; // Importă biblioteca UI pentru interfață grafică
using UnityEngine.Networking; // Importă biblioteca pentru apeluri web
using System; // Importă System pentru funcționalități de bază

public class Mapbox : MonoBehaviour
{
    public string accessToken; // Token de acces pentru API-ul Mapbox
    public float centerLatitude = -33.8873f; // Coordonata de latitudine a centrului hărții
    public float centerLongitude = 151.2189f; // Coordonata de longitudine a centrului hărții
    public float zoom = 12.0f; // Nivelul de zoom al hărții
    public int bearing = 0; // Unghiul de rotație al hărții
    public int pitch = 0; // Unghiul de înclinare al hărții

    // Enumerare pentru stilurile disponibile ale hărții
    public enum style { Light, Dark, Streets, Outdoors, Satellite, SatelliteStreets };
    public style mapStyle = style.Streets; // Stilul implicit al hărții

    // Enumerare pentru rezoluția hărții
    public enum resolution { low = 1, high = 2 };
    public resolution mapResolution = resolution.low; // Rezoluția implicită a hărții

    private float mapWidth = 1284; // Lățimea implicită a hărții
    private float mapHeight = 2778; // Înălțimea implicită a hărții

    // Array cu stilurile de hartă disponibile în Mapbox
    private string[] styleStr = new string[] { "light-v10", "dark-v10", "streets-v11", "outdoors-v11", "satellite-v9", "satellite-streets-v11" };

    private string url = ""; // URL-ul pentru cererea API
    private bool mapIsLoading = false; // Flag pentru încărcarea hărții
    private Rect rect; // Obiect care reține dimensiunea hărții
    private bool updateMap = true; // Flag pentru actualizarea hărții

    public Button zoomInButton; // Referință către butonul de zoom in
    public Button zoomOutButton; // Referință către butonul de zoom out
    public float zoomStep = 1.0f; // Pasul de zoom

    private Vector2 lastMousePosition; // Poziția mouse-ului pentru drag
    private bool isDragging = false; // Flag pentru a verifica dacă se face drag
    public float dragSpeed = 0.005f; // Viteza de drag

    private List<GameObject> instantiatedCheckpoints = new List<GameObject>(); // Listă cu checkpoint-uri create
    public GameObject checkpointPrefab; // Prefab pentru checkpoint-uri
    public List<Vector2> checkpointCoordinates = new List<Vector2>(); // Listă cu coordonatele checkpoint-urilor

    // Inițializează harta și configurează butoanele
    void Start()
    {
        // Inițializăm harta
        StartCoroutine(GetMapbox()); // Pornește coroutine-ul pentru a obține harta
        rect = gameObject.GetComponent<RawImage>().rectTransform.rect; // Obține dimensiunea imaginii
        Debug.Log(rect.width);
        Debug.Log(rect.height);
        AdjustMapSize(); // Ajustează dimensiunea hărții
        zoomInButton.onClick.AddListener(ZoomIn); // Asociază butonul de zoom in cu funcția respectivă
        zoomOutButton.onClick.AddListener(ZoomOut); // Asociază butonul de zoom out cu funcția respectivă


        //mapWidth = rawImage.rectTransform.rect.width;
        //mapHeight = rawImage.rectTransform.rect.height;
        //Debug.Log($"Map size: width={mapWidth}, height={mapHeight}");
    }

    // Descarcă harta de la Mapbox
    IEnumerator GetMapbox()
    {
        //AdjustMapSize(); // Ajustează dimensiunea hărții
        url = $"https://api.mapbox.com/styles/v1/mapbox/{styleStr[(int)mapStyle]}/static/{centerLongitude},{centerLatitude},{zoom},{bearing},{pitch}/{mapWidth}x{mapHeight}?access_token={accessToken}"; // Creează URL-ul pentru cerere
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url); // Creează cererea web
        yield return www.SendWebRequest(); // Așteaptă răspunsul

        if (www.result == UnityWebRequest.Result.Success)
        {
            gameObject.GetComponent<RawImage>().texture = ((DownloadHandlerTexture)www.downloadHandler).texture; // Aplică textura hărții
            UpdateCheckpoints(); // Actualizează checkpoint-urile
        }
        else { Debug.LogError("WWW ERROR: " + www.error); } // Afișează eroarea dacă cererea a eșuat
    }

    // Ajustează dimensiunea hărții conform UI
    private void AdjustMapSize()
    {
        mapWidth = Mathf.Clamp((int)Math.Round(rect.width), 800, 1280); // Ajustează lățimea hărții
        mapHeight = Mathf.Clamp((int)Math.Round(rect.height), 600, 1280); // Ajustează înălțimea hărții
        Debug.LogError("lățimea hărții: " + mapWidth);
        Debug.LogError("înălțimea hărții: " + mapHeight);
    }

   

    

    

    // Convertește coordonatele geografice în poziții pe hartă
    private Vector2 GeoToMapPosition(float latitude, float longitude)
    {
        float x = (longitude - centerLongitude) * (mapWidth / 360.0f);
        float latRad = latitude * Mathf.Deg2Rad;
        float centerLatRad = centerLatitude * Mathf.Deg2Rad;

        float y = (Mathf.Log(Mathf.Tan(Mathf.PI / 4 + latRad / 2)) -
                   Mathf.Log(Mathf.Tan(Mathf.PI / 4 + centerLatRad / 2)))
                   * (mapHeight / (2 * Mathf.PI));

        // Ajustăm pozițiile pentru a fi relative la dimensiunea imaginii
        x += mapWidth / 2.0f;
        y = mapHeight / 2.0f - y;

        Debug.Log($"GeoToMapPosition: lat={latitude}, lon={longitude}, x={x}, y={y}");
        if (float.IsNaN(x) || float.IsNaN(y))
        {
            Debug.LogError("❌ GeoToMapPosition a returnat NaN! Verifică valorile de input.");
        }
        return new Vector2(x, y);


    }

    // Adaugă checkpoint-uri pe hartă
    void AddCheckpoints()
    {
        // Ștergem checkpoint-urile existente înainte de a le adăuga
        foreach (GameObject checkpoint in instantiatedCheckpoints)
        {
            Destroy(checkpoint);
        }
        instantiatedCheckpoints.Clear();

        foreach (Vector2 coord in checkpointCoordinates)
        {
            Vector2 position = GeoToMapPosition(coord.x, coord.y);

            // Instanțierea checkpoint-ului
            GameObject checkpoint = Instantiate(checkpointPrefab, transform);
            RectTransform rectTransform = checkpoint.GetComponent<RectTransform>();

            // Setarea poziției în funcție de harta curentă
            rectTransform.anchoredPosition = position;

            instantiatedCheckpoints.Add(checkpoint); // Adăugăm în listă pentru update ulterior

            checkpoint = Instantiate(checkpointPrefab, transform);
            rectTransform = checkpoint.GetComponent<RectTransform>();

            // Poziționare
            rectTransform.anchoredPosition = position;




        }
    }

    // Actualizează poziția checkpoint-urilor
    void UpdateCheckpoints()
    {
        Debug.Log($"🔄 Șterg checkpoint-uri vechi: {instantiatedCheckpoints.Count}");

        foreach (GameObject checkpoint in instantiatedCheckpoints)
        {
            Destroy(checkpoint);
        }
        instantiatedCheckpoints.Clear();

        Debug.Log($"✅ Checkpoint-uri după ștergere: {instantiatedCheckpoints.Count}");

        // 📌 Obține dimensiunea RawImage unde este harta
        RectTransform mapRect = GetComponent<RectTransform>();
        Vector2 mapSize = mapRect.rect.size;
        mapWidth = mapRect.rect.width;
        mapHeight = mapRect.rect.height;
        Debug.Log($"Map size: width={mapWidth}, height={mapHeight}");

        foreach (Vector2 coord in checkpointCoordinates)
        {
            Vector2 position = GeoToMapPosition(coord.x, coord.y);

            // 📌 Normalizează coordonatele pentru UI
            float normalizedX = (position.x / mapSize.x) * mapRect.rect.width;
            float normalizedY = (position.y / mapSize.y) * mapRect.rect.height;
            Vector2 uiPosition = new Vector2(normalizedX, normalizedY);

            GameObject checkpoint = Instantiate(checkpointPrefab, transform);
            RectTransform rectTransform = checkpoint.GetComponent<RectTransform>();

            rectTransform.anchoredPosition = uiPosition;
            instantiatedCheckpoints.Add(checkpoint);

            Debug.Log($"📍 Checkpoint creat la: {uiPosition}, total={instantiatedCheckpoints.Count}");
            Debug.Log($"✅ UI rect: {rectTransform.anchoredPosition}, Parent: {rectTransform.parent}");
        }

        if (instantiatedCheckpoints.Count != checkpointCoordinates.Count)
        {
            Debug.LogWarning($"⚠️ Numărul de checkpoint-uri instantiate ({instantiatedCheckpoints.Count}) nu corespunde cu lista de coordonate ({checkpointCoordinates.Count})!");
        }
    }



    // Funcții de zoom in și zoom out
    void ZoomIn() { if (zoom < 22) { zoom += zoomStep; updateMap = true; } } // Crește nivelul de zoom
    void ZoomOut() { if (zoom > 1) { zoom -= zoomStep; updateMap = true; } } // Scade nivelul de zoom


    // Actualizează harta și verifică dacă trebuie să o reîncarce
    void Update()
    {
        HandleDrag(); // Gestionarea interacțiunii de drag

        // Reîncarcă harta și repoziționează checkpoint-urile
        if (updateMap)
        {
            StartCoroutine(GetMapbox()); // Reîncarcă harta dacă este necesar
            updateMap = false; // Resetează flag-ul de actualizare
            UpdateCheckpoints(); // Repoziționează checkpoint-urile
        }
        Debug.Log($"📌 Număr coordonate la fiecare frame: {checkpointCoordinates.Count}");

    }

    // Gestionează interacțiunea de drag a hărții
    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0)) { isDragging = true; lastMousePosition = Input.mousePosition; } // Detectează începutul drag-ului
        if (Input.GetMouseButtonUp(0)) { isDragging = false; } // Detectează sfârșitul drag-ului

        if (isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition; // Calculează diferența de poziție
            centerLongitude -= delta.x * dragSpeed * 0.5f; // Ajustează longitudinea
            centerLatitude -= delta.y * dragSpeed * 0.5f; // Ajustează latitudinea
            lastMousePosition = Input.mousePosition; // Actualizează poziția mouse-ului
            updateMap = true; // Setează flag-ul pentru actualizare
        }
    }

}







/*

//MIT License
//Copyright (c) 2023 DA LAB (https://www.youtube.com/@DA-LAB)
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

public class Mapbox : MonoBehaviour
{
    public string accessToken;
    public float centerLatitude = -33.8873f;
    public float centerLongitude = 151.2189f;
    public float zoom = 12.0f;
    public int bearing = 0;
    public int pitch = 0;
    public enum style {Light, Dark, Streets, Outdoors, Satellite, SatelliteStreets};
    public style mapStyle = style.Streets;
    public enum resolution { low = 1, high = 2 };
    public resolution mapResolution = resolution.low;

    private int mapWidth = 800;
    private int mapHeight = 600;
    private string[] styleStr = new string[] { "light-v10", "dark-v10", "streets-v11", "outdoors-v11", "satellite-v9", "satellite-streets-v11" };
    private string url = "";
    private bool mapIsLoading = false; 
    private Rect rect;
    private bool updateMap = true;

    private string accessTokenLast;
    private float centerLatitudeLast = -33.8873f;
    private float centerLongitudeLast = 151.2189f;
    private float zoomLast = 12.0f;
    private int bearingLast = 0;
    private int pitchLast = 0;
    private style mapStyleLast = style.Streets;
    private resolution mapResolutionLast = resolution.low;

    public Button zoomInButton;
    public Button zoomOutButton;
    public float zoomStep = 1.0f; // Cât de mult să crească/scadă zoom-ul

    private Vector2 lastMousePosition;
    private bool isDragging = false;
    public float dragSpeed = 0.005f; // Ajustează viteza deplasării


    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetMapbox());
        rect = gameObject.GetComponent<RawImage>().rectTransform.rect;
        mapWidth = (int)Math.Round(rect.width);
        mapHeight = (int)Math.Round(rect.height);
        Debug.Log("First map size: " + mapWidth + "x" + mapHeight);
        mapWidth = Mathf.Clamp(mapWidth, 800, 1280);
        mapHeight = Mathf.Clamp(mapHeight, 600, 1280);
        Debug.Log("Final map size: " + mapWidth + "x" + mapHeight);


        // Asociază funcțiile la butoane
        zoomInButton.onClick.AddListener(ZoomIn);
        zoomOutButton.onClick.AddListener(ZoomOut);
    }

    void ZoomIn()
    {
        if (zoom < 22) // Zoom maxim Mapbox este 22
        {
            zoom += zoomStep;
            updateMap = true;
        }
    }

    void ZoomOut()
    {
        if (zoom > 1) // Zoom minim Mapbox este 1
        {
            zoom -= zoomStep;
            updateMap = true;
        }
    }

    void Update()
    {
        // Detectează mouse drag
        if (Input.GetMouseButtonDown(0)) // Când apeși pe ecran
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0)) // Când ridici degetul
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;

            // Inversăm direcția și scădem viteza mișcării
            centerLongitude -= delta.x * dragSpeed * 0.5f; 
            centerLatitude -= delta.y * dragSpeed * 0.5f;  

            lastMousePosition = Input.mousePosition;
            updateMap = true;
        }

        // Detectează touch pe mobil
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                lastMousePosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.position - lastMousePosition;

                // Inversăm direcția și reducem viteza mișcării
                centerLongitude += delta.x * dragSpeed * 0.5f;
                centerLatitude -= delta.y * dragSpeed * 0.5f;

                lastMousePosition = touch.position;
                updateMap = true;
            }
        }

        if (updateMap && (accessTokenLast != accessToken || !Mathf.Approximately(centerLatitudeLast, centerLatitude) || !Mathf.Approximately(centerLongitudeLast, centerLongitude) || zoomLast != zoom || bearingLast != bearing || pitchLast != pitch || mapStyleLast != mapStyle || mapResolutionLast != mapResolution))
        {
            rect = gameObject.GetComponent<RawImage>().rectTransform.rect;
            mapWidth = (int)Math.Round(rect.width);
            mapHeight = (int)Math.Round(rect.height);
            mapWidth = Mathf.Clamp(mapWidth, 800, 1280);
            mapHeight = Mathf.Clamp(mapHeight, 600, 1280);

            StartCoroutine(GetMapbox());
            updateMap = false;
            UpdateCheckpoints();
        }
    }


    IEnumerator GetMapbox()
    {
        mapWidth = Mathf.Clamp(mapWidth, 800, 1280);
        mapHeight = Mathf.Clamp(mapHeight, 600, 1280);
        Debug.Log("Final map size: " + mapWidth + "x" + mapHeight);

        url = "https://api.mapbox.com/styles/v1/mapbox/" + styleStr[(int)mapStyle] + "/static/" + centerLongitude + "," + centerLatitude + "," + zoom + "," + bearing + "," + pitch + "/" + mapWidth + "x" + mapHeight + "?" + "access_token=" + accessToken;
        mapIsLoading = true;
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("WWW ERROR: " + www.error);
            Debug.Log("Mapbox URL: " + url);
            Debug.Log("Final map size: " + mapWidth + "x" + mapHeight);
        }
        else
        {
            mapIsLoading = false;
            gameObject.GetComponent<RawImage>().texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

            accessTokenLast = accessToken;
            centerLatitudeLast = centerLatitude;
            centerLongitudeLast = centerLongitude;
            zoomLast = zoom;
            bearingLast = bearing;
            pitchLast = pitch;
            mapStyleLast = mapStyle;
            mapResolutionLast = mapResolution;
            updateMap = true;

            AddCheckpoints();
        }
    }
    private Vector2 GeoToMapPosition(float latitude, float longitude)
    {
        // Convertim longitude și latitudine în coordonate de harta
        float x = (longitude - centerLongitude) * (mapWidth / 360.0f);

        // Aplicăm transformarea Mercator pentru latitudine
        float latRad = latitude * Mathf.Deg2Rad;
        float centerLatRad = centerLatitude * Mathf.Deg2Rad;
        float y = (Mathf.Log(Mathf.Tan(latRad) + 1 / Mathf.Cos(latRad)) -
                   Mathf.Log(Mathf.Tan(centerLatRad) + 1 / Mathf.Cos(centerLatRad)))
                   * (mapHeight / (2 * Mathf.PI));

        // Ajustăm pozițiile pentru a fi relative la dimensiunea imaginii
        x += mapWidth / 2.0f;
        y = mapHeight / 2.0f - y;

        Debug.Log($"GeoToMapPosition: lat={latitude}, lon={longitude}, x={x}, y={y}");
        return new Vector2(x, y);
    }





    public GameObject checkpointPrefab; // Asociază prefab-ul în Inspector
    public List<Vector2> checkpointCoordinates = new List<Vector2>(); // Adaugă coordonate în Inspector

    void AddCheckpoints()
    {

        foreach (Vector2 coord in checkpointCoordinates)
        {
            Vector2 position = GeoToMapPosition(coord.x, coord.y);

            // Instanțierea checkpoint-ului
            GameObject checkpoint = Instantiate(checkpointPrefab, transform);
            RectTransform rectTransform = checkpoint.GetComponent<RectTransform>();

            // Setarea poziției în funcție de harta curentă
            rectTransform.anchoredPosition = position;

            Debug.Log($"Checkpoint instantiated at: {position}");
        }
    }

    void UpdateCheckpoints()
    {
        for (int i = 0; i < checkpointCoordinates.Count; i++)
        {
            Vector2 coord = checkpointCoordinates[i];
            Vector2 newPos = GeoToMapPosition(coord.x, coord.y);

            // Găsim checkpoint-ul corect (presupunând că acestea au fost instanțiate)
            GameObject checkpoint = transform.GetChild(i).gameObject;
            RectTransform rectTransform = checkpoint.GetComponent<RectTransform>();

            // Actualizăm poziția checkpoint-ului pe hartă
            rectTransform.anchoredPosition = newPos;
            Debug.Log($"Updated checkpoint position to: {newPos}");
        }
    }



}


*/
