using UnityEngine;

public class DeliveryPoint : MonoBehaviour
{
    [Header("Reward")]
    public int rewardPoints = 100;

    public AudioSource DeliverySounds;
    public AudioClip deliveryClip;

    [Header("Respawn Settings")]
    public float respawnTime = 15f;

    private QuestManager questManager;
    private Collider zoneCollider;
    private Renderer zoneRenderer;

    private bool isActive = true;

    void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        zoneRenderer = GetComponent<Renderer>();
    }

    public void SetQuestManager(QuestManager manager)
    {
        questManager = manager;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
            return;

        if (!other.CompareTag("Package"))
            return;

        if (questManager == null)
            return;

        // Deliver ONE package
        questManager.CompleteDelivery(this);
        Destroy(other.gameObject);
        DeliverySounds.PlayOneShot(deliveryClip);

        DisableZone();
    }

    void DisableZone()
    {
        isActive = false;

        if (zoneCollider != null)
            zoneCollider.enabled = false;

        if (zoneRenderer != null)
            zoneRenderer.enabled = false;

        Invoke(nameof(RespawnZone), respawnTime);
    }

    void RespawnZone()
    {
        isActive = true;

        if (zoneCollider != null)
            zoneCollider.enabled = true;

        if (zoneRenderer != null)
            zoneRenderer.enabled = true;
    }

    public bool IsActive()
    {
        return isActive;
    }
}