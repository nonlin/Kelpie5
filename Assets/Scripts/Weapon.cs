using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour {

    public int weaponID;
    //Weapon Audio Clips
    public AudioClip Fire;
    public AudioClip Reload;
    public AudioClip Empty;

    //Standard weapon stats
    public float damage;
    public int clipSizeMax;
    public int clipAmountMax;
    public int clipSize;
    public int clipAmount;
    public int MaxRange;
    public float coolDown;
    //These 2 controls the spread of the cone
    public float scaleLimit;
    public float z;
    //Make spread less when aiming;
    public float aimScaleLimit;
    public float aimZ;
    //Amount of rays to shoot
    public int rayCount;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
