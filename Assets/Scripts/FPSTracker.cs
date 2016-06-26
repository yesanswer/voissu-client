using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class FPSTracker : MonoBehaviour {

    Text text;

	// Use this for initialization
	void Start () {
        this.text = this.GetComponent<Text>();
        StartCoroutine(UpdateFPS());
	}

    IEnumerator UpdateFPS() {
        while(true) {
            int fps = (int)(1.0f / Time.deltaTime);
            text.text = string.Format("FPS : {0}", fps);
            yield return new WaitForSeconds(1.0f);
        }
    }

}
