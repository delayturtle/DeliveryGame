using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject enemyPrefab;
    public float minSpawnInterval = 3f;
    public float maxSpawnInterval = 8f;
    public int maxEnemyCapacity = 10;

    [Header("Spawn Location")]
    public bool spawnAtSpawnerPosition = true;
    public float spawnRadius = 0f;

    [Header("Spawn Height")]
    public float spawnHeightOffset = 0.5f;

    [Header("Camera Visibility")]
    public bool pauseWhenVisible = true;
    public Camera playerCamera;
    public float visibilityBuffer = 2f;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showVisibilityDebug = false;

    private float nextSpawnTime;
    private int currentEnemyCount = 0;
    private bool isVisibleToCamera = false;

    void OnEnable()
    {
        RespawnManager.OnPlayerRespawned += ReacquireCamera;
    }

    void OnDisable()
    {
        RespawnManager.OnPlayerRespawned -= ReacquireCamera;
    }

    void Start()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
        ReacquireCamera();
    }

    void Update()
    {
        // 🔄 Auto reacquire camera if lost/destroyed
        if (playerCamera == null)
            ReacquireCamera();

        if (pauseWhenVisible && playerCamera != null)
            isVisibleToCamera = IsVisibleToCamera();
        else
            isVisibleToCamera = false;

        UpdateEnemyCount();

        if (Time.time >= nextSpawnTime &&
            currentEnemyCount < maxEnemyCapacity &&
            !isVisibleToCamera)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[EnemySpawner] Enemies: {currentEnemyCount}/{maxEnemyCapacity}, Visible: {isVisibleToCamera}");
        }
    }

    // =====================================================
    // CAMERA HANDLING
    // =====================================================

    void ReacquireCamera()
    {
        // Try Camera.main first
        playerCamera = Camera.main;

        // If still null, try to find camera on active player
        if (playerCamera == null && RespawnManager.Instance != null)
        {
            Transform player = RespawnManager.Instance.GetActivePlayerTransform();

            if (player != null)
                playerCamera = player.GetComponentInChildren<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("[EnemySpawner] No player camera found!");
        }
        else if (showDebugInfo)
        {
            Debug.Log("[EnemySpawner] Camera reacquired successfully.");
        }
    }

    // =====================================================
    // VISIBILITY CHECK
    // =====================================================

    bool IsVisibleToCamera()
    {
        if (playerCamera == null)
            return false;

        Vector3 spawnCenter = transform.position + Vector3.up * spawnHeightOffset;

        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(spawnCenter);

        bool isInFrustum =
            viewportPoint.z > 0 &&
            viewportPoint.x >= -visibilityBuffer && viewportPoint.x <= 1f + visibilityBuffer &&
            viewportPoint.y >= -visibilityBuffer && viewportPoint.y <= 1f + visibilityBuffer;

        if (!isInFrustum)
            return false;

        Vector3 directionToCamera = playerCamera.transform.position - spawnCenter;
        float distanceToCamera = directionToCamera.magnitude;

        if (Physics.Raycast(spawnCenter, directionToCamera.normalized, out RaycastHit hit, distanceToCamera))
        {
            if (hit.collider.transform.IsChildOf(playerCamera.transform))
                return true;

            return false;
        }

        return true;
    }

    // =====================================================
    // SPAWNING
    // =====================================================

    void UpdateEnemyCount()
    {
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
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        if (showDebugInfo)
        {
            Debug.Log($"[EnemySpawner] Spawned enemy at {spawnPosition}");
        }
    }

    Vector3 CalculateSpawnPosition()
    {
        Vector3 basePosition = spawnAtSpawnerPosition ? transform.position : Vector3.zero;

        if (spawnRadius > 0f)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            basePosition += new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        basePosition += Vector3.up * spawnHeightOffset;

        return basePosition;
    }

    // =====================================================
    // GIZMOS
    // =====================================================

    void OnDrawGizmosSelected()
    {
        Vector3 spawnCenter = transform.position + Vector3.up * spawnHeightOffset;

        Gizmos.color = isVisibleToCamera ? Color.red : Color.green;
        Gizmos.DrawWireSphere(spawnCenter, 0.5f);

        if (spawnRadius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
        }

        if (pauseWhenVisible && playerCamera != null && isVisibleToCamera)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(spawnCenter, playerCamera.transform.position);
        }
    }
}