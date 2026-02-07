using UnityEngine;

public class Package : MonoBehaviour
{
    public AudioSource packageSFX;
    public AudioClip packagePickup;
    public Transform grabDropPoint;

    [Header("Pickup Settings")]
    [Tooltip("Collider that defines the drop area (e.g., truck bed)")]
    public Collider dropAreaCollider;
    [Tooltip("If true, randomize position within drop area collider")]
    public bool randomizeDropPosition = true;

    private Rigidbody packageRb;
    private bool isInBumperZone = false;
    private Transform bumperTransform;

    void Awake()
    {
        packageRb = GetComponent<Rigidbody>();
        
        // Ensure Rigidbody has proper collision detection
        if (packageRb != null)
        {
            packageRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            packageRb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the trigger object has the "Bumper" tag
        if (other.CompareTag("Bumper"))
        {
            isInBumperZone = true;
            bumperTransform = other.transform;
            PickupPackage();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Continuously check while in trigger zone
        if (other.CompareTag("Bumper") && !isInBumperZone)
        {
            isInBumperZone = true;
            bumperTransform = other.transform;
            PickupPackage();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Bumper"))
        {
            isInBumperZone = false;
            bumperTransform = null;
        }
    }

    void Update()
    {
        // If we're in the bumper zone but not yet grabbed, force pickup
        if (isInBumperZone && transform.parent != grabDropPoint)
        {
            PickupPackage();
        }
    }

    void PickupPackage()
    {

        // Play pickup sound effect
        if (packageSFX != null && packagePickup != null)
        {
            packageSFX.PlayOneShot(packagePickup);
        }
        else
        {
            Debug.LogWarning($"[Package] Missing AudioSource or AudioClip on {gameObject.name}");
        }

        // Move package to trunk
        if (grabDropPoint != null)
        {
            Vector3 dropPosition;
            Quaternion dropRotation;

            // Get random position within drop area or use fixed point
            if (randomizeDropPosition && dropAreaCollider != null)
            {
                dropPosition = GetRandomPointInCollider(dropAreaCollider);
                dropRotation = Random.rotation; // Random rotation
            }
            else
            {
                dropPosition = grabDropPoint.position;
                dropRotation = grabDropPoint.rotation;
            }

            transform.position = dropPosition;
            transform.rotation = dropRotation;
            transform.SetParent(grabDropPoint);

            // Stop all physics motion
            if (packageRb != null)
            {
                packageRb.linearVelocity = Vector3.zero;
                packageRb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"[Package] {gameObject.name} picked up at {dropPosition}!");

            // Optionally, disable the package GameObject after pickup
            // gameObject.SetActive(false);
        }
    }

    Vector3 GetRandomPointInCollider(Collider col)
    {
        Vector3 randomPoint;
        Bounds bounds = col.bounds;

        // Box Collider
        if (col is BoxCollider)
        {
            randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }
        // Sphere Collider
        else if (col is SphereCollider)
        {
            SphereCollider sphere = col as SphereCollider;
            Vector3 randomInSphere = Random.insideUnitSphere * sphere.radius;
            randomPoint = sphere.transform.TransformPoint(sphere.center + randomInSphere);
        }
        // Generic fallback for other colliders
        else
        {
            randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        return randomPoint;
    }
}

