using UnityEngine;
using System.Collections;
using Boomlagoon.JSON;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;

public class ControlChannel
{

	private TcpClient channel;
	private NetworkStream the_stream;
	private StreamReader the_reader;
	private StreamWriter the_writer;
	private StringBuilder message_buffer;
	private char[] tmp_buffer;
	private Queue message_queue;
	bool ready;

	public ControlChannel (string ip, int port, Action<bool> callback)
	{
		channel = new TcpClient ();
		message_buffer = new StringBuilder (10000);
		tmp_buffer = new char[4096];
		message_queue = new Queue ();
		ready = false;
		VoIPManager.instance.StartCoroutine (connect_coroutine (ip, port, callback));
	}

	private IEnumerator connect_coroutine (string ip, int port, Action<bool> callback)
	{
		Debug.Log ("connect coroutine start");
		channel.BeginConnect (ip, port, null, null);

		for (int i = 0; i < 10 && !channel.Connected; i++) {
			// check tcp connection with 0.5 seconds interval
			yield return new WaitForSeconds (0.5f);
		}

		if (channel.Connected == false) {
			// tcp connect timeout
			channel.Close ();
			callback (false);
			yield break;
		}

		the_stream = channel.GetStream ();
	
		the_reader = new StreamReader (the_stream, Encoding.UTF8);
		the_writer = new StreamWriter (the_stream, Encoding.UTF8);
		ready = true;
		callback (true);
	}

	public JSONObject receive_message ()
	{
		try {
			if (ready && the_stream.DataAvailable) {
				int len = the_reader.Read (tmp_buffer, 0, tmp_buffer.Length);
				if (len > 0) {
					message_buffer.Append (tmp_buffer, 0, len);
				}

				int index = -1;
				while (true) {
					String str = message_buffer.ToString ();
					index = str.IndexOf ('\n');
					if (index == -1) {
						break;
					}

					String line = str.Substring (0, index);
					message_queue.Enqueue (JSONObject.Parse (line));
					message_buffer = new StringBuilder (str.Substring (index + 1));
				}
			}
		}
		catch(Exception e){
			Debug.Log (e.Message);
			this.close_channel ();
		}

		if (message_queue.Count == 0)
			return null;
		else
			return message_queue.Dequeue () as JSONObject;
		return null;
	}

	public void send_message (JSONObject obj)
	{
		if (ready) {
			the_writer.WriteLine (obj.ToString ());
			the_writer.Flush ();
		}
		else {
			Debug.Log ("ControlChannel.send_message call on not ready channel");
		}
	}

	public void close_channel()
	{
		the_reader.Close ();
		the_writer.Close ();
		the_stream.Close ();
		channel.Close ();
		ready = false;
	}
		
}
