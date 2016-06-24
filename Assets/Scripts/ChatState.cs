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
        this.voissuOutput.AddAudioItem("user1", 1);

        currentPacketNumber = 0;
        processPacketNumber = 0;
        packetQueue = new Queue();

        this.voissuInput.RecordStart(VoissuOutput.samplingRate, VoissuOutput.samplingSize);
        StartCoroutine(SamplingProcessor());

        /*
        this.voissuInput = this.gameObject.AddComponent<VoissuInput>();
        this.voissuOutput = this.gameObject.AddComponent<VoissuOutput>();

        this.voissuInput.AddOnRecordListener(OnRecordListener);

        ArrayList peer_list = VoIPManager.Instance.PeerList;
        foreach (Peer peer in peer_list) {
            this.voissuOutput.AddAudioItem(peer.uid, 1);
        }

        this.voissuInput.RecordStart(VoissuOutput.samplingRate, VoissuOutput.samplingSize);
        */
    }

    // Update is called once per frame
    void Update () {
	
	}

    int currentPacketNumber = 0;
    int processPacketNumber = 0;
    Queue packetQueue;
    struct Packet {
        public byte[] encryptStream;
        public int samplingBufferSize;
        public int packetNumber;
    }

    void OnRecordListener (byte[] encryptStream, int samplingBufferSize) {
        float packetLossRate = 0.01f;
        if (packetLossRate > Random.Range(0.0f, 1.0f)) {
            return;
        }

        Packet packet = new Packet();
        packet.encryptStream = encryptStream;
        packet.samplingBufferSize = samplingBufferSize;
        packet.packetNumber = currentPacketNumber;
        packetQueue.Enqueue(packet);
        currentPacketNumber += 1;
    }

    IEnumerator SamplingProcessor() {
        while (true) {
            yield return new WaitForSeconds(Random.Range(0.02f, 0.1f));
            if (packetQueue.Count == 0) {
                yield return null;
                continue;
            }

            while(packetQueue.Count > 0) {
                Packet packet = (Packet)packetQueue.Dequeue();
                if (processPacketNumber > packet.packetNumber) {
                    yield return null;
                    continue;
                }

                this.voissuOutput.AddSamplingData("user1", packet.encryptStream, packet.samplingBufferSize);
                processPacketNumber = packet.packetNumber;
            }
        }
    }

    
}
