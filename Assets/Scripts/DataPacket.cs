using System;
using System.Text;
using System.Collections.Generic;


public class DataPacket
{
	public byte[] buf;

	public string id {
		set {
			string padding_id = string.Format ("{0,-36}", value);
			byte[] byte_id = Encoding.UTF8.GetBytes (padding_id);
			Buffer.BlockCopy (byte_id, 0, buf, 0, 36);

			
		}
		get {
			return Encoding.UTF8.GetString (buf, 0, 36).Trim();
		}
	}

	public int type {
		get {
			return BitConverter.ToInt32 (buf, 36);
		}
	}

	public int seq {
		get {
			return BitConverter.ToInt32 (buf, 40);
		}
	}

	public byte[] data {
		get {
			byte[] ret = new byte[buf.Length - 44];
			Buffer.BlockCopy (buf, 44, ret, 0, buf.Length - 44);
			return ret;
		}
	}


	public DataPacket (byte[] buf)
	{
		this.buf = buf;
	}

	public DataPacket (string id, int type, int seq, byte[] data)
	{
		id = string.Format ("{0,-36}", id);
		
		byte[] byte_id = Encoding.UTF8.GetBytes (id);
		byte[] byte_type = BitConverter.GetBytes (type);
		byte[] byte_seq = BitConverter.GetBytes (seq);
		buf = new byte[44 + data.Length];
		Buffer.BlockCopy (byte_id, 0, buf, 0, 36);
		Buffer.BlockCopy (byte_type, 0, buf, 36, 4);
		Buffer.BlockCopy (byte_seq, 0, buf, 40, 4);
		Buffer.BlockCopy (data, 0, buf, 44, data.Length);
	}
}
