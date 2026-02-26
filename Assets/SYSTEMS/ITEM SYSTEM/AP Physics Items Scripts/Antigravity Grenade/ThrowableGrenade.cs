using UnityEngine;

public class ThrowableAntigravityGrenade : Item
{
    [Header("Grenade")]
    public GameObject grenadePrefab;

    [Header("Throw Settings")]
    public float throwForce = 15f;
    public Vector3 throwOffset = new Vector3(0, 1, 2);

    [Header("Inherit Vehicle Velocity")]
    public bool inheritVehicleVelocity = true;

    private Rigidbody vehicleRb;

    void Start()
    {
        vehicleRb = GetComponentInParent<Rigidbody>();
    }

    public override void UseItem(Vector3 direction)
    {
        if (grenadePrefab == null)
        {
            Debug.LogError("[ThrowableAntigravityGrenade] Grenade prefab is not assigned!");
            return;
        }

        Transform parentTransform = transform.parent != null ? transform.parent : transform;

        direction = direction.normalized;

        // Spawn slightly in look direction
        Vector3 spawnPos =
            parentTransform.position +
            direction * throwOffset.z +
            Vector3.up * throwOffset.y;

        GameObject thrownGrenade =
            Instantiate(grenadePrefab, spawnPos, Quaternion.LookRotation(direction));

        // Ensure renderers enabled
        Renderer[] renderers = thrownGrenade.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }

        Rigidbody grenadeRb = thrownGrenade.GetComponent<Rigidbody>();

        if (grenadeRb != null)
        {
            // Inherit vehicle velocity if enabled
            if (inheritVehicleVelocity && vehicleRb != null)
            {
                grenadeRb.linearVelocity = vehicleRb.linearVelocity;
            }

            grenadeRb.AddForce(direction * throwForce, ForceMode.Impulse);
        }

        // Remove item from active slot
        UseItemController itemController = GetComponentInParent<UseItemController>();
        if (itemController != null)
        {
            itemController.ActiveItem = null;
        }

        Destroy(gameObject);
    }
}