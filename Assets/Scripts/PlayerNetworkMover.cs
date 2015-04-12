using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerNetworkMover : Photon.MonoBehaviour {
	//use events and delegates to know when someone has died, Secure with events
	public delegate void Respawn(float time);
	public event Respawn RespawnMe;
	public delegate void SendMessage(string message);
	public event SendMessage SendNetworkMessage;

	Vector3 realPosition;
	Quaternion realRotation;

	float smoothing = 10f;
	float health = 100f;
	public string playerName; 

	GameObject[] weapons;
	GameObject[] bodys;
	//public GameObject injuryEffect;
	Animator injuryAnim;

	bool aim = false;
	bool sprint = false;
	bool crouch = false;
	bool onGround = true;
	float Forward = 0f;
	float turn = 0f;
	bool initialLoad = true;

	public AudioClip Fire;
	public AudioClip Reload;
	public AudioClip Empty;
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
	[SerializeField] Animator anim;
	[SerializeField] Animator animMainCam;
	[SerializeField] Animator animEthan;
	[SerializeField] Animator animHitBoxes;
	PhotonView photonView;
	PlayerShooting playerShooting;

	//ColliderControl colidcon;
	[SerializeField] bool alive;
	NetworkManager NM;
	//AudioSource audio;
	// Use this for initialization
	void Start () {

		PhotonNetwork.sendRate = 30;
		PhotonNetwork.sendRateOnSerialize = 15;

		alive = true; 
		photonView = GetComponent<PhotonView> ();
		//Disables my Character Controller interstingly enough. That way I can only enable it for the clien'ts player.  
		transform.GetComponent<Collider>().enabled = false;
		//Use this to get current player this script is attached too
		aSources = GetComponents<AudioSource> (); 
		audio0 = aSources [0];
		audio1 = aSources [1];
		audio2 = aSources [2];

		//anim = GetComponentInChildren<Animator> ();
		//animEthan = transform.Find("char_ethan").GetComponent<Animator> ();
		injuryAnim = GameObject.FindGameObjectWithTag ("InjuryEffect").GetComponent<Animator>();

		NM = GameObject.FindGameObjectWithTag ("NetworkManager").GetComponent<NetworkManager>();
		playerShooting = GetComponentInChildren<PlayerShooting> ();
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
			weapons = GameObject.FindGameObjectsWithTag("AK");
			for(int i = 0; i < weapons.Length; i++){
				//If the weapon we find has the same ID as the player its attached to, set the tag to layer 10
				if(weapons[i].GetComponentInParent<PlayerNetworkMover>().gameObject.GetInstanceID() == gameObject.GetInstanceID() )
					weapons[i].layer = 10; 
			}
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

		}
		else{
		
			StartCoroutine ("UpdateData");
		}

	}

	IEnumerator UpdateData(){

		if (initialLoad) {
			//jiiter correction incomplete, could check position if accurate to .0001 don't move them 
			initialLoad = false; 
			transform.position = realPosition; 
			transform.rotation = realRotation; 
		}
		while (true) {
			//smooths every frame for the dummy players from where they are to where they should be, prevents jitter lose some accuracy I suppose
			//Ideally we want the movement to be equal to the amount of time since the last update
			transform.position = Vector3.Lerp(transform.position, realPosition, 0.1f);// + _characterController.velocity * Time.deltaTime;
			transform.rotation = Quaternion.Lerp(transform.rotation, realRotation, 0.1f);//Time.deltaTime * smoothing
			//Sync Animation States
			anim.SetBool ("Aim", aim); 
			anim.SetBool ("Sprint", sprint); 
			animEthan.SetBool("OnGround",onGround);
			animEthan.SetFloat("Forward",Forward);
			animEthan.SetFloat("Turn",turn);
			//Be sure to set the values here for all crouching aspects
			animEthan.SetBool ("Crouch",crouch);
			animMainCam.SetBool("Crouch",crouch);
			animHitBoxes.SetBool("Crouch",crouch);
			yield return null; 
		}
	}
	//Serilize Data Across the network, we want everyone to know where they are
	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){

		if (stream.isWriting) {
			//send to clients where we are
			stream.SendNext(playerName);
			stream.SendNext(transform.position);
			stream.SendNext(transform.rotation);
			stream.SendNext(health); 
			//Sync Animation States
			stream.SendNext(anim.GetBool ("Aim"));
			stream.SendNext(anim.GetBool ("Sprint"));
			stream.SendNext(animEthan.GetBool("OnGround"));
			stream.SendNext(animEthan.GetFloat("Forward"));
			stream.SendNext(animEthan.GetFloat("Turn"));
			stream.SendNext(animEthan.GetBool("Crouch"));
			//
			stream.SendNext(alive);
		
		}
		else{
			//Get from clients where they are
			//Write in teh same order we read, if not writing we are reading. 
			playerName = (string)stream.ReceiveNext();
			realPosition = (Vector3)stream.ReceiveNext();
			realRotation = (Quaternion)stream.ReceiveNext();
			health = (float)stream.ReceiveNext();
			//Sync Animation States
			aim = (bool)stream.ReceiveNext();
			sprint = (bool)stream.ReceiveNext();
			onGround = (bool)stream.ReceiveNext();
			Forward = (float)stream.ReceiveNext();
			turn = (float)stream.ReceiveNext();
			crouch = (bool)stream.ReceiveNext();
			//
			alive = (bool)stream.ReceiveNext();
			
		}
																												
	}

	public float GetHealth(){

		return health;
	}

	[RPC]
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
				int totalKIlls = (int)enemy.customProperties["K"];
				totalKIlls ++;
				ExitGames.Client.Photon.Hashtable setPlayerKills = new ExitGames.Client.Photon.Hashtable() {{"K", totalKIlls}};
				Debug.Log ("<color=red>KillCounter Called at </color>" + totalKIlls);
				enemy.SetCustomProperties(setPlayerKills);

				//Write Kills and Deaths to File On Death 
				System.IO.File.AppendAllText (@"C:\Users\Public\PlayerStats.txt", "\n" + "KDR on Death: " + totalKIlls.ToString() + ":" + totalDeaths.ToString());

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


	[RPC]
	public void ShootingSound(bool firing){
		
		if (firing) {
			
				audio1.clip = Fire;
				audio1.Play();
			}
		}

	[RPC]
	public void ReloadingSound(){
		
		//if (firing) {
			
			//isShooting = true; 
			audio1.clip = Reload;
			audio1.Play();
		//}
	}

	[RPC]
	public void OutOfAmmo(){
		
		//if (firing) {
		
		//isShooting = true; 
		audio1.clip = Empty;
		audio1.Play();
		//}
	}


	[RPC]
	public void PlayLandingSound()
	{
		GetComponent<AudioSource>().clip = _landSound;
		GetComponent<AudioSource>().Play();
		_nextStep = _stepCycle + .5f;
		
	}
	
	[RPC]
	public void PlayJumpSound()
	{
		GetComponent<AudioSource>().clip = _jumpSound;
		GetComponent<AudioSource>().Play();
	}

	
	[RPC]
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

	[RPC]
	public void PlayFlyByShots(){

		audio2.clip = flyByShots [Random.Range (0, 8)];
		audio2.Play ();
	}

	// Update is called once per frame
	void Update () {
	
		if(Input.GetKeyDown(KeyCode.K)){

			//health = 0;
			gameObject.GetComponent<PhotonView>().RPC ("GetShot", PhotonTargets.All, 25f, PhotonNetwork.player);
			Debug.Log (health);
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
				playerShooting.clipAmount++;
				other.GetComponent<Ammo>().OnPickUp();
			}
		}
	}
}
