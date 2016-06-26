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
	CONNECTED, // control server connected
	ENTERRING1, // entering channel1
	ENTERRING2, // entering channel2, p2p connecting
	ENTERED // channel entered
}

public class VoIPManager : MonoBehaviour
{
	private ControlChannel control_channel;
	private DataChannel data_channel;

	private VoissuInput vi;
	private VoissuOutput vo;

	private int nextseq = 1;

	private ArrayList peer_list;
	private VOIP_STATUS status;

	public Action<string> enter_user_callback {
		get;
		set;
	}

	public Action<string> exit_user_callback {
		get;
		set;
	}

	public Action<string> receive_data_callback {
		get;
		set;
	}



	public static VoIPManager instance {
		get;
		private set;
	}
	private static GameObject container;

	private string my_guid;

	public string my_uid {
		get;
		private set;
	}
	public string my_appid {
		get;
		private set;
	}

	public bool connected{
		get {
			return status != VOIP_STATUS.CONNECTING;
		}
	}

	public bool entered {
		get {
			return status == VOIP_STATUS.ENTERED;
		}
	}

	private VoIPManager(){}

	public static VoIPManager make_instance(string appid, string uid) {  
		if (uid.Length > 36) {
			Debug.Log ("uid can not have length more than 36");
			return null;
		}
		if (!instance) {  
			container = new GameObject ();  
			container.name = "VoIPManager";  
			instance = container.AddComponent (typeof(VoIPManager)) as VoIPManager;
			instance.my_appid = appid;
			instance.my_uid = uid;
			DontDestroyOnLoad (container);
			Application.runInBackground = true;
			instance.connect_async ();
			return instance;
		}
		else {
			Debug.Log ("aleady have instance");
			return null;
		}

	}

	public static void delete_instance() {
		if (instance) {
			if (instance.control_channel != null)
				instance.control_channel.close_channel ();
			GameObject.Destroy (container);
			container = null;
			instance = null;

		}
	}





	private void connect_async ()
	{
		if (connected == false) {	
			status = VOIP_STATUS.CONNECTING;
			control_channel = new ControlChannel (GLOBAL.SERVER_IP, GLOBAL.SERVER_CONTROL_PORT, connect_complete);
		}
	}

	private void connect_complete(bool result)
	{
		if (result) {
			Debug.Log ("tcp connected");
			JSONObject obj = new JSONObject ();
			obj.Add ("app_id", my_appid);
			obj.Add ("uid", my_uid);
			control_channel.send_message (obj);
		}
	}


	public void exit_channel_async(Action<bool> callback)
	{
		if (status == VOIP_STATUS.ENTERED) {
			StartCoroutine (exit_channel_coroutine (callback));
		}
	}

	private IEnumerator exit_channel_coroutine(Action<bool> callback)
	{
		JSONObject message = new JSONObject ();
		message.Add ("type", PROTOCOL.REQUEST_TYPE_EXIT_CHANNEL);
		message.Add ("guid", my_guid);
		control_channel.send_message (message);
		data_channel.close_channel ();
		data_channel = null;

		vi.RecordEnd ();
		foreach (Peer p in peer_list)
			vo.DelAudioItem (p.uid);

		for (int i = 0; i < 10; i++) {
			if (status != VOIP_STATUS.ENTERED)
				break;
			yield return new WaitForSeconds (0.3f);
		}

		peer_list.Clear ();
		peer_list = null;
		callback (true);

	}

	public void enter_channel_async (string channel_id, Action<bool> callback)
	{
		Debug.Log ("enter_channel_async call");
		StartCoroutine (enter_channel_coroutine (channel_id, callback));
	}

	private IEnumerator enter_channel_coroutine (string channel_id, Action<bool> callback)
	{
		peer_list = new ArrayList ();
		data_channel = new DataChannel (GLOBAL.PEER_DATA_PORT);

		for (int i = 0; i < 10; i++) {
			if (connected)
				break;
			yield return new WaitForSeconds (0.3f);
		}
		if (connected == false) {
			callback (false);
			yield break;
		}
		
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
		for (int i = 0; i < 5; i++) {
			dp = new DataPacket (this.my_guid, 0, 0, 0,new byte[0]);
			data_channel.send_message (GLOBAL.SERVER_IP, GLOBAL.SERVER_DATA_PORT, dp);
			yield return new WaitForSeconds (0.3f);
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
		status = VOIP_STATUS.ENTERED;
	
		vi = gameObject.AddComponent<VoissuInput> ();
		vo = gameObject.AddComponent<VoissuOutput> ();
		vi.AddOnRecordListener (OnRecordListener);
		foreach (Peer p in peer_list) {
			vo.AddAudioItem (p.uid, 1);

            if (enter_user_callback != null)
                enter_user_callback(p.uid);

        }
		callback (true);
		vi.RecordStart (VoissuOutput.samplingRate, VoissuOutput.samplingSize);

	}

	

	private void OnRecordListener(byte[] data, int sampling_buffer_size)
	{
		DataPacket dp = new DataPacket (my_uid, PROTOCOL.UDP_DATA, nextseq++, sampling_buffer_size, data);

		bool relay_flag = false; 
		foreach (Peer p in peer_list) {
			string connected_ip = p.connected_ip;
			int connected_port = p.connected_port;
			if (connected_ip != null && connected_port != -1)
				data_channel.send_message (connected_ip, connected_port, dp);
			else if(p.connection_status == PEER_STATUS.RELAY_CONNECTED)
				relay_flag = true;
		}
		if (relay_flag) {
			dp.id = my_guid;
			data_channel.send_message (GLOBAL.SERVER_IP, GLOBAL.SERVER_DATA_PORT, dp);
		}
	}

	


	private void execute_packet (JSONObject obj)
	{
		Debug.Log ("execute_packet " + obj);
		int type = (int)obj.GetNumber ("type");
		switch (type) {
		case PROTOCOL.RESPONSE_TYPE_SIGN_IN:
			bool success = obj.GetBoolean ("success");
			if (success) {
				this.my_guid = obj.GetString ("guid");
				status = VOIP_STATUS.CONNECTED;
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

				Peer peer = new Peer (uid, public_ip, public_port, private_ip, GLOBAL.PEER_DATA_PORT);
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

				Peer peer = new Peer (uid, public_ip, public_port, private_ip, GLOBAL.PEER_DATA_PORT);
				peer_list.Add (peer);
				vo.AddAudioItem (peer.uid, 1);
				StartCoroutine (peer.p2p_connect (data_channel));
				if (enter_user_callback != null)
					enter_user_callback (uid);
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
		case PROTOCOL.RESPONSE_TYPE_EXIT_CHANNEL:
			Debug.Log ("response type exit channel");
			status = VOIP_STATUS.CONNECTED;
			break;
		case PROTOCOL.RESPONSE_TYPE_OTHER_USER_EXIT_CHANNEL:
			{
				string uid = obj.GetString ("exit_user_uid");
				Debug.Log ("other user exit channel " + uid);
				Peer exit_user = get_peer (uid);
				if (exit_user != null)
					peer_list.Remove (exit_user);
				vo.DelAudioItem (uid);
				if (exit_user_callback != null)
					exit_user_callback (uid);
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
		while (data_channel != null) {
			dp = data_channel.receive_message ();
			if (dp == null)
				break;
			Peer p = get_peer (dp.id);

			//Debug.Log ("receive udp message: ");
			switch (dp.type) {
			case PROTOCOL.UDP_PRIVATE_CONNECT:
				Debug.Log ("udp_private_connect message recv");
				p.connection_status = PEER_STATUS.PRIVATE_CONNECTED;

				break;
			case PROTOCOL.UDP_PUBLIC_CONNECT:
				Debug.Log ("udp_public_connect message recv");
				p.connection_status = PEER_STATUS.PUBLIC_CONNECTED;

				break;
			case PROTOCOL.UDP_DATA:
				{
					Debug.Log ("sound receive");
					vo.AddSamplingData (dp.id, dp.data, dp.int_data);
					if(receive_data_callback != null)
						receive_data_callback (dp.id);
				}				
				break;
			default:
				Debug.Log ("undefined udp message: " + dp.type);
				break;	
			}
		}

		/*
		if (status == VOIP_STATUS.ENTERED) {
			nextseq++;
			dp = new DataPacket (my_uid, PROTOCOL.UDP_DATA, nextseq, Encoding.UTF8.GetBytes ("hello " + nextseq));
			bool relay_flag = false;
			foreach (Peer p in peer_list) {
				string connected_ip = p.connected_ip;
				int connected_port = p.connected_port;
				if (connected_ip != null && connected_port != -1)
					data_channel.send_message (connected_ip, connected_port, dp);
				else if(p.connection_status == PEER_STATUS.RELAY_CONNECTED)
					relay_flag = true;
			}

			if (relay_flag) {
				dp.id = my_guid;
				data_channel.send_message (GLOBAL.SERVER_IP, GLOBAL.SERVER_DATA_PORT, dp);
			}
		}
		*/

	}


	/* below here, simple utility function */

	private Peer get_peer(string uid)
	{
		foreach(Peer p in peer_list)
		{
			if (p.uid == uid)
				return p;
		}
		return null;
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
}
