using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    private PlayerStats stats;

    private void Start()
    {
        stats = PlayerStats.Instance;
    }

    private void Update()
    {
        if (stats.getHealth() <= 0)
        {
            FindObjectOfType<GameManager>().EndGame();
        }
    }

    public void CollectGold(int g)
    {
        stats.addGold(g);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag == "Attack")
        {
            //TODO: Detect different enemy weapons
        }
    }

    public void TakeDamage(float dmg)
    {
        stats.setHealth(stats.getHealth() - dmg);
        //Debug.Log("Health: " + stats.getHealth());
    }
}
