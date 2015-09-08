using UnityEngine;
using System.Collections;

public class Shotgun : Weapon {

    public Shotgun() {

        weaponID = 1;

        damage = 2f;
        clipSizeMax = 30;
        clipAmountMax = 4;
        clipSize = 30;
        clipAmount = 3;
        MaxRange = 10000;
        coolDown = 0.1f;

        scaleLimit = 2f;
        z = 10;
        aimScaleLimit = 2;
        aimZ = 15;
        rayCount = 12;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
