using System.Collections;
using Oculus.Voice;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class STT : MonoBehaviour
{
    [Header("Meta Voice SDK")]
    [SerializeField] private AppVoiceExperience appVoiceExperience;
    [SerializeField] private bool listenOnStart = true;
    [SerializeField] private bool activateImmediately = true;
    [SerializeField] private bool restartAfterRequest = true;
    [SerializeField] private float restartDelaySeconds = 0.5f;

    [SerializeField] private AgentController agentController;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI transcriptionText;

    [Header("Voice Events")]
    [SerializeField] private UnityEvent<string> partialTranscription;
    [SerializeField] private UnityEvent<string> completeTranscription;

    private Coroutine restartCoroutine;
    private bool shouldRestart;

    private void Awake()
    {
        if (appVoiceExperience == null)
        {
            appVoiceExperience = FindAnyObjectByType<AppVoiceExperience>();
        }
    }

    private void OnEnable()
    {
        if (appVoiceExperience == null)
        {
            Debug.LogError("[STT] AppVoiceExperience is missing. Assign it in the Inspector.");
            return;
        }

        appVoiceExperience.VoiceEvents.OnStartListening.AddListener(OnStartListening);
        appVoiceExperience.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
        appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(OnRequestCompleted);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        appVoiceExperience.VoiceEvents.OnError.AddListener(OnVoiceError);
    }

    private void Start()
    {
        if (listenOnStart)
        {
            StartListening();
        }
    }

    private void OnDisable()
    {
        shouldRestart = false;
        StopRestartCoroutine();

        if (appVoiceExperience == null)
        {
            return;
        }

        appVoiceExperience.VoiceEvents.OnStartListening.RemoveListener(OnStartListening);
        appVoiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
        appVoiceExperience.VoiceEvents.OnRequestCompleted.RemoveListener(OnRequestCompleted);
        appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
        appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
        appVoiceExperience.VoiceEvents.OnError.RemoveListener(OnVoiceError);
    }

    private void OnApplicationQuit()
    {
        StopListening();
    }

    public void StartListening()
    {
        if (appVoiceExperience == null)
        {
            Debug.LogError("[STT] Cannot start listening because AppVoiceExperience is null.");
            return;
        }

        Debug.Log("[STT] Active before start: " + appVoiceExperience.Active);
        Debug.Log("[STT] MicActive before start: " + appVoiceExperience.MicActive);
        Debug.Log("[STT] CanActivateAudio: " + appVoiceExperience.CanActivateAudio());

        if (appVoiceExperience.MicActive)
        {
            Debug.Log("[STT] Mic is already active.");
            return;
        }

        if (appVoiceExperience.Active && !appVoiceExperience.MicActive)
        {
            Debug.LogWarning("[STT] Voice service is active but mic is not active. Deactivating first.");
            appVoiceExperience.Deactivate();

            StopRestartCoroutine();
            restartCoroutine = StartCoroutine(RestartAfterDelay());
            return;
        }

        if (!appVoiceExperience.CanActivateAudio())
        {
            Debug.LogError("[STT] Cannot activate audio: " + appVoiceExperience.GetActivateAudioError());
            return;
        }

        shouldRestart = restartAfterRequest;

        if (activateImmediately)
        {
            appVoiceExperience.ActivateImmediately();
        }
        else
        {
            appVoiceExperience.Activate();
        }

        StartCoroutine(CheckMicAfterActivate());
    }

    public void StopListening()
    {
        shouldRestart = false;
        StopRestartCoroutine();

        if (appVoiceExperience == null)
        {
            return;
        }

        if (appVoiceExperience.Active || appVoiceExperience.MicActive)
        {
            appVoiceExperience.Deactivate();
        }

        Debug.Log("[STT] Stopped listening.");
    }

    private IEnumerator CheckMicAfterActivate()
    {
        yield return new WaitForSeconds(0.5f);

        if (appVoiceExperience == null)
        {
            yield break;
        }

        Debug.Log("[STT] AFTER ACTIVATE Active: " + appVoiceExperience.Active);
        Debug.Log("[STT] AFTER ACTIVATE MicActive: " + appVoiceExperience.MicActive);
    }

    private void OnStartListening()
    {
        Debug.Log("[STT] Meta Voice SDK is listening.");
    }

    private void OnStoppedListening()
    {
        Debug.Log("[STT] Meta Voice SDK stopped listening.");
    }

    private void OnRequestCompleted()
    {
        Debug.Log("[STT] Request completed.");

        if (shouldRestart && isActiveAndEnabled)
        {
            StopRestartCoroutine();
            restartCoroutine = StartCoroutine(RestartAfterDelay());
        }
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelaySeconds);
        restartCoroutine = null;

        if (!shouldRestart || !isActiveAndEnabled)
        {
            yield break;
        }

        if (appVoiceExperience != null && appVoiceExperience.Active && !appVoiceExperience.MicActive)
        {
            Debug.LogWarning("[STT] Restart cleanup: deactivating stuck active state.");
            appVoiceExperience.Deactivate();
            yield return new WaitForSeconds(0.5f);
        }

        StartListening();
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

    private void OnPartialTranscription(string transcription)
    {
        if (string.IsNullOrWhiteSpace(transcription))
        {
            return;
        }

        Debug.Log("[STT] Partial transcription: " + transcription);

        if (transcriptionText != null)
        {
            transcriptionText.text = transcription;
        }

        partialTranscription?.Invoke(transcription);
    }

    private void OnFullTranscription(string transcription)
    {
        if (string.IsNullOrWhiteSpace(transcription))
        {
            return;
        }

        Debug.Log("[STT] Full transcription: " + transcription);

        completeTranscription?.Invoke(transcription);

        string lower = transcription.ToLower();

        if (lower.Contains("flower"))
        {
            Debug.Log("[STT] Flower command detected.");

            if (agentController != null)
            {
                agentController.PlayFlowerGesture();
            }
            else
            {
                Debug.LogWarning("[STT] AgentController is not assigned.");
            }
        }

        if (lower.Contains("happy"))
        {
            Debug.Log("[STT] Happy command detected.");

            if (agentController != null)
            {
                agentController.PlayHappyGesture();
            }
            else
            {
                Debug.LogWarning("[STT] AgentController is not assigned.");
            }
        }

        if (lower.Contains("excited"))
        {
            Debug.Log("[STT] Excited command detected.");

            if (agentController != null)
            {
                agentController.PlayExcitedGesture();
            }
            else
            {
                Debug.LogWarning("[STT] AgentController is not assigned.");
            }
        }

        if (lower.Contains("dance"))
        {
            Debug.Log("[STT] Dance command detected.");

            if (agentController != null)
            {
                agentController.PlayHipHopDanceGesture();
            }
            else
            {
                Debug.LogWarning("[STT] AgentController is not assigned.");
            }
        }

        if (lower.Contains("sad"))
        {
            Debug.Log("[STT] Sad command detected.");

            if (agentController != null)
            {
                agentController.PlaySadGesture();
            }
            else
            {
                Debug.LogWarning("[STT] AgentController is not assigned.");
            }
        }

        if (lower.Contains("clap") || lower.Contains("congratulate") || lower.Contains("accepted"))
        {
            Debug.Log("[STT] Clap command detected.");

            if (agentController != null)
            {
                agentController.PlayClappingGesture();
            }
            else
            {
                Debug.LogWarning("[STT] AgentController is not assigned.");
            }
        }

        if (lower.Contains("argue") || lower.Contains("mad") || lower.Contains("angry") || lower.Contains("frustrated") || lower.Contains("annoyed"))
        {
            Debug.Log("[STT] Argue command detected.");

            if (agentController != null)
            {
                agentController.StartArgueGesture();
            }
            else
            {
                Debug.LogWarning("[STT] AgentController is not assigned.");
            }
        }

        if (lower.Contains("phone") || lower.Contains("call"))
        {
            Debug.Log("[STT] Talking on phone command detected.");

            if (agentController != null)
            {
                agentController.StartTalkingOnPhoneGesture();
            }
            else
            {
                Debug.LogWarning("[STT] AgentController is not assigned.");
            }
        }
        
        
    }

    private void OnVoiceError(string error, string message)
    {
        Debug.LogError("[STT] Voice error: " + error + " " + message);
    }
}