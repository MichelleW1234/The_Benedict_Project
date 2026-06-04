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
            PlayFlowerGesture();
        }
    }
    void Update()
    {
        if(spawnedFlower != null){
            Destroy(spawnedFlower, 5);
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

}