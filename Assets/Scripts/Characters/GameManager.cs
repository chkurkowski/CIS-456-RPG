using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

	public void EndGame()
    {
        //TODO: Game over screen
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
