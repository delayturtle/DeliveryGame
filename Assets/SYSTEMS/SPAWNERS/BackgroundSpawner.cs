using UnityEngine;

public class BackgroundSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] backgroundPrefabs;

    [Header("Lanes")]
    public int laneCount = 4;
    public float laneSpacing = 3f;
    public float laneHeightVariance = 1f;

    [Header("Spawning")]
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 3f;

    [Header("Movement")]
    public float minSpeed = 8f;
    public float maxSpeed = 15f;

    private float[] _laneTimers;
    private float[] _laneIntervals;
    private float[] _laneRightOffsets;
    private float[] _laneUpOffsets;

    void Start()
    {
        _laneTimers = new float[laneCount];
        _laneIntervals = new float[laneCount];
        _laneRightOffsets = new float[laneCount];
        _laneUpOffsets = new float[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            _laneRightOffsets[i] = (i - (laneCount - 1) / 2f) * laneSpacing;
            _laneUpOffsets[i] = Random.Range(-laneHeightVariance, laneHeightVariance);
            _laneIntervals[i] = Random.Range(minSpawnInterval, maxSpawnInterval);
            _laneTimers[i] = Random.Range(0f, _laneIntervals[i]);
        }
    }

    void Update()
    {
        for (int i = 0; i < laneCount; i++)
        {
            _laneTimers[i] += Time.deltaTime;
            if (_laneTimers[i] >= _laneIntervals[i])
            {
                _laneTimers[i] = 0f;
                _laneIntervals[i] = Random.Range(minSpawnInterval, maxSpawnInterval);
                SpawnInLane(i);
            }
        }
    }

    void SpawnInLane(int laneIndex)
    {
        if (backgroundPrefabs == null || backgroundPrefabs.Length == 0)
        {
            Debug.LogWarning("BackgroundSpawner: No prefabs assigned!");
            return;
        }

        int randomIndex = Random.Range(0, backgroundPrefabs.Length);
        GameObject chosenPrefab = backgroundPrefabs[randomIndex];
        if (chosenPrefab == null) return;

        Vector3 spawnPos = transform.position
                         + transform.right * _laneRightOffsets[laneIndex]
                         + transform.up * _laneUpOffsets[laneIndex];

        // Step 1: Point the blue Z arrow toward the lane's travel direction
        Quaternion laneRot = Quaternion.LookRotation(transform.forward, transform.up);

        // Step 2: Correction to align prefab correctly after Blender/Unity axis difference
        // If your model is exported correctly from Blender (-Z Forward, Y Up)
        // set this to (0, 0, 0). Otherwise adjust until the car faces forward.
        Quaternion correction = Quaternion.Euler(0f, 0f, 0f);

        Quaternion spawnRot = laneRot * correction;

        GameObject spawnedObject = Instantiate(chosenPrefab, spawnPos, spawnRot);

        // Destroy the spawned object after 45 seconds
        Destroy(spawnedObject, 45f);

        BackgroundMover mover = spawnedObject.GetComponent<BackgroundMover>();
        if (mover != null)
            mover.speed = Random.Range(minSpeed, maxSpeed);
    }

    void OnDrawGizmosSelected()
    {
        if (laneCount <= 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < laneCount; i++)
        {
            float rightOffset = (i - (laneCount - 1) / 2f) * laneSpacing;
            Vector3 lanePos = transform.position + transform.right * rightOffset;
            Gizmos.DrawWireSphere(lanePos, 0.3f);
            Gizmos.DrawRay(lanePos, transform.forward * 5f);
        }
    }
}
