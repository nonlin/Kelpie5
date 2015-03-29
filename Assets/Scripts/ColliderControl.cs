using UnityEngine;
using System.Collections;

public class ColliderControl : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void DisableCollider(){

		gameObject.GetComponent<Collider>().enabled = false; 
	}
}
