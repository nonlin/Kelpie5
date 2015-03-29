using UnityEngine;
using System.Collections;

public class GUIManager : MonoBehaviour {

	public GUIStyle TargetNameStyle;
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void ScoreBoard(){

		GUI.Label (new Rect ((Screen.height / 2) + 150,(Screen.width / 2) - 20, 50, 50), "Kills");
		GUI.Label (new Rect ((Screen.height / 2) + 225,(Screen.width / 2) - 20, 50, 50), "Deaths");
		GUI.Label (new Rect ((Screen.height / 2) + 300,(Screen.width / 2) - 20, 50, 50), "Ping");
		GUILayout.BeginArea(new Rect((Screen.height/2),(Screen.width/2), 400,500));

		foreach (PhotonPlayer p in PhotonNetwork.playerList) {
		
			GUILayout.BeginHorizontal("Box");
			//Player Names
			GUILayout.BeginVertical(GUILayout.Width(150));
			GUILayout.Label (p.name, GUILayout.Width (150));
			GUILayout.EndVertical();
			//Player Score
			//GUILayout.BeginVertical(GUILayout.Width(75));
			//GUILayout.Label (p.playerName, GUILayout.Width (75));
			//GUILayout.EndVertical();
			//Player Kills
			GUILayout.BeginVertical(GUILayout.Width(75));
			//kills = p.customProperties["K"].ToString();
			GUILayout.Label (p.customProperties["K"].ToString(), GUILayout.Width (75));
			GUILayout.EndVertical();
			//Player Deaths
			GUILayout.BeginVertical(GUILayout.Width(75));

			GUILayout.Label (p.customProperties["D"].ToString(), GUILayout.Width (75));
			GUILayout.EndVertical();
			//Player Ping
			GUILayout.BeginVertical(GUILayout.Width(75));
	
			GUILayout.Label (p.customProperties["P"].ToString(), GUILayout.Width (75));
			GUILayout.EndVertical();

			GUILayout.EndHorizontal();
			//Debug.Log ("GUI Player Name" + p.playerName);
		}
		GUILayout.EndArea();
	}

	public void UserInterface(){


	}

	public void EnemyName(Transform location, string enemyName){

		if(location != null){
			Vector3 characterPos = Camera.main.WorldToScreenPoint(location.position);
			
			characterPos = new Vector3(Mathf.Clamp(characterPos.x,0 + (100 / 2),Screen.width - (100 / 2)),
			                           Mathf.Clamp(characterPos.y,50,Screen.height),
			                           characterPos.z);
			GUI.Label (new Rect (characterPos.x,characterPos.y, 100, 50), enemyName,TargetNameStyle);
		}

	}

}
