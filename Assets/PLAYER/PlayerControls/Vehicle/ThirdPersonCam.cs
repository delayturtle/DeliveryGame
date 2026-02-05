using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("Cinemachine")]
    [Tooltip("Reference to the Cinemachine FreeLook camera")]
    public CinemachineFreeLook freeLookCamera;

    [Header("Input Settings")]
    public float mouseSensitivity = 1f;
    public float gamepadSensitivity = 100f;
    public bool invertY = false;

    private InputAction lookAction;

    void Start()
    {
        // Get the Look input action from the Input System
        lookAction = InputSystem.actions.FindAction("Look");

        if (lookAction == null)
        {
            Debug.LogWarning("[ThirdPersonCam] 'Look' input action not found. Make sure it's defined in your Input Actions.");
        }

        if (freeLookCamera == null)
        {
            freeLookCamera = GetComponent<CinemachineFreeLook>();
        }

        if (freeLookCamera == null)
        {
            Debug.LogError("[ThirdPersonCam] No CinemachineFreeLook camera assigned or found!");
        }
    }

    void Update()
    {
        if (freeLookCamera == null || lookAction == null)
            return;

        // Get input from mouse/right stick
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // Detect if input is from gamepad or mouse
        bool isGamepad = Gamepad.current != null && 
                         (Mathf.Abs(Gamepad.current.rightStick.x.ReadValue()) > 0.1f || 
                          Mathf.Abs(Gamepad.current.rightStick.y.ReadValue()) > 0.1f);

        // Apply appropriate sensitivity
        float sensitivity = isGamepad ? gamepadSensitivity : mouseSensitivity;

        // Apply horizontal rotation (X-axis)
        freeLookCamera.m_XAxis.Value += lookInput.x * sensitivity * Time.deltaTime;

        // Apply vertical rotation (Y-axis)
        float yInput = invertY ? lookInput.y : -lookInput.y;
        freeLookCamera.m_YAxis.Value += yInput * sensitivity * Time.deltaTime;
    }
}
