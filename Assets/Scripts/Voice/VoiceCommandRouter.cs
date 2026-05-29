using System;
using UnityEngine;
using UnityEngine.Events;

public sealed class VoiceCommandRouter : MonoBehaviour
{
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

        string normalized = transcript.Trim().ToLowerInvariant();

        if (ContainsAny(normalized, "open inventory", "show inventory", "inventory"))
        {
            onOpenInventory?.Invoke();
            return;
        }

        if (ContainsAny(normalized, "close inventory", "hide inventory"))
        {
            onCloseInventory?.Invoke();
            return;
        }

        if (ContainsAny(normalized, "start", "begin", "go"))
        {
            onStart?.Invoke();
            return;
        }

        if (ContainsAny(normalized, "stop", "pause", "wait"))
        {
            onStop?.Invoke();
            return;
        }

        onUnknownCommand?.Invoke(transcript);
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
