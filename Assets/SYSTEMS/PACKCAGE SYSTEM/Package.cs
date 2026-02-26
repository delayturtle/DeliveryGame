using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class Package : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip pickupSound;

    private Rigidbody rb;
    private AudioSource audioSource;

    private float pickupCooldown = 0.3f;
    private float lastPickupTime = -10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
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

                // Play pickup sound
                if (audioSource != null && pickupSound != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                }

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