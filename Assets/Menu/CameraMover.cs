using UnityEngine;

public class CameraMover : MonoBehaviour
{
    [Tooltip("Movement speed in units per second")]
    public float moveSpeed = 5f;

    [Tooltip("Distance threshold to consider 'arrived'")]
    public float stopThreshold = 0.01f;

    private Transform targetPosition;
    private Transform lookAtTarget;
    private bool moveCamera = false;
    private bool movingBack = false;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void Update()
    {
        if (!moveCamera) return;

        Vector3 targetPos;
        Quaternion targetRot;

        if (movingBack)
        {
            targetPos = originalPosition;
            targetRot = originalRotation;
        }
        else if (targetPosition != null)
        {
            targetPos = targetPosition.position;
            if (lookAtTarget != null)
            {
                Vector3 dir = (lookAtTarget.position - transform.position).normalized;
                targetRot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : transform.rotation;
            }
            else
            {
                targetRot = targetPosition.rotation;
            }
        }
        else
        {
            return;
        }

        // Calculate distance and angular difference
        float distance = Vector3.Distance(transform.position, targetPos);
        float angle = Quaternion.Angle(transform.rotation, targetRot);

        // Compute rotation speed so rotation finishes at the same time as position
        float rotationSpeed = 0f;
        if (distance > 0.001f)
        {
            rotationSpeed = angle / distance * moveSpeed;
        }

        // Move position at constant speed
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // Rotate at calculated speed
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

        // Check if arrived
        if (distance <= stopThreshold && angle <= 0.1f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
            moveCamera = false;
            movingBack = false;
        }
    }

    public void MoveCameraToTarget(CameraTarget camTarget)
    {
        if (camTarget == null)
        {
            Debug.LogWarning("CameraMover.MoveCameraToTarget called with null CameraTarget.");
            return;
        }

        targetPosition = camTarget.moveTarget != null ? camTarget.moveTarget : camTarget.transform;
        lookAtTarget = camTarget.lookAtTarget;
        moveCamera = true;
        movingBack = false;
    }

    public void MoveCameraBack()
    {
        moveCamera = true;
        movingBack = true;
    }
}
