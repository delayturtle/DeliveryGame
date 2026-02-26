using UnityEngine;

public class PlayerCompassArrow : MonoBehaviour
{
    public Transform player;
    public float heightOffset = 3f;
    public float rotationSpeed = 5f;

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        if (player == null)
            return;

        // Follow player position
        transform.position = player.position + Vector3.up * heightOffset;

        if (target == null)
            return;

        // Direction to target (ignore height difference)
        Vector3 direction = target.position - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smooth rotate
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}