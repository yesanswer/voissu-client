using UnityEngine;
using System.Collections;

public class HelloWorld : MonoBehaviour
{
	void Start ()
	{
		VoIPManager.Instance.connect_async ("app1", "user2", (result) => {
			Debug.Log (string.Format ("connect async callback : {0}", result));
			if (result) {
				VoIPManager.Instance.enter_channel_async ("channel1", (re) => {
					
				});
			}
		});
	}
}
