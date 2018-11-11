using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    //Public values and Enum
    public NavMeshAgent agent;
    [SerializeField]
    private GameObject target;
    private Player playerScript;
    public enum State
    {
        PATROL,
        CHASE,
        ATTACK
    }
    public State state;
    public Rect patrolArea;
    //public Transform patrolBase;

    private NavigationBaker baker;
    private RoomGeneration gen;

    //Waypoint values
    public GameObject[] waypoints;
    private int waypointInd;
    [SerializeField]
    private float patrolSpeed = 2f;
    [SerializeField]
    private float chaseSpeed = 3.2f;
    [SerializeField]
    private float attackRange = 1f;
    [SerializeField]
    private float detectionRange = 6f;
    [SerializeField]
    private float chaseRange = 8f;
    [SerializeField]
    private float attackDamage = 1f;
    private float offset = 5f;

    //Transform for targeting
    private Transform targetedTransform;
    private bool alive;

    //Move timer
    private float idleTimer = 0f;
    private const float IDLETIME = 1.5f;

    //Attack cooldown
    private float attackTimer = 0f;
    [SerializeField]
    private float attackCooldown = 0.5f;

    // Use this for initialization
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        target = GameObject.Find("Character");
        baker = FindObjectOfType<NavigationBaker>();
        gen = FindObjectOfType<RoomGeneration>();
        playerScript = FindObjectOfType<Player>();
        //patrolBase.Find("Map");

        alive = true;

        state = State.PATROL;

        StartCoroutine("FSM");
    }

    IEnumerator FSM()
    {
        while (alive)
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
        float proximity = 2f;
        if (Vector3.Distance(transform.position, agent.destination) >= proximity)
        {
            float step = patrolSpeed * Time.deltaTime;

            //transform.position = Vector3.MoveTowards(transform.position, agent.destination, step);
            agent.isStopped = false;
        }
        else if (Vector3.Distance(transform.position, agent.destination) < proximity)
        {
            agent.isStopped = true;
            idleTimer += Time.deltaTime;

            //print(timer);
            if (idleTimer >= IDLETIME)
            {
                agent.destination = getRandomTargetPosition();
                idleTimer = 0;
            }

        }

        if (Vector3.Distance(target.transform.position, transform.position) <= detectionRange)
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
                    agent.isStopped = false;
                    state = State.CHASE;
                }
            }
        }
    }

    private void Chase()
    {
        agent.speed = chaseSpeed;
        attackTimer -= Time.deltaTime;

        if (Vector3.Distance(target.transform.position, transform.position) <= attackRange)
        {
            //1
            agent.isStopped = true;
            state = State.ATTACK;
        }
        else if (Vector3.Distance(target.transform.position, transform.position) <= chaseRange)
        {
            targetedTransform = target.transform;
            agent.destination = new Vector3(targetedTransform.position.x, transform.position.y, targetedTransform.position.z);
            //print("Chasing");
        }
        else
        {
            attackTimer = 0;
            state = State.PATROL;
        }
    }

    private void Attack()
    {
        if (Vector3.Distance(target.transform.position, transform.position) > attackRange)
        {
            agent.isStopped = false;
            state = State.CHASE;
        }

        targetedTransform = target.transform;
        agent.destination = new Vector3(targetedTransform.position.x, transform.position.y, targetedTransform.position.z);

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0)
        {
            attackTimer = attackCooldown;
            playerScript.TakeDamage(attackDamage);
        }
    }

    private Vector3 getRandomTargetPosition()
    {
        Vector3 randomPos = transform.position;
        bool validPos = false;
        float minimumDistance = 4f;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            if (hit.collider.tag == "Walkable" && hit.point.y < .5f)
            {
                Room current = null;
                List<Room> rooms = gen.getAllRooms();
                List<Room> levelOneRoomChoices = new List<Room>();
                List<Room> levelTwoRoomChoices = new List<Room>();
                List<Room> allRoomChoices = new List<Room>();

                foreach (Room r in rooms)
                {
                    if (r.roomRef.transform.position == hit.collider.transform.position)
                    {
                        current = r;
                        break;
                    }
                }

                if (current == null)
                {
                    //Not on the map
                    return transform.position;
                }

                //Get neighbors of current
                levelOneRoomChoices = current.getUniqueNeighboringRooms();
                levelOneRoomChoices.Add(current);

                //Get neighbors of neighbors of current
                foreach (Room roomL1 in levelOneRoomChoices)
                {
                    levelTwoRoomChoices.Add(roomL1);
                    foreach (Room roomL2 in roomL1.getUniqueNeighboringRooms())
                    {
                        levelTwoRoomChoices.Add(roomL2);
                    }
                }

                //Remove duplicates
                allRoomChoices = levelTwoRoomChoices.Distinct().ToList();

                //Select a random room
                Room randomRoom = allRoomChoices[Random.Range(0, allRoomChoices.Count)];

                //Select a random position inside the random room
                randomPos = randomRoom.getRandomPosition();

                if (Vector3.Distance(randomPos, transform.position) >= minimumDistance)
                {
                    validPos = true;
                }
                else
                {
                    validPos = false;
                }
            }
        }

        if (validPos)
        {
            return randomPos;
        }
        else
        {
            return transform.position;
        }
    }
}