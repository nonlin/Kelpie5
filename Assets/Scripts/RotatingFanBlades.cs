using UnityEngine;
using System.Collections;

public class RotatingFanBlades : MonoBehaviour {

    public float RPM = 1200f;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        //Rotate fans from the center of GameObjects's Mass (Bounds), around the Y axis, at a rate of RPM every sec
        transform.RotateAround(this.GetComponent<Renderer>().bounds.center, transform.parent.up, RPM * Time.deltaTime);
	}
}
