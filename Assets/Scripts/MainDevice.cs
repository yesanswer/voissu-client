using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainDevice : MonoBehaviour {
    readonly bool debug = true;
    readonly int logMaxLines = 100;
    readonly string appID = "app1";

    public GameObject panelInit;
    public GameObject panelChat;
    public GameObject panelDebug;
    public Text UserTemplate;
    //public Mic microphone;

    Button btnEnter;
    Button btnConnecting;

    InputField inputID;
    InputField inputChannel;
    Button btnExit;
    Button btnMic;
    VerticalLayoutGroup layoutUser;

    ScrollRect scrollRectLog;
    Text textLog;

    Text textMessage;
    Coroutine coroutineMessage = null;

    State currentState = null;
    int logLineCount = 0;
    float connectingTimeForSeconds = 0.0f;
    bool isRequestConnecting = false;
    bool isRecording = true;

    // Use this for initialization
    void Start () {
        this.currentState = null;

        if (panelInit) {
            this.btnEnter = panelInit.transform.FindChild("Enter Button").GetComponent<Button>();
            this.btnEnter.onClick.AddListener(EnterChannel);

            this.btnConnecting = panelInit.transform.FindChild("Connecting Button").GetComponent<Button>();
            this.btnConnecting.interactable = false;

            this.inputID = panelInit.transform.FindChild("ID InputField").GetComponent<InputField>();
            this.inputChannel = panelInit.transform.FindChild("Channel InputField").GetComponent<InputField>();
        }

        if (panelChat) {
            this.btnExit = panelChat.transform.FindChild("Exit Button").GetComponent<Button>();
            this.btnExit.onClick.AddListener(ExitChannel);

            this.btnMic = panelChat.transform.FindChild("Mic Button").GetComponent<Button>();
            this.btnMic.onClick.AddListener(MicButton);

            this.layoutUser = panelChat.transform.Find("User Container").GetComponent<VerticalLayoutGroup>();
        }

        if (panelDebug) {
            this.scrollRectLog = panelDebug.transform.FindChild("Log Scroll View").GetComponent<ScrollRect>();
            this.textLog = this.scrollRectLog.content.GetComponent<Text>();

            if (!this.debug) {
                panelDebug.SetActive(false);
            }
        }

        this.textMessage = GameObject.Find("Message").GetComponent<Text>();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        ChangeState<InitState>();
    }
    
	void Update() {
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			Debug.Log ("backspace");
			Application.Quit ();
		}
	}

    void EnterChannel() {
        string id = this.inputID.text;
        string channel = this.inputChannel.text;

        if (id.Length == 0) {
            Message("Does not exists ID");
            return;
        }

        if (channel.Length == 0) {
            Message("Does not exists Chennel");
            return;
        }

        Text btnText = this.btnMic.GetComponentInChildren<Text>();
        btnText.text = "Mic Off";
        this.isRecording = true;

        BeginConnectingState();
        VoIPManager.make_instance ("app1", id);
        VoIPManager.instance.enter_user_callback = EnterUser;
        VoIPManager.instance.exit_user_callback = ExitUser;

        VoIPManager.instance.enter_channel_async (channel, (enter_channel_result) => {
            Debug.Log (string.Format ("enter channel callback : {0}", enter_channel_result));

            if (enter_channel_result) {
                Message (string.Format ("Enter Channel : {0}", channel));
                EndConnectingState ();
                ChangeState<ChatState> ();
            } else {
                EndConnectingState ();
                Message ("Enter Channel Failed");
            }
        });
    }

    void ExitChannel() {
		VoIPManager.instance.exit_channel_async ((exit_channel_result) => {
			VoIPManager.delete_instance();

            foreach (Transform child in this.layoutUser.transform) {
                Destroy(child.gameObject);
            }

            ChangeState<InitState>();		
		});
    }

    void BeginConnectingState () {
        this.inputID.interactable = false;
        this.inputChannel.interactable = false;
        this.btnEnter.gameObject.SetActive(false);
        this.btnConnecting.gameObject.SetActive(true);

        this.connectingTimeForSeconds = 0.0f;
        this.isRequestConnecting = true;

        StartCoroutine(CoroutineConnectingChannel());
    }

    void EndConnectingState () {
        this.connectingTimeForSeconds = 0.0f;
        this.isRequestConnecting = false;

        this.inputID.interactable = true;
        this.inputChannel.interactable = true;
        this.btnEnter.gameObject.SetActive(true);
        this.btnConnecting.gameObject.SetActive(false);
    }

    public void Log (string text) {
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

    public void Message(string text) {
        if (!this.isActiveAndEnabled) {
            return;
        }

        if (this.coroutineMessage != null) {
            StopCoroutine(this.coroutineMessage);
        }

        Color color = new Color(
            this.textMessage.color.r,
            this.textMessage.color.g,
            this.textMessage.color.b,
            1.0f);

        this.textMessage.color = color;
        this.textMessage.text = text;
        this.coroutineMessage = StartCoroutine(CoroutineMessage());
    }

    public void EnterUser(string uid) {
        Text textUser = Instantiate<Text>(this.UserTemplate);
        textUser.gameObject.SetActive(true);
        textUser.name = "user-" + uid;
        textUser.text = uid;
        textUser.transform.SetParent(this.layoutUser.transform);
        textUser.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

        Debug.Log("Enter " + uid);
    }

    public void ExitUser (string uid) {
        Transform child = this.layoutUser.transform.Find("user-" + uid);
        child.parent = null;
        Destroy(child.gameObject);

        Debug.Log("Exit " + uid);
    }

    public void MicButton() {
        if (isRecording) {
            RecordEnd();
        } else {
            RecordStart();
        }
    }

    public void RecordStart() {
        if (this.currentState is ChatState) {
            Text btnText = this.btnMic.GetComponentInChildren<Text>();
            btnText.text = "Mic Off";
            isRecording = true;
            VoIPManager.instance.record_start();
        }
    }

    public void RecordEnd () {
        if (this.currentState is ChatState) {
            Text btnText = this.btnMic.GetComponentInChildren<Text>();
            btnText.text = "Mic On";
            isRecording = false;
            VoIPManager.instance.record_end();
        }
    }

    public void ChangeState<T> () where T : State {
        if (this.currentState) {
            Destroy(this.currentState);
            this.currentState = null;
        }

        this.currentState = this.gameObject.AddComponent<T>();
    }

    IEnumerator CoroutineConnectingChannel () {
        while (this.isRequestConnecting) {
            yield return new WaitForSeconds(0.5f);

            Text btnText = this.btnConnecting.GetComponentInChildren<Text>();
            btnText.text = "Connecting";
            for (int i = 0; i <= (this.connectingTimeForSeconds % 3); ++i) {
                btnText.text += ".";
            }

            this.connectingTimeForSeconds += 1.0f;
        }
    }

    public IEnumerator CoroutineMessage() {
        yield return new WaitForSeconds(3.0f);

        const float decAlphaPerSec = 0.7f;
        float waitForSec = 0.1f;

        while(true) {
            yield return new WaitForSeconds(waitForSec);
            float alpha = this.textMessage.color.a - (decAlphaPerSec * waitForSec);
            if (alpha <= 0.0f) {
                alpha = 0.0f;
            }

            this.textMessage.color = new Color(
                this.textMessage.color.r,
                this.textMessage.color.g,
                this.textMessage.color.b,
                alpha);

            if (alpha <= 0.0f) {
                break;
            }
        }

        this.coroutineMessage = null;
    }
}
