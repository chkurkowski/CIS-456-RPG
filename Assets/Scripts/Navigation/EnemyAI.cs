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
    private GameObject[] waypoints;
    private int watpointInd;
    private float patrolSpeed = 1.0f;
    private float chaseSpeed = 1.8f;

    //Transform for targeting
    private Transform targetedTransform;
    private bool alive;

    // Use this for initialization
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        target = GameObject.Find("Character");

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
        agent.destination = transform.position;
        print("Patrolling");
        state = State.CHASE;
        //if()
    }

    private void Chase()
    {
        agent.speed = chaseSpeed;
        print("Chasing");
        if (Vector3.Distance(target.transform.position, transform.position) <= 5)
        {
            targetedTransform = target.transform;
            agent.destination = new Vector3(targetedTransform.position.x, transform.position.y, targetedTransform.position.z);
            agent.isStopped = false;
        }
        else
            state = State.PATROL;
    }

    private void Attack()
    {

    }
}