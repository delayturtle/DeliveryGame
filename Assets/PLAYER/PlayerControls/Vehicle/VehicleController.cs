using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public float maxMotorTorque = 1500f;
    public float maxBrakeTorque = 8000f;
    public float turnSensitivity = 1.0f;
    public float maxSteeringAngle = 30.0f;

    public Vector3 _centerOfMass = Vector3.zero;
    public Color gizmoColor = Color.yellow;
    public float gizmoRadius = 0.2f;

    [Header("Engine Audio")]
    public AudioSource engineAudioSource;
    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;
    public float minSpeed = 0f;
    public float maxSpeed = 30f;
    public float pitchSmoothSpeed = 3f;

    [Header("Tire Screech Audio")]
    public AudioSource tireScreechAudioSource;
    public float skidThreshold = 0.3f;
    public float maxSkidIntensity = 1.5f;
    public float screechVolumeSmoothSpeed = 5f;
    public float minSpeedForScreech = 2f;
    public float minScreechVolume = 0.6f;

    public List<Wheel> wheels = new List<Wheel>();

    private InputAction accelerateAction;
    private InputAction reverseAction;
    private InputAction steerAction;

    private Rigidbody carRb;
    private Vector3 lastAppliedCoM;

    void Awake()
    {
        carRb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        ApplyCenterOfMass();
        
        accelerateAction = InputSystem.actions.FindAction("Accelerate");
        reverseAction = InputSystem.actions.FindAction("Reverse");
        steerAction = InputSystem.actions.FindAction("Steer");
        
        if (accelerateAction == null)
            Debug.LogWarning("[VehicleController] 'Accelerate' input action not found!");
        if (reverseAction == null)
            Debug.LogWarning("[VehicleController] 'Reverse' input action not found!");
        if (steerAction == null)
            Debug.LogWarning("[VehicleController] 'Steer' input action not found!");
        
        if (engineAudioSource != null)
        {
            engineAudioSource.loop = true;
            engineAudioSource.pitch = minPitch;
            engineAudioSource.Play();
        }

        if (tireScreechAudioSource != null)
        {
            tireScreechAudioSource.loop = true;
            tireScreechAudioSource.volume = 0f;
            tireScreechAudioSource.Play();
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
        if (Application.isPlaying)
        {
            if (carRb == null) carRb = GetComponent<Rigidbody>();
            if (carRb != null && _centerOfMass != lastAppliedCoM)
                ApplyCenterOfMass();
        }

        UpdateEngineAudio();
        UpdateTireScreechAudio();
    }

    void FixedUpdate()
    {
        Move();
        Steer();
        UpdateWheelVisuals();
    }

    void UpdateEngineAudio()
    {
        if (carRb == null || engineAudioSource == null)
            return;

        float currentSpeed = carRb.linearVelocity.magnitude;
        float speedFactor = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);
        
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * pitchSmoothSpeed);
    }

    void UpdateTireScreechAudio()
    {
        if (tireScreechAudioSource == null || carRb == null)
            return;

        float currentSpeed = carRb.linearVelocity.magnitude;

        // Don't play screech if below minimum speed
        if (currentSpeed < minSpeedForScreech)
        {
            float newVolume = Mathf.Lerp(
                tireScreechAudioSource.volume, 
                0f, 
                Time.deltaTime * screechVolumeSmoothSpeed
            );
            
            // Clamp and validate before setting
            tireScreechAudioSource.volume = Mathf.Clamp01(newVolume);
            return;
        }

        float maxSkid = 0f;

        foreach (var w in wheels)
        {
            if (w.wheelCollider == null) continue;

            WheelHit hit;
            if (w.wheelCollider.GetGroundHit(out hit))
            {
                float forwardSlip = Mathf.Abs(hit.forwardSlip);
                float sidewaysSlip = Mathf.Abs(hit.sidewaysSlip);
                float totalSlip = Mathf.Max(forwardSlip, sidewaysSlip);

                if (totalSlip > maxSkid)
                    maxSkid = totalSlip;
            }
        }

        float targetVolume = 0f;
        if (maxSkid > skidThreshold)
        {
            float skidIntensity = Mathf.InverseLerp(skidThreshold, maxSkidIntensity, maxSkid);
            targetVolume = Mathf.Lerp(minScreechVolume, 1f, skidIntensity);
        }

        // Calculate new volume with lerp
        float lerpedVolume = Mathf.Lerp(
            tireScreechAudioSource.volume, 
            targetVolume, 
            Time.deltaTime * screechVolumeSmoothSpeed
        );

        // IMPORTANT: Clamp and validate the volume to prevent NaN/Infinity
        lerpedVolume = Mathf.Clamp01(lerpedVolume);
        
        // Extra safety check to ensure it's a valid number
        if (float.IsNaN(lerpedVolume) || float.IsInfinity(lerpedVolume))
        {
            lerpedVolume = 0f;
        }

        tireScreechAudioSource.volume = lerpedVolume;
    }

    void Move()
    {
        if (carRb == null) return;

        // Read inputs DIRECTLY - no smoothing/accumulation
        float accelerateInput = accelerateAction != null ? accelerateAction.ReadValue<float>() : 0f;
        float reverseInput = reverseAction != null ? reverseAction.ReadValue<float>() : 0f;
        
        float forwardVel = Vector3.Dot(carRb.linearVelocity, transform.forward);

        // Determine if braking
        bool shouldBrake = reverseInput > 0.1f && forwardVel > 1f;
        bool shouldBrakeReverse = accelerateInput > 0.1f && forwardVel < -1f;

        foreach (var w in wheels)
        {
            if (w.wheelCollider == null) continue;

            float motor = 0f;
            float brake = 0f;

            // BRAKING
            if (shouldBrake || shouldBrakeReverse)
            {
                motor = 0f;
                brake = maxBrakeTorque;
            }
            // DRIVING - Apply torque ONLY if input is pressed
            else if (accelerateInput > 0.01f || reverseInput > 0.01f)
            {
                if (w.isDriven)
                {
                    float inputValue = accelerateInput - reverseInput;
                    motor = inputValue * maxMotorTorque;
                    brake = 0f;
                }
            }
            // NO INPUT - COAST (no motor, no brake)
            else
            {
                motor = 0f;
                brake = 0f;
            }

            w.wheelCollider.motorTorque = motor;
            w.wheelCollider.brakeTorque = brake;
        }
    }

    void Steer()
    {
        float rawSteerInput = steerAction != null ? steerAction.ReadValue<float>() : 0f;
        
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
        foreach (var w in wheels)
        {
            if (w.wheelCollider == null || w.wheelModel == null) continue;

            Vector3 pos;
            Quaternion rot;
            w.wheelCollider.GetWorldPose(out pos, out rot);

            Quaternion offset = Quaternion.Euler(w.modelRotationOffset);
            Quaternion invert = w.invertedModelRotation ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
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