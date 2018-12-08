using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HighScore : MonoBehaviour {

    public TextMeshProUGUI textMesh;

    public void DisplayScore()
    {
        textMesh.text = "SCORE: " + PlayerStats.Instance.getGold().ToString();
    }
}
