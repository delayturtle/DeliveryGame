using UnityEngine;

public class OrbitMarker : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 15f;
    public LayerMask affectedLayers;

    [Header("Gravity Model")]
    public float gravityStrength = 120f;
    public float minDistanceClamp = 1.5f;

    [Header("Orbit Motion")]
    public float orbitStrength = 25f;

    [Header("Spiral")]
    public float spiralDecay = 0.5f;

    [Header("Stabilization")]
    public float velocityDamping = 0.998f;

    [Header("Lifetime")]
    public float duration = 6f;

    void Start()
    {
        Invoke(nameof(RemoveMarker), duration);
    }

    void FixedUpdate()
    {
        ApplyOrbitPhysics();
    }

    void ApplyOrbitPhysics()
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

            // Clamp to avoid singularities
            float safeDistance = Mathf.Max(distance, minDistanceClamp);

            // -------- INVERSE-SQUARE GRAVITY --------
            float gravityForce = gravityStrength / (safeDistance * safeDistance);
            rb.AddForce(radialDir * gravityForce, ForceMode.Acceleration);

            // -------- DISTANCE-BASED ORBIT --------
            Vector3 tangentialDir = Vector3.Cross(radialDir, rb.linearVelocity.normalized + Vector3.up * 0.01f);
            rb.AddForce(tangentialDir.normalized * orbitStrength / safeDistance, ForceMode.Acceleration);

            // -------- SPIRAL DECAY --------
            rb.AddForce(radialDir * spiralDecay, ForceMode.Acceleration);

            // -------- MASS-AWARE DAMPING --------
            float massFactor = Mathf.Clamp(rb.mass * 0.1f, 0.85f, 0.995f);
            rb.linearVelocity *= velocityDamping * massFactor;
        }
    }

    void RemoveMarker()
    {
        Destroy(this);
    }
}
