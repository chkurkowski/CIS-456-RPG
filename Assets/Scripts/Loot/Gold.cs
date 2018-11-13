using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gold : MonoBehaviour {

    private GameObject player;

    private int value = 100;

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update () {
        if(Vector3.Distance(transform.position, player.transform.position) < 1)
        {
            player.GetComponent<Player>().CollectGold(value);
            Destroy(gameObject);
        }
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
