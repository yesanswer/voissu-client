using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public class DataPacket
{
	public byte[] buf;

	public string id {
		set {
			if (value.Length <= 36) {
				string padded_id = value.PadRight (GLOBAL.ID_LEN, ' ');
				Buffer.BlockCopy (Encoding.UTF8.GetBytes (padded_id), 0, buf, 0, 36);
			}
			else {
				Debug.Log ("DataPacket.id can not have length more than 36");
			}
		}
		get {
			return Encoding.UTF8.GetString (buf, 0, 36).Trim();
		}
	}

	public int type {
		set {
			byte[] type_byte = BitConverter.GetBytes (value);
			if (BitConverter.IsLittleEndian == false)
				Array.Reverse (type_byte);

			Buffer.BlockCopy (type_byte, 0, buf, 36, 4);
		}
		get {
			byte[] type_byte = new byte[4];
			Buffer.BlockCopy (buf, 36, type_byte, 0, 4);
			if (BitConverter.IsLittleEndian == false)
				Array.Reverse (type_byte);
			return BitConverter.ToInt32 (type_byte, 0);
		}
	}

	public int seq {
		set {
			byte[] seq_byte = BitConverter.GetBytes (value);
			if (BitConverter.IsLittleEndian == false)
				Array.Reverse (seq_byte);
			Buffer.BlockCopy (seq_byte, 0, buf, 40, 4);
		}
		get {
			byte[] type_byte = new byte[4];
			Buffer.BlockCopy (buf, 40, type_byte, 0, 4);
			if (BitConverter.IsLittleEndian == false)
				Array.Reverse (type_byte);
			return BitConverter.ToInt32 (type_byte, 0);
		}
	}

	public byte[] data {
		get {
			byte[] ret = new byte[buf.Length - 44];
			Buffer.BlockCopy (buf, 44, ret, 0, buf.Length - 44);
			return ret;
		}
		set {
			Buffer.BlockCopy (value, 0, buf, 44, value.Length);
		}
	}


	public DataPacket (byte[] buf)
	{
		this.buf = buf;
	}

	public DataPacket (string _id, int _type, int _seq, byte[] _data)
	{
		buf = new byte[44 + _data.Length];
		id = _id;
		type = _type;
		seq = _seq;
		data = _data;
	}
}
