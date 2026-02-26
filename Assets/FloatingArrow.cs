using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    public float floatHeight = 3f;
    public float floatSpeed = 2f;
    public float rotateSpeed = 90f;

    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        if (target == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Follow target
        Vector3 targetPosition = target.position + Vector3.up * floatHeight;
        transform.position = targetPosition;

        // Spin
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }
}
