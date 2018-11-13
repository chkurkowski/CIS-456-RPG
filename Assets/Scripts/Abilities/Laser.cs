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
        if (targets.Count == 0)
        {
            isEnemy = false;
            enemyHealthInst = null;
            return;
        }

        //foreach(GameObject enemy in targets)
        //{
        //    if(isEnemy)
        //    {
        //        enemyHealthInst = enemy.GetComponent<EnemyHealth>();
        //        enemyHealthInst.TakeDamage(damagePerSecond * Time.deltaTime);
        //    }
        //}
    }

    public void setTarget(GameObject t)
    {
        targets.Add(t);

        if (targets.Count == 0)
        {
            isEnemy = false;
            enemyHealthInst = null;
            return;
        }

        isEnemy = true;
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

    private void OnTriggerEnter(Collider col)
    {
        print("Target added");
        if (col.gameObject.tag == "Enemy")
        {
            setTarget(col.gameObject);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if(col.gameObject.tag == "Enemy")
        {
            targets.Remove(col.gameObject);
        }
    }
}
