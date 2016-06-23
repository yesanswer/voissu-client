using UnityEngine;
using System.Collections;

public class ChatState : State {
    VoissuInput voissuInput;
    VoissuOutput voissuOutput;

    void OnEnable () {
        if (this.mainDevice) {
            this.mainDevice.panelChat.SetActive(true);
        }
    }

    void OnDisable () {
        if (this.voissuInput) {
            this.voissuInput.RecordEnd();
        }

        if (this.mainDevice) {
            this.mainDevice.panelChat.SetActive(false);
        }
    }

    // Use this for initialization
    void Start () {
        this.voissuInput = this.gameObject.AddComponent<VoissuInput>();
        this.voissuOutput = this.gameObject.AddComponent<VoissuOutput>();

        this.voissuInput.AddOnRecordListener(OnRecordListener);
        this.voissuOutput.AddAudioItem("test", 1);

        this.voissuInput.RecordStart(VoissuOutput.samplingRate, VoissuOutput.samplingSize);
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnRecordListener (byte[] encryptStream, int samplingBufferSize) {
        this.voissuOutput.AddSamplingData("test", encryptStream, samplingBufferSize);
    }
}
