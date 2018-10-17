using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour {

    public GameObject character;

    private Transform charTransform;
    private Transform cameraTransform;
    //private float[] xBounds;
    //private float yBound;
    //private float[] zBounds;
    private float xOffset = 2.9f;
    private float yOffset = 4.6f;
    private float zOffset = 3.25f;

    private void Awake()
    {
        charTransform = character.transform;
        Vector3 charPos = charTransform.position;
        //xBounds = new float[2] { charPos.x + .5f, charPos.x - .5f };
        //zBounds = new float[2] { charPos.x + .5f, charPos.y - .5f };
        //yBound = charPos.y;
        
    }

    // Update is called once per frame
    void Update ()
    {
        this.transform.position = new Vector3(character.transform.position.x + xOffset, character.transform.position.y + yOffset, character.transform.position.z + zOffset);
    }
}
