using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerNetworkMover : Photon.MonoBehaviour {
	//use events and delegates to know when someone has died, Secure with events
	public delegate void Respawn(float time);
	public event Respawn RespawnMe;
	public delegate void SendMessage(string message);
	public event SendMessage SendNetworkMessage;

	Vector3 realPosition;
	Quaternion realRotation;
    //Net Optimization variables 
    private Vector3 realVelocity = Vector3.zero;
    private float lerpRate = 4.5f;
    private float normalLerpRate = 4.5f;
    private float fasterLerpRate = 8;
    private Vector3 lastPos;
    private List<Vector3> syncPosList = new List<Vector3>();
    private bool useHistoricalLerping = true;
    private float closeEnough = 0.1f;

	float smoothing = 10f;
	float health = 100f;
	public string playerName; 
	public int pickedUpAmmo = 0;
	public GameObject[] weaponMains;
    public GameObject[] weaponParts;
	GameObject[] bodys;
    public GameObject firstPersonChar;
    Quaternion firstPersonCharRealRotation;
	//public GameObject injuryEffect;
	Animator injuryAnim;

	bool aim = false;
	bool sprint = false;
	bool crouch = false;
	bool onGround = true;
	float Forward = 0f;
	float turn = 0f;
	bool initialLoad = true;
	public bool muzzleFlashToggle = false;

    public Weapon currentWeapon;
    UnitySampleAssets.Characters.FirstPerson.FirstPersonController FPC;
   
	[SerializeField] private AudioClip _jumpSound; // the sound played when character leaves the ground.
	[SerializeField] private AudioClip _landSound; // the sound played when character touches back on ground.
	[SerializeField] private AudioClip[] _footstepSounds;
	[SerializeField] private AudioClip[] fleshImpactSounds;
	[SerializeField] private AudioClip[] flyByShots;
	private CharacterController _characterController;
	private float _stepCycle = 0f;
	private float _nextStep = 0f;
	//CharacterController cc;
	AudioSource audio0;
	AudioSource audio1;
	AudioSource audio2;
	AudioSource[] aSources;
	public Animator anim;
	[SerializeField] Animator animMainCam;
	[SerializeField] Animator animEthan;
	[SerializeField] Animator animHitBoxes;
	PhotonView photonView;
	private PlayerShooting playerShooting;
    public GameObject[] WeaponArray;
	public Light muzzleLightFlash;
	public GameObject[] muzzleLightFlashGO;
	//ColliderControl colidcon;
	[SerializeField] bool alive;
	GameManager GMan;
	NetworkManager NM;
    public UnityStandardAssets.ImageEffects.CameraMotionBlur cameraMotionBlur;
    public UnityStandardAssets.ImageEffects.DepthOfField depthOfField;
    bool CMBEnabled = false;
    bool DOFEnabled = false;
    bool myAim = false;
    public int currentWeaponIndex;
    public bool updateWeaponIndex;
    private Vector3 velocity = Vector3.zero;
	//AudioSource audio;
	// Use this for initialization
	void Start () {

		PhotonNetwork.sendRate = 30;
		PhotonNetwork.sendRateOnSerialize = 15;
        currentWeaponIndex = 0;
		alive = true; 
		photonView = GetComponent<PhotonView> ();
		//Disables my Character Controller interstingly enough. That way I can only enable it for the clien'ts player.  
		transform.GetComponent<Collider>().enabled = false;
		//Use this to get current player this script is attached too
		aSources = GetComponents<AudioSource> (); 
		audio0 = aSources [0];
		audio1 = aSources [1];
		audio2 = aSources [2];
        lerpRate = normalLerpRate;
		anim = GetComponentInChildren<Animator> ();
		//animEthan = transform.Find("char_ethan").GetComponent<Animator> ();
		injuryAnim = GameObject.FindGameObjectWithTag ("InjuryEffect").GetComponent<Animator>();
		GMan = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
		NM = GameObject.FindGameObjectWithTag ("NetworkManager").GetComponent<NetworkManager>();
		playerShooting = GetComponentInChildren<PlayerShooting> ();

		/*muzzleLightFlashGO = GameObject.FindGameObjectsWithTag("LightFlash");
		
		//To assign the each players own muzzle flash toggle and not someone elses. 
		for(int i = 0; i < muzzleLightFlashGO.Length; i++){
			//If the weapon we find has the same ID as the player its attached to, set the tag to layer 10
			if(muzzleLightFlashGO[i].GetComponentInParent<PlayerShooting>().gameObject.GetInstanceID() == playerShooting.gameObject.GetInstanceID() ){
				muzzleLightFlash = muzzleLightFlashGO[i].GetComponent<Light>();
				//muzzleLightFlash.enabled = false;
				//muzzleFlashToggle = false;
				
			}
		}*/
		//If its my player, not anothers
		Debug.Log ("<color=red>Joined Room </color>" + PhotonNetwork.player.name + " " + photonView.isMine);
		if (photonView.isMine) {
            
			//Enable CC so we can control character. 
			transform.GetComponent<Collider>().enabled = true;
			//Use for Sound toggle
			_characterController = GetComponent<CharacterController>();
	
			playerName = PhotonNetwork.player.name;
			//enable each script just for the player being spawned and not the others
			GetComponent<Rigidbody>().useGravity = true; 
			GetComponent<UnitySampleAssets.Characters.FirstPerson.FirstPersonController>().enabled = true;
			playerShooting.enabled = true;
			foreach(Camera cam in GetComponentsInChildren<Camera>()){
				cam.enabled = true; 
			}
			foreach(AudioListener AL in GetComponentsInChildren<AudioListener>()){
				AL.enabled = true; 
			}

			//So that we can see our own weapons on the second camera and not other player weapons through walls
            WeaponSetup();
     
			//Change Body Part Collider Layers from default to body just for the player's own game not all players so that they can collide with others
			//We need to ignore colliders cause we layer a lot of them together
			//So we find all body parts and if it matches our own we are good to change it so it can be ignored.
			for(int i = 0; i < GameObject.FindGameObjectsWithTag("Body").Length; i++){

				if(GameObject.FindGameObjectsWithTag("Body")[i].GetComponentInParent<PlayerNetworkMover>().gameObject.GetInstanceID() == gameObject.GetInstanceID() ){
					GameObject.FindGameObjectsWithTag("Body")[i].layer = 12;
				}
			}
			//Now for the head
			for(int i = 0; i < GameObject.FindGameObjectsWithTag("Head").Length; i++){
				
				if(GameObject.FindGameObjectsWithTag("Head")[i].GetComponentInParent<PlayerNetworkMover>().gameObject.GetInstanceID() == gameObject.GetInstanceID() ){
					GameObject.FindGameObjectsWithTag("Head")[i].layer = 12;
				}
			}
			//If player is ours have CC ignore body parts
			Physics.IgnoreLayerCollision(0,12, true);
            InitPostProcessingEffects();
		}
		else{
            //All other players
			StartCoroutine ("UpdateData");
		}
		/*if(muzzleLightFlash != null){
			muzzleFlashToggle = true;
			if(muzzleFlashToggle){
				Debug.Log ("muzzleFlash True");
				muzzleLightFlash.enabled = true;
			}
			else{
				muzzleLightFlash.enabled = false;
			}
		}*/
	}

    public void InitPostProcessingEffects() {

        cameraMotionBlur = this.GetComponentInChildren<UnityStandardAssets.ImageEffects.CameraMotionBlur>();
        depthOfField = this.GetComponentInChildren<UnityStandardAssets.ImageEffects.DepthOfField>();
        cameraMotionBlur.enabled = false;
        depthOfField.enabled = false;
        //Enable CameraMotionBlur if Setting is allowed to be on
        if (PlayerPrefs.GetInt("CMB") == 1)
        {
            CMBEnabled = true;
        }
        else
        {
            CMBEnabled = false;
            cameraMotionBlur.enabled = false;
        }

        if (PlayerPrefs.GetInt("DOF") == 1)
        {
            DOFEnabled = true;
        }
        else
        {
            DOFEnabled = false;
            depthOfField.enabled = false;
        }
    }

    Vector3 SuperSmoothLerp(Vector3 pastPosition, Vector3 pastTargetPosition, Vector3 targetPosition, float time, float speed)
    {
        Vector3 f = pastPosition - pastTargetPosition + (targetPosition - pastTargetPosition) / (speed * time);
        return targetPosition - (targetPosition - pastTargetPosition) / (speed * time) + f * Mathf.Exp(-speed * time);
    }

    void OrdinaryLerping() {

        transform.position = Vector3.Lerp(transform.position, realPosition, Time.deltaTime * lerpRate);

    }

    void HistoricalLerping() {

        if (syncPosList.Count > 0) {

            transform.position = Vector3.Lerp(transform.position, syncPosList[0], Time.deltaTime * lerpRate);
            if (Vector3.Distance(transform.position, syncPosList[0]) < closeEnough) { 
            
                //no longer trying to move toward position remove form list
                syncPosList.RemoveAt(0);
            }
            //Means we are moving fast and should thus lerp faster
            if (syncPosList.Count > 10)
            {
                lerpRate = fasterLerpRate;
            }
            else
            {
                lerpRate = normalLerpRate;
            }

            Debug.Log(syncPosList.Count.ToString());
        }
    }

    void SyncPositionValues(Vector3 latestPos) {

        realPosition = latestPos;
        syncPosList.Add(realPosition);

    }

	IEnumerator UpdateData(){

		if (initialLoad) {

			//jiiter correction incomplete, could check position if accurate to .0001 don't move them 
			initialLoad = false; 
			transform.position = realPosition; 
			transform.rotation = realRotation;
            velocity = realVelocity;
            firstPersonChar.transform.rotation = firstPersonCharRealRotation;
            SelectWeapon(currentWeaponIndex);     
		}//This is where we set all other player prefab settings that isn't the local player's settings
		while (true) {
			//smooths every frame for the dummy players from where they are to where they should be, prevents jitter lose some accuracy I suppose
			//Ideally we want the movement to be equal to the amount of time since the last update
            // (Don't need to update pos when not moving so check using threshold) 
           // if (Vector3.Distance(transform.position, lastPos) > threshold) { 
            //transform.position = Vector3.SmoothDamp(transform.position, realPosition,  ,0.06f);// + _characterController.velocity * Time.deltaTime;
            //transform.position = SuperSmoothLerp(transform.position, lastPos, realPosition, Time.deltaTime, 0.5f);
            //SyncPositionValues(realPosition);
            syncPosList.Add(realPosition);
            if (useHistoricalLerping){

                HistoricalLerping();
            }
            else {

                OrdinaryLerping();
            }
            
            transform.rotation = Quaternion.Lerp(transform.rotation, realRotation, Time.deltaTime * lerpRate);//Time.deltaTime * smoothing
            firstPersonChar.transform.rotation = Quaternion.Lerp(firstPersonChar.transform.rotation, firstPersonCharRealRotation, Time.deltaTime * lerpRate);
			//Sync Animation States by tell the respctive animators what the bools we have synced over network are
            if (anim != null) { 
			    anim.SetBool ("Aim", aim); 
			    anim.SetBool ("Sprint", sprint); 
			    animEthan.SetBool("OnGround",onGround);
			    animEthan.SetFloat("Forward",Forward);
			    animEthan.SetFloat("Turn",turn);
			    //Be sure to set the values here for all crouching aspects
			    animEthan.SetBool ("Crouch",crouch);
			    animMainCam.SetBool("Crouch",crouch);
			    animHitBoxes.SetBool("Crouch",crouch);
            }
			//muzzleFlashToggle = playerShooting.shooting;
			//playerShooting.shooting = muzzleFlashToggle;
            //Need to update weapon index from FPC to the network, so we don't spam we do it just once everytime a player switches weapons in their FPC
            if (updateWeaponIndex) {
                SelectWeapon(currentWeaponIndex);
                updateWeaponIndex = false;
            }
			yield return null; 
		}
	}

	//Serilize Data Across the network, we want everyone to know where they are
	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){
        //Send to everyone else a local players variables to be synced and recieved by all other players on network
		if (stream.isWriting) {
			//send to clients where we are
			stream.SendNext(playerName);
            stream.SendNext(_characterController.velocity);
			stream.SendNext(transform.position);
			stream.SendNext(transform.rotation);
            stream.SendNext(firstPersonChar.transform.rotation);
			stream.SendNext(health);
            stream.SendNext(currentWeaponIndex);
            stream.SendNext(updateWeaponIndex);
			//Sync Animation States
			stream.SendNext(anim.GetBool ("Aim"));
			stream.SendNext(anim.GetBool ("Sprint"));
			stream.SendNext(animEthan.GetBool("OnGround"));
			stream.SendNext(animEthan.GetFloat("Forward"));
			stream.SendNext(animEthan.GetFloat("Turn"));
			stream.SendNext(animEthan.GetBool("Crouch"));
			//stream.SendNext(muzzleFlashToggle);

			stream.SendNext(alive);
		
		}
		else{
			//Get from clients where they are
			//Write in the same order we read, if not writing we are reading. 
			playerName = (string)stream.ReceiveNext();
            realVelocity = (Vector3)stream.ReceiveNext();
			realPosition = (Vector3)stream.ReceiveNext();
			realRotation = (Quaternion)stream.ReceiveNext();
            firstPersonCharRealRotation = (Quaternion)stream.ReceiveNext();
			health = (float)stream.ReceiveNext();
            currentWeaponIndex = (int)stream.ReceiveNext();
            updateWeaponIndex = (bool)stream.ReceiveNext();
			//Sync Animation States
			aim = (bool)stream.ReceiveNext();
			sprint = (bool)stream.ReceiveNext();
			onGround = (bool)stream.ReceiveNext();
			Forward = (float)stream.ReceiveNext();
			turn = (float)stream.ReceiveNext();
			crouch = (bool)stream.ReceiveNext();
		    //muzzleFlashToggle = (bool)stream.ReceiveNext();

			alive = (bool)stream.ReceiveNext();
			
		}																										
	}

    public void WeaponSetup() {

        //Photon view gets called before its intialized (in FPC) on spawn so we just make a check here to prevent trying to use it before its intialized
        if (photonView != null) {
            //If we are the owner of the weapons change the tags, other wise leave the tags for networked players to deafult so we can't see their weapons
            if (photonView.isMine) { 

                //So that we can see our own weapons on the second camera and not other player weapons through walls
                weaponMains = GameObject.FindGameObjectsWithTag("WeaponMain");
                for (int i = 0; i < weaponMains.Length; i++)
                {
                    //If the weapon we find has the same ID as the player its attached to, set the tag to layer 10
                    if (weaponMains[i].GetComponentInParent<PlayerNetworkMover>().gameObject.GetInstanceID() == gameObject.GetInstanceID())
                    {
                        weaponMains[i].layer = 10;
                        Debug.Log(" <color=red> Current Weapon Name </color> " + " " + weaponMains[i].name);
                        // WeaponList.Add(weapons[i].GetComponent<Weapon>());
                    }
                }

                weaponParts = GameObject.FindGameObjectsWithTag("WeaponParts");
                for (int i = 0; i < weaponParts.Length; i++)
                {
                    //If the weapon we find has the same ID as the player its attached to, set the tag to layer 10
                    if (weaponParts[i].GetComponentInParent<PlayerNetworkMover>().gameObject.GetInstanceID() == gameObject.GetInstanceID())
                    {
                        weaponParts[i].layer = 10;
                    }
                }
            }
        }
    }

    public void SelectWeapon(int index){

        //Update all networked players weapons, FPC updates localy, this updates for the network 
        for (int i = 0; i < WeaponArray.Length; i++)
        {
            if (i == index)
            {             
                WeaponArray[i].SetActive(true);
                currentWeapon = WeaponArray[i].GetComponent<Weapon>();
                
            }
            else { WeaponArray[i].SetActive(false); }
        }
        anim = currentWeapon.GetComponent<Animator>();
        Debug.Log("<color=red> Weapon Name </color>" + currentWeapon.name);
    }

	public float GetHealth(){

		return health;
	}

	[PunRPC]
	public void GetShot(float damage, PhotonPlayer enemy){
		//Take Damage and check for death
		health -= damage;
		//Play a random Impact Sounds
		audio2.clip = fleshImpactSounds [Random.Range (0, 6)];
		audio2.Play ();
		Debug.Log ("<color=green>Got Shot with </color>" + damage + " damage. Is alive: " + alive + " PhotonView is" + photonView.isMine);
		//Once dead
		if(health <=0 && alive){
			
			alive = false; 
			Debug.Log ("<color=blue>Checking Health</color>" + health + " Photon State " + photonView.isMine + " Player Name " + PhotonNetwork.player.name);
			if (photonView.isMine) {

				//Only owner can remove themselves
				Debug.Log ("<color=red>Death</color>");
				if(SendNetworkMessage != null){
					if(damage < 100f)
						SendNetworkMessage(enemy.name + " owned " + PhotonNetwork.player.name + ".");
					if(damage == 100f)
						SendNetworkMessage(enemy.name + " headshot " + PhotonNetwork.player.name + "!");
						
				}
				//Subscribe to the event so that when a player dies 3 sec later respawn
				if(RespawnMe != null)
					RespawnMe(3f);

				//Create deaths equal to stored hashtable deaths, increment, Set
				int totalDeaths = (int)PhotonNetwork.player.customProperties["D"];
				totalDeaths ++;
				ExitGames.Client.Photon.Hashtable setPlayerDeaths = new ExitGames.Client.Photon.Hashtable() {{"D", totalDeaths}};
				PhotonNetwork.player.SetCustomProperties(setPlayerDeaths);

				//Increment Kill Count for the enemy player
                if(!(enemy.name == "DroneAI")){
				int totalKIlls = (int)enemy.customProperties["K"];
				totalKIlls ++;
				ExitGames.Client.Photon.Hashtable setPlayerKills = new ExitGames.Client.Photon.Hashtable() {{"K", totalKIlls}};
				Debug.Log ("<color=red>KillCounter Called at </color>" + totalKIlls);
				enemy.SetCustomProperties(setPlayerKills);

				//If we reach the kill limit by checking rooms custom property and if this is enabled
                if (totalKIlls == (int)(PhotonNetwork.room.customProperties["KL"]) && (int)(PhotonNetwork.room.customProperties["TKL"]) == 1)
                {
					//Display Win Screen
					NM.DisplayWinPrompt(enemy.name);
				}
               
				//Write Kills and Deaths to File On Death 
				//System.IO.File.AppendAllText (@"C:\Users\Public\PlayerStats.txt", "\n" + "KDR on Death: " + ((int)(PhotonNetwork.player.customProperties["K"])).ToString() + ":" + totalDeaths.ToString());
				//Write amount of ammo picked up so far until death. 
                //System.IO.File.AppendAllText(@"C:\Users\Public\PlayerStats.txt", "\n" + "Total Amount of ammmo picked up so far: " + pickedUpAmmo.ToString());
                }
				//Spawn ammo on death
				PhotonNetwork.Instantiate("Ammo_AK47",transform.position - new Vector3 (0,0.9f,0), Quaternion.Euler(1.5f,149f,95f),0);
				//Finally destroy the game Object.
				PhotonNetwork.Destroy(gameObject);
					
			}
		}
		//Play Hit Effect Animation for player getting hit. Without isMine would play for everyone. 
		if (photonView.isMine) {
			injuryAnim.SetBool ("Hit", true);
			StartCoroutine( WaitForAnimation (1.2f));
		}
	}


	[PunRPC]
	public void ShootingSound(bool firing){
		
		if (firing) {
			
				audio1.clip = currentWeapon.Fire;
				audio1.Play();
		}
	}

	[PunRPC]
	public void ReloadingSound(){

		audio1.clip = currentWeapon.Reload;
		audio1.Play();

	}

	[PunRPC]
	public void OutOfAmmo(){
	
		audio1.clip = currentWeapon.Empty;
		audio1.Play();

	}
	
	[PunRPC]
	public void PlayLandingSound()
	{
		GetComponent<AudioSource>().clip = _landSound;
		GetComponent<AudioSource>().Play();
		_nextStep = _stepCycle + .5f;
		
	}
	
	[PunRPC]
	public void PlayJumpSound()
	{
		GetComponent<AudioSource>().clip = _jumpSound;
		GetComponent<AudioSource>().Play();
	}

	
	[PunRPC]
	public void PlayFootStepAudio()
	{
		//if (!_characterController.isGrounded) return;
		// pick & play a random footstep sound from the array,
		// excluding sound at index 0
		int n = Random.Range(1, _footstepSounds.Length);
		GetComponent<AudioSource>().clip = _footstepSounds[n];
		GetComponent<AudioSource>().PlayOneShot(GetComponent<AudioSource>().clip);
		// move picked sound to index 0 so it's not picked next time
		_footstepSounds[n] = _footstepSounds[0];
		_footstepSounds[0] = GetComponent<AudioSource>().clip;
	}

	[PunRPC]
	public void PlayFlyByShots(){

        if (flyByShots != null) { 

		audio2.clip = flyByShots [Random.Range (0, 8)];
		audio2.Play ();
        }
	}

	[PunRPC]
	public void ToggleMuzzleFlash(bool toggle, int ID){
		/*GameObject[] muzzleLightFlashGO = GameObject.FindGameObjectsWithTag("LightFlash");
		
		//To assign the each players own muzzle flash toggle and not someone elses. 
		for(int i = 0; i < muzzleLightFlashGO.Length; i++){
			//If the weapon we find has the same ID as the player its attached to, set the tag to layer 10
			if(muzzleLightFlashGO[i].GetComponentInParent<PlayerNetworkMover>().GetComponent<PhotonView>().networkView.viewID == ID){
				muzzleLightFlashGO[i].GetComponent<Light>().enabled = true;
				yield return new WaitForSeconds(0.05f);
				muzzleLightFlashGO[i].GetComponent<Light>().enabled = false;
				//muzzleLightFlash.enabled = false;
				//muzzleFlashToggle = false;
				
			}
		}*/
		//NM.AddMessage("Toggled: " + toggle);
		//playerShooting.muzzleFlashToggle = toggle;
		if(muzzleFlashToggle)
			playerShooting.muzzleFlash.Emit(1);
		//yield return new WaitForSeconds(0.05f);
		//playerShooting.muzzleFlashToggle = !toggle;
	}

	// Update is called once per frame
	void Update () {
	
		if(Input.GetKeyDown(KeyCode.K)){

			//health = 0;
			gameObject.GetComponent<PhotonView>().RPC ("GetShot", PhotonTargets.All, 25f, PhotonNetwork.player);
			Debug.Log (health);
		}

        myAim = Input.GetButton("Fire2");
        //We use these settings for aiming only, if they are on and we are aiming use them
        if (depthOfField != null)
        {
            if (myAim && DOFEnabled)
            {
                depthOfField.enabled = true;
            }
            else if (depthOfField.enabled = true && !myAim) { depthOfField.enabled = false; }
        }

        if (cameraMotionBlur != null)
        {
            if (myAim && CMBEnabled)
            {
                cameraMotionBlur.enabled = true;
            }
            else if (cameraMotionBlur.enabled = true && !myAim) { cameraMotionBlur.enabled = false; }
        }

	}

	private IEnumerator WaitForAnimation ( float waitTime )
	{
		yield return new WaitForSeconds(waitTime);
		injuryAnim.SetBool ("Hit", false);
		//injuryEffect.SetActive (false);
	}

	void OnDestroy() {
		// Unsubscribe, so this object can be collected by the garbage collection
		RespawnMe -=  NM.StartSpawnProcess;
		SendNetworkMessage -= NM.AddMessage;

	}

	void OnTriggerEnter(Collider other) {
		
		if(other.gameObject.tag == "PickUp"){

			if(other.GetComponent<Ammo>().canGet){
				Debug.Log ("<color=red>Picked Up Ammo</color>");
                //Don't excede clip size limit
                if (playerShooting.WeaponStats.clipAmount < playerShooting.WeaponStats.clipAmountMax)
                    playerShooting.WeaponStats.clipAmount++;
				pickedUpAmmo++;
                //Update teh ammo shown
				playerShooting.UpdateAmmoText();
				other.GetComponent<Ammo>().OnPickUp();
			}
		}
	}

}
