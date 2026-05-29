using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    [Header("Agent Settings")]
    public GameObject[] agentPrefabs; // Array to hold your agent prefabs
    // 0: michelle
    // 1: remy 
    // add on later...
    private Vector3 spawnPoint = new Vector3(0f,-1f,1.48000002f); // The location where the agent should appear

    private GameObject currentSpawnedAgent; // Keeps track of the currently active agent

    public void OnAgentButtonClicked(int prefabIndex)
    {
        // 1. Validate the index to prevent errors
        if (prefabIndex < 0 || prefabIndex >= agentPrefabs.Length)
        {
            Debug.LogError("Invalid prefab index!");
            return;
        }

        // Destroy the existing agent if there is one
        if (currentSpawnedAgent != null)
        {
            Destroy(currentSpawnedAgent);
        }

        // Instantiate the new selected agent prefab
        currentSpawnedAgent = Instantiate(
            agentPrefabs[prefabIndex], 
            spawnPoint, 
            Quaternion.Euler(0f, 180f, 0f)        
        );

        Debug.Log("Successfully spawned agent: " + agentPrefabs[prefabIndex].name);
    }
}
