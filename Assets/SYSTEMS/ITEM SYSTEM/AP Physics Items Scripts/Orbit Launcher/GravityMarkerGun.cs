using UnityEngine;

public class GravityMarkerGun : Item
{
    [Header("Grenade")]
    public GameObject grenadePrefab;
    
    [Header("Throw Settings")]
    public float throwForce = 15f;
    public Vector3 throwOffset = new Vector3(0, 1, 2);
    
    [Header("Inherit Vehicle Velocity")]
    public bool inheritVehicleVelocity = true;

    private Rigidbody vehicleRb;
    private Transform cachedParent;
    private Vector3 cachedPosition;
    private Quaternion cachedRotation;

    void Start()
    {
        // Cache the vehicle's Rigidbody and transform info
        vehicleRb = GetComponentInParent<Rigidbody>();
        cachedParent = transform.parent;
    }

    void Update()
    {
        // Continuously update cached transform info while parented
        if (transform.parent != null)
        {
            cachedParent = transform.parent;
            cachedPosition = cachedParent.position;
            cachedRotation = cachedParent.rotation;
        }
    }

    public override void UseItem()
    {
        if (grenadePrefab == null)
        {
            Debug.LogError("[GravityMarkerGun] Grenade prefab is not assigned!");
            return;
        }

        // Use cached parent transform (since we're already unparented by UseItemController)
        Vector3 spawnPos = cachedPosition + cachedRotation * throwOffset;
        
        // Instantiate the grenade projectile
        GameObject thrownGrenade = Instantiate(grenadePrefab, spawnPos, cachedRotation);
        
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
            
            // Add throw force forward using cached rotation
            grenadeRb.AddForce(cachedRotation * Vector3.forward * throwForce, ForceMode.Impulse);
            
            Debug.Log("[GravityMarkerGun] Grenade thrown!");
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
