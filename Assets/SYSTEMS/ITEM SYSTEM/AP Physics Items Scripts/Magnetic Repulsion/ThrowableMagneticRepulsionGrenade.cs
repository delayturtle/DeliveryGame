using UnityEngine;

public class ThrowableMagneticRepulsionGrenade : Item
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

    public override void UseItem()
    {
        if (grenadePrefab == null)
        {
            Debug.LogError("[ThrowableMagneticRepulsionGrenade] Grenade prefab is not assigned!");
            return;
        }

        Transform parentTransform = transform.parent != null ? transform.parent : transform;

        Vector3 spawnPos =
            parentTransform.position +
            parentTransform.TransformDirection(throwOffset);

        GameObject thrownGrenade =
            Instantiate(grenadePrefab, spawnPos, parentTransform.rotation);

        // Ensure visuals are enabled
        Renderer[] renderers = thrownGrenade.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }

        Rigidbody grenadeRb = thrownGrenade.GetComponent<Rigidbody>();
        if (grenadeRb != null)
        {
            if (inheritVehicleVelocity && vehicleRb != null)
            {
                grenadeRb.linearVelocity = vehicleRb.linearVelocity;
            }

            grenadeRb.AddForce(
                parentTransform.forward * throwForce,
                ForceMode.Impulse
            );
        }

        // Remove from inventory
        UseItemController controller = GetComponentInParent<UseItemController>();
        if (controller != null)
        {
            controller.ActiveItem = null;
        }

        Destroy(gameObject);
    }
}
