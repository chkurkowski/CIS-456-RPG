using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSpawnObjScript : MonoBehaviour {

    public GameObject OnexOneRoom;
    public GameObject RoomDoor;
    public Transform map;

	// Use this for initialization
	void Start () {
        return;

        GameObject rm = Instantiate(OnexOneRoom, new Vector3(-10, 50, 60), Quaternion.identity);
        rm.transform.parent = map;

        GameObject leftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
        leftDoor.transform.parent = rm.transform;
        leftDoor.transform.localPosition = new Vector3(24.5f, 1f, 0f);
        leftDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));

        GameObject rightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
        rightDoor.transform.parent = rm.transform;
        rightDoor.transform.localPosition = new Vector3(-24.5f, 1f, 0f);
        rightDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));

        GameObject topDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
        topDoor.transform.parent = rm.transform;
        topDoor.transform.localPosition = new Vector3(0f, 1f, -24.5f);

        GameObject bottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
        bottomDoor.transform.parent = rm.transform;
        bottomDoor.transform.localPosition = new Vector3(0f, 1f, 24.5f);
    }
}
