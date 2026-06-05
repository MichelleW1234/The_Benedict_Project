using UnityEngine;

public class AudioManager : MonoBehaviour 
{
    [Header("Audio References")]
    [SerializeField] private AudioSource audioSource; 
    [SerializeField] private AudioClip heartbeatClip; // Only one clip needed!

    [Header("Speed Settings")]
    public float normalPitch = 1.0f; // Standard speed
    public float fastPitch = 1.8f;   // Higher number = faster speed

    void Start () {
        PlayNormalHeartBeatSound();
    }
    // Call this to play one single normal heartbeat
    public void PlayNormalHeartBeatSound()
    {
        if (heartbeatClip != null && audioSource != null)
        {
            // Set the AudioSource to normal speed before playing
            audioSource.pitch = normalPitch; 
            audioSource.PlayOneShot(heartbeatClip);
        }
    }

    // Call this to play one single fast heartbeat using the exact same clip
    public void PlayFastHeartBeatSound()
    {
        if (heartbeatClip != null && audioSource != null)
        {
            // Set the AudioSource to fast speed before playing
            audioSource.pitch = fastPitch; 
            audioSource.PlayOneShot(heartbeatClip);
        }
    }
}