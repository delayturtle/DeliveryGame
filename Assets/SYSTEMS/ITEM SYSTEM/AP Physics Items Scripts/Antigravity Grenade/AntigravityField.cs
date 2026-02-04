using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntigravityField : MonoBehaviour
{
    [Header("Lifetime")]
    public float duration = 5f;

    [Header("Affectable Objects")]
    public List<string> affectedTags = new List<string>();
    public List<string> vehicleTags = new List<string>();

    [Header("Upward Pull (Drag Style)")]
    public float upwardPull = 14f;
    public float velocityResistance = 0.6f;
    public float maxUpwardAcceleration = 16f;

    [Header("Vehicle Scaling")]
    public float vehiclePullMultiplier = 0.4f;
    public float angularDamping = 1.2f;

    [Header("WheelCollider Handling")]
    public float liftKickVelocity = 2.5f;
    public float suspensionSoftening = 0.35f;
    public float frictionSoftening = 0.4f;

    [Header("Gravity Return")]
    public float gravityRestoreTime = 1.5f;

    [Header("Spawn Expansion VFX")]
    [Tooltip("Optional visual object (mesh sphere, particle system root, VFX graph object, etc.)")]
    public Transform vfxRoot;

    [Tooltip("Seconds it takes for the field to expand from 0 to full size")]
    public float expandTime = 0.35f;

    [Tooltip("Expansion curve (0->1). Leave default for nice ease-in/out.")]
    public AnimationCurve expandCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private SphereCollider sphereTrigger;

    private float initialTriggerRadius;
    private Vector3 initialVfxScale;

    // Internal state
    private HashSet<Rigidbody> affectedBodies = new HashSet<Rigidbody>();
    private Dictionary<Rigidbody, Coroutine> restoreRoutines = new Dictionary<Rigidbody, Coroutine>();

    private class WheelBackup
    {
        public WheelCollider wheel;
        public JointSpring spring;
        public WheelFrictionCurve forwardFriction;
        public WheelFrictionCurve sidewaysFriction;
    }

    private Dictionary<Rigidbody, List<WheelBackup>> wheelData =
        new Dictionary<Rigidbody, List<WheelBackup>>();

    private void Awake()
    {
        sphereTrigger = GetComponent<SphereCollider>();
        if (sphereTrigger != null)
            initialTriggerRadius = sphereTrigger.radius;

        if (vfxRoot != null)
            initialVfxScale = vfxRoot.localScale;
    }

    private void Start()
    {
        // Start collapsed then expand
        StartCoroutine(ExpandFieldRoutine());

        Invoke(nameof(DisableField), duration);
    }

    private IEnumerator ExpandFieldRoutine()
    {
        // Collapse instantly first
        if (sphereTrigger != null)
            sphereTrigger.radius = 0f;

        if (vfxRoot != null)
            vfxRoot.localScale = Vector3.zero;

        // Expand smoothly
        float t = 0f;
        while (t < expandTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / expandTime);
            float eased = expandCurve.Evaluate(a);

            if (sphereTrigger != null)
                sphereTrigger.radius = Mathf.Lerp(0f, initialTriggerRadius, eased);

            if (vfxRoot != null)
                vfxRoot.localScale = Vector3.Lerp(Vector3.zero, initialVfxScale, eased);

            yield return null;
        }

        // Ensure final values
        if (sphereTrigger != null)
            sphereTrigger.radius = initialTriggerRadius;

        if (vfxRoot != null)
            vfxRoot.localScale = initialVfxScale;
    }

    private void FixedUpdate()
    {
        foreach (Rigidbody rb in affectedBodies)
        {
            if (rb == null) continue;

            bool isVehicle = IsVehicle(rb);

            float pull = upwardPull;
            if (isVehicle)
                pull *= vehiclePullMultiplier;

            float verticalSpeed = rb.linearVelocity.y;
            float adjustedPull = pull - (verticalSpeed * velocityResistance);
            adjustedPull = Mathf.Clamp(adjustedPull, 0f, maxUpwardAcceleration);

            rb.AddForce(Vector3.up * adjustedPull, ForceMode.Acceleration);

            if (isVehicle)
            {
                rb.angularVelocity *= Mathf.Clamp01(
                    1f - angularDamping * Time.fixedDeltaTime
                );
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!affectedTags.Contains(other.tag))
            return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || affectedBodies.Contains(rb))
            return;

        rb.useGravity = false;
        affectedBodies.Add(rb);

        // Cancel restore if re-entering
        if (restoreRoutines.TryGetValue(rb, out Coroutine routine))
        {
            StopCoroutine(routine);
            restoreRoutines.Remove(rb);
        }

        if (IsVehicle(rb))
        {
            ApplyVehicleOverrides(rb);
            rb.AddForce(Vector3.up * liftKickVelocity, ForceMode.VelocityChange);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || !affectedBodies.Contains(rb))
            return;

        affectedBodies.Remove(rb);
        restoreRoutines[rb] = StartCoroutine(RestoreGravityAndVehicle(rb));
    }

    private void DisableField()
    {
        foreach (Rigidbody rb in affectedBodies)
        {
            if (rb != null)
                restoreRoutines[rb] = StartCoroutine(RestoreGravityAndVehicle(rb));
        }

        affectedBodies.Clear();
        Destroy(gameObject);
    }

    private IEnumerator RestoreGravityAndVehicle(Rigidbody rb)
    {
        rb.useGravity = true;
        RestoreVehicleOverrides(rb);

        float t = 0f;
        while (t < gravityRestoreTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        restoreRoutines.Remove(rb);
    }

    // ---------------- VEHICLE HELPERS ----------------

    private void ApplyVehicleOverrides(Rigidbody rb)
    {
        if (wheelData.ContainsKey(rb))
            return;

        WheelCollider[] wheels = rb.GetComponentsInChildren<WheelCollider>();
        if (wheels.Length == 0)
            return;

        List<WheelBackup> backups = new List<WheelBackup>();

        foreach (WheelCollider wheel in wheels)
        {
            WheelBackup backup = new WheelBackup
            {
                wheel = wheel,
                spring = wheel.suspensionSpring,
                forwardFriction = wheel.forwardFriction,
                sidewaysFriction = wheel.sidewaysFriction
            };

            JointSpring softenedSpring = wheel.suspensionSpring;
            softenedSpring.spring *= suspensionSoftening;
            wheel.suspensionSpring = softenedSpring;

            WheelFrictionCurve fwd = wheel.forwardFriction;
            fwd.stiffness *= frictionSoftening;
            wheel.forwardFriction = fwd;

            WheelFrictionCurve side = wheel.sidewaysFriction;
            side.stiffness *= frictionSoftening;
            wheel.sidewaysFriction = side;

            backups.Add(backup);
        }

        wheelData.Add(rb, backups);
    }

    private void RestoreVehicleOverrides(Rigidbody rb)
    {
        if (!wheelData.TryGetValue(rb, out List<WheelBackup> backups))
            return;

        foreach (WheelBackup backup in backups)
        {
            if (backup.wheel == null) continue;

            backup.wheel.suspensionSpring = backup.spring;
            backup.wheel.forwardFriction = backup.forwardFriction;
            backup.wheel.sidewaysFriction = backup.sidewaysFriction;
        }

        wheelData.Remove(rb);
    }

    private bool IsVehicle(Rigidbody rb)
    {
        foreach (string tag in vehicleTags)
        {
            if (rb.CompareTag(tag))
                return true;
        }
        return false;
    }
}
