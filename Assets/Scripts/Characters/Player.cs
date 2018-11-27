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
        Debug.Log("Armor: " + stats.GetArmorModifier() + " Damage: " + stats.GetDamageModifier());

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
        if (stats.GetArmorModifier() > 0)
            dmg = dmg / (stats.GetArmorModifier() / 4);

        stats.setHealth(stats.getHealth() - dmg);
        //Debug.Log("Health: " + stats.getHealth());
    }
}
