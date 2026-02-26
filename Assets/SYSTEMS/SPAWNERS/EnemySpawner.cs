using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("Enemy prefab to spawn")]
    public GameObject enemyPrefab;

    [Tooltip("Time between spawns (seconds)")]
    public float spawnInterval = 5f;

    [Tooltip("Maximum number of enemies allowed in the scene")]
    public int maxEnemyCapacity = 10;

    [Header("Spawn Location")]
    [Tooltip("Spawn enemies at this spawner's position")]
    public bool spawnAtSpawnerPosition = true;

    [Tooltip("Random spawn radius around spawner (0 = exact position)")]
    public float spawnRadius = 0f;

    [Header("Spawn Height")]
    [Tooltip("Height offset above spawner position")]
    public float spawnHeightOffset = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private float nextSpawnTime;
    private int currentEnemyCount = 0;

    void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        // Count current enemies in scene
        UpdateEnemyCount();

        // Check if we should spawn
        if (Time.time >= nextSpawnTime && currentEnemyCount < maxEnemyCapacity)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }

        if (showDebugInfo)
        {
            Debug.Log($"[EnemySpawner] Enemies: {currentEnemyCount}/{maxEnemyCapacity}");
        }
    }

    void UpdateEnemyCount()
    {
        // Find all enemies with EnemyAI component
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        currentEnemyCount = enemies.Length;
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] No enemy prefab assigned!");
            return;
        }

        Vector3 spawnPosition = CalculateSpawnPosition();

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        if (showDebugInfo)
        {
            Debug.Log($"[EnemySpawner] Spawned enemy at {spawnPosition}. Total: {currentEnemyCount + 1}/{maxEnemyCapacity}");
        }
    }

    Vector3 CalculateSpawnPosition()
    {
        Vector3 basePosition = spawnAtSpawnerPosition ? transform.position : Vector3.zero;

        // Add random offset within radius
        if (spawnRadius > 0f)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            basePosition += new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        // Add height offset
        basePosition += Vector3.up * spawnHeightOffset;

        return basePosition;
    }

    void OnDrawGizmosSelected()
    {
        // Draw spawn position
        Gizmos.color = Color.red;
        Vector3 spawnCenter = transform.position + Vector3.up * spawnHeightOffset;
        Gizmos.DrawWireSphere(spawnCenter, 0.5f);

        // Draw spawn radius
        if (spawnRadius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
        }

        // Draw capacity info
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, spawnCenter);
    }
}
