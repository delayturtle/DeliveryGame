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

    [Header("Obstacle Avoidance")]
    public bool enableObstacleAvoidance = true;
    public float obstacleDetectionDistance = 15f;
    public float safetyMargin = 3f; // How far to stay away from obstacles
    public float obstacleAvoidanceForce = 3f;
    public float avoidancePriority = 0.9f;
    public LayerMask obstacleLayer = ~0;
    public int numberOfRays = 9;
    public float raySpreadAngle = 70f;
    public float sideRayOffset = 1.5f; // Offset rays to sides of vehicle
    public bool debugDrawRays = true;

    [Header("AI Behavior")]
    public float maxSpeed = 20f;
    public float minSpeed = 5f;
    public float steeringSensitivity = 1.5f;
    public float sharpTurnAngle = 45f;
    public float sharpTurnSpeedMultiplier = 0.5f;

    [Header("Vehicle Physics (Same as VehicleController)")]
    public float maxAcceleration = 30.0f;
    public float brakeAcceleration = 50.0f;
    public float maxMotorTorque = 1500f;
    public float maxBrakeTorque = 8000f;
    public float accelResponse = 6f;
    public float decelResponse = 12f;
    public float turnSensitivity = 1.0f;
    public float maxSteeringAngle = 30.0f;

    [Header("Center of Mass")]
    public Vector3 _centerOfMass = Vector3.zero;
    public Color gizmoColor = Color.red;
    public float gizmoRadius = 0.2f;

    [Header("Wheels")]
    public List<Wheel> wheels = new List<Wheel>();

    // AI-controlled inputs (replaces player input)
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
    }

    void FixedUpdate()
    {
        UpdateThrottleSmoothing();
        Move();
        Steer();
        UpdateWheelVisuals();
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
            
            if (obstacleDetected > 0.1f) // If obstacle is detected
            {
                isAvoidingObstacle = true;
                // Prioritize avoidance - blend heavily toward avoidance steering
                aiSteerInput = Mathf.Lerp(targetSteering, avoidanceSteering, avoidancePriority);
                
                // Slow down when avoiding obstacles
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
            // Drive forward
            if (currentSpeed < maxSpeed)
            {
                aiMoveInput = 1f;
            }
            else
            {
                aiMoveInput = 0f; // Coast at max speed
            }

            // Slow down for sharp turns
            if (Mathf.Abs(angleToTarget) > sharpTurnAngle)
            {
                aiMoveInput *= sharpTurnSpeedMultiplier;
            }

            // Ensure minimum speed
            if (currentSpeed < minSpeed)
            {
                aiMoveInput = 1f;
            }
        }
        else
        {
            // Brake when within stopping distance
            aiMoveInput = -1f;
        }
    }

    float GetObstacleAvoidanceSteering(out float obstacleStrength)
    {
        float avoidanceSteering = 0f;
        obstacleStrength = 0f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f; // Slightly above ground

        float closestObstacleDistance = obstacleDetectionDistance;
        float preferredSteerDirection = 0f;

        // Cast multiple rays in a cone in front of the vehicle
        for (int i = 0; i < numberOfRays; i++)
        {
            // Calculate angle for this ray
            float t = numberOfRays > 1 ? (float)i / (numberOfRays - 1) : 0.5f; // 0 to 1
            float angle = Mathf.Lerp(-raySpreadAngle, raySpreadAngle, t);
            
            // Calculate ray direction
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward;
            
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, obstacleDetectionDistance, obstacleLayer))
            {
                // Ignore self collisions
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                    continue;

                // Track closest obstacle
                if (hit.distance < closestObstacleDistance)
                {
                    closestObstacleDistance = hit.distance;
                }

                // Calculate avoidance strength based on:
                // 1. How close the obstacle is (closer = stronger)
                // 2. How centered the ray is (center rays = stronger avoidance)
                float distanceFactor = 1f - (hit.distance / obstacleDetectionDistance);
                float angleFactor = 1f - (Mathf.Abs(angle) / raySpreadAngle);
                float rayAvoidanceStrength = distanceFactor * angleFactor;

                // Accumulate obstacle strength
                obstacleStrength = Mathf.Max(obstacleStrength, rayAvoidanceStrength);

                // Determine which way to steer
                // If obstacle is on the right (positive angle), steer left (negative)
                // If obstacle is on the left (negative angle), steer right (positive)
                float steerDirection = -Mathf.Sign(angle);
                
                // Weight the steering direction by how strong this detection is
                preferredSteerDirection += steerDirection * rayAvoidanceStrength;
                
                // Accumulate avoidance steering
                avoidanceSteering += steerDirection * rayAvoidanceStrength * obstacleAvoidanceForce;

                // Debug visualization
                if (debugDrawRays)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.red);
                }
            }
            else
            {
                // No hit - draw green ray
                if (debugDrawRays)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * obstacleDetectionDistance, Color.green);
                }
            }
        }

        // If we detected an obstacle, make sure we're steering away strongly
        if (obstacleStrength > 0.1f)
        {
            // Normalize and amplify the steering
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
                brake = 0f; // Coast
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
        // Draw center of mass
        Vector3 worldCoM;
        Rigidbody rb = carRb ? carRb : GetComponent<Rigidbody>();
        if (rb != null)
            worldCoM = rb.worldCenterOfMass;
        else
            worldCoM = transform.TransformPoint(_centerOfMass);

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(worldCoM, gizmoRadius);
        Gizmos.DrawWireSphere(worldCoM, gizmoRadius * 1.2f);

        // Draw AI target info
        if (targetPoint != null)
        {
            Gizmos.color = isAvoidingObstacle ? Color.red : Color.cyan;
            Gizmos.DrawLine(transform.position, targetPoint.position);
            Gizmos.DrawWireSphere(targetPoint.position, stoppingDistance);
        }

        // Draw obstacle detection cone
        if (enableObstacleAvoidance)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            Gizmos.color = isAvoidingObstacle ? Color.red : Color.yellow;
            
            // Draw cone edges
            Vector3 leftDir = Quaternion.Euler(0, -raySpreadAngle, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, raySpreadAngle, 0) * transform.forward;
            
            Gizmos.DrawLine(rayOrigin, rayOrigin + leftDir * obstacleDetectionDistance);
            Gizmos.DrawLine(rayOrigin, rayOrigin + rightDir * obstacleDetectionDistance);
        }
    }
}
