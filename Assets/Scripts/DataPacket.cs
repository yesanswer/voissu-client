using System;
using System.Text;
using System.Collections.Generic;
/*
public class DataPacket
{
	private static Stack<DataPacket> pool;
	private byte[] buf;
	private int len;

	private DataPacket(int size)
	{
		buf = new byte[size];
		len = 0;
	}

	public static DataPacket Instance
	{
		get{
			DataPacket dp = null;
			if (pool.Count == 0)
				dp = new DataPacket (10000);
			else
				dp = pool.Pop ();
			return dp;
		}
	}

	public void close()
	{
		len = 0;
		pool.Push (this);
	}
	public void set_data(string id, int type, int seq, byte[] data)
	{
		byte[] byte_id = Encoding.UTF8.GetBytes (id);
		byte[] byte_type = BitConverter.GetBytes (type);
		byte[] byte_seq = BitConverter.GetBytes (seq);

		Buffer.BlockCopy (byte_id, 0, buf, 0, 36);
		Buffer.BlockCopy (byte_type, 0, buf, 36, 4);
		Buffer.BlockCopy (byte_seq, 0, buf, 40, 4);
		Buffer.BlockCopy (data, 0, buf, 44, data.Length);
		len = data.Length + 44;
	}

	public void set_data(byte[] b)
	{
	}

	public byte[] wrap_packet()
	{
		byte[] ret = new byte[len];
		Buffer.BlockCopy (buf, 0, ret, 0, len);
		return ret;
	}

}

*/

public class DataPacket
{
	public byte[] buf;

	public DataPacket(byte[] buf)
	{
		this.buf = buf;
	}

	public DataPacket(string id, int type, int seq, byte[] data)
	{
		byte[] byte_id = Encoding.UTF8.GetBytes (id);
		byte[] byte_type = BitConverter.GetBytes (type);
		byte[] byte_seq = BitConverter.GetBytes (seq);
		buf = new byte[44 + data.Length];
		Buffer.BlockCopy (byte_id, 0, buf, 0, 36);
		Buffer.BlockCopy (byte_type, 0, buf, 36, 4);
		Buffer.BlockCopy (byte_seq, 0, buf, 40, 4);
		Buffer.BlockCopy (data, 0, buf, 44, data.Length);
	}

	public string id
	{
		get
		{
			return Encoding.UTF8.GetString (buf, 0, 36);
		}
	}

	public int type
	{
		get
		{
			return BitConverter.ToInt32 (buf, 36);
		}
	}

	public int seq
	{
		get
		{
			return BitConverter.ToInt32 (buf, 40);
		}
	}	
		
	public byte[] data
	{
		get 
		{
			byte[] ret = new byte[buf.Length - 44];
			Buffer.BlockCopy (buf, 0, ret, 0, buf.Length - 44);
			return ret;
		}
	}
}
