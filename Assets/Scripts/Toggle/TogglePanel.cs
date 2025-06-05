using UnityEngine;

public class TogglePanel : MonoBehaviour
{
    public GameObject[] panels; // Array cu toate panourile

    public void ShowPanel(int panelIndex)
    {
        // Dezactivează toate panourile
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
        }

        // Activează panoul dorit
        if (panelIndex >= 0 && panelIndex < panels.Length)
        {
            panels[panelIndex].SetActive(true);
        }
    }
}