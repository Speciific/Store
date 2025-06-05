using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using SimpleJSON;
using System.Collections.Generic;

public class ProfiLocator : MonoBehaviour
{
    public GameObject prefabMarker; // Prefab pentru marker
    public RectTransform mapTransform; // Containerul UI pentru harta
    public float mapCenterLat = 44.3302f; // Latitudine centrală a hărții
    public float mapCenterLon = 23.7949f; // Longitudine centrală a hărții
    public float scaleFactor = 1000f; // Factor de scalare pentru UI

    private string overpassUrl = "https://overpass-api.de/api/interpreter";

    void Start()
    {
        StartCoroutine(GetProfiLocations());
    }

    IEnumerator GetProfiLocations()
    {
        string query = "[out:json];node[\"shop\"=\"supermarket\"][\"name\"=\"Profi\"](around:5000,44.3302,23.7949);out;";
        string url = overpassUrl + "?data=" + UnityWebRequest.EscapeURL(query);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Profi Store Data: " + request.downloadHandler.text);
                ProcessStoreData(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error getting data: " + request.error);
            }
        }
    }

    void ProcessStoreData(string jsonData)
    {
        var json = SimpleJSON.JSON.Parse(jsonData);
        if (json == null || !json.HasKey("elements"))
        {
            Debug.LogError("Invalid JSON data received!");
            return;
        }

        foreach (JSONNode element in json["elements"])
        {
            if (!element.HasKey("lat") || !element.HasKey("lon")) continue;

            float lat = element["lat"].AsFloat;
            float lon = element["lon"].AsFloat;
            Debug.Log("Profi found at: " + lat + ", " + lon);

            CreateStoreMarker(lat, lon);
        }
    }

    void CreateStoreMarker(float lat, float lon)
    {
        if (prefabMarker == null || mapTransform == null)
        {
            Debug.LogError("PrefabMarker or MapTransform is not assigned in the Inspector!");
            return;
        }

        GameObject marker = Instantiate(prefabMarker, mapTransform);
        marker.name = "Profi Store";

        Vector2 position = ConvertLatLonToUI(lat, lon);
        marker.GetComponent<RectTransform>().anchoredPosition = position;

        Debug.Log($"Marker added at: {lat}, {lon} -> UI Pos: {position}");

    }

    Vector2 ConvertLatLonToUI(float lat, float lon)
    {
        float mapWidth = mapTransform.rect.width;
        float mapHeight = mapTransform.rect.height;

        // Limitele hărții (lat/lon minime și maxime)
        float minLat = 44.2900f; // Ajustează după hartă
        float maxLat = 44.3700f;
        float minLon = 23.7300f;
        float maxLon = 23.8100f;

        // Normalizare coordonate
        float x = (lon - minLon) / (maxLon - minLon) * mapWidth;
        float y = (1 - (lat - minLat) / (maxLat - minLat)) * mapHeight;

        return new Vector2(x, y);
    }

}
