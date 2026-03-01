using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("Enemy prefab to spawn")]
    public GameObject enemyPrefab;

    [Tooltip("Minimum time between spawns (seconds)")]
    public float minSpawnInterval = 3f;

    [Tooltip("Maximum time between spawns (seconds)")]
    public float maxSpawnInterval = 8f;

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

    [Header("Camera Visibility")]
    [Tooltip("Pause spawning when visible to player camera")]
    public bool pauseWhenVisible = true;

    [Tooltip("Player camera (auto-finds if null)")]
    public Camera playerCamera;

    [Tooltip("Additional buffer distance for visibility check")]
    public float visibilityBuffer = 2f;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showVisibilityDebug = false;

    private float nextSpawnTime;
    private int currentEnemyCount = 0;
    private bool isVisibleToCamera = false;

    void Start()
    {
        // Set initial random spawn time
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);

        // Auto-find player camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogWarning("[EnemySpawner] No player camera found! Visibility check disabled.");
            }
        }
    }

    void Update()
    {
        // Check if spawner is visible to camera
        if (pauseWhenVisible && playerCamera != null)
        {
            isVisibleToCamera = IsVisibleToCamera();
        }
        else
        {
            isVisibleToCamera = false;
        }

        // Count current enemies in scene
        UpdateEnemyCount();

        // Check if we should spawn (not if visible to camera)
        if (Time.time >= nextSpawnTime && currentEnemyCount < maxEnemyCapacity && !isVisibleToCamera)
        {
            SpawnEnemy();
            // Set next spawn time with random interval
            nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[EnemySpawner] Enemies: {currentEnemyCount}/{maxEnemyCapacity}, Visible: {isVisibleToCamera}");
        }
    }

    bool IsVisibleToCamera()
    {
        if (playerCamera == null)
            return false;

        Vector3 spawnCenter = transform.position + Vector3.up * spawnHeightOffset;

        // Check if point is within camera frustum
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(spawnCenter);

        // Check if point is in front of camera and within viewport bounds (with buffer)
        bool isInFrustum = viewportPoint.z > 0 && 
                           viewportPoint.x >= -visibilityBuffer && viewportPoint.x <= 1f + visibilityBuffer &&
                           viewportPoint.y >= -visibilityBuffer && viewportPoint.y <= 1f + visibilityBuffer;

        if (!isInFrustum)
            return false;

        // Raycast to check if spawner is actually visible (not blocked by objects)
        Vector3 directionToCamera = playerCamera.transform.position - spawnCenter;
        float distanceToCamera = directionToCamera.magnitude;

        RaycastHit hit;
        if (Physics.Raycast(spawnCenter, directionToCamera.normalized, out hit, distanceToCamera))
        {
            // If raycast hits the camera or nothing blocks the view, it's visible
            if (hit.collider.gameObject == playerCamera.gameObject || 
                hit.collider.transform.IsChildOf(playerCamera.transform))
            {
                return true;
            }

            // If something else is blocking the view, it's not visible
            if (showVisibilityDebug)
            {
                Debug.DrawLine(spawnCenter, hit.point, Color.green, 0.1f);
            }
            return false;
        }

        // Nothing blocking, spawner is visible
        if (showVisibilityDebug)
        {
            Debug.DrawLine(spawnCenter, playerCamera.transform.position, Color.red, 0.1f);
        }
        return true;
    }

    void UpdateEnemyCount()
    {
        // Find all enemies with EnemyAI component (using older API)
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>();
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
        Gizmos.color = isVisibleToCamera ? Color.red : Color.green;
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

        // Draw line to camera if visible
        if (pauseWhenVisible && playerCamera != null && isVisibleToCamera)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(spawnCenter, playerCamera.transform.position);
        }
    }
}