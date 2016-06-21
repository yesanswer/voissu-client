using UnityEngine;
using System.Collections;
using Boomlagoon.JSON;
using System.Net;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;
public class ControlChannel : MonoBehaviour
{

	private TcpClient channel;
	private NetworkStream the_stream;
	private StreamReader the_reader;
	private StreamWriter the_writer;
	private StringBuilder message_buffer;
	private char[] tmp_buffer;
	private Queue message_queue;

	private bool ready;

	private static ControlChannel instance;
	private static GameObject container;

	private ControlChannel()
	{
		Debug.Log ("instancing");
		channel = new TcpClient ();
		message_buffer = new StringBuilder (10000);
		tmp_buffer = new char[4096];
		message_queue = new Queue ();
		ready = false;
	}

	public static ControlChannel Instance 
	{  
		get {
			if (!instance) {  
				container = new GameObject ();  
				container.name = "ControlChannel";  
				instance = container.AddComponent (typeof(ControlChannel)) as ControlChannel;  
				DontDestroyOnLoad(container);
				Application.runInBackground = true;
			}

			return instance;  
		}
	}


	/// <summary>
	/// connect to control channel async
	/// </summary>
	/// <param name="callback"> called on connected or timeout. parameter is connection true/false </param>
	public void connect_async(async_callback callback)
	{
		Debug.Log ("connect async call");
		StartCoroutine (connect_coroutine (GLOBAL.SERVER_IP, callback));
	}

	private IEnumerator connect_coroutine(string ip, async_callback callback)
	{
		Debug.Log ("connect coroutine start");
		channel.BeginConnect (GLOBAL.SERVER_IP, GLOBAL.PORT, null, null);

		for (int i = 0; i < 10; i++) 
		{
			// check tcp connection with 0.5 seconds interval
			yield return new WaitForSeconds (0.5f);
			if(channel.Connected == true) 
				break;
		}

		if (channel.Connected == false) 
		{
			// tcp connect timeout
			channel.Close ();
			callback (false);
			return false;
		}

		the_stream = channel.GetStream ();
	
		the_reader = new StreamReader (the_stream, Encoding.UTF8);
		the_writer = new StreamWriter (the_stream, Encoding.UTF8);
		ready = true;
		callback (true);
	}

	void Update()
	{
	//	Debug.Log ("ready" + ready + "data" + the_stream.DataAvailable);
		if (ready && the_stream.DataAvailable)
		{
			int len = the_reader.Read (tmp_buffer, 0, tmp_buffer.Length);
			if (len > 0)
				message_buffer.Append (tmp_buffer, 0, len);

			int index = -1;
			while (true) 
			{
				String str = message_buffer.ToString ();
				index = str.IndexOf ('\n');
				if (index == -1)
					break;

				String line = str.Substring (0, index);
				message_queue.Enqueue (JSONObject.Parse (line));
				message_buffer = new StringBuilder (str.Substring (index + 1));
			}

		}
	}

	public JSONObject receive_message()
	{
		if (message_queue.Count == 0)
			return null;
		else
			return message_queue.Dequeue () as JSONObject;
	}

	public void send_message(JSONObject obj)
	{
		if (obj is JSONObject) 
		{
			the_writer.WriteLine (obj.ToString ());
			the_writer.Flush ();
		}	
	}
		
}
