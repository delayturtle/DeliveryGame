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
        fixedDeltaTime = Time.fixedDeltaTime;
    }

    void Start()
    {
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

    // ✅ Updated signature to match new Item base class
    public override void UseItem(Vector3 direction)
    {
        // Direction not needed for this item

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

        freeLookCamera = FindObjectOfType<CinemachineFreeLook>();

        if (freeLookCamera == null)
        {
            Debug.LogWarning("[SlowTime] No FreeLook camera found in scene!");
        }

        Debug.Log("[SlowTime] Activating slow motion");

        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = fixedDeltaTime * Time.timeScale;

        if (freeLookCamera != null)
        {
            yield return StartCoroutine(
                TransitionFOV(normalFOV, slowedFOV, fovTransitionTime)
            );
        }

        yield return new WaitForSecondsRealtime(slowTimeDuration);

        Debug.Log("[SlowTime] Restoring normal time");

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = fixedDeltaTime;

        if (freeLookCamera != null)
        {
            yield return StartCoroutine(
                TransitionFOV(slowedFOV, normalFOV, fovTransitionTime)
            );
        }

        if (itemController != null)
        {
            itemController.ActiveItem = null;
        }

        Destroy(gameObject);
    }

    private IEnumerator TransitionFOV(float startFOV, float endFOV, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            freeLookCamera.m_Lens.FieldOfView =
                Mathf.Lerp(startFOV, endFOV, t);

            yield return null;
        }

        freeLookCamera.m_Lens.FieldOfView = endFOV;
    }
}