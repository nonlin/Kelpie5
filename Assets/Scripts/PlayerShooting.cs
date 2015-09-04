using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class PlayerShooting: MonoBehaviour {
	
	public ParticleSystem muzzleFlash;
	public GameObject[] muzzleLightFlashGO;
	public Light muzzleLightFlash;
	public bool muzzleFlashToggle;
	Animator anim;
	bool doOnce = false;
	public GameObject impactPrefab;
	public GameObject bloodSplatPrefab;
	GameObject currentSplat;
	private float timeStamp;
//	List < RaycastAllSort > raycastSort = new List < RaycastAllSort > ();
	//To show name when looked at
	GUIManager guiMan;
	Transform enemyTransform;
	bool showEnemyName = false;
	string enemyName;
	//For Impact Holes and Impact Effects
	static List < GameObject > impacts = new List < GameObject > ();
	List < GameObject > .Enumerator e;
	GameObject CurrentImpact;
	//GameObject[] impacts;
	NetworkManager NM;
	int currentImpact = 0;
	int maxImpacts = 20;
	public bool shooting = false;
	float damage = 16f;
	int clipSize = 30;
	public int clipAmount = 3;
	bool reloading = false;
	public Text ammoText;
	public Transform target;
	bool enumDeclared = false;
	int bulletsFired = 0;
    int MaxRange = 10000;
	// Use this for initialization
	void Start() {
		
		guiMan = GameObject.Find("NetworkManager").GetComponent < GUIManager > ();
		NM = GameObject.Find("NetworkManager").GetComponent < NetworkManager > ();
		muzzleLightFlashGO = GameObject.FindGameObjectsWithTag("LightFlash");

		//To assign the each players own muzzle flash toggle and not someone elses. 
		for(int i = 0; i < muzzleLightFlashGO.Length; i++){
			//If the weapon we find has the same ID as the player its attached to, set the tag to layer 10
			if(muzzleLightFlashGO[i].GetComponentInParent<PlayerShooting>().gameObject.GetInstanceID() == gameObject.GetInstanceID() ){
				muzzleLightFlash = muzzleLightFlashGO[i].GetComponent<Light>();
				//muzzleLightFlash.enabled = false;
				//muzzleFlashToggle = false;

			}
		}

		ammoText = GameObject.FindGameObjectWithTag("Ammo").GetComponent < Text > ();
		anim = GetComponentInChildren < Animator > ();
		timeStamp = 0;
		//Intilize Current Ammo then call it every time the ammo changes when shooting to update
		UpdateAmmoText();
		
	}
	
	// Update is called once per frame
	void Update() {

		if (Input.GetButton("Fire1") && !Input.GetKey(KeyCode.LeftShift) && timeStamp <= Time.time && clipSize > 0) {

			doOnce = false;
			//Play flash particle for a second
			muzzleFlash.Emit(1);
	
			//Then enable muzzle flash light
			muzzleLightFlash.enabled = true;

			clipSize--;//To display the current clipsize
			bulletsFired++;//For Stats Purposes 
			UpdateAmmoText();
			anim.SetBool("Fire", true);
			shooting = true;
			//NM.player.GetComponent<PhotonView>().RPC("ToggleMuzzleFlash",PhotonTargets.All,true,0);
			//NM.player.GetComponent<PlayerNetworkMover>().muzzleFlashToggle = true;
			timeStamp = Time.time + 0.1f;
			NM.player.GetComponent < PhotonView > ().RPC("ShootingSound", PhotonTargets.All, true);
		} else {
			if(!doOnce){
				doOnce = true;
				anim.SetBool("Fire", false);
				muzzleLightFlash.enabled = false;
			}

		}
		
		if (Input.GetKeyDown(KeyCode.R) && !Input.GetButton("Fire1") && clipSize < 30 && clipAmount != 0 && !reloading) {
			
			Debug.Log("Reloading");
			reloading = true;
			anim.SetBool("Reloading", true);
			NM.player.GetComponent < PhotonView > ().RPC("ReloadingSound", PhotonTargets.All);
			StartCoroutine(Reload());
			//Write Kills and Deaths to File On Death 
			System.IO.File.AppendAllText (@"C:\Users\Public\PlayerSootingStats.txt", "\n" + "Bullets Fired: " + (bulletsFired).ToString() + " On Reload");
		}
		if (clipSize <= 0 && Input.GetButtonDown("Fire1")) {
			//StartCoroutine(EmptyGun());
			NM.player.GetComponent < PhotonView > ().RPC("OutOfAmmo", PhotonTargets.All);
		}

		Debug.DrawRay(transform.position, transform.forward * 10, Color.red);
		if (shooting ) {
			
			shooting = false;
			
			RaycastHit[] hits;
			bool flyByTrue = true;
			int decalHitCount = 0;//to know how many walls we've hit limit bullet decal spray
			hits = RaycastAllNonConvex(transform.position, transform.forward * MaxRange);
			// Physics.RaycastAll(transform.position, transform.forward);//.OrderBy(h=>h.distance).ToArray();
		
			Debug.Log("Origin: " + transform.position + ", direction: " + transform.forward);
            Debug.DrawRay(transform.position, transform.forward * MaxRange, Color.red);
			
			foreach(RaycastHit hit in hits) {
				
				Debug.Log("<color=blue>Hit Order: </color>" + hit.collider.name + " " + hit.distance);
			}
			
			for (int i = 0; i < hits.Length; i++) {

				if(hits != null){

					Quaternion hitRotation = Quaternion.FromToRotation(Vector3.up, hits[i].normal);
					Debug.Log("<color=red>Tag of Hit Object</color> " + hits[i].transform.tag + " " + hits[i].transform.name + " " + hits.Length);
					//If they hit the FlyByRange first or second then we know we can check to see if they hit the player
					if (i == 1 || i == 2 && (hits[0].collider.tag == "FlyByRange" || hits[1].collider.tag == "FlyByRange")) if (hits[i].collider.tag == "Body") {
						flyByTrue = false;
						//Play hitmarker sound
						gameObject.GetComponent < AudioSource > ().Play();

						//If we hit the head colliderr change the damage
						if (hits[i].collider.name == "Head") {
							
							Debug.Log("<color=red>HeadShot!</color> " + hits[i].collider.name);
							damage = 100f;
						}
						//If we hit the body change the damage
						if (hits[i].collider.name == "Torso") {
							
							damage = 16f;
						}
						//Damage Through Walls
						if(decalHitCount >= 1 && hits[i].collider.name != "Head"){

							damage = 10f;
						}

						Debug.Log("<color=red>Collider Tag</color> " + hits[i].collider.tag);
						Instantiate(bloodSplatPrefab, hits[i].point, hitRotation);
						//Tell all we shot a player and call the RPC function GetShot passing damage runs on person shooting
						hits[i].transform.GetComponent < PhotonView > ().RPC("GetShot", PhotonTargets.All, damage, PhotonNetwork.player);
						Debug.Log("<color=red>Target Health</color> " + hits[i].transform.GetComponent < PlayerNetworkMover > ().GetHealth());
					}

					//Every time we add a decal add to the count, once count limit is reach we won't place any more decals
					if(decalHitCount <= 1){
						//Initial creation of bullet decals, once max limit of decals are made we have our pool
						//We only make them when it hasn't hit a player body part or the FlyRange Collider
						if (hits[i].collider.tag != "FlyByRange" && hits[i].collider.tag != "Body" && impacts.Count < maxImpacts) {
				
							CurrentImpact = (GameObject) Instantiate(impactPrefab, hits[i].point, hitRotation);
							impacts.Add(CurrentImpact);
							CurrentImpact.GetComponent < ParticleSystem > ().Emit(1);
							decalHitCount++;
						}
						
						//Just need to set the Enum once after its set, we can't call it again until we are ready to reset again to loop back.
						if (impacts.Count >= maxImpacts && !enumDeclared) {
							enumDeclared = true;
							e = impacts.GetEnumerator();
						}
						//But now we still need know when to iterate through the list of impacts
						if (impacts.Count >= maxImpacts && hits[i].collider.tag != "FlyByRange" && hits[i].collider.tag != "Body") {
							
							decalHitCount++;
							if (e.MoveNext()) {
								//This is why we bothered to use enum. Now we don't have to create and destroy, instead we interate through the list
								//to move the already created impact to a new impact point. 
								CurrentImpact = e.Current;
								CurrentImpact.transform.position = hits[i].point;
								CurrentImpact.transform.rotation = hitRotation;
								//and play the smoke effect again
								CurrentImpact.GetComponent<ParticleSystem>().Play();
							} else {
								//Reset
								e = impacts.GetEnumerator();
							}
							
						}
					}
					//If fly by range is the first hit or second hit we shall play the sound(second hit implies hit through wall)
					if (hits[0].collider.tag == "FlyByRange" && flyByTrue) {
						//if(temphit.collider.tag == "FlyByRange" ){
						Debug.Log("<color=green>FlyRange Sound</color>");
						hits[0].transform.GetComponent < PhotonView > ().RPC("PlayFlyByShots", PhotonTargets.Others);
					}
					//Second Hit on FlyByRange
					if(hits.Length > 1){//If there is more than 1 element in hits we can check at location 1
						if (hits[1].collider.tag == "FlyByRange" && flyByTrue) {
							//if(temphit.collider.tag == "FlyByRange" ){
							Debug.Log("<color=green>FlyRange Sound</color>");
							hits[1].transform.GetComponent < PhotonView > ().RPC("PlayFlyByShots", PhotonTargets.Others);
						}
					}
				}
			}
		}
	}
	
	IEnumerator Reload() {
		
		yield
			return new WaitForSeconds(2.0f);
		clipSize = 30;
		clipAmount--;
		ammoText.text = clipAmount.ToString() + "/" + clipSize.ToString();
		reloading = false;
		anim.SetBool("Reloading", false);

	}
	
	IEnumerator EmptyGun() {
		
		yield
			return new WaitForSeconds(1.0f);
		NM.player.GetComponent < PhotonView > ().RPC("OutOfAmmo", PhotonTargets.All);
	}
	
	void FixedUpdate() {
		//For Physic related stuff like adding force to rigidbody apprently 
		
	}
	
	void OnDrawGizmosSelected() {
		
		if (target != null) {
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(transform.position, target.position);
		}
	}
	
	void OnGUI() {
		
		if (showEnemyName) guiMan.EnemyName(enemyTransform, enemyName);
	}
	//Use this to get multiple hits from one giant mesh, like the nonConvex Mesh Maps where the map is entire mesh of its own 
	public static RaycastHit[] RaycastAllNonConvex(Vector3 origin, Vector3 destination) {
		
		List < RaycastHit > hits = new List < RaycastHit > ();
		Vector3 delta = destination - origin;
		Vector3 dir = destination.normalized;
        int RayCastPenetratinLimit = 9;
		while (true) {
			RaycastHit hit;
			float dist = delta.magnitude; //get raycast distance
            if (Physics.Raycast(origin, dir, out hit, dist) && hits.Count < RayCastPenetratinLimit)
            {
				origin = hit.point + dir * 0.01f; //rem that collision is inclusive
				hits.Add(hit); //Add this point to the list
			} else {
				break; //Done for good, break out of loop
			}
		}
		return hits.OrderBy(h => delta.magnitude).ToArray();
	}

	public void UpdateAmmoText(){

		ammoText.text = clipAmount.ToString() + "/" + clipSize.ToString();
	}
	
}
