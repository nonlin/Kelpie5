using UnityEngine;
using System.Collections;

public class Ammo : MonoBehaviour {

	public AudioClip pickupSound;//sound to play when picking up weapon
	AudioSource audio1;
	//public AudioClip fullSound;//sound to play when ammo is full
	PhotonView photonView;
	public bool canGet = true;
	// Use this for initialization
	void Start () {
		photonView = GetComponent<PhotonView> ();
		audio1 = GetComponent<AudioSource> ();
		audio1.clip = pickupSound;
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void OnPickUp(){

		canGet = false;
		StartCoroutine (DestroyOnSoundEnd ());
	}

	IEnumerator DestroyOnSoundEnd() {

		audio1.Play ();
		yield return new WaitForSeconds(audio1.clip.length);
		if(photonView.isMine)
			PhotonNetwork.Destroy (gameObject);
	}

	void OnDestroy() {

	}
}
