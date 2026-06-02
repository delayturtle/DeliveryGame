using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TagGunItem : Item
{
    [Header("Raycast Settings")]
    public float range = 100f;
    public LayerMask hitMask;
    public string[] validTags;

    [Header("Laser")]
    public float laserHeightOffset = 1.5f;

    private Rigidbody vehicleRb;
    private LineRenderer lineRenderer;

    void Start()
    {
        vehicleRb = GetComponentInParent<Rigidbody>();
       
        lineRenderer = GetComponent<LineRenderer>();
        

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
        }
    }
  
    void Update()
    {
        UpdateLaser();
    }

    void UpdateLaser()
    {
        if (vehicleRb == null || lineRenderer == null || Camera.main == null)
            return;

        Transform vehicleTransform = vehicleRb.transform;

        Vector3 origin = vehicleTransform.position + Vector3.up * laserHeightOffset;
        
        Vector3 direction = Camera.main.transform.forward;

        RaycastHit hit;

        Vector3 endPoint = origin + direction * range;

        if (Physics.Raycast(origin, direction, out hit, range, hitMask))
        {
            endPoint = hit.point;
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    public override void UseItem(Vector3 direction)
    {
        if (vehicleRb == null)
            return;

        Transform vehicleTransform = vehicleRb.transform;

        Vector3 origin = vehicleTransform.position + Vector3.up * laserHeightOffset;
        direction = direction.normalized;

        RaycastHit hit;

        if (Physics.Raycast(origin, direction, out hit, range, hitMask))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (IsValidTag(hitObject.tag))
            {
                ApplyOrbitMarker(hitObject);
                Debug.Log("[TagGunItem] Tagged: " + hitObject.name);
            }
        }

        ClearActiveItem();
        Destroy(gameObject);
    }

    bool IsValidTag(string tagToCheck)
    {
        foreach (string tag in validTags)
        {
            if (tagToCheck == tag)
                return true;
        }
        return false;
    }

    void ApplyOrbitMarker(GameObject target)
    {
        if (target.GetComponent<OrbitMarker>() != null)
            return;

        target.AddComponent<OrbitMarker>();
    }

    void ClearActiveItem()
    {
        UseItemController controller =
            vehicleRb.GetComponentInChildren<UseItemController>();

        if (controller != null)
            controller.ActiveItem = null;
    }
    
}