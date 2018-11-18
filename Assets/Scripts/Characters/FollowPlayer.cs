using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour {

    public GameObject character;

    private Transform charTransform;
    private Transform cameraTransform;
    private Vector3 offset = new Vector3(2.9f, 4.6f, 3.25f);

    private void Awake()
    {
        charTransform = character.transform;
        Vector3 charPos = charTransform.position; 
    }

    void LateUpdate ()
    {
        moveCameraToPlayer();
    }

    public void moveCameraToPlayer()
    {
        this.transform.position = charTransform.position + offset;
    }
}
