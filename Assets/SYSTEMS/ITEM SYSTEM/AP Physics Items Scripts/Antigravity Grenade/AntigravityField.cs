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
    [Tooltip("Instant upward velocity applied to break ground contact")]
    public float liftKickVelocity = 2.5f;

    [Tooltip("Multiplier applied to suspension spring while inside field")]
    public float suspensionSoftening = 0.35f;

    [Tooltip("Multiplier applied to wheel friction while inside field")]
    public float frictionSoftening = 0.4f;

    [Header("Gravity Return")]
    public float gravityRestoreTime = 1.5f;

    [Header("Random Rotation")]
    [Tooltip("How much torque to apply to objects caught in the field.")]
    public float randomTorqueStrength = 1.5f;

    [Tooltip("Random rotation speed cap to prevent crazy spinning.")]
    public float maxAngularVelocity = 3.5f;

    [Tooltip("Vehicles get less random torque than normal objects.")]
    public float vehicleTorqueMultiplier = 0.25f;

    // Internal state
    private HashSet<Rigidbody> affectedBodies = new HashSet<Rigidbody>();
    private Dictionary<Rigidbody, Coroutine> restoreRoutines = new Dictionary<Rigidbody, Coroutine>();

    // Each RB gets a consistent random spin axis
    private Dictionary<Rigidbody, Vector3> torqueAxes = new Dictionary<Rigidbody, Vector3>();

    private class WheelBackup
    {
        public WheelCollider wheel;
        public JointSpring spring;
        public WheelFrictionCurve forwardFriction;
        public WheelFrictionCurve sidewaysFriction;
    }

    private Dictionary<Rigidbody, List<WheelBackup>> wheelData =
        new Dictionary<Rigidbody, List<WheelBackup>>();

    private void Start()
    {
        Invoke(nameof(DisableField), duration);
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

            // ------------------ RANDOM ROTATION ------------------
            if (randomTorqueStrength > 0f)
            {
                if (!torqueAxes.TryGetValue(rb, out Vector3 axis))
                {
                    axis = Random.onUnitSphere;
                    torqueAxes[rb] = axis;
                }

                float torque = randomTorqueStrength;

                // vehicles spin less
                if (isVehicle)
                    torque *= vehicleTorqueMultiplier;

                rb.AddTorque(axis * torque, ForceMode.Acceleration);

                // clamp spin
                if (rb.angularVelocity.magnitude > maxAngularVelocity)
                {
                    rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
                }
            }
            // -----------------------------------------------------

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

        // Assign a stable random torque axis for this RB
        if (!torqueAxes.ContainsKey(rb))
            torqueAxes[rb] = Random.onUnitSphere;

        // Cancel restore if re-entering
        if (restoreRoutines.TryGetValue(rb, out Coroutine routine))
        {
            StopCoroutine(routine);
            restoreRoutines.Remove(rb);
        }

        if (IsVehicle(rb))
        {
            ApplyVehicleOverrides(rb);

            // Small vertical kick to break wheel-ground contact
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

        // cleanup torque axis when object exits
        torqueAxes.Remove(rb);
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
