using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gold : MonoBehaviour {

    private GameObject player;

    private int value = 100;
    private bool canPickup = false;
    private float pickupRange = 1.25f;

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
        Invoke("Pickup", .8f);
    }

    // Update is called once per frame
    void Update () {
        if(Vector3.Distance(transform.position, player.transform.position) < pickupRange && canPickup)
        {
            player.GetComponent<Player>().CollectGold(value);
            Destroy(gameObject);
        }
	}

    private void Pickup()
    {
        canPickup = true;
    }

    public void SetWorth(int i)
    {
        if (i == 0)
            value = 100;
        else if (i == 1)
            value = 250;
        else if (i == 2)
            value = 500;
    }
}
