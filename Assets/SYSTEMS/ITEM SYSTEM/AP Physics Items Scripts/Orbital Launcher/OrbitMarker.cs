using UnityEngine;

public class OrbitMarker : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 12f;
    public LayerMask affectedLayers;

    [Header("Gravity Pull")]
    public float pullForce = 60f;

    [Header("Orbit Motion")]
    public float orbitForce = 12f;
    public float spiralStrength = 6f;

    [Header("Stabilization")]
    public float velocityDamping = 0.98f;

    [Header("Lifetime")]
    public float duration = 6f;

    void Start()
    {
        Invoke(nameof(RemoveMarker), duration);
    }

    void FixedUpdate()
    {
        ApplyOrbitForces();
    }

    void ApplyOrbitForces()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);

        foreach (Collider col in hits)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb == null || rb.gameObject == gameObject) continue;

            if (((1 << rb.gameObject.layer) & affectedLayers) == 0)
                continue;

            Vector3 toCenter = transform.position - rb.worldCenterOfMass;
            float distance = toCenter.magnitude;

            if (distance < 0.01f)
                continue;

            Vector3 radialDir = toCenter.normalized;

            // -------- INWARD GRAVITY --------
            rb.AddForce(radialDir * pullForce, ForceMode.Acceleration);

            // -------- 3D ORBIT FORCE --------
            Vector3 orbitAxis = Vector3.Cross(radialDir, rb.linearVelocity.normalized + Random.insideUnitSphere * 0.2f);
            rb.AddForce(orbitAxis.normalized * orbitForce, ForceMode.Acceleration);

            // -------- SPIRAL INWARD --------
            rb.AddForce(radialDir * spiralStrength, ForceMode.Acceleration);

            // -------- DAMPING --------
            rb.linearVelocity *= velocityDamping;
        }
    }

    void RemoveMarker()
    {
        Destroy(this);
    }
}
