using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum Axel { Front, Rear }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;
        public Axel axel;
        public bool isDriven;
        public bool invertedModelRotation;
        public Vector3 modelRotationOffset;
    }

    [Header("AI Target")]
    public Transform targetPoint;
    public float stoppingDistance = 5f;
    public float targetUpdateInterval = 0.2f;
    
    [Header("Player Targeting")]
    [Tooltip("Tag to identify the player (default: 'Player')")]
    public string playerTag = "Player";
    
    [Tooltip("Auto-find and target player on spawn")]
    public bool autoTargetPlayer = true;

    [Header("Obstacle Avoidance")]
    public bool enableObstacleAvoidance = true;
    public float obstacleDetectionDistance = 15f;
    public float safetyMargin = 3f;
    public float obstacleAvoidanceForce = 3f;
    public float avoidancePriority = 0.9f;
    public LayerMask obstacleLayer = ~0;
    public int numberOfRays = 9;
    public float raySpreadAngle = 70f;
    public float sideRayOffset = 1.5f;
    public bool debugDrawRays = true;

    [Header("AI Behavior")]
    public float maxSpeed = 20f;
    public float minSpeed = 5f;
    public float steeringSensitivity = 1.5f;
    public float sharpTurnAngle = 45f;
    public float sharpTurnSpeedMultiplier = 0.5f;

    [Header("Vehicle Physics")]
    public float maxAcceleration = 30.0f;
    public float brakeAcceleration = 50.0f;
    public float maxMotorTorque = 1500f;
    public float maxBrakeTorque = 8000f;
    public float accelResponse = 6f;
    public float decelResponse = 12f;
    public float turnSensitivity = 1.0f;
    public float maxSteeringAngle = 30.0f;

    [Header("Engine Audio")]
    public AudioSource engineAudioSource;
    public float minPitch = 0.8f;
    public float maxPitch = 2.0f;
    public float minSpeedForPitch = 0f;
    public float maxSpeedForPitch = 30f;
    public float pitchSmoothSpeed = 3f;

    [Header("Tire Screech Audio")]
    public AudioSource tireScreechAudioSource;
    public float skidThreshold = 0.3f;
    public float maxSkidIntensity = 1.5f;
    public float screechVolumeSmoothSpeed = 5f;
    public float minSpeedForScreech = 2f;
    public float minScreechVolume = 0.6f;

    [Header("Center of Mass")]
    public Vector3 _centerOfMass = Vector3.zero;
    public Color gizmoColor = Color.red;
    public float gizmoRadius = 0.2f;

    [Header("Wheels")]
    public List<Wheel> wheels = new List<Wheel>();

    // AI-controlled inputs
    private float aiMoveInput;
    private float aiSteerInput;
    private float currentThrottle = 0f;

    private Rigidbody carRb;
    private Vector3 lastAppliedCoM;
    private float targetUpdateTimer;
    private bool isAvoidingObstacle = false;

    void Awake()
    {
        carRb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        ApplyCenterOfMass();
        
        // Auto-find player if enabled
        if (autoTargetPlayer && targetPoint == null)
        {
            FindAndTargetPlayer();
        }

        // Initialize engine audio
        if (engineAudioSource != null)
        {
            engineAudioSource.loop = true;
            engineAudioSource.pitch = minPitch;
            engineAudioSource.Play();
        }

        // Initialize tire screech audio
        if (tireScreechAudioSource != null)
        {
            tireScreechAudioSource.loop = true;
            tireScreechAudioSource.volume = 0f;
            tireScreechAudioSource.Play();
        }
    }

    void FindAndTargetPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        
        if (player != null)
        {
            targetPoint = player.transform;
            Debug.Log($"[EnemyAI] Found and targeting player: {player.name}");
        }
        else
        {
            Debug.LogWarning($"[EnemyAI] Could not find player with tag '{playerTag}'");
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
        // Update AI inputs based on target
        UpdateAIInputs();

        // Apply center of mass if changed
        if (Application.isPlaying)
        {
            if (carRb == null) carRb = GetComponent<Rigidbody>();
            if (carRb != null && _centerOfMass != lastAppliedCoM)
                ApplyCenterOfMass();
        }

        // Update audio
        UpdateEngineAudio();
        UpdateTireScreechAudio();
    }

    void FixedUpdate()
    {
        UpdateThrottleSmoothing();
        Move();
        Steer();
        UpdateWheelVisuals();
    }

    void UpdateEngineAudio()
    {
        if (carRb == null || engineAudioSource == null)
            return;

        float currentSpeed = carRb.linearVelocity.magnitude;
        float speedFactor = Mathf.InverseLerp(minSpeedForPitch, maxSpeedForPitch, currentSpeed);
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
            tireScreechAudioSource.volume = Mathf.Lerp(
                tireScreechAudioSource.volume, 
                0f, 
                Time.deltaTime * screechVolumeSmoothSpeed
            );
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

        tireScreechAudioSource.volume = Mathf.Lerp(
            tireScreechAudioSource.volume, 
            targetVolume, 
            Time.deltaTime * screechVolumeSmoothSpeed
        );
    }

    void UpdateAIInputs()
    {
        if (targetPoint == null)
        {
            aiMoveInput = 0;
            aiSteerInput = 0;
            return;
        }

        // Calculate direction to target
        Vector3 directionToTarget = targetPoint.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        // Calculate local target position for steering
        Vector3 localTarget = transform.InverseTransformPoint(targetPoint.position);
        float angleToTarget = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

        // Base AI Steering (-1 to 1)
        float targetSteering = Mathf.Clamp(angleToTarget / maxSteeringAngle * steeringSensitivity, -1f, 1f);

        // Apply obstacle avoidance
        if (enableObstacleAvoidance)
        {
            float avoidanceSteering = GetObstacleAvoidanceSteering(out float obstacleDetected);
            
            if (obstacleDetected > 0.1f)
            {
                isAvoidingObstacle = true;
                aiSteerInput = Mathf.Lerp(targetSteering, avoidanceSteering, avoidancePriority);
                aiMoveInput *= (1f - obstacleDetected * 0.5f);
            }
            else
            {
                isAvoidingObstacle = false;
                aiSteerInput = targetSteering;
            }
        }
        else
        {
            aiSteerInput = targetSteering;
        }

        // AI Throttle/Brake
        float currentSpeed = carRb.linearVelocity.magnitude;

        if (distanceToTarget > stoppingDistance)
        {
            if (currentSpeed < maxSpeed)
            {
                aiMoveInput = 1f;
            }
            else
            {
                aiMoveInput = 0f;
            }

            if (Mathf.Abs(angleToTarget) > sharpTurnAngle)
            {
                aiMoveInput *= sharpTurnSpeedMultiplier;
            }

            if (currentSpeed < minSpeed)
            {
                aiMoveInput = 1f;
            }
        }
        else
        {
            aiMoveInput = -1f;
        }
    }

    float GetObstacleAvoidanceSteering(out float obstacleStrength)
    {
        float avoidanceSteering = 0f;
        obstacleStrength = 0f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        float closestObstacleDistance = obstacleDetectionDistance;
        float preferredSteerDirection = 0f;

        for (int i = 0; i < numberOfRays; i++)
        {
            float t = numberOfRays > 1 ? (float)i / (numberOfRays - 1) : 0.5f;
            float angle = Mathf.Lerp(-raySpreadAngle, raySpreadAngle, t);
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward;
            
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, obstacleDetectionDistance, obstacleLayer))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                    continue;

                if (hit.distance < closestObstacleDistance)
                {
                    closestObstacleDistance = hit.distance;
                }

                float distanceFactor = 1f - (hit.distance / obstacleDetectionDistance);
                float angleFactor = 1f - (Mathf.Abs(angle) / raySpreadAngle);
                float rayAvoidanceStrength = distanceFactor * angleFactor;

                obstacleStrength = Mathf.Max(obstacleStrength, rayAvoidanceStrength);

                float steerDirection = -Mathf.Sign(angle);
                preferredSteerDirection += steerDirection * rayAvoidanceStrength;
                avoidanceSteering += steerDirection * rayAvoidanceStrength * obstacleAvoidanceForce;

                if (debugDrawRays)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.red);
                }
            }
            else
            {
                if (debugDrawRays)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * obstacleDetectionDistance, Color.green);
                }
            }
        }

        if (obstacleStrength > 0.1f)
        {
            avoidanceSteering = Mathf.Sign(preferredSteerDirection) * Mathf.Clamp01(obstacleStrength) * obstacleAvoidanceForce;
        }

        return Mathf.Clamp(avoidanceSteering, -1f, 1f);
    }

    void UpdateThrottleSmoothing()
    {
        float target = aiMoveInput;
        float responseRate = (Mathf.Abs(target) > Mathf.Abs(currentThrottle)) ? accelResponse : decelResponse;
        currentThrottle = Mathf.MoveTowards(currentThrottle, target, responseRate * Time.fixedDeltaTime);
    }

    void Move()
    {
        if (carRb == null) carRb = GetComponent<Rigidbody>();
        if (carRb == null) return;

        float forwardVel = Vector3.Dot(carRb.linearVelocity, transform.forward);

        foreach (var w in wheels)
        {
            if (w.wheelCollider == null) continue;

            float motor = 0f;
            float brake = 0f;

            if (Mathf.Abs(currentThrottle) > 0.001f)
            {
                bool reversingAgainstMotion = 
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
                motor = 0f;
                brake = 0f;
            }

            w.wheelCollider.motorTorque = motor;
            w.wheelCollider.brakeTorque = brake;
        }
    }

    void Steer()
    {
        float steerAngle = aiSteerInput * turnSensitivity * maxSteeringAngle;
        
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

        if (targetPoint != null)
        {
            Gizmos.color = isAvoidingObstacle ? Color.red : Color.cyan;
            Gizmos.DrawLine(transform.position, targetPoint.position);
            Gizmos.DrawWireSphere(targetPoint.position, stoppingDistance);
        }

        if (enableObstacleAvoidance)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            Gizmos.color = isAvoidingObstacle ? Color.red : Color.yellow;
            
            Vector3 leftDir = Quaternion.Euler(0, -raySpreadAngle, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, raySpreadAngle, 0) * transform.forward;
            
            Gizmos.DrawLine(rayOrigin, rayOrigin + leftDir * obstacleDetectionDistance);
            Gizmos.DrawLine(rayOrigin, rayOrigin + rightDir * obstacleDetectionDistance);
        }
    }
}
