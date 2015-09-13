using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class DroneNetworkMover : Photon.MonoBehaviour
{
    Vector3 realPosition;
    Quaternion realRotation;
    PhotonView photonView;
    bool initialLoad = true;
    float smoothing = 10f;
    float health = 100f;

    //AudioSource audio;
    // Use this for initialization
    void Start()
    {

        PhotonNetwork.sendRate = 30;
        PhotonNetwork.sendRateOnSerialize = 15;
        photonView = GetComponent<PhotonView>();

        if (photonView.isMine)
        {

  
        }
        else
        {
            //All other players
            StartCoroutine("UpdateData");
        }

    }

    IEnumerator UpdateData()
    {

        if (initialLoad)
        {

            //jiiter correction incomplete, could check position if accurate to .0001 don't move them 
            initialLoad = false;
            transform.position = realPosition;
            transform.rotation = realRotation;

        }//This is where we set all other player prefab settings that isn't the local player's settings
        while (true)
        {
            //smooths every frame for the dummy players from where they are to where they should be, prevents jitter lose some accuracy I suppose
            //Ideally we want the movement to be equal to the amount of time since the last update
            transform.position = Vector3.Lerp(transform.position, realPosition, 0.1f);// + _characterController.velocity * Time.deltaTime;
            transform.rotation = Quaternion.Lerp(transform.rotation, realRotation, 0.1f);//Time.deltaTime * smoothing

            yield return null;
        }
    }

    //Serilize Data Across the network, we want everyone to know where they are
    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //Send to everyone else a local players variables to be synced and recieved by all other players on network
        if (stream.isWriting)
        {
            //send to clients where we are
            
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(health);
 
            //Sync Animation States


        }
        else
        {
            //Get from clients where they are
            //Write in the same order we read, if not writing we are reading. 
            realPosition = (Vector3)stream.ReceiveNext();
            realRotation = (Quaternion)stream.ReceiveNext();
            health = (float)stream.ReceiveNext();
            //Sync Animation States


        }
    }


    // Update is called once per frame
    void Update()
    {


    }

}
