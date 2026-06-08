using System;
using System.Collections;
using Oculus.Voice;
using UnityEngine;
using UnityEngine.Events;

public sealed class VoiceRecognitionClient : MonoBehaviour
{
    [Header("Meta Voice SDK")]
    [SerializeField] private AppVoiceExperience voiceService;
    [SerializeField] private bool listenOnStart = true;
    [SerializeField] private bool activateImmediately = false;
    [SerializeField] private bool restartAfterUtterance = true;
    [SerializeField] private float restartDelaySeconds = 0.25f;
    [SerializeField] private bool logVoiceDetection = true;

    [Header("Hard-Coded LLM")]
    [SerializeField] private VoiceCommandRouter commandRouter;
    [SerializeField] private HardCodedVoiceResponse[] responses =
    {
        new HardCodedVoiceResponse
        {
            keywords = new[] { "open inventory", "show inventory", "inventory" },
            outputText = "Opening inventory.",
            commandTranscript = "open inventory"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "close inventory", "hide inventory" },
            outputText = "Closing inventory.",
            commandTranscript = "close inventory"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "stop", "pause", "wait" },
            outputText = "Stopping the current gesture.",
            commandTranscript = "stop"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "excited", "celebrate", "rally", "victory", "cheer" },
            outputText = "I'll celebrate.",
            commandTranscript = "excited"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "happy", "smile", "joy", "pleased" },
            outputText = "I'll look happy.",
            commandTranscript = "happy"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "sad", "upset", "unhappy", "depressed" },
            outputText = "I'll look sad.",
            commandTranscript = "sad"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "clap", "clapping", "applaud", "applause" },
            outputText = "I'll clap.",
            commandTranscript = "clapping"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "dance", "hip hop", "hip-hop" },
            outputText = "Starting the dance.",
            commandTranscript = "hip hop dance"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "argue", "argument", "mad", "angry", "debate" },
            outputText = "Starting the arguing gesture.",
            commandTranscript = "argue"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "phone", "call", "cell phone", "telephone" },
            outputText = "Starting the phone gesture.",
            commandTranscript = "phone"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "flower", "flowers", "give flowers", "give flower", "kneel", "kneel down", "kneeling" },
            outputText = "Offering flowers.",
            commandTranscript = "flower"
        },
        new HardCodedVoiceResponse
        {
            keywords = new[] { "start", "begin", "go" },
            outputText = "Starting.",
            commandTranscript = "start"
        }
    };
    [SerializeField] private string unknownOutputText = "I heard you, but I do not have a hard-coded response for that yet.";

    [Header("Events")]
    public TranscriptEvent onInputTranscript;
    public TranscriptEvent onOutputTranscript;
    public TranscriptEvent onCommandTranscript;
    public StatusEvent onStatusChanged;
    public AudioEvent onAudioReceived;

    private Coroutine restartCoroutine;
    private bool shouldAutoRestart;
    private bool voiceDetectedThisUtterance;

    public bool IsConnected => voiceService != null;
    public bool IsMicrophoneStreaming => voiceService != null && voiceService.MicActive;

    private void Awake()
    {
    }

    private void OnEnable()
    {
        SubscribeVoiceEvents(true);
    }

    private void Start()
    {
        if (listenOnStart)
        {
            StartMicrophoneStream();
        }
    }

    private void OnDisable()
    {
        shouldAutoRestart = false;
        StopRestartCoroutine();
        SubscribeVoiceEvents(false);
    }

    private void OnApplicationQuit()
    {
        StopMicrophoneStream();
    }

    public System.Threading.Tasks.Task Connect()
    {
        ResolveDependencies();
        PublishStatus(voiceService == null
            ? "Meta Voice SDK AppVoiceExperience was not found."
            : "Meta Voice SDK voice service is ready.");
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public System.Threading.Tasks.Task Disconnect()
    {
        StopMicrophoneStream();
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public void StartMicrophoneStream()
    {
        Debug.Log("[VoiceRecognitionClient] Voice Service name: " + voiceService.name);
        Debug.Log("[VoiceRecognitionClient] Active: " + voiceService.Active);
        Debug.Log("[VoiceRecognitionClient] MicActive: " + voiceService.MicActive);
        Debug.Log("[VoiceRecognitionClient] CanActivateAudio: " + voiceService.CanActivateAudio());

        if (!voiceService.CanActivateAudio())
        {
            Debug.LogError("[VoiceRecognitionClient] Activate audio error: " + voiceService.GetActivateAudioError());
            return;
        }
        ResolveDependencies();

        if (voiceService == null)
        {
            PublishStatus("Cannot start microphone: add an AppVoiceExperience to the scene or assign one here.");
            return;
        }

        if (voiceService.Active || voiceService.MicActive)
        {
            return;
        }

        if (!voiceService.CanActivateAudio())
        {
            PublishStatus("Cannot start Meta Voice SDK microphone: " + voiceService.GetActivateAudioError());
            return;
        }

        shouldAutoRestart = restartAfterUtterance;
        if (activateImmediately)
        {
            voiceService.ActivateImmediately();
        }
        else
        {
            voiceService.Activate();
        }


        //on success
        PublishStatus("Meta Voice SDK microphone listening started.");
    }

    public void StopMicrophoneStream()
    {
        shouldAutoRestart = false;
        StopRestartCoroutine();

        if (voiceService == null)
        {
            return;
        }

        if (voiceService.Active || voiceService.MicActive)
        {
            voiceService.Deactivate();
        }

        PublishStatus("Meta Voice SDK microphone listening stopped.");
    }

    public void HandleTranscriptForTesting(string transcript)
    {
        HandleFullTranscription(transcript);
    }

    private void SubscribeVoiceEvents(bool subscribe)
    {
        if (voiceService == null)
        {
            return;
        }

        if (subscribe)
        {
            voiceService.VoiceEvents.OnMinimumWakeThresholdHit.AddListener(HandleVoiceDetected);
            voiceService.VoiceEvents.OnPartialTranscription.AddListener(HandlePartialTranscription);
            voiceService.VoiceEvents.OnFullTranscription.AddListener(HandleFullTranscription);
            voiceService.VoiceEvents.OnStartListening.AddListener(HandleStartListening);
            voiceService.VoiceEvents.OnStoppedListening.AddListener(HandleStoppedListening);
            voiceService.VoiceEvents.OnError.AddListener(HandleVoiceError);
        }
        else
        {
            voiceService.VoiceEvents.OnMinimumWakeThresholdHit.RemoveListener(HandleVoiceDetected);
            voiceService.VoiceEvents.OnPartialTranscription.RemoveListener(HandlePartialTranscription);
            voiceService.VoiceEvents.OnFullTranscription.RemoveListener(HandleFullTranscription);
            voiceService.VoiceEvents.OnStartListening.RemoveListener(HandleStartListening);
            voiceService.VoiceEvents.OnStoppedListening.RemoveListener(HandleStoppedListening);
            voiceService.VoiceEvents.OnError.RemoveListener(HandleVoiceError);
        }
    }

    private void HandleStartListening()
    {
        voiceDetectedThisUtterance = false;
        PublishStatus("Meta Voice SDK is listening.");
    }

    private void HandleStoppedListening()
    {
        PublishStatus("Meta Voice SDK stopped listening.");
        if (logVoiceDetection && !voiceDetectedThisUtterance)
        {
            PublishStatus("No voice detected during that listening window.");
        }

        if (shouldAutoRestart && isActiveAndEnabled)
        {
            StopRestartCoroutine();
            restartCoroutine = StartCoroutine(RestartListeningAfterDelay());
        }
    }

    private IEnumerator RestartListeningAfterDelay()
    {
        yield return new WaitForSeconds(restartDelaySeconds);
        restartCoroutine = null;

        if (shouldAutoRestart && isActiveAndEnabled)
        {
            StartMicrophoneStream();
        }
    }

    private void HandleVoiceError(string error, string message)
    {
        PublishStatus("Meta Voice SDK error: " + error + " " + message);
    }

    private void HandleVoiceDetected()
    {
        voiceDetectedThisUtterance = true;

        if (logVoiceDetection)
        {
            PublishStatus("Voice/audio detected by Meta Voice SDK.");
        }
    }

    private void HandlePartialTranscription(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return;
        }

        voiceDetectedThisUtterance = true;

        if (logVoiceDetection)
        {
            string trimmedTranscript = transcript.Trim();
            Debug.Log("[VoiceRecognitionClient] Partial words heard: " + trimmedTranscript);
            PublishStatus("Partial voice detected: " + trimmedTranscript);
        }
    }

    private void HandleFullTranscription(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return;
        }

        voiceDetectedThisUtterance = true;
        string trimmedTranscript = transcript.Trim();
        Debug.Log("[VoiceRecognitionClient] Final words heard: " + trimmedTranscript);
        PublishStatus("Voice recognized: " + trimmedTranscript);
        onInputTranscript?.Invoke(trimmedTranscript);

        HardCodedVoiceResponse response = FindResponse(trimmedTranscript);
        if (response == null)
        {
            PublishStatus("No hard-coded LLM response matched: " + trimmedTranscript);
            onOutputTranscript?.Invoke(unknownOutputText);
            return;
        }

        string outputText = string.IsNullOrWhiteSpace(response.outputText)
            ? response.commandTranscript
            : response.outputText;
        string commandTranscript = string.IsNullOrWhiteSpace(response.commandTranscript)
            ? outputText
            : response.commandTranscript;

        PublishStatus("Hard-coded LLM response: " + outputText);
        onOutputTranscript?.Invoke(outputText);
        onCommandTranscript?.Invoke(commandTranscript);
        commandRouter?.HandleTranscript(commandTranscript);
    }

    private HardCodedVoiceResponse FindResponse(string transcript)
    {
        string normalized = transcript.ToLowerInvariant();
        foreach (HardCodedVoiceResponse response in responses)
        {
            if (response == null || response.keywords == null)
            {
                continue;
            }

            foreach (string keyword in response.keywords)
            {
                if (!string.IsNullOrWhiteSpace(keyword) && normalized.Contains(keyword.ToLowerInvariant()))
                {
                    return response;
                }
            }
        }

        return null;
    }

    private void ResolveDependencies()
    {
        if (voiceService == null)
        {
            voiceService = FindAnyObjectByType<AppVoiceExperience>();
            SubscribeVoiceEvents(true);
        }

        if (commandRouter == null)
        {
            commandRouter = FindAnyObjectByType<VoiceCommandRouter>();
        }
    }

    private void StopRestartCoroutine()
    {
        if (restartCoroutine == null)
        {
            return;
        }

        StopCoroutine(restartCoroutine);
        restartCoroutine = null;
    }

    private void PublishStatus(string message)
    {
        Debug.Log("[VoiceRecognitionClient] " + message);
        onStatusChanged?.Invoke(message);
    }
}

[Serializable]
public sealed class HardCodedVoiceResponse
{
    public string[] keywords;
    public string outputText;
    public string commandTranscript;
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
