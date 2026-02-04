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
        // Cache the vehicle's Rigidbody
        vehicleRb = GetComponentInParent<Rigidbody>();
    }

    public override void UseItem()
    {

        if (grenadePrefab == null)
        {
            Debug.LogError("[ThrowableAntigravityGrenade] Grenade prefab is not assigned!");
            return;
        }

        // Get the parent transform (vehicle)
        Transform parentTransform = transform.parent != null ? transform.parent : transform;

        // Calculate spawn position in front of vehicle
        Vector3 spawnPos = parentTransform.position + parentTransform.TransformDirection(throwOffset);
        
        // Instantiate the grenade projectile
        GameObject thrownGrenade = Instantiate(grenadePrefab, spawnPos, parentTransform.rotation);
        
        // Ensure all renderers are enabled on the thrown grenade
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
            
            // Add throw force forward
            grenadeRb.AddForce(parentTransform.forward * throwForce, ForceMode.Impulse);
            
            Debug.Log("[ThrowableAntigravityGrenade] Grenade thrown!");
        }

        // Remove item from active slot
        UseItemController itemController = GetComponentInParent<UseItemController>();
        if (itemController != null)
        {
            itemController.ActiveItem = null;
        }

        // Single-use item - destroy after throwing
        Destroy(gameObject);
    }
}