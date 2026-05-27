using UnityEngine;
//using System.Diagnostics;

public class AgentController : MonoBehaviour
{
    public Animator animator;
    public float sampleRadius = 1.0f;

    void Start()
    {

    }
    void Update()
    {
        if (animator != null) {
            // call gesture functions here for testing
        }
    }

    void PlayExcitedGesture() {
        animator.SetTrigger("Excited");
    }

    void PlayHappyGesture() {
        animator.SetTrigger("Happy");
    }

    void PlaySadGesture() {
        animator.SetTrigger("Sad");
    }

    void PlayClappingGesture() {
        animator.SetTrigger("Clapping");
    }

    void PlayHipHopDanceGesture() {
        animator.SetTrigger("Hip_hop_dancing");
    }

    void StartArgueGesture() {
        animator.SetBool("Argue", true);
    }

    void StopArgueGesture() {
        animator.SetBool("Argue", false);
    }

    void StartTalkingOnPhoneGesture() {
        animator.SetBool("Talking_on_phone", true);
    }

    void StopTalkingOnPhoneGesture() {
        animator.SetBool("Talking_on_phone", false);
    }



}