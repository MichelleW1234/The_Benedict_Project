using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public sealed class VoiceCommandRouter : MonoBehaviour
{
    [Header("NLP Intent Classification")]
    [SerializeField] private bool useNlpClassification = true;
    [SerializeField] private string intentApiUrl = "http://127.0.0.1:3000/api/intent";
    [SerializeField] private bool fallbackToLocalPhrases = true;

    [Header("Command Events")]
    public UnityEvent onOpenInventory;
    public UnityEvent onCloseInventory;
    public UnityEvent onStart;
    public UnityEvent onStop;
    public StringEvent onUnknownCommand;

    public void HandleTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return;
        }

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

        using var request = new UnityWebRequest(intentApiUrl, UnityWebRequest.kHttpVerbPOST);
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

        if (ContainsAny(normalized, "start", "begin", "go"))
        {
            return "start";
        }

        if (ContainsAny(normalized, "stop", "pause", "wait"))
        {
            return "stop";
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
                break;
            default:
                onUnknownCommand?.Invoke(transcript);
                break;
        }
    }

    private static string NormalizeIntent(string intent)
    {
        return string.IsNullOrWhiteSpace(intent) ? "unknown" : intent.Trim().ToLowerInvariant();
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
