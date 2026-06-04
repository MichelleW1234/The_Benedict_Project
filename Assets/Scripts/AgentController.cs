using UnityEngine;
//using System.Diagnostics;

public class AgentController : MonoBehaviour
{
    public Animator animator;
    public float sampleRadius = 1.0f;

    void Start()
    {
        if (animator != null) {
            // call gesture functions here for testing
        }
    }
    void Update()
    {
        
    }

    public void PlayExcitedGesture() {
        if (!HasAnimator()) {
            return;
        }

        ClearPersistentGestures();
        animator.SetTrigger("Excited");
    }

    public void PlayHappyGesture() {
        if (!HasAnimator()) {
            return;
        }

        ClearPersistentGestures();
        animator.SetTrigger("Happy");
    }

    public void PlaySadGesture() {
        if (!HasAnimator()) {
            return;
        }

        ClearPersistentGestures();
        animator.SetTrigger("Sad");
    }

    public void PlayClappingGesture() {
        if (!HasAnimator()) {
            return;
        }

        ClearPersistentGestures();
        animator.SetTrigger("Clapping");
    }

    public void PlayHipHopDanceGesture() {
        if (!HasAnimator()) {
            return;
        }

        ClearPersistentGestures();
        animator.SetTrigger("Hip_hop_dancing");
    }

    public void StartArgueGesture() {
        if (!HasAnimator()) {
            return;
        }

        ClearPersistentGestures();
        animator.SetBool("Argue", true);
    }

    public void StartTalkingOnPhoneGesture() {
        if (!HasAnimator()) {
            return;
        }

        ClearPersistentGestures();
        animator.SetBool("Talking_on_phone", true);
    }

    public void StopPersistentGestures() {
        if (!HasAnimator()) {
            return;
        }

        ClearPersistentGestures();
    }

    private void ClearPersistentGestures() {
        animator.SetBool("Argue", false);
        animator.SetBool("Talking_on_phone", false);
    }

    private bool HasAnimator() {
        if (animator != null) {
            return true;
        }

        Debug.LogWarning("[AgentController] Cannot play gesture because Animator is not assigned.");
        return false;
    }
}
