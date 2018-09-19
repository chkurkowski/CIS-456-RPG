using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missle : MonoBehaviour {

    private GameObject target;

    [SerializeField] private float speed = 5f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float attackRate = 1f;

	// Update is called once per frame
	void Update () {
		if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = target.transform.position - transform.position;
        float distance = speed * Time.deltaTime;

        transform.Translate(dir.normalized * distance, Space.World);
        transform.LookAt(target.transform);
	}

    public void setTarget(GameObject t)
    {
        target = t;
    }

    public float getDamage()
    {
        return damage;
    }

    public float getSpeed()
    {
        return speed;
    }

    public float getAttackRate()
    {
        return attackRate;
    }
}
