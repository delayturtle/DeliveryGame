using UnityEngine;

public class GravityMarkerGun : Item
{
    [Header("Gun")]
    public float range = 120f;
    public LayerMask hitLayers;

    [Header("Marker Settings")]
    public float markerDuration = 6f;

    private Transform cameraTransform;

    public override void UseItem()
    {
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInParent<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }

        if (cameraTransform == null)
            return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
        {
            ApplyMarker(hit.collider.gameObject);
        }
    }

    void ApplyMarker(GameObject target)
    {
        OrbitMarker existing = target.GetComponent<OrbitMarker>();
        if (existing != null)
            Destroy(existing);

        OrbitMarker marker = target.AddComponent<OrbitMarker>();
        marker.duration = markerDuration;
    }
}
