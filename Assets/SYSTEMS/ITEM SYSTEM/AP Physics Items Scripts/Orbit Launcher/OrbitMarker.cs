using UnityEngine;

public class OrbitMarker : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRadius = 18f;
    public LayerMask affectedLayers;

    [Header("Orbit Band")]
    public float preferredOrbitRadius = 18f;
    public float inwardPullStrength = 2f;
    public float orbitStrength = 3f;

    [Header("Gravity Dampening")]
    [Range(0f, 1f)]
    public float gravityScaleInsideField = 0.9f;

    [Header("Stabilization")]
    public float velocityDamping = 0.9995f;

    [Header("Inner Safety")]
    public float innerDeadZone = 3f;

    [Header("Lifetime")]
    public float duration = 10f;

    void Start()
{
    // If no layers selected, default to Everything
    if (affectedLayers == 0)
    {
        affectedLayers = ~0; // All layers
    }

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
            if (distance < 0.01f) continue;

            Vector3 radialDir = toCenter.normalized;

            // -------- STABLE ORBIT BAND CORRECTION --------
            // Pull toward preferred orbit radius
            float radiusError = preferredOrbitRadius - distance;
            rb.AddForce(radialDir * radiusError * inwardPullStrength, ForceMode.Acceleration);

            // -------- STABLE TANGENT DIRECTION --------
            Vector3 tangent = Vector3.Cross(radialDir, Vector3.up);

            if (tangent.sqrMagnitude < 0.01f)
                tangent = Vector3.Cross(radialDir, Vector3.right);

            tangent.Normalize();

            // Orbit slightly weaker at outer edge
            float orbitFalloff = Mathf.Clamp01(1f - (distance / detectionRadius));
            rb.AddForce(tangent * orbitStrength * orbitFalloff, ForceMode.Acceleration);

            // -------- REDUCE WORLD GRAVITY --------
            Vector3 gravityCompensation = Physics.gravity * (1f - gravityScaleInsideField);
            rb.AddForce(-gravityCompensation, ForceMode.Acceleration);

            // -------- INNER DEAD ZONE (ANTI-JITTER) --------
            if (distance < innerDeadZone)
            {
                rb.linearVelocity *= 0.85f;
            }

            // -------- GLOBAL STABILIZATION --------
            rb.linearVelocity *= velocityDamping;
        }
    }

    void RemoveMarker()
    {
        Destroy(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, preferredOrbitRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, innerDeadZone);
    }
}