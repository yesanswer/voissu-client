using UnityEngine;
using System.Collections;

public class Util
{
	public static byte[] ToByteArray (float[] floatArray)
	{
		int len = floatArray.Length * 4;
		byte[] byteArray = new byte[len];
		int count = 0;
		foreach (float f in floatArray) {
			byte[] data = System.BitConverter.GetBytes (f);
			System.Array.Copy (data, 0, byteArray, count, 4);
			count += 4;
		}
		return byteArray;
	}

	public static byte[] ToByteArray (short[] shortArray)
	{
		int len = shortArray.Length * 2;
		byte[] byteArray = new byte[len];
		int count = 0;
		foreach (short s in shortArray) {
			byte[] data = System.BitConverter.GetBytes (s);
			System.Array.Copy (data, 0, byteArray, count, 2);
			count += 2;
		}
		return byteArray;
	}

	public static float[] ToFloatArray (byte[] byteArray)
	{
		int len = byteArray.Length / 4;
		float[] floatArray = new float[len];
		for (int i = 0; i < byteArray.Length; i += 4) {
			floatArray [i / 4] = System.BitConverter.ToSingle (byteArray, i);
		}
		return floatArray;
	}

	public static short[] ToShortArray (byte[] byteArray)
	{
		int len = byteArray.Length / 2;
		short[] shortArray = new short[len];
		for (int i = 0; i < byteArray.Length; i += 2) {
			shortArray [i / 2] = System.BitConverter.ToInt16 (byteArray, i);
		}
		return shortArray;
	}

	public static short[] ToShortArray (float[] floatArray)
	{
		int len = floatArray.Length;
		short[] shortArray = new short[len];
		for (int i = 0; i < floatArray.Length; ++i) {
			shortArray [i] = (short)Mathf.Clamp ((int)(floatArray [i] * 32767.0f), short.MinValue, short.MaxValue);
		}
		return shortArray;
	}

	public static short[] ToShortArray (float[] floatArray, short[] shortArray)
	{
		for (int i = 0; i < floatArray.Length; ++i) {
			shortArray [i] = (short)Mathf.Clamp ((int)(floatArray [i] * 32767.0f), short.MinValue, short.MaxValue);
		}

		return shortArray;
	}

	public static float[] ToFloatArray (short[] shortArray)
	{
		int len = shortArray.Length;
		float[] floatArray = new float[len];
		for (int i = 0; i < shortArray.Length; ++i) {
			floatArray [i] = shortArray [i] / (float)short.MaxValue;
		}

		return floatArray;
	}

	public static float[] ToFloatArray (short[] shortArray, float[] floatArray) {
		for (int i = 0; i < shortArray.Length; ++i) {
			floatArray[i] = shortArray[i] / (float)short.MaxValue;
		}

		return floatArray;
	}
}
