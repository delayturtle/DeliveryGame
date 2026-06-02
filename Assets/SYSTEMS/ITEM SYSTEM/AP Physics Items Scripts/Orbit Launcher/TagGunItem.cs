using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TagGunItem : Item
{
    [Header("Raycast Settings")]
    public float range = 100f;
    public LayerMask hitMask;
    public string[] validTags;

    [Header("Fire Point")]
    public Transform firePoint;
    public string firePointName = "FirePoint";

    private Rigidbody vehicleRb;
    private LineRenderer lineRenderer;

    void Start()
    {
        vehicleRb = GetComponentInParent<Rigidbody>();
        lineRenderer = GetComponent<LineRenderer>();

        if (firePoint == null)
        {
            Transform found = transform.Find(firePointName);
            if (found != null)
                firePoint = found;
        }

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
        if (lineRenderer == null || Camera.main == null)
            return;

        Vector3 origin = GetFireOrigin();
        Vector3 targetPoint = GetCameraTargetPoint();
        Vector3 direction = (targetPoint - origin).normalized;

        RaycastHit hit;
        Vector3 endPoint = origin + direction * range;

        if (Physics.Raycast(origin, direction, out hit, range, hitMask))
        {
            endPoint = hit.point;
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);
    }

    public override void UseItem(Vector3 _)
    {
        if (Camera.main == null)
            return;

        Vector3 origin = GetFireOrigin();
        Vector3 targetPoint = GetCameraTargetPoint();
        Vector3 direction = (targetPoint - origin).normalized;

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

    Vector3 GetCameraTargetPoint()
    {
        Ray camRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit camHit;

        if (Physics.Raycast(camRay, out camHit, range, hitMask))
        {
            return camHit.point;
        }

        return camRay.origin + camRay.direction * range;
    }

    Vector3 GetFireOrigin()
    {
        if (firePoint != null)
            return firePoint.position;

        if (vehicleRb != null)
            return vehicleRb.transform.position;

        return transform.position;
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
        if (vehicleRb == null) return;

        UseItemController controller =
            vehicleRb.GetComponentInChildren<UseItemController>();

        if (controller != null)
            controller.ActiveItem = null;
    }

    // -----------------------------
    // Gizmo (Selected Only)
    // -----------------------------
    void OnDrawGizmosSelected()
    {
        if (Camera.main == null)
            return;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 targetPoint = GetCameraTargetPoint();
        Vector3 direction = (targetPoint - origin).normalized;

        RaycastHit hit;
        Vector3 endPoint = origin + direction * range;

        if (Physics.Raycast(origin, direction, out hit, range, hitMask))
        {
            endPoint = hit.point;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, endPoint);
        Gizmos.DrawSphere(endPoint, 0.3f);
    }
}