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
	private bool ready;

	public DataChannel (int local_port)
	{
		message_queue = new Queue ();
		channel = new UdpClient ();
		channel.Client.Bind (new IPEndPoint (IPAddress.Parse ("0.0.0.0"), local_port));
		channel.BeginReceive (receive_complete, null);
		ready = true;
	}

	public void send_message (string ip, int port, DataPacket dp)
	{
		if (ready) {
			byte[] buf = dp.buf;
			channel.BeginSend (buf, buf.Length, ip, port, send_complete, null);
		}
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
		if(dp.type != PROTOCOL.UDP_DATA)
			Debug.Log ("udp data receive: " + dp.id + " " + dp.type + " " + addr);
		channel.BeginReceive (receive_complete, null);
	}

	public DataPacket receive_message ()
	{
		if (ready == false)
			return null;
		if (message_queue.Count == 0) 
			return null;

		return message_queue.Dequeue () as DataPacket;
	}

	public void close_channel()
	{
		message_queue.Clear ();
		channel.Close ();
		ready = false;
	}


}
