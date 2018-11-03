using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour {

    [SerializeField] private float health = 100f;

    private void Update()
    {
        if (health <= 0)
        {
            //FindObjectOfType<GameManager>().EndGame();
        }
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
        health -= dmg;
        Debug.Log("Health: " + health);
    }
}
