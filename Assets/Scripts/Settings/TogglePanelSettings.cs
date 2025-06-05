using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class TogglePanelSettings : MonoBehaviour
{
    public GameObject[] panels; // Array cu toate panourile
    public GameObject calendar;
    public Text Incorecttext;

    public void ShowPanel(int panelIndex)
    {
        // Dezactivează toate panourile
        foreach (GameObject panel in panels)
        {
            panel.SetActive(false);
            Debug.LogError($"!Panel dezactivat {panel}");
        }

        // Activează panoul dorit
        if (panelIndex >= 0 && panelIndex < panels.Length)
        {
            panels[panelIndex].SetActive(true);
            Debug.LogError($"?Panel activat {panels[panelIndex]}");
        }
    }
    /*
    public void ShowPanel(int panelIndex)
    {
        // Dezactivează toate panourile
        for (int i = 0; i < panels.Length; i++)
        {
            if (i != 2) // Nu dezactivăm panel2 în mod normal
            {
                panels[i].SetActive(i == panelIndex); // Activează doar panoul selectat
                panels[2].SetActive(false);
            }
        }

        if (panelIndex == 2)
        {
            panels[2].SetActive(true);
            panels[1].SetActive(true); // Ne asigurăm că panel1 este activ
        }

        // Dacă panelIndex este 1, ascunde Incorecttext
        if (panelIndex == 1)
        {
            Incorecttext.gameObject.SetActive(false);
        }
    }
    */
    public void ShowCalendar()
    {
        calendar.SetActive(true);
    }
}



/*
using UnityEngine;
using UnityEngine.UI;

public class TogglePanelSettings : MonoBehaviour
{
    public GameObject panel0;
    public GameObject panel1;
    public GameObject panel2;
    public GameObject panel3;
    public GameObject panel4;
    public GameObject calendar;
    public Text Incorecttext;


    // Method to show Panel3
    public void ShowPanel0()
    {
        panel0.SetActive(true);
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);

    }

    // Method to show Panel1
    public void ShowPanel1()
    {
        panel0.SetActive(false);
        panel1.SetActive(true);
        Incorecttext.gameObject.SetActive(false);
    }

    public void ShowPanel2()
    {
        //panel0.SetActive(false);
        //panel1.SetActive(false);
        panel2.SetActive(true);
    }

    public void ShowCalendar()
    {
        calendar.SetActive(true);
    }

    public void ShowPanel3()
    {
        panel0.SetActive(false);
        panel3.SetActive(true);
        

    }

    public void ShowPanel4()
    {
        panel0.SetActive(false);
        panel4.SetActive(true);


    }
}

    /*

    public GameObject panel1; // The first panel
    public GameObject panel2; // The second panel
    public GameObject panel3;
    public GameObject panel4;
    public GameObject panel5;
    public GameObject panel6;
    public GameObject panel7;
    public GameObject panel8;
    public GameObject panel9;
    public GameObject panel10;
    public GameObject panel11;

    // Method to show Panel1 and hide Panel2
    public void ShowPanel1()
    {
        panel1.SetActive(true);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(false);
        
    }

    // Method to hide Panel1 and show Panel2
    public void ShowPanel2()
    {
        panel1.SetActive(false);
        panel2.SetActive(true);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(false);
    }

    public void ShowPanel3()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(true);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(false);
    }

    public void ShowPanel4()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(true);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(false);
    }

    public void ShowPanel5()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(true);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(false);
    }

    public void ShowPanel6()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(true);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(false);
    }
    public void ShowPanel7()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(true);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(false);
    }
    public void ShowPanel8()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(true);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(false);
    }
    public void ShowPanel9()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(true);
        panel10.SetActive(false);
        panel11.SetActive(false);
    }
    public void ShowPanel10()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(true);
        panel11.SetActive(false);
    }
    public void ShowPanel11()
    {
        panel1.SetActive(false);
        panel2.SetActive(false);
        panel3.SetActive(false);
        panel4.SetActive(false);
        panel5.SetActive(false);
        panel6.SetActive(false);
        panel7.SetActive(false);
        panel8.SetActive(false);
        panel9.SetActive(false);
        panel10.SetActive(false);
        panel11.SetActive(true);
    }
    */

