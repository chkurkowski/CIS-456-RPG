using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileHandler : MonoBehaviour {

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag != "Player" || col.gameObject.tag != "Attack")
        {
            print(col.gameObject.tag);
            Destroy(this.gameObject);
        }
    }
}
