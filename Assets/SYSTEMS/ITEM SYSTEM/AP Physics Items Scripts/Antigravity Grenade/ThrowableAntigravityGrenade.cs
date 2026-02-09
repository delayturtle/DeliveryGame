using UnityEngine;

public class ThrowableAntigravityGrenade : Item
{
    [Header("Grenade")]
    public GameObject grenadePrefab;

    [Header("Fire Point")]
    public Transform firePoint;
    public bool aimWithCamera = true;
    public Transform cameraTransform;

    [Header("Throw Force (Local XYZ)")]
    public Vector3 throwForce = new Vector3(0f, 5f, 15f);

    [Header("Fallback Offset (if no firePoint)")]
    public Vector3 throwOffset = new Vector3(0, 1, 2);

    [Header("Inherit Vehicle Velocity")]
    public bool inheritVehicleVelocity = true;

    private Rigidbody vehicleRb;

    void Start()
    {
        vehicleRb = GetComponentInParent<Rigidbody>();

        // ---------- AUTO FIND FIREPOINT ----------
        if (firePoint == null)
        {
            FirePoint fp = GetComponentInParent<FirePoint>();
            if (fp != null)
                firePoint = fp.transform;
        }

        // ---------- AUTO FIND CAMERA ----------
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInParent<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }
    }

    public override void UseItem()
    {
        if (grenadePrefab == null)
        {
            Debug.LogError("[ThrowableAntigravityGrenade] Grenade prefab not assigned!");
            return;
        }

        Transform parentTransform = transform.parent != null ? transform.parent : transform;

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (firePoint != null)
        {
            spawnPos = firePoint.position;
            spawnRot = firePoint.rotation;
        }
        else
        {
            spawnPos = parentTransform.position + parentTransform.TransformDirection(throwOffset);
            spawnRot = parentTransform.rotation;
        }

        GameObject thrownGrenade = Instantiate(grenadePrefab, spawnPos, spawnRot);

        Renderer[] renderers = thrownGrenade.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
            renderer.enabled = true;

        Rigidbody grenadeRb = thrownGrenade.GetComponent<Rigidbody>();

        if (grenadeRb != null)
        {
            if (inheritVehicleVelocity && vehicleRb != null)
            {
                grenadeRb.linearVelocity = vehicleRb.linearVelocity;
            }

            Transform aimSource = parentTransform;

            if (aimWithCamera && cameraTransform != null)
                aimSource = cameraTransform;
            else if (firePoint != null)
                aimSource = firePoint;

            Vector3 forceVector =
                aimSource.right * throwForce.x +
                aimSource.up * throwForce.y +
                aimSource.forward * throwForce.z;

            grenadeRb.AddForce(forceVector, ForceMode.Impulse);

            Debug.Log("[ThrowableAntigravityGrenade] Grenade thrown!");
        }

        UseItemController itemController = GetComponentInParent<UseItemController>();
        if (itemController != null)
            itemController.ActiveItem = null;

        Destroy(gameObject);
    }
}
