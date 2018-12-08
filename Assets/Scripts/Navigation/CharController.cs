using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class CharController : MonoBehaviour
{

    //Objects in Scene
    public Camera cam;
    public GameObject atkOrigin;
    public GameObject laser;
    public GameObject magicMissile;


    private RoomGeneration roomGen;
    public bool canMove;

    //Instances of Laser
    private Laser laserInst;
    private LineRenderer laserLineRendInst;
    private BoxCollider laserBoxCollInst;

    //access the attack script
    private NavMeshAgent navMeshAgent;
    private Transform targetedTransform;

    //use this if you need to shoot on this character.
    private Ray shootRay;
    private RaycastHit shotHit;
    private bool ranged;
    private float missleNextFire = 0f; //Used for missle cooldown

    // Use this for initialization
    void Awake()
    {
        canMove = true;
        navMeshAgent = GetComponent<NavMeshAgent>();
        laserInst = laser.GetComponent<Laser>();
        laserLineRendInst = laser.GetComponent<LineRenderer>();
        laserBoxCollInst = laser.GetComponent<BoxCollider>();
        roomGen = FindObjectOfType<RoomGeneration>();
    }

    // Update is called once per frame
    void Update()
    {
        discoverRoom();
        ClickToMove();
        ClickToPickUp();
        MagicMissileAttack();
        LaserAttack();
    }

    private void ClickToMove()
    {
        if (!canMove || EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Input.GetKey(KeyCode.Mouse0) && !(Input.GetKey(KeyCode.LeftShift)))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                ItemPickup pickup = hit.collider.gameObject.GetComponent<ItemPickup>();
                if(pickup != null)
                {
                    pickup.focused = true;
                }

                if(hit.point.y <= .8)
                {
                    navMeshAgent.destination = hit.point;
                    navMeshAgent.isStopped = false;
                }
            }
        }
    }

    private void ClickToPickUp()
    {
        if (Input.GetKey(KeyCode.Mouse0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                ItemPickup pickup = hit.collider.gameObject.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    pickup.focused = true;
                }
            }
        }
    }

    private void MagicMissileAttack()
    {
        missleNextFire -= Time.deltaTime;
        missleNextFire = Mathf.Clamp(missleNextFire, 0, missleNextFire);

        //&& Input.GetKey(KeyCode.LeftShift)
        if (Input.GetMouseButtonDown(1) && missleNextFire <= 0f)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                if (hit.collider.gameObject.tag != "Enemy") //Makes it so magic missles can only hit enemies (looks weird when you click the floor)
                {
                    return;
                }
                navMeshAgent.isStopped = true;
                this.transform.LookAt(new Vector3(hit.point.x, this.transform.position.y, hit.point.z));
                GameObject mm = Instantiate(magicMissile, atkOrigin.transform.position, Quaternion.identity);
                Missle mmInst = mm.GetComponent<Missle>();
                mmInst.setTarget(hit.collider.gameObject);
                missleNextFire = (1 / mmInst.getAttackRate());
            }
        }
    }

    // Legacy Laser Code
    private void LaserAttack()
    {
        //if(Input.GetKey(KeyCode.Mouse1))
        //{
        //    Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        //    RaycastHit hit; 
        //    if (Physics.Raycast(ray, out hit, 100))
        //    {
        //        navMeshAgent.isStopped = true;
        //        laserLineRendInst.enabled = true;
        //        laserBoxCollInst.enabled = true;
        //        this.transform.LookAt(new Vector3(hit.point.x, this.transform.position.y, hit.point.z));

        //        float dist = Vector3.Distance(atkOrigin.transform.position, hit.collider.transform.position);

        //        if (dist >= laserInst.getRange() || hit.collider.gameObject.tag != "Enemy")
        //        {
        //            laserInst.setTarget(null);
        //            return;
        //        }

        //        laserInst.setTarget(hit.collider.gameObject);
        //    }
        //}
        //else
        //{
        //    laserLineRendInst.enabled = false;
        //    laserBoxCollInst.enabled = false;
        //    laserInst.setTarget(null);
        //}
    }

    private void discoverRoom()
    {
        RaycastHit hit;
        if (Physics.Raycast(atkOrigin.transform.position, Vector3.down, out hit, 100))
        {
            Vector2 centerOfRoom = new Vector2 (hit.transform.position.x / 10, hit.transform.position.z / 10);
            List<Room> rooms = roomGen.getAllRooms();

            foreach (Room room in rooms)
            {
                if (!room.discovered && room.center.Equals(centerOfRoom))
                {
                    room.discovered = true;
                    room.roomRef.layer = 9;
                }
            }
        }
    }
}