using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NSpeex;
using System;

[RequireComponent(typeof(AudioSource))]
public class VoissuInput : MonoBehaviour {
    public const int loopTIme = 1;
    public const int samplingRate = 44100;

    // Component
    MainDevice mainDevice;
    AudioSource recordAudio = null;

    // Variable
    int ouputSamplingSize = 0;
    int ouputSamplingRate = 0;
    int lastSamplePos = 0;
    int totalSampleSize = 0;

    // NSpeex
    SpeexEncoder speexEncoder;
    int recordSampleSize = 0;
    float[] sampleBuffer = null;
    int sampleIndex = 0;

    // Handler
    public delegate void OnRecordListener (byte[] encryptStream, int samplingBufferSize);
    event OnRecordListener onRecordListener;

    void Awake () {
        this.mainDevice = GameObject.Find("Main Device").GetComponent<MainDevice>();
        this.recordAudio = this.gameObject.AddComponent<AudioSource>();
        this.lastSamplePos = 0;
    }

    void OnDisable () {
        RecordEnd();
    }

    void Update () {
        if (recordAudio && Microphone.IsRecording(null)) {
            int currentPosition = Microphone.GetPosition(null);

            // This means we wrapped around
            if (currentPosition < lastSamplePos) {
                while (sampleIndex < samplingRate) {
                    ReadSample();
                }

                sampleIndex = 0;
            }

            // Read non-wrapped samples
            lastSamplePos = currentPosition;

            while (sampleIndex + recordSampleSize <= currentPosition) {
                ReadSample();
            }
        }
    }

    void ReadSample () {
        // Extract data
        recordAudio.clip.GetData(sampleBuffer, sampleIndex);

        // Grab a new sample buffer
        float[] targetSampleBuffer = new float[this.ouputSamplingSize];

        // Resample our real sample into the buffer
        Resample(sampleBuffer, targetSampleBuffer);

        // Forward index
        sampleIndex += recordSampleSize;

        short[] data = ToShortArray(targetSampleBuffer);
        byte[] buf = new byte[recordSampleSize * 4];
        int len = speexEncoder.Encode(data, 0, data.Length, buf, 0, buf.Length);
        if (len != 0) {
            this.onRecordListener(buf.Take(len).ToArray(), buf.Length);
        }

        totalSampleSize += (recordSampleSize * 4);
    }

    void Resample (float[] src, float[] dst) {
        if (src.Length == dst.Length) {
            Array.Copy(src, 0, dst, 0, src.Length);
        } else {
            //TODO: Low-pass filter 
            float rec = 1.0f / (float)dst.Length;

            for (int i = 0; i < dst.Length; ++i) {
                float interp = rec * (float)i * (float)src.Length;
                dst[i] = src[(int)interp];
            }
        }
    }

    float GetAveragedVolume () {
        float[] data = new float[256];
        float a = 0;
        recordAudio.GetOutputData(data, 0);
        foreach (float s in data) {
            a += Mathf.Abs(s);
        }

        return a / 256;
    }

    void ShowMicrophoneList () {
        this.mainDevice.log("Device List:");
        foreach (string device in Microphone.devices) {
            this.mainDevice.log(device);
        }
    }

    void ShowMicrophoneDeviceCaps (string deviceName) {
        int minFreq;
        int maxFreq;
        Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);
        this.mainDevice.log("max freq : " + maxFreq);
    }

    byte[] ToByteArray (float[] floatArray) {
        int len = floatArray.Length * 4;
        byte[] byteArray = new byte[len];
        int count = 0;
        foreach (float f in floatArray) {
            byte[] data = System.BitConverter.GetBytes(f);
            System.Array.Copy(data, 0, byteArray, count, 4);
            count += 4;
        }
        return byteArray;
    }

    byte[] ToByteArray (short[] shortArray) {
        int len = shortArray.Length * 2;
        byte[] byteArray = new byte[len];
        int count = 0;
        foreach (short s in shortArray) {
            byte[] data = System.BitConverter.GetBytes(s);
            System.Array.Copy(data, 0, byteArray, count, 2);
            count += 2;
        }
        return byteArray;
    }


    float[] ToFloatArray (byte[] byteArray) {
        int len = byteArray.Length / 4;
        float[] floatArray = new float[len];
        for (int i = 0; i < byteArray.Length; i += 4) {
            floatArray[i / 4] = System.BitConverter.ToSingle(byteArray, i);
        }
        return floatArray;
    }

    short[] ToShortArray (byte[] byteArray) {
        int len = byteArray.Length / 2;
        short[] shortArray = new short[len];
        for (int i = 0; i < byteArray.Length; i += 2) {
            shortArray[i / 2] = System.BitConverter.ToInt16(byteArray, i);
        }
        return shortArray;
    }

    short[] ToShortArray (float[] floatArray) {
        int len = floatArray.Length;
        short[] shortArray = new short[len];
        for (int i = 0; i < floatArray.Length; ++i) {
            shortArray[i] = (short)Mathf.Clamp((int)(floatArray[i] * 32767.0f), short.MinValue, short.MaxValue);
        }
        return shortArray;
    }

    float[] ToFloatArray (short[] shortArray) {
        int len = shortArray.Length;
        float[] floatArray = new float[len];
        for (int i = 0; i < shortArray.Length; ++i) {
            floatArray[i] = shortArray[i] / (float)short.MaxValue;
        }

        return floatArray;
    }

    public void RecordStart (int ouputSamplingRate, int ouputSamplingSize) {
        this.ouputSamplingSize = ouputSamplingSize;
        this.ouputSamplingRate = ouputSamplingRate;

        this.lastSamplePos = 0;
        this.totalSampleSize = 0;
        this.sampleIndex = 0;

        ShowMicrophoneList();
        recordAudio.clip = Microphone.Start(null, true, loopTIme, samplingRate);
        this.mainDevice.log("" + recordAudio.clip.length);
        ShowMicrophoneDeviceCaps(null);

        // speex
        speexEncoder = new SpeexEncoder(BandMode.Narrow);
        recordSampleSize = samplingRate / (ouputSamplingRate / ouputSamplingSize);
        sampleBuffer = new float[recordSampleSize];

        this.mainDevice.log("---RecordStart---");
    }

    public void RecordEnd () {
        if (Microphone.IsRecording(null)) {
            this.mainDevice.log("---RecordEnd---");
            Microphone.End(null);
        }
    }

    public void AddOnRecordListener (OnRecordListener listener) {
        this.onRecordListener += listener;
    }
}
