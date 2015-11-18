using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DroneNavigationAI : Photon.MonoBehaviour {

    Animator droneAnimator;
    Rigidbody physics;
    NavMeshAgent agent;
    GameObject[] wayPoints;
    PhotonView photonView;
    Vector3 realPosition;
    Quaternion realRotation;
    private float realAgentVeloctiyX;
    private float realAgentVeloctiyY;
    public AudioClip FanBlowing;
    public AudioClip FanSwitchingOff;
    bool initialLoad = true;
    //Weather we've reached the destination or not
    bool destination1 = false;
    bool destination2 = false;
    bool destination3 = false;
    //Paths for Drone To Take
    Vector3 destination1Loc = new Vector3(-23f, 0f, 29f);
    Vector3 destination2Loc = new Vector3(-11.7f, 0f, 17.3f);
    Vector3 destination3Loc =new Vector3(-11.6f, 0f, 42f);
    //Set Custom PhotonPlayer for Drone since it isn't really a player connecting to be automatically added to PhotonPlayerList
    PhotonPlayer DroneAI = new PhotonPlayer(false,-1,"DroneAI");
    bool playerDetected = false;
    Animator anim;
    public DroneDetectionLogic DroneDetection;
    public bool droneDisabled = false;
    public float health = 100f;
    // Use this for initialization
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        droneAnimator = gameObject.GetComponent<Animator>();
        physics = gameObject.GetComponent<Rigidbody>();
        //wayPoints = GameObject.FindGameObjectsWithTag("WayPoints");
        agent = GetComponent<NavMeshAgent>();
        //CameraDetection = GameObject.FindGameObjectWithTag("DroneDetectionRadius").GetComponent<SphereCollider>();
        if (photonView.isMine)
        {
            agent.enabled = true;
        }
        else
        {
            //All other players
            StartCoroutine("UpdateData");
        }
        //agent.destination = new Vector3(-11.7f,0f,17.3f);
    }

    // Update is called once per frame
    void Update()
    {

        //Route to patrol done only on Master Client
        if (PhotonNetwork.isMasterClient)
        {
            if (!droneDisabled) {

                PatrolNavigation();
                droneAnimator.SetFloat("VelocityX", agent.velocity.x);
                droneAnimator.SetFloat("VelocityZ", agent.velocity.y);
            }

        }
        else
        {

        }
    }

    IEnumerator UpdateData()
    {

        if (initialLoad)
        {

            //jiiter correction incomplete, could check position if accurate to .0001 don't move them 
            initialLoad = false;
            transform.position = realPosition;
            transform.rotation = realRotation;

        }//This is where we set all other player prefab settings that isn't the local player's settings
        while (true)
        {
            //smooths every frame for the dummy players from where they are to where they should be, prevents jitter lose some accuracy I suppose
            //Ideally we want the movement to be equal to the amount of time since the last update
            transform.position = Vector3.Lerp(transform.position, realPosition, 0.1f);// + _characterController.velocity * Time.deltaTime;
            transform.rotation = Quaternion.Lerp(transform.rotation, realRotation, 0.1f);//Time.deltaTime * smoothing

            //Sync Animation
            AnimateMovements();
            yield return null;
        }
    }

    //Serilize Data Across the network, we want everyone to know where they are
    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //Send to everyone else a local players variables to be synced and recieved by all other players on network
        if (stream.isWriting)
        {
            //send to clients where we are
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(agent.velocity.x);
            stream.SendNext(agent.velocity.y);
            //stream.SendNext(playerDetected);

        }
        else
        {
            //Get from clients where they are
            //Write in the same order we read, if not writing we are reading. 
            realPosition = (Vector3)stream.ReceiveNext();
            realRotation = (Quaternion)stream.ReceiveNext();
            realAgentVeloctiyX = (float)stream.ReceiveNext();
            realAgentVeloctiyY = (float)stream.ReceiveNext();
            //playerDetected = (bool)stream.ReceiveNext();
        }
    }

    void AnimateMovements()
    {
        droneAnimator.SetFloat("VelocityX", realAgentVeloctiyX);
        droneAnimator.SetFloat("VelocityZ", realAgentVeloctiyY);
    }

    void PatrolNavigation()
    {

        //We look to see if we've reached a destination by checking x and z location (ignore height (y)) and if so move to next target location

        if (agent.velocity.z == 0 && !destination2)
        {
            agent.SetDestination(destination2Loc);
            Debug.Log("Going to Destination 2");
        }

        if (transform.position.x == destination2Loc.x && transform.position.z == destination2Loc.z && !destination3)
        {
            destination2 = true;
            Debug.Log("Arravived at Destination 2");
            agent.SetDestination(destination3Loc);
        }

        if (transform.position.x == destination3Loc.x && transform.position.z == destination3Loc.z && destination2 && !destination3)
        {
            destination3 = true;
            Debug.Log("Arravived at Destination 3, going to 1");
            agent.SetDestination(destination1Loc);
        }
        if (transform.position.x == destination1Loc.x && transform.position.z == destination1Loc.z)
        {
            destination2 = false;
            destination3 = false;
        }

    }

    void OnTriggerStay(Collider col)
    {

    }

    [PunRPC]
    public void GetShot(float damage, PhotonPlayer enemy)
    {
        //Take Damage and check for death
        health -= damage;

        //Debug.Log("<color=green>Got Shot with </color>" + damage + " damage. " + " PhotonView is" + photonView.isMine);
        //Once dead
        if (health <= 0)
        {

            //Debug.Log("<color=blue>Checking Health</color>" + health + " Photon State " + photonView.isMine + " Player Name " + PhotonNetwork.player.name);
            if (PhotonNetwork.isMasterClient)
            {

                //Only owner can remove themselves
                Debug.Log("<color=red>Death</color>");
                droneDisabled = true;
                DroneDetection.droneDisabled = true;
                gameObject.GetComponent<AudioSource>().PlayOneShot(FanSwitchingOff);
                //droneDetection.droneDisabled = true;
                StartCoroutine(EnableDrone(10.0f));
                //PhotonNetwork.Destroy(gameObject);

            }
        }

    }

    IEnumerator EnableDrone(float waitTime)
    {
        Debug.Log("Callilng Enable Drone");
        yield return new WaitForSeconds(waitTime);
        Debug.Log("Callilng Enable Drone Time passed");
        droneDisabled = false;
        DroneDetection.droneDisabled = false;
        //droneDetection.droneDisabled = false;
        health = 100f;
    }

    public float GetHealth()
    {

        return health;
    }
}
