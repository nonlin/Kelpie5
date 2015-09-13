using UnityEngine;
using System.Collections;
using System.Text;
using System.IO;
using UnityEngine.UI;

public class VersionTextUpdater : MonoBehaviour {

    public Text versionText;
    string textFromFile;
	// Use this for initialization
	void Start () {

        textFromFile = System.IO.File.ReadAllText("version.txt");
        versionText.text = "Version" + textFromFile;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
