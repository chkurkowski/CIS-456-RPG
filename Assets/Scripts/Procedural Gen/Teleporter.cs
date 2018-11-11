using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Teleporter : MonoBehaviour {

    RoomGeneration roomGen;

    private void Start()
    {
        roomGen = FindObjectOfType<RoomGeneration>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            if (Input.GetKey(KeyCode.T))
            {
                Teleport();
            }
        }
    }

    public void Teleport()
    {
        FindObjectOfType<GameManager>().levelPP();
        //roomGen.ResetRooms();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
