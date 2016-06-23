using UnityEngine;
using System.Collections;
using Boomlagoon.JSON;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;

public class DataChannel
{
	private UdpClient channel;
	private Queue message_queue;

	private static DataChannel instance;
	private static GameObject container;


	public DataChannel ()
	{
		message_queue = new Queue ();
		channel = new UdpClient ();
		channel.Client.Bind (new IPEndPoint (IPAddress.Parse ("0.0.0.0"), GLOBAL.DATA_PORT));
		channel.BeginReceive (receive_complete, null);
	}

	public void send_message (string ip, int port, DataPacket dp)
	{
		byte[] buf = dp.buf;
		channel.BeginSend (buf, buf.Length, ip, port, send_complete, null);
	}

	private void send_complete (IAsyncResult ar)
	{
		channel.EndSend (ar);
	}

	private void receive_complete (IAsyncResult ar)
	{
		IPEndPoint addr = null;
		byte[] buf = channel.EndReceive (ar, ref addr);
		DataPacket dp = new DataPacket (buf);
		message_queue.Enqueue (dp);
		channel.BeginReceive (receive_complete, null);
	}

	public DataPacket receive_message ()
	{
		if (message_queue.Count == 0) {
			return null;
		}

		return message_queue.Dequeue () as DataPacket;
	}
}
