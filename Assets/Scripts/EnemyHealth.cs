using UnityEngine;

public class EnemyHealth : MonoBehaviour {

    [SerializeField] private float health = 100f;

    private void Update()
    {
        if (health <= 0)
        {
            Destroy(this.gameObject);
        }
        Move();
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag == "Attack")
        {
            Missle missle = col.gameObject.GetComponent<Missle>();
            if (missle != null)
            {
                health -= missle.getDamage();
                Debug.Log("Hit for: " + missle.getDamage());
                return;
            }
        }
    }

    public void TakeDamage(float dmg)
    {
        health -= dmg;
        Debug.Log("Hit for: " + dmg);
    }

    //Target movement system
    private bool right = false;
    private float timer = 0;

    private void Move()
    {
        timer += Time.deltaTime;
        if (right)
            this.transform.position += new Vector3(.75f * Time.deltaTime, 0, 0);
         else
            this.transform.position += new Vector3(-.75f * Time.deltaTime, 0, 0);
        if (timer >= 2f)
        {
            timer = 0;
            if (right)
                right = false;
            else
                right = true;
        }
    }
}
