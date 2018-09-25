using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class clickToMove : MonoBehaviour {

    //Camera
    public Camera cam;

    //For future use
    private float attackDistance;
    private float attackRate;
    //access the attack script

    private NavMeshAgent navMeshAgent;
    private Transform targetedTransform;
    //use this if you need to shoot on this character.
    private Ray shootRay; 
    private RaycastHit shotHit;
    private bool ranged;
    private bool enemyClicked;
    private float nextFire;

    // Use this for initialization
    void Awake() {
        navMeshAgent = GetComponent<NavMeshAgent>();
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetButtonDown("Fire1")) 
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                print(hit.point);
                navMeshAgent.destination = hit.point;
                navMeshAgent.isStopped = false;
            }
        }
	}
}
