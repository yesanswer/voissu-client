using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainDevice : MonoBehaviour {
    readonly bool debug = true;
    readonly int logMaxLines = 100;

    public GameObject panelInit;
    public GameObject panelChat;
    public GameObject panelDebug;
    //public Mic microphone;

    Button btnEnter;
    Button btnConnecting;

    InputField inputChannel;
    Button btnExit;

    ScrollRect scrollRectLog;
    Text textLog;

    State currentState = null;
    int logLineCount = 0;
    float connectingTimeForSeconds = 0.0f;
    bool connectComplete = false;

    // Use this for initialization
    void Start () {
        this.currentState = null;

        if (panelInit) {
            this.btnEnter = panelInit.transform.FindChild("Enter Button").GetComponent<Button>();
            this.btnEnter.onClick.AddListener(enterChannel);

            this.btnConnecting = panelInit.transform.FindChild("Connecting Button").GetComponent<Button>();
            this.btnConnecting.interactable = false;

            this.inputChannel = panelInit.transform.FindChild("Channel InputField").GetComponent<InputField>();
        }

        if (panelChat) {
            this.btnExit = panelChat.transform.FindChild("Exit Button").GetComponent<Button>();
            this.btnExit.onClick.AddListener(exitChannel);
        }

        if (panelDebug) {
            this.scrollRectLog = panelDebug.transform.FindChild("Log Scroll View").GetComponent<ScrollRect>();
            this.textLog = this.scrollRectLog.content.GetComponent<Text>();

            if (!this.debug) {
                panelDebug.SetActive(false);
            }
        }

        /*
        if (btnRecord) {
            RecordButton recordButton = btnRecord.GetComponent<RecordButton>();
            recordButton.onButtonDown = microphone.RecordStart;
            recordButton.onButtonUp = microphone.RecordEnd;
        }
        */

        changeState<InitState>();
    }
	
	// Update is called once per frame
	void Update () {
	
	}
    
    void enterChannel() {
        string channel = this.inputChannel.text;

        this.inputChannel.interactable = false;
        this.btnEnter.gameObject.SetActive(false);
        this.btnConnecting.gameObject.SetActive(true);

        this.connectingTimeForSeconds = 0.0f;
        this.connectComplete = false;
        StartCoroutine(connectingChannel());
    }

    void exitChannel() {
        changeState<InitState>();

        this.inputChannel.interactable = true;
        this.btnEnter.gameObject.SetActive(true);
        this.btnConnecting.gameObject.SetActive(false);
        this.connectingTimeForSeconds = 0.0f;
        this.connectComplete = false;
    }

    IEnumerator connectingChannel() {
        while (!this.connectComplete) {
            yield return new WaitForSeconds(0.5f);

            Text btnText = this.btnConnecting.GetComponentInChildren<Text>();
            btnText.text = "Connecting";
            for (int i=0; i<=(this.connectingTimeForSeconds % 3); ++i) {
                btnText.text += ".";
            }

            this.connectingTimeForSeconds += 0.5f;

            // test code
            if (this.connectingTimeForSeconds > 3.0f) {
                this.connectComplete = true;
                changeState<ChatState>();
            }
        }
    }

    public void log (string text) {
        if (!this.debug) {
            return;
        }

        if (!this.isActiveAndEnabled) {
            return;
        }

        if (this.logLineCount > this.logMaxLines) {
            this.logLineCount = 0;
            this.textLog.text = "";
        }

        this.textLog.text += (text + "\n");
        this.logLineCount += 1;
        this.scrollRectLog.normalizedPosition = new Vector2(0, 0);
    }

    public void changeState<T> () where T : State {
        if (this.currentState) {
            Destroy(this.currentState);
            this.currentState = null;
        }

        this.currentState = this.gameObject.AddComponent<T>();
    }
}
