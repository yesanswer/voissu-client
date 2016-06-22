using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
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

	private string guid;
	private string uid;
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
		this.uid = string.Format ("{0,-36}", uid);
		this.guid = null;
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

	public void enter_channel_async (string channel_id)
	{
		Debug.Log ("enter_channel_async call");
		StartCoroutine (enter_channel_coroutine (channel_id));
	}

	private IEnumerator enter_channel_coroutine (string channel_id)
	{
		DataPacket dp = null;
		JSONObject obj = new JSONObject ();
		string private_ip = get_private_ip ();
		if (private_ip == null) {
			Debug.Log ("get private ip faild");
			yield break;
		}
		obj.Add ("type", PROTOCOL.REQUEST_TYPE_ENTER_CHANNEL);
		obj.Add ("guid", guid);
		obj.Add ("uid", uid);
		obj.Add ("private_udp_address", private_ip);
		obj.Add ("channel_id", channel_id);
		control_channel.send_message (obj);
		status = VOIP_STATUS.ENTERRING1;

		for (int i = 0; i < 3; i++) {
			dp = new DataPacket (guid, 0, 0, new byte[0]);
			data_channel.send_message (GLOBAL.SERVER_IP, GLOBAL.DATA_PORT, dp);
			yield return new WaitForSeconds (0.5f);
			if (status == VOIP_STATUS.ENTERRING2)
				break;
		}

		if (status != VOIP_STATUS.ENTERRING2) {
			Debug.Log ("enter channel timeout");
			yield break;
		}

		foreach (Peer peer in peer_list) {
			Debug.Log ("<peer> private ip: " + peer.private_ip + " public ip: " + peer.public_ip + " public port: " + peer.public_port);
			yield return StartCoroutine (peer.p2p_connect (data_channel));
		}
	}

	/// <summary>
	/// Gets the private ip on success, null on fail
	/// </summary>
	private string get_private_ip ()
	{
		Debug.Log (Dns.GetHostName ());
		IPAddress[] address = Dns.GetHostAddresses (Dns.GetHostName ());
		if (address.Length > 0) {
			return address [address.Length - 1].ToString ();
		} else {
			return null;
		}
	}

	private void execute_packet (JSONObject obj)
	{
		Debug.Log ("execute_packet");

		int type = (int)obj.GetNumber ("type");
		switch (type) {
		case PROTOCOL.RESPONSE_TYPE_SIGN_IN:
			bool success = obj.GetBoolean ("success");
			if (success) {
				guid = obj.GetString ("guid");
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
				Debug.Log ("other user join channel");
				string private_ip = obj.GetString ("private_udp_address");
				JSONObject public_address = obj.GetObject ("public_udp_address");
				string public_ip = public_address.GetString ("ip");
				int public_port = (int)public_address.GetNumber ("port");

				Peer peer = new Peer (uid, public_ip, public_port, private_ip, GLOBAL.DATA_PORT);
				peer_list.Add (peer);
				StartCoroutine (peer.p2p_connect (data_channel));

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
			Debug.Log ("recv packet: " + obj.ToString ());
			execute_packet (obj);
		}
		DataPacket dp = null;
		while (true) {
			dp = data_channel.receive_message ();
			if (dp == null)
				break;
			Debug.Log ("receive udp message: ");
			if (dp.type == 1) {
				byte[] data = dp.data;
				string str = string.Format ("{0} {1} {2} {3}", dp.id, dp.type, dp.seq, Encoding.UTF8.GetString (data));
				Debug.Log (str);

			}
		}
	}
}
