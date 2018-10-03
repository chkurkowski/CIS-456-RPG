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

        waypointInd = Random.Range(0, waypoints.Length);

        alive = true;

        state = State.PATROL;

        StartCoroutine("FSM");
    }

    IEnumerator FSM()
    {
        while(alive)
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

    private void Patrol()
    {
        agent.speed = patrolSpeed;
        agent.destination = RandomPosition();
        if(Vector3.Distance(transform.position, waypoints[waypointInd].transform.position) >= 1)
        {
            agent.SetDestination(waypoints[waypointInd].transform.position);
            agent.isStopped = false;
            print("Patrolling");
        }
        else if(Vector3.Distance(transform.position, waypoints[waypointInd].transform.position) < 1)
        {
            waypointInd = Random.Range(0, waypoints.Length);
        }

        if(Vector3.Distance(target.transform.position, transform.position) <= 5)
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
        Vector3 newPos = Vector3.zero; 
        while(Vector3.Distance(newPos, transform.position) < 3)
        {
            newPos = new Vector3(Random.Range(transform.position.x - offset, transform.position.x + offset), transform.position.y, Random.Range(transform.position.z - offset, transform.position.z + offset));
        }
        return newPos;
    }
}