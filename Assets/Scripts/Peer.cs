using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Timers;
using System.Text;

public enum CONNECTION_STATUS
{
	UNDEFINED,
	CONNECTING,
	CONNECTED,
	FAIL
}

public class Peer
{
	public string uid;
	public string public_ip;
	public int public_port;
	public string private_ip;
	public int private_port;
	public CONNECTION_STATUS connection_status;

	public Peer(string uid, string public_ip, int public_port, 
		string private_ip, int private_port)
	{
		this.uid = uid;
		this.public_ip = public_ip;
		this.public_port = public_port;
		this.private_ip = private_ip;
		this.private_port = private_port;
		connection_status = CONNECTION_STATUS.UNDEFINED;
	}

	public IEnumerator p2p_connect(DataChannel data_channel)
	{
		DataPacket dp = new DataPacket (VoIPManager.Instance.my_uid, 1, 1, Encoding.UTF8.GetBytes("Hello world!!"));
		Debug.Log ("peer.p2p_connect call");

		for (int i = 0; i < 10; i++) 
		{
			Debug.Log ("private udp send " + private_ip + " " + private_port);
			data_channel.send_message (private_ip, private_port, dp);
			yield return new WaitForSeconds (0.3f);
		}

		for (int i = 0; i < 10; i++) 
		{
			Debug.Log ("public udp send");
			data_channel.send_message (public_ip, public_port, dp);
			yield return new WaitForSeconds (0.3f);
		}

	}


}