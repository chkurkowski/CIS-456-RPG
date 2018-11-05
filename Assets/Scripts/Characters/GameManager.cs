using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

    public static int level = 1;

    public void LoadScene(string s)
    {
        SceneManager.LoadScene(s);
    }

	public void EndGame()
    {
        //TODO: Game over screen
        level = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
