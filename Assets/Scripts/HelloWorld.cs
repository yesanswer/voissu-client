using UnityEngine;
using System.Collections;

public class HelloWorld : MonoBehaviour {

	// Use this for initialization
	VoIPManager voip;

	void Start () {
		voip = VoIPManager.Instance;
		voip.connect_async ("app1", "uid1", callback);
	}

	void callback(bool result)
	{
		Debug.Log (result);
		if (result)
			voip.enter_channel_async ("channel1");
		
	}
	
	// Update is called once per frame
	void Update () {

	}
}
