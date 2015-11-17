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
    public int timeLimit;
	bool doOnce = false;
    public bool killToWin;
    public bool timeToWin;
    public GameObject crosshair;

	[SerializeField] InputField killLimitInput;
    [SerializeField] Toggle toggleKillLimit;
    [SerializeField] Toggle toggleTimeLimit;
    [SerializeField] InputField timeLimitInput;
	// Use this for initialization
	void Start () {

        GameObject.Find("Crosshair").GetComponent<RawImage>().enabled = false;
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
        timeLimit = 10;
        killToWin = toggleKillLimit.isOn;
        timeToWin = toggleTimeLimit.isOn;

	}
	
	// Update is called once per frame
	void Update () {
        
        //Once there is a master client update to the network the game configuration just once
		if (PhotonNetwork.isMasterClient && !doOnce){
			doOnce = true;

			ExitGames.Client.Photon.Hashtable setKillLimit = new Hashtable(); 
			setKillLimit["KL"] = killLimit;

            ExitGames.Client.Photon.Hashtable toggleKillLimit = new Hashtable();
            setKillLimit["TKL"] = killToWin ? 1 : 0;

            ExitGames.Client.Photon.Hashtable setTimeLimit = new Hashtable();
            setKillLimit["TL"] = timeLimit;

            ExitGames.Client.Photon.Hashtable toggleTimeLimit = new Hashtable();
            setKillLimit["TTL"] = timeToWin ? 1 : 0;
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

    public void SetTimeLimit()
    {

        bool result = int.TryParse(timeLimitInput.text, out timeLimit);
        if (result)
        {
            Debug.Log("KillLimit Accepted: " + timeLimit);
        }
        else
        {
            Debug.Log("Error timeLimit Can't be parsed");
            timeLimit = 10;
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
        //Show Crosshair for color changing
        crosshair.SetActive(true);
        crosshair.GetComponent<RawImage>().enabled = true;
    }

	public void HideOptions(){

        //Hide crosshair
        crosshair.GetComponent<RawImage>().enabled = false;
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

    public void SetKillToWin(bool optionChoice)
    {

        killToWin = optionChoice;
        timeToWin = !optionChoice;
    }


    public void SetTimeToWin(bool optionChoice)
    {

        timeToWin = optionChoice;
        killToWin = !optionChoice;
    }

    public void ColorPicker(int color)
    {

        if(crosshair != null) { 
            switch (color) {

                case 0:
                    crosshair.GetComponent<RawImage>().color = Color.white;
                    break;
                case 1:
                    crosshair.GetComponent<RawImage>().color = Color.red;
                    break;
                case 2:
                    crosshair.GetComponent<RawImage>().color = Color.blue;
                    break;
                case 3:
                    crosshair.GetComponent<RawImage>().color = Color.green;
                    break;
                case 4:
                    crosshair.GetComponent<RawImage>().color = Color.black;
                    break;
                default:
                    crosshair.GetComponent<RawImage>().color = Color.white;
                    break;

            }
        }
    }

	public void QuitGame(){
		
		Application.Quit();
	}
}
