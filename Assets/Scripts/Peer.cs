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

	/// <summary>
	/// Gets the connected ip. if not connected, return null
	/// </summary>
	/// <value>The connected ip.</value>
	public string connected_ip {
		get{
			if (connection_status == PEER_STATUS.PUBLIC_CONNECTED)
				return public_ip;
			else if (connection_status == PEER_STATUS.PRIVATE_CONNECTED)
				return private_ip;
			else
				return null;
		}
	}

	/// <summary>
	/// Gets the connected port. if not connected, return -1
	/// </summary>
	/// <value>The connected port.</value>
	public int connected_port {
		get{
			if (connection_status == PEER_STATUS.PUBLIC_CONNECTED)
				return public_port;
			else if (connection_status == PEER_STATUS.PRIVATE_CONNECTED)
				return private_port;
			else
				return -1;
		}
	}

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


}