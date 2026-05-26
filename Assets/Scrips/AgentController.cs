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

}