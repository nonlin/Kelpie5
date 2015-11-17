using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DroneNavigationAI : MonoBehaviour {

    Animator droneAnimator;
    Rigidbody physics;
    NavMeshAgent agent;
    GameObject[] wayPoints;
    SphereCollider CameraDetection;
    PhotonView photonView;
    Vector3 realPosition;
    Quaternion realRotation;
    public float realAgentVeloctiyX;
    public float realAgentVeloctiyY;
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
    float timeStampShootingRate = 0;
    float ShootingRate = 0.50f;
    public ParticleSystem muzzleFlash;
    public GameObject[] muzzleLightFlashGO;
    public Light muzzleLightFlash;
    public bool muzzleFlashToggle;
    Animator anim;
    bool doOnce = false;
    public GameObject impactPrefab;
    public GameObject bloodSplatPrefab;
    GameObject currentSplat;
    Transform enemyTransform;
    //For Impact Holes and Impact Effects
    static List<GameObject> impacts = new List<GameObject>();
    List<GameObject>.Enumerator e;
    GameObject CurrentImpact;
    int currentImpact = 0;
    int maxImpacts = 50;
    public bool shooting = false;
    float damage = 16f;
    bool reloading = false;
    public Transform target;
    bool enumDeclared = false;
    int bulletsFired = 0;

	// Use this for initialization
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        droneAnimator = gameObject.GetComponent<Animator>();
        physics = gameObject.GetComponent<Rigidbody>();
        //wayPoints = GameObject.FindGameObjectsWithTag("WayPoints");
        agent = GetComponent<NavMeshAgent>();
        CameraDetection = GameObject.FindGameObjectWithTag("DroneDetectionRadius").GetComponent<SphereCollider>();
        if (photonView.isMine){
            agent.enabled = true;
        }
        else {
            //All other players
            StartCoroutine("UpdateData");
        }
        //agent.destination = new Vector3(-11.7f,0f,17.3f);
    }
	
	// Update is called once per frame
	void Update () {

        //Route to patrol done only on Master Client
        if (PhotonNetwork.isMasterClient)
        {

            PatrolNavigation();
            droneAnimator.SetFloat("VelocityX", agent.velocity.x);
            droneAnimator.SetFloat("VelocityZ", agent.velocity.y);
        }
        else {

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
            stream.SendNext(playerDetected);
            
        }
        else
        {
            //Get from clients where they are
            //Write in the same order we read, if not writing we are reading. 
            realPosition = (Vector3)stream.ReceiveNext();
            realRotation = (Quaternion)stream.ReceiveNext();
            realAgentVeloctiyX = (float)stream.ReceiveNext();
            realAgentVeloctiyY = (float)stream.ReceiveNext();
            playerDetected = (bool)stream.ReceiveNext();
        }
    }

    void AnimateMovements()
    {
        droneAnimator.SetFloat("VelocityX", realAgentVeloctiyX);
        droneAnimator.SetFloat("VelocityZ", realAgentVeloctiyY);
    }

    void PatrolNavigation() {

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
    {//Use stay for shooting
       // if (PhotonNetwork.isMasterClient) { 
            if (col.gameObject.tag == "Player")
            {
                Debug.Log("<color=Red>Target Locked</color>");
                //playerDetected = true;
                //agent.Stop();
                if (timeStampShootingRate <= Time.time)
                {
                    timeStampShootingRate = Time.time + ShootingRate;
                    ShootTarget(col.gameObject);
                }
            }
            else {
                //playerDetected = false;
                //If no player detected keep going
            
                 //agent.Resume(); 
            }
       // }
    }

    void OnTriggerEnter(Collider col)
    {//We use Enter and Exit so we don't spam = true or = false
       
        if (col.gameObject.tag == "Player")
        {
            Debug.Log("<color=Yellow>Detected Player</color>");

            playerDetected = true;
            //if (PhotonNetwork.isMasterClient)
            //agent.Stop();
        }    
    }

    void OnTriggerExit(Collider col)
    {

        if (col.gameObject.tag == "Player")
        {
            Debug.Log("<color=Green>Sights Lost</color>");
            playerDetected = false;
           // if (PhotonNetwork.isMasterClient)
            //agent.Resume();
        }
    }

    void ShootTarget(GameObject target) {

        RaycastHit[] hits;
        bool flyByTrue = true;
        //Offset was to help avoid the Collision Detection Sphere but I just moved the sphere instead. 
        Vector3 DroneBodyOffset = transform.forward * 0.5f;
        //to know how many walls we've hit limit bullet decal spray
        int decalHitCount = 0;

        hits = RaycastAllNonConvex(transform.position + DroneBodyOffset, (target.transform.position - (transform.position)));
        
        Debug.DrawRay(transform.position + DroneBodyOffset, (target.transform.position - transform.position ), Color.red);
        Debug.Log(target.transform.position);

        for (int i = 0; i < hits.Length; i++)
        {

            if (hits != null)
            {

                Quaternion hitRotation = Quaternion.FromToRotation(Vector3.up, hits[i].normal);
                Debug.Log("<color=red>Tag of Hit Object</color> " + hits[i].transform.tag + " " + hits[i].transform.name + " " + hits.Length);
                //If they hit the FlyByRange first or second then we know we can check to see if they hit the player
                if (i == 1 || i == 2 && (hits[0].collider.tag == "FlyByRange" || hits[1].collider.tag == "FlyByRange")) if (hits[i].collider.tag == "Body")
                    {
                        flyByTrue = false;
                        //Play hitmarker sound
                        gameObject.GetComponent<AudioSource>().Play();

                        //If we hit the head colliderr change the damage
                        if (hits[i].collider.name == "Head")
                        {

                            Debug.Log("<color=red>HeadShot!</color> " + hits[i].collider.name);
                            damage = 100f;
                        }
                        //If we hit the body change the damage
                        if (hits[i].collider.name == "Torso")
                        {

                            damage = 16f;
                        }
                        //Damage Through Walls
                        if (decalHitCount >= 1 && hits[i].collider.name != "Head")
                        {

                            damage = 10f;
                        }

                       // Debug.Log("<color=red>Collider Tag</color> " + hits[i].collider.tag);
                        Instantiate(bloodSplatPrefab, hits[i].point, hitRotation);
                        //Tell all we shot a player and call the RPC function GetShot passing damage runs on person shooting
                        hits[i].transform.GetComponent<PhotonView>().RPC("GetShot", PhotonTargets.All, damage, DroneAI);
                        //Might do Custom GetShot for Drones
                        Debug.Log("<color=red>Target Health</color> " + hits[i].transform.GetComponent<PlayerNetworkMover>().GetHealth());
                    }

                //Every time we add a decal add to the count, once count limit is reach we won't place any more decals
                if (decalHitCount <= 1)
                {
                    //Initial creation of bullet decals, once max limit of decals are made we have our pool
                    //We only make them when it hasn't hit a player body part or the FlyRange Collider
                    if (hits[i].collider.tag != "FlyByRange" && hits[i].collider.tag != "Body" && hits[i].collider.tag != "Player" && impacts.Count < maxImpacts)
                    {

                        CurrentImpact = (GameObject)Instantiate(impactPrefab, hits[i].point, hitRotation);
                        impacts.Add(CurrentImpact);
                        CurrentImpact.GetComponent<ParticleSystem>().Emit(1);
                        decalHitCount++;
                    }

                    //Just need to set the Enum once after its set, we can't call it again until we are ready to reset again to loop back.
                    if (impacts.Count >= maxImpacts && !enumDeclared)
                    {
                        enumDeclared = true;
                        e = impacts.GetEnumerator();
                    }
                    //But now we still need know when to iterate through the list of impacts
                    if (impacts.Count >= maxImpacts && hits[i].collider.tag != "FlyByRange" && hits[i].collider.tag != "Body" && hits[i].collider.tag != "Player")
                    {

                        decalHitCount++;
                        if (e.MoveNext())
                        {
                            //This is why we bothered to use enum. Now we don't have to create and destroy, instead we interate through the list
                            //to move the already created impact to a new impact point. 
                            CurrentImpact = e.Current;
                            CurrentImpact.transform.position = hits[i].point;
                            CurrentImpact.transform.rotation = hitRotation;
                            //and play the smoke effect again
                            CurrentImpact.GetComponent<ParticleSystem>().Play();
                        }
                        else
                        {
                            //Reset
                            e = impacts.GetEnumerator();
                        }

                    }
                }
                //If fly by range is the first hit or second hit we shall play the sound(second hit implies hit through wall)
                if (hits[0].collider.tag == "FlyByRange" && flyByTrue)
                {
                    //if(temphit.collider.tag == "FlyByRange" ){
                    Debug.Log("<color=green>FlyRange Sound</color>");
                    hits[0].transform.GetComponent<PhotonView>().RPC("PlayFlyByShots", PhotonTargets.Others);
                }
                //Second Hit on FlyByRange
                if (hits.Length > 1)
                {//If there is more than 1 element in hits we can check at location 1
                    if (hits[1].collider.tag == "FlyByRange" && flyByTrue)
                    {
                        //if(temphit.collider.tag == "FlyByRange" ){
                        Debug.Log("<color=green>FlyRange Sound</color>");
                        hits[1].transform.GetComponent<PhotonView>().RPC("PlayFlyByShots", PhotonTargets.Others);
                    }
                }
            }
        }
    }

    public static RaycastHit[] RaycastAllNonConvex(Vector3 origin, Vector3 destination)
    {

        List<RaycastHit> hits = new List<RaycastHit>();
        Vector3 delta = destination - origin;
        Vector3 dir = destination.normalized;
        int RayCastPenetrationLimit = 6;

        while (true)
        {
            RaycastHit hit;
            float dist = delta.magnitude; //get raycast distance
            //Set hit penetration limit to try and reduce lag
            if (Physics.Raycast(origin, dir, out hit, dist) && hits.Count < RayCastPenetrationLimit)
            {
                origin = hit.point + dir * 0.01f; //rem that collision is inclusive
                hits.Add(hit); //Add this point to the list
            }
            else
            {
                break; //Done for good, break out of loop
            }
        }
        return hits.OrderBy(h => delta.magnitude).ToArray();
    }
}
