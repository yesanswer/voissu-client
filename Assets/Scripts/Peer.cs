using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Timers;

public enum CONNECTION_STATUS
{
	UNDEFINED,
	CONNECTING1,
	CONNECTING2,
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



}