using UnityEngine;
using System.Collections;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance;

    public static System.Action OnPlayerRespawned;

    [Header("Setup")]
    public GameObject playerPrefab;
    public Transform spawnPoint;

    [Header("Respawn Settings")]
    public float respawnDelay = 3f;

    private GameObject activePlayer;
    private bool isRespawning = false;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterPlayer(GameObject player)
    {
        activePlayer = player;
    }

    public Transform GetActivePlayerTransform()
    {
        if (activePlayer == null)
            return null;

        return activePlayer.transform;
    }

    public void Respawn()
    {
        if (!isRespawning)
            StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        yield return new WaitForSeconds(respawnDelay);

        if (activePlayer != null)
            Destroy(activePlayer);

        GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        activePlayer = newPlayer;

        // 🔥 Notify listeners (QuestManager etc)
        OnPlayerRespawned?.Invoke();

        isRespawning = false;
    }
}