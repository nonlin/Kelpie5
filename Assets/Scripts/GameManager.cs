using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {

	Animator optionsAnim;
	public Text xAxis_Text;
	public Text yAxis_Text;
	public Toggle smoothToggle;
	public Toggle vSync;
	NetworkManager NM;
	float xAx;
	float yAx;
	public int killLimit;
	bool doOnce = false;
	[SerializeField] InputField killLimitInput;
	// Use this for initialization
	void Start () {

		//Set Default Mouse Settings if there isn't one
		if(PlayerPrefs.GetFloat("xAxis") <= 0f || PlayerPrefs.GetFloat("yAxis") <= 0f){
			PlayerPrefs.SetFloat ("xAxis", 15f);
			PlayerPrefs.SetFloat ("yAxis", 15f);
			PlayerPrefs.SetInt("smooth", (false ? 1 : 0));
		}

		NM = GameObject.FindGameObjectWithTag ("NetworkManager").GetComponent<NetworkManager> ();
		yAxis_Text.text = PlayerPrefs.GetFloat("yAxis").ToString();
		xAxis_Text.text = PlayerPrefs.GetFloat("xAxis").ToString();
		smoothToggle.isOn = (PlayerPrefs.GetInt("smooth") != 0);
		vSync.isOn = (PlayerPrefs.GetInt("vSync") != 0);
		optionsAnim = GameObject.FindGameObjectWithTag ("OptionsPanel").GetComponent<Animator> ();
		killLimit = 10;

	}
	
	// Update is called once per frame
	void Update () {

		if (PhotonNetwork.isMasterClient && !doOnce){
			doOnce = true;
			ExitGames.Client.Photon.Hashtable setKillLimit = new Hashtable(); 
			setKillLimit["KL"] = killLimit;
			PhotonNetwork.room.SetCustomProperties(setKillLimit);
		}

	}

	public void SetKillLimit(){
		
		bool result = int.TryParse(killLimitInput.text, out killLimit);
		if(result){
			Debug.Log ("KillLimit Accepted: " + killLimit);
		}
		else{
			Debug.Log ("Error KillLimit Can't be parsed");
			killLimit = 10;
		}
	}

	public void SetMouseX(float xAxis){
		
		PlayerPrefs.SetFloat("xAxis", xAxis);
		xAxis_Text.text = xAxis.ToString();
	}
	
	public void SetMouseY(float yAxis){
		
		PlayerPrefs.SetFloat("yAxis", yAxis);
		yAxis_Text.text = yAxis.ToString();
	}

	public void SmoothMouse(bool on){
		
		PlayerPrefs.SetInt("smooth", (on ? 1 : 0));
		PlayerPrefs.Save();
	}

	public void ShowOptions(){
		NM.optionsMenu.SetActive (true);
		optionsAnim.SetBool ("Show", true);
	}

	public void HideOptions(){

		optionsAnim.SetBool ("Show", false);
		NM.optionsMenu.SetActive (false);
	}

	public void ShowServerOptions(){
		NM.serverOptionsMenu.SetActive (true);
		//serverOptionsAnim.SetBool ("Show", true);
	}
	
	public void HideServerOptions(){
		
		//serverOptionsAnim.SetBool ("Show", false);
		NM.serverOptionsMenu.SetActive (false);
	}

	public void VsyncToggle(bool on){

		PlayerPrefs.SetInt("vSync", (on ? 1 : 0));
	}

	public void QuitGame(){
		
		Application.Quit();
	}
}
