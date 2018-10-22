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
    //public Transform patrolBase;

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
        //patrolBase.Find("Map");

        alive = true;

        state = State.PATROL;

        StartCoroutine("FSM");
    }

    IEnumerator FSM()
    {
        while(alive)
        {
            print(state);
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
        if (Vector3.Distance(transform.position, agent.destination) >= 2)
        {
            print("Move to Pos");
            float step = patrolSpeed * Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, agent.destination, step);
        }
        else if (Vector3.Distance(transform.position, agent.destination) < 2)
        {
            print("Search new Pos");
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

        float xOffset = 5f;
        float zOffset = 5f;

        float xPos = Random.Range(transform.position.x + xOffset, transform.position.x - xOffset);
        float zPos = Random.Range(transform.position.z + zOffset, transform.position.z - zOffset);

        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            print(hit.collider.name);
            if (hit.collider.tag == "Walkable")
            {
                //Room roomRef = hit.collider.gameObject.GetComponent<Room>();
                //Room[] rooms = new Room[roomRef.getNeighboringRooms().Count];
                //roomRef.getNeighboringRooms().CopyTo(rooms);

                //Room selectedRoom = rooms[Random.Range(0, rooms.Length)];
                //newPos = new Vector3(selectedRoom)

                newPos = new Vector3(xPos, transform.position.y, zPos);

                validPos = true;
            }
            else
                validPos = false;
        }

        if (validPos)
            return newPos;
        else
            return transform.position;
    }
}