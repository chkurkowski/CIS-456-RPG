using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

    private static int level = 1;

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
        //TODO: Game over screen
        level = 1;
        PlayerStats.Instance.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
