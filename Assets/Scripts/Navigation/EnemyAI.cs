using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    //Public values and Enum
    public NavMeshAgent agent;
    [SerializeField]
    private GameObject target;
    public enum State {
        PATROL,
        CHASE,
        ATTACK
    }
    public State state;
    public Rect patrolArea;
    public Transform patrolBase;

    public NavigationBaker baker;
    public RoomGeneration gen;

    //Waypoint values
    public GameObject[] waypoints;
    private int waypointInd;
    private float patrolSpeed = 1f;
    private float chaseSpeed = 1.8f;
    private float offset = 5f;

    //Transform for targeting
    private Transform targetedTransform;
    private bool alive;

    // Use this for initialization
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        target = GameObject.Find("Character");
        baker = FindObjectOfType<NavigationBaker>();
        gen = FindObjectOfType<RoomGeneration>();

        waypointInd = Random.Range(0, waypoints.Length);
        agent.destination = RandomPosition();

        alive = true;

        state = State.PATROL;

        StartCoroutine("FSM");
    }

    IEnumerator FSM()
    {
        while(alive && baker.generated)
        {
            switch (state)
            {
                case State.PATROL:
                    Patrol();
                    break;
                case State.CHASE:
                    Chase();
                    break;
                case State.ATTACK:
                    Attack();
                    break;
            }
            yield return null;
        }
    }

    //private void Patrol()
    //{
    //    agent.speed = patrolSpeed;
    //    agent.destination = RandomPosition();
    //    if(Vector3.Distance(transform.position, waypoints[waypointInd].transform.position) >= 1)
    //    {
    //        agent.destination = waypoints[waypointInd].transform.position;
    //        agent.isStopped = false;
    //        print("Patrolling");
    //    }
    //    else if(Vector3.Distance(transform.position, waypoints[waypointInd].transform.position) < 1)
    //    {
    //        waypointInd = Random.Range(0, waypoints.Length);
    //    }

    //    if(Vector3.Distance(target.transform.position, transform.position) <= 5)
    //    {
    //        state = State.CHASE;
    //    }
    //}

    private void Patrol()
    {
        agent.speed = patrolSpeed;
        if (Vector3.Distance(transform.position, agent.destination) >= 2)
        {
            float step = patrolSpeed * Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, agent.destination, step);
        }
        else if (Vector3.Distance(transform.position, agent.destination) < 2)
        {
            agent.destination = RandomPosition();
        }

        if (Vector3.Distance(target.transform.position, transform.position) <= 5)
        {
            state = State.CHASE;
        }
    }

    private void Chase()
    {
        agent.speed = chaseSpeed;
        if (Vector3.Distance(target.transform.position, transform.position) <= 5)
        {
            targetedTransform = target.transform;
            agent.destination = new Vector3(targetedTransform.position.x, transform.position.y, targetedTransform.position.z);
            print("Chasing");
        }
        else
            state = State.PATROL;
    }

    private void Attack()
    {

    }

    private Vector3 RandomPosition()
    {
        Vector3 newPos = transform.position;
        bool validPos = false;
        RaycastHit hit;

        float newX = Random.Range(0f, gen.areaSizeX);
        float newZ = Random.Range(0f, gen.areaSizeY);

        newPos = new Vector3(newX, transform.position.y, newZ);

        if (Physics.Raycast(newPos, Vector3.down, out hit))
        {
            print(hit.collider.name);
            if (hit.collider.tag == "Walkable")
                validPos = true;
            else
                validPos = false;
        }

        if (validPos)
            return newPos;
        else
            return transform.position;
    }
}