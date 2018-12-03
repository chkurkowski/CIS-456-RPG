using UnityEngine;

public class EnemyHealth : MonoBehaviour {

    public GameObject[] goldLoot;
    public GameObject[] itemLoot;
    public GameObject[] potions;

    // Chances in order to get 1 stack, 2 stacks, 3 stacks
    private float[] goldChance = {
        65, 85, 90
    };
    private float[] itemChance = {
        5, 10, 15, 20
    };
    private float[] potionChance ={
        15
    };

    [SerializeField] private float health = 5f;

    private void Update()
    {
        if (health <= 0)
        {
            SpawnGold();
            SpawnItem();
            SpawnPotion();
            EnemyAI AI = GetComponent<EnemyAI>();
            AI.alive = false;
            AI.agent.enabled = false;

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
                TakeDamage(missle.getDamage());
                Debug.Log("Hit for: " + missle.getDamage());
                return;
            }
        }
    }

    private void OnCollisionStay(Collision col)
    {
        print("Hit Stay");
        Laser laser = col.gameObject.GetComponent<Laser>();
        if (col.gameObject.tag == "Attack")
        {
            if (laser != null)
            {
                TakeDamage(laser.getDamagePerSecond() * Time.deltaTime);
                print("Dealt damage");
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
        if(gameObject.name == "Target")
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

    private void SpawnGold()
    {
        float roll = Random.Range(0, 100f);

        if (roll < goldChance[0])
        {
            GameObject gm = Instantiate(goldLoot[0], transform.position + Vector3.up, Quaternion.identity);
            gm.GetComponent<Gold>().SetWorth(0);
        }
        else if (roll < goldChance[1])
        {
            GameObject gm = Instantiate(goldLoot[1], transform.position + Vector3.up, Quaternion.identity);
            gm.GetComponent<Gold>().SetWorth(1);
        }
        else if (roll < goldChance[2])
        {
            GameObject gm = Instantiate(goldLoot[2], transform.position + Vector3.up, Quaternion.identity);
            gm.GetComponent<Gold>().SetWorth(2);
        }
    }

    private void SpawnItem()
    {
        float roll = Random.Range(0, 100f);

        if (roll < itemChance[0])
        {
            GameObject gm = Instantiate(itemLoot[0], transform.position + Vector3.up, Quaternion.identity);
            //Set Quality here?
        }
        else if (roll < itemChance[1])
        {
            GameObject gm = Instantiate(itemLoot[1], transform.position + Vector3.up, Quaternion.identity);
            //Set Quality here?
        }
        else if (roll < itemChance[2])
        {
            GameObject gm = Instantiate(itemLoot[2], transform.position + Vector3.up, Quaternion.identity);
            //Set quiality here?
        }
        else if(roll < itemChance[3])
        {
            GameObject gm = Instantiate(itemLoot[3], transform.position + Vector3.up, Quaternion.identity);
            //Set Quality here?
        }
    }

    private void SpawnPotion()
    {
        float roll = Random.Range(0, 100f);

        if (roll < potionChance[0])
        {
            GameObject gm = Instantiate(potions[0], transform.position + Vector3.up, Quaternion.identity);
            //Set Quality here?
        }
    }
}
