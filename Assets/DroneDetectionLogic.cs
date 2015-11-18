using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DroneDetectionLogic : MonoBehaviour {

    PhotonPlayer DroneAI = new PhotonPlayer(false, -1, "DroneAI");
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
    public bool droneDisabled = false;
    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerStay(Collider col)
    {//Use stay for shooting
     // if (PhotonNetwork.isMasterClient) { 
        if (col.gameObject.tag == "Player")
        {
            Debug.Log("<color=Red>Target Locked</color>");
            //playerDetected = true;
            //agent.Stop();
            if (timeStampShootingRate <= Time.time && !droneDisabled)
            {
                timeStampShootingRate = Time.time + ShootingRate;
                ShootTarget(col.gameObject);
            }
        }
        else
        {
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

    void ShootTarget(GameObject target)
    {

        RaycastHit[] hits;
        bool flyByTrue = true;
        //Offset was to help avoid the Collision Detection Sphere but I just moved the sphere instead. 
        Vector3 DroneBodyOffset = transform.forward * 0.5f;
        //to know how many walls we've hit limit bullet decal spray
        int decalHitCount = 0;

        hits = RaycastAllNonConvex(transform.position + DroneBodyOffset, (target.transform.position - (transform.position)));

        Debug.DrawRay(transform.position + DroneBodyOffset, (target.transform.position - transform.position), Color.red);
        Debug.Log(target.transform.position);

        for (int i = 0; i < hits.Length; i++)
        {

            if (hits != null)
            {

                Quaternion hitRotation = Quaternion.FromToRotation(Vector3.up, hits[i].normal);
                Debug.Log("<color=red>Tag of Hit Object</color> " + hits[i].transform.tag + " " + hits[i].transform.name + " " + hits.Length);
                //If they hit the FlyByRange first or second then we know we can check to see if they hit the player
                if (i == 1 || i == 2 && (hits[0].collider.tag == "FlyByRange" || hits[1].collider.tag == "FlyByRange"))
                    if (hits[i].collider.tag == "Body")
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
