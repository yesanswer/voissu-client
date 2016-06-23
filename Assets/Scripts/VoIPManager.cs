using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Boomlagoon.JSON;
using System.Text;
using System;


public enum VOIP_STATUS
{
	CONNECTING,
	CONNECTED,
	ENTERRING1,
	ENTERRING2,
	ENTERED
}

public class VoIPManager : MonoBehaviour
{

	ControlChannel control_channel;
	DataChannel data_channel;

	private VoissuInput vi;
	private VoissuOutput vo;
	private int nextseq = 1;
	public string my_guid {
		get;
		private set;
	}

	public string my_uid {
		get;
		private set;
	}
		
	private ArrayList peer_list;
	VOIP_STATUS status;


	private static VoIPManager instance;
	private static GameObject container;

	Action<bool> user_connect_callback;

	public static VoIPManager Instance {  
		get {
			if (!instance) {  
				container = new GameObject ();  
				container.name = "VoIPManager";  
				instance = container.AddComponent (typeof(VoIPManager)) as VoIPManager;  
				DontDestroyOnLoad (container);
				Application.runInBackground = true;
			}

			return instance;  
		}
	}


	private void Awake ()
	{
		this.peer_list = new ArrayList ();
		user_connect_callback = null;

	}


	public void connect_async (string appid, string uid, Action<bool> callback)
	{
		this.my_uid = uid;
		this.my_guid = null;
		status = VOIP_STATUS.CONNECTING;
		control_channel = new ControlChannel ();
		data_channel = new DataChannel ();

		user_connect_callback = callback;
		control_channel.connect_async ((result) => {
			Debug.Log ("tcp connected");
			if (result) {
				JSONObject obj = new JSONObject ();
				obj.Add ("app_id", appid);
				obj.Add ("uid", uid);
				control_channel.send_message (obj);
			} else {
				callback (false);
			}
		});
	}

	public void enter_channel_async (string channel_id, Action<bool> callback)
	{
		Debug.Log ("enter_channel_async call");
		StartCoroutine (enter_channel_coroutine (channel_id, callback));
	}

	private IEnumerator enter_channel_coroutine (string channel_id, Action<bool> callback)
	{
		vi = this.gameObject.AddComponent<VoissuInput> ();
		vo = this.gameObject.AddComponent<VoissuOutput> ();

		DataPacket dp = null;
		JSONObject obj = new JSONObject ();
		string private_ip = get_private_ip ();
		if (private_ip == null) {
			Debug.Log ("get private ip faild");
			callback (false);
			yield break;
		}
		obj.Add ("type", PROTOCOL.REQUEST_TYPE_ENTER_CHANNEL);
		obj.Add ("guid", this.my_guid);
		obj.Add ("uid", this.my_uid);
		obj.Add ("private_udp_address", private_ip);
		obj.Add ("channel_id", channel_id);
		control_channel.send_message (obj);
		status = VOIP_STATUS.ENTERRING1;

		for (int i = 0; i < 3; i++) {
			dp = new DataPacket (this.my_guid, 0, 0, new byte[0]);
			data_channel.send_message (GLOBAL.SERVER_IP, GLOBAL.DATA_PORT, dp);
			yield return new WaitForSeconds (0.5f);
			if (status == VOIP_STATUS.ENTERRING2)
				break;
		}

		if (status != VOIP_STATUS.ENTERRING2) {
			Debug.Log ("enter channel timeout");
			callback (false);
			yield break;
		}

		foreach (Peer peer in peer_list) {
			Debug.Log ("<peer> private ip: " + peer.private_ip + " public ip: " + peer.public_ip + " public port: " + peer.public_port);
			StartCoroutine (peer.p2p_connect (data_channel));
		}
			
		for (int i = 0; i < 10; i++) {
			bool result = true;
			foreach(Peer p in peer_list){
				if (p.connection_status != PEER_STATUS.PRIVATE_CONNECTED && p.connection_status != PEER_STATUS.PUBLIC_CONNECTED)
					result = false;
			}

			if (result == true) {
				Debug.Log("enter channel success");
				callback (true);
				break;
			}
			yield return new WaitForSeconds (0.5f);
		}

		obj = new JSONObject ();
		JSONArray success_list = new JSONArray ();
		foreach (Peer p in peer_list) {
			if (p.connection_status != PEER_STATUS.RELAY_CONNECTED)
				success_list.Add (new JSONValue(p.uid));
		}
		obj.Add ("type", PROTOCOL.REQUEST_TYPE_P2P_STATUS_SYNC);
		obj.Add ("users", success_list);
		control_channel.send_message (obj);
		Debug.Log ("p2p connection result send");


		vi.AddOnRecordListener (OnRecordListener);

		vi.RecordStart (VoissuOutput.samplingRate, VoissuOutput.samplingSize);
		foreach (Peer p in peer_list) {
			vo.AddAudioItem (p.uid, 1);
		}
		callback (true);
	}
	

	private void OnRecordListener(byte[] data, int sampling_buffer_size)
	{
		byte[] buf = new byte[data.Length + 4];
		Buffer.BlockCopy (BitConverter.GetBytes (sampling_buffer_size), 0, data, 0, 4);
		Buffer.BlockCopy (data, 0, buf, 4, data.Length);

		DataPacket dp = new DataPacket (my_uid, PROTOCOL.UDP_DATA, nextseq++, buf);
		foreach (Peer p in peer_list) {
			p.send_data (dp, data_channel);
		}
		Debug.Log ("sound send");
	}


	/// <summary>
	/// Gets the private ip on success, null on fail
	/// </summary>
	private string get_private_ip ()
	{
		string localIP;
		using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
		{
			socket.Connect("8.8.8.8", 65530);
			IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
			socket.Close ();
			localIP = endPoint.Address.ToString();
			Debug.Log ("local ip: " + localIP);
			return localIP;
		}
		return null;
	}

	private void execute_packet (JSONObject obj)
	{
		//Debug.Log ("execute_packet");

		int type = (int)obj.GetNumber ("type");
		switch (type) {
		case PROTOCOL.RESPONSE_TYPE_SIGN_IN:
			bool success = obj.GetBoolean ("success");
			if (success) {
				this.my_guid = obj.GetString ("guid");
				status = VOIP_STATUS.CONNECTED;
				user_connect_callback (true);
			} else {
				user_connect_callback (false);
			}
			break;
		case PROTOCOL.RESPONSE_TYPE_NEW_USER_JOIN_CHANNEL:
			Debug.Log ("response new user recv");
			JSONArray arr = obj.GetArray ("users");
			for (int i = 0; i < arr.Length; i++) {
				JSONObject entry = arr [i].Obj;
				string uid = entry.GetString ("uid");
				string private_ip = entry.GetString ("private_udp_address");
				JSONObject public_address = entry.GetObject ("public_udp_address");
				string public_ip = public_address.GetString ("ip");
				int public_port = (int)public_address.GetNumber ("port");

				Peer peer = new Peer (uid, public_ip, public_port, private_ip, GLOBAL.DATA_PORT);
				peer_list.Add (peer);
			}

			status = VOIP_STATUS.ENTERRING2;
			break;
		case PROTOCOL.RESPONSE_TYPE_OTHER_USER_JOIN_CHANNEL:
			{
				Debug.Log ("other user join channel :" + obj);
				string uid = obj.GetString ("uid");
				string private_ip = obj.GetString ("private_udp_address");
				JSONObject public_address = obj.GetObject ("public_udp_address");
				string public_ip = public_address.GetString ("ip");
				int public_port = (int)public_address.GetNumber ("port");

				Peer peer = new Peer (uid, public_ip, public_port, private_ip, GLOBAL.DATA_PORT);
				peer_list.Add (peer);
				vo.AddAudioItem (peer.uid, 1);
				StartCoroutine (peer.p2p_connect (data_channel));

			}
			break;
		case PROTOCOL.REQUEST_TYPE_PING:
			{
				JSONObject response = new JSONObject ();
				response.Add ("type", PROTOCOL.PONG);
				control_channel.send_message (response);
		//		Debug.Log ("pong");
			}
			break;
		default:
			Debug.Log ("Undefined protocol: " + type);	
			break;
		}
	}

	void Update ()
	{
		JSONObject obj = control_channel.receive_message ();
		if (obj != null) {
	//		Debug.Log ("recv packet: " + obj.ToString ());
			execute_packet (obj);
		}
		DataPacket dp = null;
		while (true) {
			dp = data_channel.receive_message ();
			if (dp == null)
				break;
			Peer p = get_peer (dp.id);

			Debug.Log ("receive udp message: ");
			switch (dp.type) {
			case PROTOCOL.UDP_PRIVATE_CONNECT:
				p.connection_status = PEER_STATUS.PRIVATE_CONNECTED;

				break;
			case PROTOCOL.UDP_PUBLIC_CONNECT:
				p.connection_status = PEER_STATUS.PUBLIC_CONNECTED;

				break;
			case PROTOCOL.UDP_DATA:
				{
					Debug.Log ("sound receive");
					byte[] buf = dp.data;
					byte[] stream = new byte[buf.Length - 4];
					int sambufsize;
					Buffer.BlockCopy (buf, 4, stream, 0, buf.Length - 4);
					sambufsize = BitConverter.ToInt32 (buf, 0);
					vo.AddSamplingData (dp.id, stream, sambufsize);

				}				
				break;
			default:
				Debug.Log ("undefined udp message: " + dp.type);
				break;	
			}
		}

	}

	private Peer get_peer(string uid)
	{
		foreach(Peer p in peer_list)
		{
			if (p.uid == uid)
				return p;
		}
		return null;
	}
}
