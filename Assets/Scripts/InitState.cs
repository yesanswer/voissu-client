using UnityEngine;
using System.Collections;

public class InitState : State {
    
    void OnEnable () {
        if (this.mainDevice) {
            this.mainDevice.panelInit.SetActive(true);
        }
    }

    void OnDisable () {
        if (this.mainDevice) {
            this.mainDevice.panelInit.SetActive(false);
        }
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
