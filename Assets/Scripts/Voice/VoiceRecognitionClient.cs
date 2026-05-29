using System;
using System.Collections;
using System.Text;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Events;

public sealed class VoiceRecognitionClient : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string websocketUrl = "ws://127.0.0.1:3000/ws/live";
    [SerializeField] private bool connectOnStart = true;
    [SerializeField] private bool streamMicrophoneOnConnect = true;

    [Header("Microphone")]
    [SerializeField] private string microphoneDevice;
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int recordingBufferSeconds = 10;
    [SerializeField] private float chunkSeconds = 0.1f;

    [Header("Events")]
    public TranscriptEvent onInputTranscript;
    public TranscriptEvent onOutputTranscript;
    public StatusEvent onStatusChanged;
    public AudioEvent onAudioReceived;

    private WebSocket websocket;
    private AudioClip microphoneClip;
    private Coroutine streamCoroutine;
    private int lastSamplePosition;
    private bool microphoneStreaming;

    public bool IsConnected => websocket != null && websocket.State == WebSocketState.Open;
    public bool IsMicrophoneStreaming => microphoneStreaming;

    private async void Start()
    {
        if (connectOnStart)
        {
            await Connect();
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        await Disconnect();
    }

    public async System.Threading.Tasks.Task Connect()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            return;
        }

        websocket = new WebSocket(websocketUrl);
        websocket.OnOpen += HandleOpen;
        websocket.OnMessage += HandleMessage;
        websocket.OnError += error => PublishStatus("WebSocket error: " + error);
        websocket.OnClose += code => PublishStatus("WebSocket closed: " + code);

        PublishStatus("Connecting to voice backend...");
        await websocket.Connect();
    }

    public async System.Threading.Tasks.Task Disconnect()
    {
        StopMicrophoneStream();

        if (websocket == null)
        {
            return;
        }

        await websocket.Close();
        websocket = null;
    }

    public void StartMicrophoneStream()
    {
        if (microphoneStreaming)
        {
            return;
        }

        if (!IsConnected)
        {
            PublishStatus("Cannot start microphone: WebSocket is not connected.");
            return;
        }

        string device = SelectedMicrophoneDevice();
        microphoneClip = Microphone.Start(device, true, recordingBufferSeconds, sampleRate);
        lastSamplePosition = 0;
        microphoneStreaming = true;
        streamCoroutine = StartCoroutine(StreamMicrophone());
        PublishStatus("Microphone streaming started.");
    }

    public async void StopMicrophoneStream()
    {
        if (!microphoneStreaming)
        {
            return;
        }

        microphoneStreaming = false;

        if (streamCoroutine != null)
        {
            StopCoroutine(streamCoroutine);
            streamCoroutine = null;
        }

        string device = SelectedMicrophoneDevice();
        if (Microphone.IsRecording(device))
        {
            Microphone.End(device);
        }

        microphoneClip = null;
        await SendAudioStreamEnd();
        PublishStatus("Microphone streaming stopped.");
    }

    private void HandleOpen()
    {
        PublishStatus("Voice backend connected.");

        if (streamMicrophoneOnConnect)
        {
            StartMicrophoneStream();
        }
    }

    private void HandleMessage(byte[] bytes)
    {
        string json = Encoding.UTF8.GetString(bytes);
        VoiceServerMessage message = JsonUtility.FromJson<VoiceServerMessage>(json);

        switch (message.type)
        {
            case "ready":
                PublishStatus("Gemini Live ready: " + message.model);
                break;
            case "input_transcript":
                if (!string.IsNullOrWhiteSpace(message.text))
                {
                    onInputTranscript?.Invoke(message.text);
                }
                break;
            case "output_transcript":
                if (!string.IsNullOrWhiteSpace(message.text))
                {
                    onOutputTranscript?.Invoke(message.text);
                }
                break;
            case "audio":
                if (!string.IsNullOrWhiteSpace(message.data))
                {
                    onAudioReceived?.Invoke(Convert.FromBase64String(message.data));
                }
                break;
            case "turn_complete":
                PublishStatus("Voice turn complete.");
                break;
            case "interrupted":
                PublishStatus("Voice response interrupted.");
                break;
            case "error":
                PublishStatus("Voice backend error: " + message.message);
                break;
        }
    }

    private IEnumerator StreamMicrophone()
    {
        int chunkFrames = Mathf.Max(1, Mathf.RoundToInt(sampleRate * chunkSeconds));
        var wait = new WaitForSeconds(chunkSeconds * 0.5f);

        while (microphoneStreaming)
        {
            int currentPosition = Microphone.GetPosition(SelectedMicrophoneDevice());
            if (currentPosition < 0 || microphoneClip == null)
            {
                yield return wait;
                continue;
            }

            int availableFrames = GetAvailableFrames(currentPosition);
            while (availableFrames >= chunkFrames)
            {
                float[] samples = ReadFrames(lastSamplePosition, chunkFrames);
                byte[] pcm = ConvertTo16BitMonoPcm(samples, microphoneClip.channels);
                SendAudioChunk(pcm);

                lastSamplePosition = (lastSamplePosition + chunkFrames) % microphoneClip.samples;
                availableFrames -= chunkFrames;
            }

            yield return wait;
        }
    }

    private int GetAvailableFrames(int currentPosition)
    {
        if (currentPosition >= lastSamplePosition)
        {
            return currentPosition - lastSamplePosition;
        }

        return microphoneClip.samples - lastSamplePosition + currentPosition;
    }

    private float[] ReadFrames(int startFrame, int frameCount)
    {
        int channels = microphoneClip.channels;
        int framesUntilWrap = microphoneClip.samples - startFrame;

        if (frameCount <= framesUntilWrap)
        {
            float[] data = new float[frameCount * channels];
            microphoneClip.GetData(data, startFrame);
            return data;
        }

        int firstFrameCount = framesUntilWrap;
        int secondFrameCount = frameCount - firstFrameCount;

        float[] first = new float[firstFrameCount * channels];
        float[] second = new float[secondFrameCount * channels];
        microphoneClip.GetData(first, startFrame);
        microphoneClip.GetData(second, 0);

        float[] combined = new float[frameCount * channels];
        Array.Copy(first, combined, first.Length);
        Array.Copy(second, 0, combined, first.Length, second.Length);
        return combined;
    }

    private byte[] ConvertTo16BitMonoPcm(float[] samples, int channels)
    {
        int frameCount = samples.Length / channels;
        byte[] pcm = new byte[frameCount * 2];

        for (int frame = 0; frame < frameCount; frame++)
        {
            float mono = 0f;
            int offset = frame * channels;

            for (int channel = 0; channel < channels; channel++)
            {
                mono += samples[offset + channel];
            }

            mono = Mathf.Clamp(mono / channels, -1f, 1f);
            short value = (short)Mathf.RoundToInt(mono * short.MaxValue);
            int byteIndex = frame * 2;
            pcm[byteIndex] = (byte)(value & 0xff);
            pcm[byteIndex + 1] = (byte)((value >> 8) & 0xff);
        }

        return pcm;
    }

    private async void SendAudioChunk(byte[] pcm)
    {
        if (!IsConnected)
        {
            return;
        }

        var message = new VoiceAudioMessage
        {
            type = "audio",
            data = Convert.ToBase64String(pcm),
            mime_type = $"audio/pcm;rate={sampleRate}"
        };

        await websocket.SendText(JsonUtility.ToJson(message));
    }

    private async System.Threading.Tasks.Task SendAudioStreamEnd()
    {
        if (!IsConnected)
        {
            return;
        }

        await websocket.SendText(JsonUtility.ToJson(new VoiceControlMessage { type = "audio_stream_end" }));
    }

    private void PublishStatus(string message)
    {
        Debug.Log("[VoiceRecognitionClient] " + message);
        onStatusChanged?.Invoke(message);
    }

    private string SelectedMicrophoneDevice()
    {
        return string.IsNullOrWhiteSpace(microphoneDevice) ? null : microphoneDevice;
    }

    [Serializable]
    private sealed class VoiceServerMessage
    {
        public string type;
        public string text;
        public string data;
        public string message;
        public string model;
    }

    [Serializable]
    private sealed class VoiceAudioMessage
    {
        public string type;
        public string data;
        public string mime_type;
    }

    [Serializable]
    private sealed class VoiceControlMessage
    {
        public string type;
    }
}

[Serializable]
public sealed class TranscriptEvent : UnityEvent<string>
{
}

[Serializable]
public sealed class StatusEvent : UnityEvent<string>
{
}

[Serializable]
public sealed class AudioEvent : UnityEvent<byte[]>
{
}
