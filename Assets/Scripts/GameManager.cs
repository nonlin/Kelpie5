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
    public Toggle DOF;
    public Toggle CMB;
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
            PlayerPrefs.Save();
		}

		NM = GameObject.FindGameObjectWithTag ("NetworkManager").GetComponent<NetworkManager> ();
        //Load Saved Settings when game is loaded, setting is on if not 0 (aka equal to 1)
		yAxis_Text.text = PlayerPrefs.GetFloat("yAxis").ToString();
		xAxis_Text.text = PlayerPrefs.GetFloat("xAxis").ToString();
		smoothToggle.isOn = (PlayerPrefs.GetInt("smooth") != 0);
		vSync.isOn = (PlayerPrefs.GetInt("vSync") != 0);
        DOF.isOn = (PlayerPrefs.GetInt("DOF") != 0);
        CMB.isOn = (PlayerPrefs.GetInt("CMB") != 0);
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
        PlayerPrefs.Save();
	}
	
	public void SetMouseY(float yAxis){
		
		PlayerPrefs.SetFloat("yAxis", yAxis);
		yAxis_Text.text = yAxis.ToString();
        PlayerPrefs.Save();
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
        PlayerPrefs.Save();
	}

	public void ShowServerOptions(){
		NM.serverOptionsMenu.SetActive (true);
		//serverOptionsAnim.SetBool ("Show", true);
	}
	
	public void HideServerOptions(){
		
		//serverOptionsAnim.SetBool ("Show", false);
		NM.serverOptionsMenu.SetActive (false);
        PlayerPrefs.Save();
	}

	public void VsyncToggle(bool on){

		PlayerPrefs.SetInt("vSync", (on ? 1 : 0));
        PlayerPrefs.Save();
	}

    public void DOFToggle(bool on){

        PlayerPrefs.SetInt("DOF", (on ? 1 : 0));
        PlayerPrefs.Save();
        Debug.Log("DOF" + PlayerPrefs.GetInt("DOF"));
    }

    public void CMBToggle(bool on)
    {

        PlayerPrefs.SetInt("CMB", (on ? 1 : 0));
        PlayerPrefs.Save();
        Debug.Log("CMB" + PlayerPrefs.GetInt("CMB"));
    }

	public void QuitGame(){
		
		Application.Quit();
	}
}
