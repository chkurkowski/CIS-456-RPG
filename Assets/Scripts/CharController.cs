using UnityEngine;
using UnityEngine.AI;

public class CharController : MonoBehaviour
{

    //Objects in Scene
    public Camera cam;
    public GameObject atkOrigin;
    public GameObject laser;
    public GameObject magicMissile;

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
        navMeshAgent = GetComponent<NavMeshAgent>();
        laserInst = laser.GetComponent<Laser>();
        laserLineRendInst = laser.GetComponent<LineRenderer>();
        laserBoxCollInst = laser.GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        ClickToMove();
        MagicMissileAttack();
        LaserAttack();
    }

    private void ClickToMove()
    {
        if (Input.GetButtonDown("Fire1") && !(Input.GetKey(KeyCode.LeftShift)))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                navMeshAgent.destination = hit.point;
                navMeshAgent.isStopped = false;
            }
        }
    }

    private void MagicMissileAttack()
    {
        missleNextFire -= Time.deltaTime;
        missleNextFire = Mathf.Clamp(missleNextFire, 0, missleNextFire);

        if (Input.GetButton("Fire1") && Input.GetKey(KeyCode.LeftShift) && missleNextFire <= 0f)
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

    private void LaserAttack()
    {
        if(Input.GetButton("Fire2"))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit; 
            if (Physics.Raycast(ray, out hit, 100))
            {
                navMeshAgent.isStopped = true;
                laserLineRendInst.enabled = true;
                laserBoxCollInst.enabled = true;
                this.transform.LookAt(new Vector3(hit.point.x, this.transform.position.y, hit.point.z));

                float dist = Vector3.Distance(atkOrigin.transform.position, hit.collider.transform.position);

                if (dist >= laserInst.getRange() || hit.collider.gameObject.tag != "Enemy")
                {
                    laserInst.setTarget(null);
                    return;
                }

                laserInst.setTarget(hit.collider.gameObject);
            }
        }
        else
        {
            laserLineRendInst.enabled = false;
            laserBoxCollInst.enabled = false;
            laserInst.setTarget(null);
        }
    }
}