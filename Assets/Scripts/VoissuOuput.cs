﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using NSpeex;

public class VoissuOutput : MonoBehaviour {
    public const int samplingRate = 8000;
    public const int samplingSize = 320;
    // public const int samplingRate = 16000;
    // public const int samplingSize = 640;
    public const int samplingCount = 10;
    public const int minPlaySamplingCount = 5;
    public const int maxPlaySamplingCount = 10;

    MainDevice mainDevice;

    // AudioItem 
    public class AudioItem {
        public AudioSource playAudio = null;
        public int prevTimeSamples = 0;
        public int remainedSamples = 0;
        public Queue<KeyValuePair<byte[], int>> streamPool = null;
        public float[] clipData = null;
        public int clipOffset = 0;
        public SpeexDecoder speexDecoder;

        // Memory Optimaization
        short[] decodedFrame = null;
        float[] fdecodedFrame = null;

        public void Update (VoissuOutput vo) {
            if (this.playAudio.isPlaying) {
                if (this.playAudio.timeSamples >= this.prevTimeSamples) {
                    this.remainedSamples -= (this.playAudio.timeSamples - this.prevTimeSamples);

                    if (this.remainedSamples <= 0) {
                        // Debug.Log("this.remainedSamples <= 0" + this.playAudio.timeSamples + "----" + this.clipOffset + "--" + Time.time);
                        this.playAudio.Stop();
                        this.playAudio.time = 0;
                        this.remainedSamples = 0;
                        this.clipOffset = 0;
                    }
                }

                this.prevTimeSamples = this.playAudio.timeSamples;

            } else {
                this.playAudio.time = 0;
                this.prevTimeSamples = 0;
            }

            while (this.streamPool.Count > 0) {
                KeyValuePair<byte[], int> streamData = this.streamPool.Dequeue();
                byte[] samples = streamData.Key;
                int samplingBufferSize = streamData.Value;

                if (this.decodedFrame == null) {
                    this.decodedFrame = new short[samplingBufferSize]; // should be the same number of samples as on the capturing side
                }

                int len = this.speexDecoder.Decode(samples, 0, samples.Length, decodedFrame, 0, false);

                if (this.fdecodedFrame == null) {
                    this.fdecodedFrame = new float[this.decodedFrame.Length];
                }

                float[] fsamples = Util.ToFloatArray(decodedFrame, this.fdecodedFrame);
                Array.Copy(fsamples, 0, this.clipData, this.clipOffset, len);
                this.playAudio.clip.SetData(this.clipData, 0);

                this.clipOffset += len;
                if (this.clipOffset >= this.playAudio.clip.samples) {
                    this.clipOffset = 0;
                }

                this.remainedSamples += len;

                if (!this.playAudio.isPlaying) {
                    if (this.remainedSamples >= (VoissuOutput.samplingSize * VoissuOutput.minPlaySamplingCount)) {
                        this.playAudio.Play();
                    }
                }
            }

            // delay remove
            if (this.playAudio.isPlaying) {
                if (this.remainedSamples / VoissuOutput.samplingSize > minPlaySamplingCount) {
                    while (this.remainedSamples / VoissuOutput.samplingSize >= minPlaySamplingCount) {
                        int timeSamples = this.playAudio.timeSamples;
                        timeSamples += VoissuOutput.samplingSize;
                        this.playAudio.timeSamples = timeSamples % this.playAudio.clip.samples;
                        this.prevTimeSamples = this.playAudio.timeSamples;

                        this.remainedSamples -= VoissuOutput.samplingSize;
                        if (this.remainedSamples < 0) {
                            this.remainedSamples = 0;
                        }
                    }
                }
            }
        }
    }

    Dictionary<string, AudioItem> audioItemDict;

    // Use this for initialization
    void Awake () {
        this.mainDevice = GameObject.Find("Main Device").GetComponent<MainDevice>();
        this.audioItemDict = new Dictionary<string, AudioItem>();
    }

    void Update () {
        foreach (KeyValuePair<string, AudioItem> kv in this.audioItemDict) {
            kv.Value.Update(this);
        }
    }

    public AudioItem AddAudioItem(string key, int channel) {
        if (this.audioItemDict.ContainsKey(key)) {
            return null;
        }

        int size = VoissuOutput.samplingRate * VoissuOutput.samplingCount;

        AudioItem item = new AudioItem();
        item.playAudio = this.gameObject.AddComponent<AudioSource>();
        item.playAudio.clip = AudioClip.Create(key, size, channel, VoissuOutput.samplingRate, false);
        item.streamPool = new Queue<KeyValuePair<byte[], int>>();
        item.speexDecoder = new SpeexDecoder(BandMode.Narrow);
        item.clipData = new float[size];
        item.clipOffset = 0;
        item.prevTimeSamples = 0;
        item.remainedSamples = 0;

        this.audioItemDict.Add(key, item);
        return item;
    }

    public void DelAudioItem(string key) {
        if (this.audioItemDict.ContainsKey(key)) {
            return;
        }

        AudioItem item = this.audioItemDict[key];
        if (item.playAudio) {
            if (item.playAudio.isPlaying) {
                item.playAudio.Stop();
            }
            GameObject.Destroy(item.playAudio);
        }

        this.audioItemDict.Remove(key);
    }

    public void AddSamplingData(string key, byte[] encryptStream, int samplingBufferSize) {
        if (!this.audioItemDict.ContainsKey(key)) {
            return;
        }

        AudioItem item = this.audioItemDict[key];
        item.streamPool.Enqueue(new KeyValuePair<byte[], int>(encryptStream, samplingBufferSize));
    }
}
