﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    public Text goldText;

    private PlayerStats stats;
    private static int level = 1;

    private void Start()
    {
        stats = PlayerStats.Instance;
    }

    private void Update()
    {
        goldText.text = stats.getGold().ToString();
    }

    public void levelPP()
    {
        level++;
    }

    public void LoadScene(string s)
    {
        SceneManager.LoadScene(s);
    }

	public void EndGame()
    {
        //TODO: Game over screen ON THIS LINE
        level = 1;
        PlayerStats.Instance.Reset();
        EquipmentManager.instance.UnequipAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
