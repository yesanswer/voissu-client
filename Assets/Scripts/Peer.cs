using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Timers;
using System.Text;

public enum PEER_STATUS
{
	UNDEFINED,
	PRIVATE_CONNECTING,
	PRIVATE_CONNECTED,
	PUBLIC_CONNECTED,
	PUBLIC_CONNECTING,
	RELAY_CONNECTED
}

public class Peer
{
	public string uid;
	public string public_ip;
	public int public_port;
	public string private_ip;
	public int private_port;
	public PEER_STATUS connection_status;

	public Peer(string uid, string public_ip, int public_port, 
		string private_ip, int private_port)
	{
		this.uid = uid;
		this.public_ip = public_ip;
		this.public_port = public_port;
		this.private_ip = private_ip;
		this.private_port = private_port;
		connection_status = PEER_STATUS.UNDEFINED;
	}

	public IEnumerator p2p_connect(DataChannel data_channel)
	{
		DataPacket dp = new DataPacket (VoIPManager.Instance.my_uid, 1, 1, Encoding.UTF8.GetBytes("Hello world!!"));
		Debug.Log ("peer.p2p_connect call " + VoIPManager.Instance.my_uid);

		connection_status = PEER_STATUS.PRIVATE_CONNECTING;
		for (int i = 0; i < 5; i++) 
		{
			Debug.Log ("private udp send " + private_ip + " " + private_port);
			data_channel.send_message (private_ip, private_port, dp);
			yield return new WaitForSeconds (0.3f);
			if (connection_status != PEER_STATUS.PRIVATE_CONNECTED)
				yield break;	
		}

		connection_status = PEER_STATUS.PUBLIC_CONNECTING;
		for (int i = 0; i < 5; i++) 
		{
			Debug.Log ("public udp send");
			data_channel.send_message (public_ip, public_port, dp);
			yield return new WaitForSeconds (0.3f);
			if (connection_status != PEER_STATUS.PUBLIC_CONNECTED)
				yield break;	
		}

		connection_status = PEER_STATUS.RELAY_CONNECTED;
			
	}

	public void send_data(DataPacket dp, DataChannel data_channel)
	{
		if (connection_status == PEER_STATUS.PRIVATE_CONNECTED) {
			data_channel.send_message (private_ip, private_port, dp);
		} else if (connection_status == PEER_STATUS.PUBLIC_CONNECTED) {
			data_channel.send_message (public_ip, public_port, dp);
		} else if (connection_status == PEER_STATUS.RELAY_CONNECTED) {
			dp.id = VoIPManager.Instance.my_guid;
			data_channel.send_message (GLOBAL.SERVER_IP, GLOBAL.DATA_PORT, dp);
		} else {
			Debug.Log ("send to not connected peer");
		}
	}

}