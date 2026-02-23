using System.Collections;
using UnityEngine;
using Cinemachine;

public class SlowTime : Item
{
    private float fixedDeltaTime;
    
    [Header("Slow Time Settings")]
    public float slowTimeDuration = 5.0f;
    public float slowTimeScale = 0.5f;

    [Header("Camera Settings")]
    [Tooltip("FOV during normal time")]
    public float normalFOV = 60f;
    
    [Tooltip("FOV during slow motion")]
    public float slowedFOV = 75f;

    [Tooltip("Time to transition FOV in/out")]
    public float fovTransitionTime = 0.5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip slowTimeActivateSound;

    private bool isActive = false;
    private UseItemController itemController;
    private CinemachineFreeLook freeLookCamera;

    void Awake()
    {
        this.fixedDeltaTime = Time.fixedDeltaTime;
    }

    void Start()
    {
        // Find the UseItemController on the parent (player)
        itemController = GetComponentInParent<UseItemController>();

        // Auto-find AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = GetComponentInParent<AudioSource>();
            }
        }
    }

    public override void UseItem()
    {
        // Prevent using if already active
        if (isActive)
        {
            Debug.Log("[SlowTime] Already active, cannot use again");
            return;
        }

        StartCoroutine(SlowTimeRoutine());
    }

    private IEnumerator SlowTimeRoutine()
    {
        isActive = true;

        // Play activation sound
        if (audioSource != null && slowTimeActivateSound != null)
        {
            audioSource.PlayOneShot(slowTimeActivateSound);
            Debug.Log("[SlowTime] Played activation sound");
        }
        else
        {
            if (audioSource == null)
                Debug.LogWarning("[SlowTime] No AudioSource assigned!");
            if (slowTimeActivateSound == null)
                Debug.LogWarning("[SlowTime] No activation sound clip assigned!");
        }

        // Find the FreeLook camera at runtime
        freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        
        if (freeLookCamera == null)
        {
            Debug.LogWarning("[SlowTime] No FreeLook camera found in scene!");
        }

        // Start slow time
        Debug.Log("[SlowTime] Activating slow motion");
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = fixedDeltaTime * Time.timeScale;

        // Smoothly transition FOV to slowed value
        if (freeLookCamera != null)
        {
            yield return StartCoroutine(TransitionFOV(normalFOV, slowedFOV, fovTransitionTime));
            Debug.Log($"[SlowTime] Transitioned camera FOV to {slowedFOV}");
        }

        // Wait for duration (in real time, not scaled time)
        yield return new WaitForSecondsRealtime(slowTimeDuration);

        // Restore normal time
        Debug.Log("[SlowTime] Restoring normal time");
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = fixedDeltaTime;

        // Smoothly transition FOV back to normal value
        if (freeLookCamera != null)
        {
            yield return StartCoroutine(TransitionFOV(slowedFOV, normalFOV, fovTransitionTime));
            Debug.Log($"[SlowTime] Restored camera FOV to {normalFOV}");
        }

        // Remove this item from the active slot
        if (itemController != null)
        {
            itemController.ActiveItem = null;
            Debug.Log("[SlowTime] Removed from active item slot");
        }

        // Destroy this item instance
        Destroy(gameObject);
    }

    private IEnumerator TransitionFOV(float startFOV, float endFOV, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time so it works during slow-mo
            float t = Mathf.Clamp01(elapsed / duration);
            
            freeLookCamera.m_Lens.FieldOfView = Mathf.Lerp(startFOV, endFOV, t);
            
            yield return null;
        }

        // Ensure final value is set
        freeLookCamera.m_Lens.FieldOfView = endFOV;
    }
}
