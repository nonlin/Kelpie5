using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;


public class NetworkManager : MonoBehaviour {

	[SerializeField] Text connectionText;
	[SerializeField] Transform[] spawnPoints;
	[SerializeField] Camera sceneCamera;

	[SerializeField] GameObject lobbyWindow;
	[SerializeField] GameObject mainMenu;
	[SerializeField] GameObject ammoText;
	[SerializeField] GameObject versionText;
	public GameObject optionsMenu;

	[SerializeField] InputField userName;
	[SerializeField] InputField roomName;
	[SerializeField] InputField roomList;
	[SerializeField] InputField messageWindow;

	[SerializeField] GameObject pausePanel;
	[SerializeField] Canvas mainCanvas;
	public GameObject WinPrompt;
	public Text WinPromptText;

	public GameObject player;
	Queue<string> messages;
	const int messageCount = 6;
	PhotonView photonView;
	public bool spawning = false; 
	bool paused = false;
	public bool joinedRoom = false;

	ExitGames.Client.Photon.Hashtable setPlayerKills = new ExitGames.Client.Photon.Hashtable() {{"K", 0}};
	ExitGames.Client.Photon.Hashtable setPlayerDeaths = new ExitGames.Client.Photon.Hashtable() {{"D", 0}};
	ExitGames.Client.Photon.Hashtable setPlayerPing = new ExitGames.Client.Photon.Hashtable() {{"P", 0}};

	//ExitGames.Client.Photon.Hashtable setPlayerHealth= new ExitGames.Client.Photon.Hashtable() {{"H", 100}};
	// Use this for initialization
	void Start () {
	
		photonView = GetComponent<PhotonView> ();//Initillze PhotonView
		messages = new Queue<string> (messageCount);//Specify Size for garbage Collection 
		PhotonNetwork.sendRate = 30;
		PhotonNetwork.sendRateOnSerialize = 15;
		PhotonNetwork.logLevel = PhotonLogLevel.Full;//So we see everything in output
		//connect to Server with setup info and sets game version
		PhotonNetwork.ConnectUsingSettings ("0.4");
		StartCoroutine("UpdateConnectionString");
		PhotonNetwork.player.SetCustomProperties(setPlayerKills);
		PhotonNetwork.player.SetCustomProperties(setPlayerDeaths);
		//Game Managing Stuff
		ammoText.SetActive (false);
		pausePanel.SetActive(false);
		WinPrompt.SetActive(false);
		Cursor.lockState = CursorLockMode.None;
		versionText.SetActive (true);
		optionsMenu.SetActive (false);
		GameObject.FindGameObjectWithTag ("LobbyCam").GetComponent<AudioListener> ().enabled = true;
		//Update Current Ping every second
		InvokeRepeating ("pingUpdate", 0, 1);
	}
	void Update(){

		//if(player != null && photonView.isMine){

		if (Input.GetKeyDown (KeyCode.Escape)) {

			Debug.Log ("Esc hit");
				if(!paused){

				//Time.timeScale = 0;//To freeze time
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
				//Disable Shooting and movement
				player.GetComponent<UnitySampleAssets.Characters.FirstPerson.FirstPersonController>().enabled = false;
				player.GetComponentInChildren<PlayerShooting>().enabled = false;
				pausePanel.SetActive(true);
				//mainCanvas.enabled = false;
				paused = !paused;
			}
			else{

				//Time.timeScale = 1;
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
				//Re-Enable
				player.GetComponent<UnitySampleAssets.Characters.FirstPerson.FirstPersonController>().enabled = true;
				player.GetComponentInChildren<PlayerShooting>().enabled = true;
				pausePanel.SetActive(false);
				//mainCanvas.enabled = true;
				paused = !paused;
			}
		}
		//}
		//RoundTimer();

	}

	void FixedUpdate(){

		//Constantly update players current ping
		//pingUpdate();
	}
	
	void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){


	}

	// Update is called once per frame
	IEnumerator UpdateConnectionString () {

		while(true){
			connectionText.text = PhotonNetwork.connectionStateDetailed.ToString ();
			yield return null; 
		}

	}

	void OnJoinedLobby(){

		lobbyWindow.SetActive (true);

	}

	void OnReceivedRoomListUpdate(){

		roomList.text = "";
		RoomInfo[] rooms = PhotonNetwork.GetRoomList ();
		foreach(RoomInfo room in rooms)
			roomList.text += room.name + "\n";
	}

	public void JoinRoom(){

		PhotonNetwork.player.name = userName.text;
		RoomOptions rm = new RoomOptions (){isVisible = true, maxPlayers = 10};
		//Create a room called lobby, with rm settings using default lobby type
		PhotonNetwork.JoinOrCreateRoom (roomName.text, rm, TypedLobby.Default);

	}

	void OnJoinedRoom(){

		joinedRoom = true;

		//Toggle On/Off Lobby GUI and InGame GUI
		lobbyWindow.SetActive (false);
		mainMenu.SetActive (false);
		optionsMenu.SetActive (false);
		ammoText.SetActive (true);
		versionText.SetActive (false);
		//
		StopCoroutine ("UpdateConnectionString");
		connectionText.text = "";
		StartSpawnProcess (0f);
		AddMessage ("Player " + PhotonNetwork.player.name + " has joined.");
		GameObject.FindGameObjectWithTag ("LobbyCam").GetComponent<AudioListener> ().enabled = false;

	}

	public void StartSpawnProcess (float respawnTime){
		//Show Lobby cam on death vs blank screen
		sceneCamera.enabled = true; 
		StartCoroutine ("SpawnPlayer", respawnTime);

		//Enable Lobby Sound
		GameObject.FindGameObjectWithTag ("LobbyCam").GetComponent<AudioListener> ().enabled = true;

	}

	IEnumerator SpawnPlayer(float respawnTime){

		yield return new WaitForSeconds(respawnTime);
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
		//Turn Lobby Listner off again
		GameObject.FindGameObjectWithTag ("LobbyCam").GetComponent<AudioListener> ().enabled = false;
		//Debug.Log ("<color=red>Joined Room </color>" + PhotonNetwork.player.name + " " + photonView.isMine);
		int index = Random.Range (0, spawnPoints.Length);
		//Create/Spawn player on network
		player = PhotonNetwork.Instantiate ("FPSPlayer", spawnPoints[index].position, spawnPoints[index].rotation, 0);
		//Once Player dies on network it will call Respawn me which will then call StartSpawn
		player.GetComponent<PlayerNetworkMover> ().RespawnMe += StartSpawnProcess;
		//player.GetComponent<PlayerNetworkMover> ().ScoreStats += onDeath;
		player.GetComponent<PlayerNetworkMover> ().SendNetworkMessage += AddMessage;//"Subscribe" to it

		sceneCamera.enabled = false;
		AddMessage ("Player " + PhotonNetwork.player.name + " has spawned.");
		//Add player that just spawned to player list. 
	
	}

	public void AddMessage(string message){

		photonView.RPC ("AddMessage_RPC", PhotonTargets.All, message);
	}

	[RPC]
	void AddMessage_RPC(string message){

		//Update queues for all clients
		messages.Enqueue (message);
		if (messages.Count > messageCount) { messages.Dequeue ();}
		//then write the messages to display on clients screen
		messageWindow.text = "";
		Debug.Log ("<color=red>Messages Count</color>" +messages.Count);
		foreach(string m in messages)
			messageWindow.text += m + "\n";
	}

	public void DisplayWinPrompt(string playerName){

		photonView.RPC ("DisplayWinPrompt_RPC", PhotonTargets.All, playerName);
	}

	[RPC]
	void DisplayWinPrompt_RPC(string playerName){

		//Display Win Screen
		WinPrompt.SetActive(true);
		WinPromptText.text = "Score Limit Reached! \n" + playerName + " Won";
		
	}

	void pingUpdate(){

		ExitGames.Client.Photon.Hashtable setPlayerPing = new ExitGames.Client.Photon.Hashtable() {{"P", PhotonNetwork.GetPing()}};
		PhotonNetwork.player.SetCustomProperties(setPlayerPing);
	}
	
	void OnApplicationQuit() {
		PhotonNetwork.Disconnect ();
	}

	public void QuitGame(){

		Application.Quit();
	}
	void OnPhotonPlayerDisconnected(PhotonPlayer playerDC){
		//Remove DC player from list of players

		AddMessage ("Player " + playerDC.name + " disconnected.");
	}
}
