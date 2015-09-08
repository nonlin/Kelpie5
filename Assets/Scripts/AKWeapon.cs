using UnityEngine;
using System.Collections;

public class AKWeapon : Weapon {

    public AKWeapon() {

        weaponID = 0;

        damage = 16f;
        clipSizeMax = 30;
        clipAmountMax = 4;
        clipSize = 30;
        clipAmount = 3;
        MaxRange = 10000;
        coolDown = 0.1f;

        scaleLimit = 2f;
        z = 50;
        aimScaleLimit = 2;
        aimZ = 70;
        rayCount = 1;
    }

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
