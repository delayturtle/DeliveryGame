using System;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public enum Axel { Front, Rear }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;        // visual mesh (optional)
        public WheelCollider wheelCollider;
        public Axel axel;
        public bool isDriven;                // mark true for wheels that receive motor/brake
        public bool invertedModelRotation;   // flip visual rotation direction if needed
        public Vector3 modelRotationOffset;  // tweak in Inspector (degrees) to align model with collider
    }

    // ... your drive / brake / tuning fields ...
    public float maxAcceleration = 30.0f;
    public float brakeAcceleration = 50.0f;
    public float maxMotorTorque = 1500f;
    public float maxBrakeTorque = 8000f;
    public float accelResponse = 6f;
    public float decelResponse = 12f;
    public bool brakeOnReverseInput = true;
    public KeyCode brakeKey = KeyCode.None;
    public float turnSensitivity = 1.0f;
    public float maxSteeringAngle = 30.0f;

    public Vector3 _centerOfMass = Vector3.zero;
    public Color gizmoColor = Color.yellow;
    public float gizmoRadius = 0.2f;

    public List<Wheel> wheels = new List<Wheel>();

    float rawMoveInput;
    float rawSteerInput;
    float currentThrottle = 0f;

    private Rigidbody carRb;
    private Vector3 lastAppliedCoM;

    void Awake()
    {
        carRb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        ApplyCenterOfMass();
    }

    void OnValidate()
    {
        Rigidbody rb = carRb ? carRb : GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass = _centerOfMass;
            lastAppliedCoM = _centerOfMass;
        }
    }

    void Update()
    {
        rawMoveInput = Input.GetAxis("Vertical");
        rawSteerInput = Input.GetAxis("Horizontal");

        if (Application.isPlaying)
        {
            if (carRb == null) carRb = GetComponent<Rigidbody>();
            if (carRb != null && _centerOfMass != lastAppliedCoM)
                ApplyCenterOfMass();
        }
    }

    void FixedUpdate()
    {
        UpdateThrottleSmoothing();
        Move();
        Steer();
        UpdateWheelVisuals(); // keep visuals in sync with physics
    }

    void UpdateThrottleSmoothing()
    {
        float target = rawMoveInput;
        float responseRate = (Mathf.Abs(target) > Mathf.Abs(currentThrottle)) ? accelResponse : decelResponse;
        currentThrottle = Mathf.MoveTowards(currentThrottle, target, responseRate * Time.fixedDeltaTime);
    }

    void Move()
    {
        if (carRb == null) carRb = GetComponent<Rigidbody>();
        if (carRb == null) return;

        float forwardVel = Vector3.Dot(carRb.linearVelocity, transform.forward);
        bool explicitBrakePressed = (brakeKey != KeyCode.None) && Input.GetKey(brakeKey);

        foreach (var w in wheels)
        {
            if (w.wheelCollider == null) continue;

            float motor = 0f;
            float brake = 0f;

            if (Mathf.Abs(currentThrottle) > 0.001f)
            {
                bool reversingAgainstMotion = brakeOnReverseInput &&
                    Mathf.Sign(currentThrottle) != 0f &&
                    Mathf.Sign(currentThrottle) != Mathf.Sign(forwardVel) &&
                    Mathf.Abs(forwardVel) > 0.5f;

                if (reversingAgainstMotion)
                {
                    brake = maxBrakeTorque;
                    motor = 0f;
                }
                else
                {
                    if (w.isDriven)
                    {
                        motor = currentThrottle * maxMotorTorque;
                        brake = 0f;
                    }
                    else
                    {
                        motor = 0f;
                        brake = 0f;
                    }
                }
            }
            else
            {
                if (explicitBrakePressed)
                {
                    motor = 0f;
                    brake = maxBrakeTorque;
                }
                else
                {
                    motor = 0f;
                    brake = 0f; // coast
                }
            }

            w.wheelCollider.motorTorque = motor;
            w.wheelCollider.brakeTorque = brake;
        }
    }

    void Steer()
    {
        float steerAngle = rawSteerInput * turnSensitivity * maxSteeringAngle;
        if (carRb != null)
        {
            float speed = carRb.linearVelocity.magnitude;
            float steerFactor = Mathf.Lerp(1f, 0.25f, Mathf.InverseLerp(0f, 30f, speed));
            steerAngle *= steerFactor;
        }

        foreach (var wheel in wheels)
        {
            if (wheel.wheelCollider == null) continue;
            if (wheel.axel == Axel.Front)
                wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, steerAngle, 0.6f);
        }
    }

    void UpdateWheelVisuals()
    {
        // Use WheelCollider.GetWorldPose() and apply per-wheel rotation offset to align the mesh.
        foreach (var w in wheels)
        {
            if (w.wheelCollider == null || w.wheelModel == null) continue;

            Vector3 pos;
            Quaternion rot;
            w.wheelCollider.GetWorldPose(out pos, out rot);

            // Apply the per-wheel inspector offset (degrees)
            Quaternion offset = Quaternion.Euler(w.modelRotationOffset);

            // If you need to invert rotation direction for the model (rare), flip around Y.
            Quaternion invert = w.invertedModelRotation ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;

            // Final model rotation: collider rotation * offset * optional invert
            Quaternion modelRot = rot * offset * invert;

            w.wheelModel.transform.position = pos;
            w.wheelModel.transform.rotation = modelRot;
        }
    }

    private void ApplyCenterOfMass()
    {
        if (carRb == null) carRb = GetComponent<Rigidbody>();
        if (carRb != null)
        {
            carRb.centerOfMass = _centerOfMass;
            lastAppliedCoM = _centerOfMass;
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 worldCoM;
        Rigidbody rb = carRb ? carRb : GetComponent<Rigidbody>();
        if (rb != null)
            worldCoM = rb.worldCenterOfMass;
        else
            worldCoM = transform.TransformPoint(_centerOfMass);

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(worldCoM, gizmoRadius);
        Gizmos.DrawWireSphere(worldCoM, gizmoRadius * 1.2f);
    }
}