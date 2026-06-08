using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public sealed class VoiceCommandRouter : MonoBehaviour
{
    [Header("NLP Intent Classification")]
    [SerializeField] private bool useNlpClassification = false;
    [SerializeField] private string intentApiUrl = "http://127.0.0.1:3000/api/intent";
    [SerializeField] private string androidIntentApiUrl;
    [SerializeField] private bool fallbackToLocalPhrases = true;

    [Header("Agent Animation")]
    [SerializeField] private AgentController targetAgent;

    [Header("Command Events")]
    public UnityEvent onOpenInventory;
    public UnityEvent onCloseInventory;
    public UnityEvent onStart;
    public UnityEvent onStop;
    public UnityEvent onExcited;
    public UnityEvent onHappy;
    public UnityEvent onSad;
    public UnityEvent onClapping;
    public UnityEvent onHipHopDance;
    public UnityEvent onArgue;
    public UnityEvent onTalkingOnPhone;
    public UnityEvent onFlower;
    public StringEvent onUnknownCommand;

    private void Awake()
    {
        if (targetAgent == null)
        {
            targetAgent = FindAnyObjectByType<AgentController>();
        }
    }

    public void HandleTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return;
        }

        Debug.Log("[VoiceCommandRouter] Processing voice command: " + transcript.Trim());

        if (useNlpClassification)
        {
            StartCoroutine(ClassifyAndDispatch(transcript.Trim()));
            return;
        }

        DispatchIntent(ClassifyWithLocalPhrases(transcript), transcript);
    }

    private IEnumerator ClassifyAndDispatch(string transcript)
    {
        var payload = new IntentRequest { transcript = transcript };
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        using var request = new UnityWebRequest(ResolveIntentApiUrl(), UnityWebRequest.kHttpVerbPOST);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("[VoiceCommandRouter] Intent classification failed: " + request.error);
            if (fallbackToLocalPhrases)
            {
                DispatchIntent(ClassifyWithLocalPhrases(transcript), transcript);
            }
            else
            {
                onUnknownCommand?.Invoke(transcript);
            }
            yield break;
        }

        IntentResponse response = JsonUtility.FromJson<IntentResponse>(request.downloadHandler.text);
        DispatchIntent(response?.intent, transcript);
    }

    private string ClassifyWithLocalPhrases(string transcript)
    {
        string normalized = transcript.Trim().ToLowerInvariant();

        if (ContainsAny(normalized, "open inventory", "show inventory", "inventory"))
        {
            return "open_inventory";
        }

        if (ContainsAny(normalized, "close inventory", "hide inventory"))
        {
            return "close_inventory";
        }

        if (ContainsAny(normalized, "stop", "pause", "wait"))
        {
            return "stop";
        }

        if (ContainsAny(normalized, "excited", "celebrate", "rally", "victory", "cheer"))
        {
            return "play_excited";
        }

        if (ContainsAny(normalized, "happy", "smile", "joy", "pleased"))
        {
            return "play_happy";
        }

        if (ContainsAny(normalized, "sad", "upset", "unhappy", "depressed"))
        {
            return "play_sad";
        }

        if (ContainsAny(normalized, "clap", "clapping", "applaud", "applause"))
        {
            return "play_clapping";
        }

        if (ContainsAny(normalized, "dance", "hip hop", "hip-hop"))
        {
            return "play_hip_hop_dance";
        }

        if (ContainsAny(normalized, "argue", "argument", "mad", "angry", "debate"))
        {
            return "start_argue";
        }

        if (ContainsAny(normalized, "phone", "call", "cell phone", "telephone"))
        {
            return "start_talking_on_phone";
        }

        if (ContainsAny(normalized, "flower", "flowers", "give flowers", "give flower", "kneel", "kneel down", "kneeling"))
        {
            return "play_flower";
        }

        if (ContainsAny(normalized, "start", "begin", "go"))
        {
            return "start";
        }

        return "unknown";
    }

    private void DispatchIntent(string intent, string transcript)
    {
        switch (NormalizeIntent(intent))
        {
            case "open_inventory":
                onOpenInventory?.Invoke();
                break;
            case "close_inventory":
                onCloseInventory?.Invoke();
                break;
            case "start":
                onStart?.Invoke();
                break;
            case "stop":
                onStop?.Invoke();
                targetAgent?.StopPersistentGestures();
                break;
            case "play_excited":
            case "excited":
                targetAgent?.PlayExcitedGesture();
                onExcited?.Invoke();
                break;
            case "play_happy":
            case "happy":
                targetAgent?.PlayHappyGesture();
                onHappy?.Invoke();
                break;
            case "play_sad":
            case "sad":
                targetAgent?.PlaySadGesture();
                onSad?.Invoke();
                break;
            case "play_clapping":
            case "clapping":
                targetAgent?.PlayClappingGesture();
                onClapping?.Invoke();
                break;
            case "play_hip_hop_dance":
            case "hip_hop_dance":
            case "hip_hop_dancing":
                targetAgent?.PlayHipHopDanceGesture();
                onHipHopDance?.Invoke();
                break;
            case "start_argue":
            case "argue":
                targetAgent?.StartArgueGesture();
                onArgue?.Invoke();
                break;
            case "start_talking_on_phone":
            case "talking_on_phone":
                targetAgent?.StartTalkingOnPhoneGesture();
                onTalkingOnPhone?.Invoke();
                break;
            case "play_flower":
            case "give_flowers":
            case "give_flower":
            case "flower":
            case "flowers":
            case "kneel":
            case "kneel_down":
            case "kneeling":
                targetAgent?.PlayFlowerGesture();
                onFlower?.Invoke();
                break;
            default:
                onUnknownCommand?.Invoke(transcript);
                break;
        }
    }

    private static string NormalizeIntent(string intent)
    {
        return string.IsNullOrWhiteSpace(intent)
            ? "unknown"
            : intent.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
    }

    private string ResolveIntentApiUrl()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(androidIntentApiUrl))
        {
            return androidIntentApiUrl;
        }
#endif

        return intentApiUrl;
    }

    private static bool ContainsAny(string text, params string[] phrases)
    {
        foreach (string phrase in phrases)
        {
            if (text.Contains(phrase))
            {
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public sealed class StringEvent : UnityEvent<string>
{
}

[Serializable]
public sealed class IntentRequest
{
    public string transcript;
}

[Serializable]
public sealed class IntentResponse
{
    public string intent;
}
