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

    [Header("Engine Audio")]
    public AudioSource idleAudioSource;
    public AudioSource drivingAudioSource;
    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;
    public float minSpeed = 0f;
    public float maxSpeed = 30f;
    public float idleThreshold = 0.5f; // Speed below which idle plays

    public List<Wheel> wheels = new List<Wheel>();

    float rawMoveInput;
    float rawSteerInput;
    float currentThrottle = 0f;
    float previousInput = 0f;

    private Rigidbody carRb;
    private Vector3 lastAppliedCoM;
    private bool isPlayingIdle = false;

    void Awake()
    {
        carRb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        ApplyCenterOfMass();
        
        // Set up audio sources to loop
        if (idleAudioSource != null)
        {
            idleAudioSource.loop = true;
            idleAudioSource.Play();
            isPlayingIdle = true;
        }
        
        if (drivingAudioSource != null)
        {
            drivingAudioSource.loop = true;
            drivingAudioSource.volume = 0f; // Start silent
            drivingAudioSource.Play();
        }
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

        UpdateEngineAudio();
    }

    void FixedUpdate()
    {
        UpdateThrottleSmoothing();
        Move();
        Steer();
        UpdateWheelVisuals(); // keep visuals in sync with physics
    }

    void UpdateEngineAudio()
    {
        if (carRb == null || (idleAudioSource == null && drivingAudioSource == null))
            return;

        float currentSpeed = carRb.linearVelocity.magnitude;

        // Switch between idle and driving based on speed
        if (currentSpeed < idleThreshold)
        {
            // Playing idle sound
            if (!isPlayingIdle)
            {
                if (idleAudioSource != null)
                    idleAudioSource.volume = Mathf.Lerp(idleAudioSource.volume, 1f, Time.deltaTime * 5f);
                if (drivingAudioSource != null)
                    drivingAudioSource.volume = Mathf.Lerp(drivingAudioSource.volume, 0f, Time.deltaTime * 5f);

                if (idleAudioSource != null && idleAudioSource.volume > 0.9f)
                    isPlayingIdle = true;
            }
            else
            {
                if (idleAudioSource != null)
                    idleAudioSource.volume = 1f;
                if (drivingAudioSource != null)
                    drivingAudioSource.volume = 0f;
            }
        }
        else
        {
            // Playing driving sound with dynamic pitch
            if (isPlayingIdle)
            {
                if (idleAudioSource != null)
                    idleAudioSource.volume = Mathf.Lerp(idleAudioSource.volume, 0f, Time.deltaTime * 5f);
                if (drivingAudioSource != null)
                    drivingAudioSource.volume = Mathf.Lerp(drivingAudioSource.volume, 1f, Time.deltaTime * 5f);

                if (drivingAudioSource != null && drivingAudioSource.volume > 0.9f)
                    isPlayingIdle = false;
            }
            else
            {
                if (idleAudioSource != null)
                    idleAudioSource.volume = 0f;
                if (drivingAudioSource != null)
                    drivingAudioSource.volume = 1f;
            }

            // Adjust pitch based on speed
            if (drivingAudioSource != null)
            {
                float speedFactor = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
                float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);
                drivingAudioSource.pitch = Mathf.Lerp(drivingAudioSource.pitch, targetPitch, Time.deltaTime * 3f);
            }
        }
    }

    void UpdateThrottleSmoothing()
    {
        float target = rawMoveInput;
        
        // Detect direction change (opposite input from current throttle)
        bool directionChanged = Mathf.Sign(target) != 0 && 
                               Mathf.Sign(currentThrottle) != 0 && 
                               Mathf.Sign(target) != Mathf.Sign(currentThrottle);
        
        // If direction changed, instantly zero throttle
        if (directionChanged)
        {
            currentThrottle = 0f;
            previousInput = target;
            return;
        }
        
        // If no input, decelerate faster
        if (Mathf.Abs(target) < 0.01f)
        {
            currentThrottle = Mathf.MoveTowards(currentThrottle, 0f, decelResponse * Time.fixedDeltaTime);
        }
        else
        {
            float responseRate = (Mathf.Abs(target) > Mathf.Abs(currentThrottle)) ? accelResponse : decelResponse;
            currentThrottle = Mathf.MoveTowards(currentThrottle, target, responseRate * Time.fixedDeltaTime);
        }
        
        previousInput = target;
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
                    // Instantly kill throttle when braking against motion
                    currentThrottle = 0f;
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
                    // Ensure throttle is zero when braking
                    currentThrottle = 0f;
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