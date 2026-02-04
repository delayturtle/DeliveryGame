using System.Collections;
using UnityEngine;
using Cinemachine;

public class SlowTime : Item
{
    private float fixedDeltaTime;
    
    [Header("Slow Time Settings")]
    public float slowTimeDuration = 5.0f;
    public float slowTimeScale = 0.5f;

    private bool isActive = false;
    private UseItemController itemController;

    void Awake()
    {
        this.fixedDeltaTime = Time.fixedDeltaTime;
    }

    void Start()
    {
        // Find the UseItemController on the parent (player)
        itemController = GetComponentInParent<UseItemController>();
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

        // Start slow time
        Debug.Log("[SlowTime] Activating slow motion");
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = fixedDeltaTime * Time.timeScale;

        // Wait for duration (in real time, not scaled time)
        yield return new WaitForSecondsRealtime(slowTimeDuration);

        // Restore normal time
        Debug.Log("[SlowTime] Restoring normal time");
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = fixedDeltaTime;

        // Remove this item from the active slot
        if (itemController != null)
        {
            itemController.ActiveItem = null;
            Debug.Log("[SlowTime] Removed from active item slot");
        }

        // Destroy this item instance
        Destroy(gameObject);
    }
}
