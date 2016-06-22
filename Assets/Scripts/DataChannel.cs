using UnityEngine;
using System.Collections;
using Boomlagoon.JSON;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;

public class DataChannel : MonoBehaviour
{
	private UdpClient channel;
	private Queue message_queue;
	private bool ready;

	private static DataChannel instance;
	private static GameObject container;

	private DataChannel()
	{
		Debug.Log ("data channel instancing");
		message_queue = new Queue ();

		channel = new UdpClient ();
		channel.BeginReceive (receive_complete, null);
		ready = false;
	}

	public static DataChannel Instance 
	{  
		get {
			if (!instance) {  
				container = new GameObject ();  
				container.name = "DataChannel";  
				instance = container.AddComponent (typeof(DataChannel)) as DataChannel;  
				DontDestroyOnLoad(container);
				Application.runInBackground = true;
			}

			return instance;  
		}
	}

	public void send_message(string ip, int port, DataPacket dp)
	{
		byte[] buf = dp.buf;
		channel.BeginSend (buf, buf.Length, ip, port, send_complete, null);
	}

	private void send_complete(IAsyncResult ar)
	{
		channel.EndSend (ar);			
	}

	private void receive_complete(IAsyncResult ar)
	{
		IPEndPoint addr = null;
		byte[] buf = channel.EndReceive (ar, ref addr);
		DataPacket dp = new DataPacket (buf);
		message_queue.Enqueue (dp);
		channel.BeginReceive (receive_complete, null);
	}
		
	public DataPacket receive_message()
	{
		if (message_queue.Count == 0)
			return null;
		else
			return message_queue.Dequeue () as DataPacket;
	}
		
}
