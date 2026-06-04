using UnityEngine;
using System.Collections; // Required for Coroutines
//using System.Diagnostics;

public class AgentController : MonoBehaviour
{
    public Animator animator;
    public float sampleRadius = 1.0f;

    [SerializeField] 
    private Transform rightHand;
    [SerializeField]
    private GameObject flowerPrefab;

    private GameObject spawnedFlower;
    private bool FlowerPlaying = false;

    void Start()
    {
        if (animator != null) {
            // call gesture functions here for testing
            StartTalkingOnPhoneGesture();
        }
    }
    void Update()
    {
        if(spawnedFlower != null){
            Destroy(spawnedFlower, 5);
        }
        
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

    void PlayFlowerGesture() {
        FlowerPlaying = true;
        StartCoroutine(FlowerSequence());
        FlowerPlaying = false;
    }

    private IEnumerator FlowerSequence() {
        animator.SetTrigger("Flower");
        CreateFlower();

        yield return null;

        float animationLength = animator.GetCurrentAnimatorStateInfo(0).length;

        yield return new WaitForSeconds(animationLength + 3.0f);

        
    }

    void CreateFlower() {
        if (rightHand != null && flowerPrefab != null) 
        {
            // Instantiate the flower and make it a child of the rightHand
            spawnedFlower = Instantiate(flowerPrefab, rightHand);
            spawnedFlower.transform.localPosition = Vector3.zero;
            spawnedFlower.transform.localScale = Vector3.one * 80f;
        }
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
