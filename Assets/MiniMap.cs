using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMap : MonoBehaviour {

    public Camera cam;
    public GameObject player;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update () 
    {
        cam.gameObject.transform.position = new Vector3(player.transform.position.x, 20, player.transform.position.z);
	}
}
