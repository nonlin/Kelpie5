using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple script that uses a property to sync a start time for a multiplayer game.
/// </summary>
/// <remarks>
/// When entering a room, the first player will store the synchronized timestamp. 
/// You can't set the room's synchronized time in CreateRoom, because the clock on the Master Server
/// and those on the Game Servers are not in sync. We use many servers and each has it's own timer.
/// 
/// Everyone else will join the room and check the property to calculate how much time passed since start.
/// You can start a new round whenever you like.
/// 
/// Based on this, you should be able to implement a synchronized timer for turns between players.
/// </remarks>
public class InRoomRoundTimer : MonoBehaviour
{
    public int SecondsPerTurn = 5;                  // time per round/turn
    public double StartTime;                        // this should could also be a private. i just like to see this in inspector
    public Rect TextPos = new Rect(0,80,150,300);   // default gui position. inspector overrides this!
	bool doOnce = false;
	bool playerWait = true;
    private bool startRoundWhenTimeIsSynced;        // used in an edge-case when we wanted to set a start time but don't know it yet.
    private const string StartTimeKey = "st";       // the name of our "start time" custom property.
	[SerializeField] GameObject gameTimerText;
	public Text gameTimeMinText;
	public Text gameTimeSecText;
	public float minutes;
	public double seconds;
	[SerializeField] float timeLimit = 10;
	NetworkManager NM; 
	[SerializeField] InputField timeLimitInput;

	void Start(){

		gameTimerText.SetActive (false);
		NM = GameObject.FindGameObjectWithTag ("NetworkManager").GetComponent<NetworkManager>();

	}

    private void StartRoundNow()
    {

        // in some cases, when you enter a room, the server time is not available immediately.
        // time should be 0.0f but to make sure we detect it correctly, check for a very low value.
        if (PhotonNetwork.time < 0.0001f)
        {
            // we can only start the round when the time is available. let's check that in Update()
            startRoundWhenTimeIsSynced = true;
            return;
        }
        startRoundWhenTimeIsSynced = false;

		ExitGames.Client.Photon.Hashtable setTimeLimit = new Hashtable(); 
		setTimeLimit["TL"] = (int)timeLimit;
		PhotonNetwork.room.SetCustomProperties(setTimeLimit);

        ExitGames.Client.Photon.Hashtable startTimeProp = new Hashtable();  // only use ExitGames.Client.Photon.Hashtable for Photon
        startTimeProp[StartTimeKey] = PhotonNetwork.time;
        PhotonNetwork.room.SetCustomProperties(startTimeProp);              // implement OnPhotonCustomRoomPropertiesChanged(Hashtable propertiesThatChanged) to get this change everywhere

	}

    
    /// <summary>Called by PUN when this client entered a room (no matter if joined or created).</summary>
    public void OnJoinedRoom()
    {
		gameTimerText.SetActive (true);
		//Only want master client to set the initial values. 
		if (PhotonNetwork.isMasterClient && !doOnce)
		{	doOnce = true;
            this.StartRoundNow();
        }
        else
        {
            // as the creator of the room sets the start time after entering the room, we may enter a room that has no timer started yet
            Debug.Log("StartTime already set: " + PhotonNetwork.room.customProperties.ContainsKey(StartTimeKey));
        }
    }

    /// <summary>Called by PUN when new properties for the room were set (by any client in the room).</summary>
    public void OnPhotonCustomRoomPropertiesChanged(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(StartTimeKey))
        {
            StartTime = (double)propertiesThatChanged[StartTimeKey];
        }
    }

    /// <remarks>
    /// In theory, the client which created the room might crash/close before it sets the start time.
    /// Just to make extremely sure this never happens, a new masterClient will check if it has to
    /// start a new round.
    /// </remarks>
    public void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        if (!PhotonNetwork.room.customProperties.ContainsKey(StartTimeKey))
        {
            Debug.Log("The new master starts a new round, cause we didn't start yet.");
            this.StartRoundNow();
        }
    }


    void Update()
    {


		if (startRoundWhenTimeIsSynced)
        {
            this.StartRoundNow();   // the "time is known" check is done inside the method.
        }
		//Start round time over once we have 2 or more players. 
		//Buggy causes the round to restart iif there is a new masterClint I think(Doesn't do just once)
		if(PhotonNetwork.playerList.Count () > 1 && playerWait && PhotonNetwork.isMasterClient){
			playerWait = false;
			//this.StartRoundNow(); 
		}
		DisplayTimer();
    }

    public void OnGUI()
    {
        // alternatively to doing this calculation here:
        // calculate these values in Update() and make them publicly available to all other scripts
        double elapsedTime = (PhotonNetwork.time - StartTime);
        double remainingTime = SecondsPerTurn - (elapsedTime % SecondsPerTurn);
        int turn = (int)(elapsedTime / SecondsPerTurn);


        // simple gui for output
       /* GUILayout.BeginArea(TextPos);
        GUILayout.Label(string.Format("elapsed: {0:0.000}", elapsedTime));
        GUILayout.Label(string.Format("remaining: {0:0.000}", remainingTime));
        GUILayout.Label(string.Format("turn: {0:0}", turn));
        if (GUILayout.Button("new round"))
        {
            this.StartRoundNow();
        }
        GUILayout.EndArea();*/
    }

	public void DisplayTimer(){

		//Only care about time related stuff like updating and win checking if there are two or more players
		if(PhotonNetwork.playerList.Count() > 1){

			double elapsedTime = (PhotonNetwork.time - StartTime);

			minutes = (Mathf.Floor((float)elapsedTime / 60));
			seconds = (elapsedTime % 60);
			//If we've enabled the timer then we can set the time to the GUI
			if(gameTimerText.activeSelf){

				gameTimeMinText = GameObject.FindGameObjectWithTag("GameTimer").GetComponent < Text > ();
				gameTimeSecText = GameObject.FindGameObjectWithTag("GameSec").GetComponent < Text > ();
				gameTimeMinText.text = minutes.ToString("00");
				gameTimeSecText.text = seconds.ToString("00");

				//If we run out time declare the person with the highest score as winner, no logic for ties atm.
				if(minutes == (float)((int)(PhotonNetwork.room.customProperties["TL"]))){

					SortedDictionary<string, int> playerKills = new SortedDictionary<string, int>();
					foreach (PhotonPlayer p in PhotonNetwork.playerList) {

						playerKills.Add (p.name, (int)p.customProperties["K"]);
					}
					//Order Dictionary by value with highest value first, then get the first and display the key (AKA Player Name)
					NM.DisplayWinPrompt(playerKills.OrderByDescending(d => d.Value).First().Key);
				}
			}
		}
	}

	public void SetTimeLimit(){

		bool result = float.TryParse(timeLimitInput.text, out timeLimit);
		if(result){

			Debug.Log ("TimeLimit Accepted: " + timeLimit);
		}
		else{
			Debug.Log ("Error TimeLimit Can't be parsed");
			timeLimit = 10;
		}
	}
}
