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
    [SerializeField]
    private float patrolSpeed = 2f;
    [SerializeField]
    private float chaseSpeed = 3.2f;
    private float offset = 5f;

    //Transform for targeting
    private Transform targetedTransform;
    private bool alive;

    //Move timer
    private float idleTimer = 0f;
    private const float IDLETIME = 1.5f;

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
            //print(state);
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
            float step = patrolSpeed * Time.deltaTime;

            //transform.position = Vector3.MoveTowards(transform.position, agent.destination, step);
            agent.isStopped = false;
        }
        else if (Vector3.Distance(transform.position, agent.destination) < 2)
        {
            agent.isStopped = true;
            idleTimer += Time.deltaTime;

            //print(timer);
            if (idleTimer >= IDLETIME)
            {
                agent.destination = RandomPosition();
                idleTimer = 0;
            }
                
        }

        if (Vector3.Distance(target.transform.position, transform.position) <= 6)
        {
            RaycastHit hit;
            Vector3 direction = target.transform.position - transform.position;
            direction.Normalize();
            //print("ATTEMPT TO CHASE");

            Debug.DrawRay(transform.position, direction, Color.red);

            if (Physics.Raycast(transform.position, direction, out hit))
            {
                //print("RAYCAST HIT:" + hit.collider.tag);
                if (hit.collider.tag == "Player")
                {
                    //print("IS PLAYER");
                    state = State.CHASE;
                }
            }
        }
    }

    private void Chase()
    {
        agent.speed = chaseSpeed;
        if (Vector3.Distance(target.transform.position, transform.position) <= 8)
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
        float maxOffset = 16.0f;
        RaycastHit hit;

        float xOffset = Random.Range(-maxOffset, maxOffset);
        float zOffset = Random.Range(-maxOffset, maxOffset);

        newPos.x += xOffset;
        newPos.z += zOffset;

        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            if (hit.collider.tag == "Walkable" && hit.point.y < .5f)
            {
                //TODO: Get Neighbors as a list, then get the neighbors of the neighbors. Put all those in a list and remove any that are the same.
                //Pick a room and then a point in that room (using room.getRandomPosition())

                /*
                List<Room> neighbors = new List<Room>();
                List<Room> temp = new List<Room>();
                List<Room> rooms = gen.getAllRooms();
                Room thisRoom;

                foreach(Room r in rooms)
                {
                    if(r.roomRef.transform.position == hit.collider.transform.position)
                    {
                        thisRoom = r;
                        List<Room> firstNeighbors = thisRoom.getNeighboringRooms();

                        foreach (Room n in firstNeighbors)
                        {
                            temp = n.getNeighboringRooms();

                            neighbors.Add(n);

                            for (int i = 0; i < n.getNeighboringRooms().Count; i++)
                            {
                                if(!neighbors.Contains(temp[i]))
                                    neighbors.Add(temp[i]);
                            }
                        }
                        break;
                    }
                }

                print(neighbors[0]);
                
                Room chosenRoom = neighbors[Random.Range(0, neighbors.Count)];
                newPos = chosenRoom.roomRef.transform.position;
                float offsetAmount;
                float offsetX;
                float offsetZ;

                if(chosenRoom.size == chosenRoom.OnexOne)
                {
                    offsetAmount = 9;
                    offsetX = Random.Range(-offsetAmount, offsetAmount);
                    offsetZ = Random.Range(-offsetAmount, offsetAmount);
                }
                else if(chosenRoom.size == chosenRoom.OnexTwo)
                {
                    offsetAmount = 9;
                    offsetX = Random.Range(-offsetAmount, offsetAmount);
                    offsetZ = Random.Range(-offsetAmount, offsetAmount);
                }
                else if (chosenRoom.size == chosenRoom.TwoxOne)
                {
                    offsetAmount = 9;
                    offsetX = Random.Range(-offsetAmount, offsetAmount);
                    offsetZ = Random.Range(-offsetAmount, offsetAmount);
                }
                else if (chosenRoom.size == chosenRoom.TwoxTwo)
                {
                    offsetAmount = 9;
                    offsetX = Random.Range(-offsetAmount, offsetAmount);
                    offsetZ = Random.Range(-offsetAmount, offsetAmount);
                }
                else if (chosenRoom.size == chosenRoom.ThreexThree)
                {
                    offsetAmount = 9;
                    offsetX = Random.Range(-offsetAmount, offsetAmount);
                    offsetZ = Random.Range(-offsetAmount, offsetAmount);
                }
                */

                if (Vector3.Distance(newPos, transform.position) > 4)
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