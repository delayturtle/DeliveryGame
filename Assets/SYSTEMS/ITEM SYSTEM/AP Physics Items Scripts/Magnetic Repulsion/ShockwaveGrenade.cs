using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockwaveGrenade : MonoBehaviour
{
    [Header("Trigger")]
    public LayerMask hitLayers = ~0;
    public bool triggerOnce = true;

    [Header("Delay")]
    public float shockwaveDelay = 0.25f;

    [Header("Shockwave Settings")]
    public float shockwaveRadius = 8f;
    public float shockwaveForce = 20f;
    public float upwardBoost = 0.5f;
    public bool useDistanceFalloff = true;

    [Header("Force Falloff Curve")]
    [Tooltip("X = normalized distance (0=center, 1=edge). Y = force multiplier.")]
    public AnimationCurve forceFalloffCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 0.25f)
    );

    [Header("Tags to Affect")]
    public List<string> affectedTags = new List<string>();

    [Header("Line of Sight")]
    [Tooltip("Layers that block the shockwave (walls, terrain, props, etc.)")]
    public LayerMask obstacleLayers;

    [Tooltip("Small vertical offset so the ray doesn't immediately hit the ground")]
    public float raycastStartHeight = 0.25f;

    [Header("VFX")]
    public GameObject shockwaveVFXPrefab;
    public float vfxDestroyAfter = 5f;

    [Header("Debug")]
    public bool drawRadiusGizmo = true;
    public bool debugDrawRays = false;

    private bool hasTriggered = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (triggerOnce && hasTriggered) return;

        if (((1 << collision.gameObject.layer) & hitLayers) == 0)
            return;

        hasTriggered = true;

        Vector3 impactPoint = collision.GetContact(0).point;

        StartCoroutine(ShockwaveRoutine(impactPoint));
    }

    private IEnumerator ShockwaveRoutine(Vector3 center)
    {
        if (shockwaveDelay > 0f)
            yield return new WaitForSeconds(shockwaveDelay);

        SpawnVFX(center);
        TriggerShockwave(center);

        Destroy(gameObject);
    }

    private void SpawnVFX(Vector3 center)
    {
        if (shockwaveVFXPrefab == null) return;

        GameObject vfx = Instantiate(shockwaveVFXPrefab, center, Quaternion.identity);

        if (vfxDestroyAfter > 0f)
            Destroy(vfx, vfxDestroyAfter);
    }

    private void TriggerShockwave(Vector3 center)
    {
        Collider[] hitColliders = Physics.OverlapSphere(center, shockwaveRadius);

        foreach (Collider col in hitColliders)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb == null) continue;

            if (!affectedTags.Contains(col.tag)) continue;

            if (!HasLineOfSight(center, rb.worldCenterOfMass, col))
                continue;

            Vector3 direction = (rb.worldCenterOfMass - center).normalized;
            direction.y += upwardBoost;

            float finalForce = shockwaveForce;

            if (useDistanceFalloff)
            {
                float dist = Vector3.Distance(center, rb.worldCenterOfMass);
                float normalizedDistance = Mathf.Clamp01(dist / shockwaveRadius);

                float multiplier = forceFalloffCurve.Evaluate(normalizedDistance);
                finalForce *= multiplier;
            }

            rb.AddForce(direction.normalized * finalForce, ForceMode.Impulse);
        }
    }

    private bool HasLineOfSight(Vector3 center, Vector3 target, Collider targetCollider)
    {
        Vector3 start = center + Vector3.up * raycastStartHeight;
        Vector3 end = target;

        Vector3 dir = (end - start);
        float dist = dir.magnitude;

        if (dist <= 0.01f) return true;

        dir /= dist;

        if (Physics.Raycast(start, dir, out RaycastHit hit, dist, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            if (debugDrawRays)
                Debug.DrawLine(start, hit.point, Color.red, 1f);

            if (hit.collider != null && hit.collider != targetCollider)
                return false;
        }

        if (debugDrawRays)
            Debug.DrawLine(start, end, Color.green, 1f);

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawRadiusGizmo) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}
