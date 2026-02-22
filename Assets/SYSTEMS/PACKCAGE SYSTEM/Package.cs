using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Package : MonoBehaviour
{
    private Rigidbody rb;

    private float pickupCooldown = 0.3f;
    private float lastPickupTime = -10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bumper"))
        {
            if (Time.time - lastPickupTime < pickupCooldown)
                return;

            TruckCargo truck = other.GetComponentInParent<TruckCargo>();

            if (truck != null)
            {
                lastPickupTime = Time.time;
                TeleportToDropPoint(truck.GetDropPoint());
            }
        }
    }

    private void TeleportToDropPoint(Transform dropPoint)
    {
        if (dropPoint == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = dropPoint.position;
        rb.rotation = dropPoint.rotation;
    }
}