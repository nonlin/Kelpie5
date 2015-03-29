using UnityEngine;
using System.Collections;

public class RemoveAfterTime : MonoBehaviour {

	// Use this for initialization
	public float LifeTime = 0.7f;
	void Start () {

		Destroy (gameObject, LifeTime);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
