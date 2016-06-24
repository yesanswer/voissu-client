using UnityEngine;
using System.Collections;

public class HelloWorld : MonoBehaviour
{
	VoIPManager voip;
	void Start ()
	{
		voip = VoIPManager.make_instance ("app1", "sangjun");
		voip.enter_channel_async ("channel1", enter_channel_result);
	
	}

	void Update()
	{
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			Debug.Log ("backspace");
			voip.exit_channel_async (exit_channel_result);
		}
	}

	void enter_channel_result(bool result)
	{
	}

	void exit_channel_result(bool result)
	{	
		Debug.Log ("quit");
		Application.Quit();
		
	}
}
