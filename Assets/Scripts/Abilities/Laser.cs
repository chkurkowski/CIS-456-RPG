using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour {

    /* 
     * Raycast out from the player and then send the laser based on whats hit and the distance.
     */

    [SerializeField] private GameObject atkOrigin;

    private GameObject target;
    private List<GameObject> targets;
    private EnemyHealth enemyHealthInst;

    [SerializeField] private float damagePerSecond = 100f;
    [SerializeField] private float range;
    private bool isEnemy = false;

    public void Awake()
    {
        //Gets the length of the red line
        range = this.transform.localScale.z * this.GetComponent<BoxCollider>().size.z * this.transform.parent.localScale.z;
        targets = new List<GameObject>();
    }

    public void Update()
    {
        if (target == null)
        {
            isEnemy = false;
            enemyHealthInst = null;
            return;
        }

        if (isEnemy) //If the target being lasered is an enemy
        {
            enemyHealthInst.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }

    public void setTarget(GameObject t)
    {
        target = t;

        if (target == null)
        {
            isEnemy = false;
            enemyHealthInst = null;
            return;
        }

        if (target.tag == "Enemy")
        {
            isEnemy = true;
            enemyHealthInst = target.GetComponent<EnemyHealth>();
        }
    }

    public float getDamagePerSecond()
    {
        return damagePerSecond;
    }

    public float getRange()
    {
        //Recalculates the range in case it has updated since last function call
        range = this.transform.localScale.z * this.GetComponent<BoxCollider>().size.z * this.transform.parent.localScale.z;
        return range;
    }
}
