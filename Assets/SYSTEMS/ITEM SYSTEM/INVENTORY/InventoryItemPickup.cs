using UnityEngine;

public class InventoryItemPickup : MonoBehaviour
{
    [Header("Item Configuration")]
    [Tooltip("Item prefab to instantiate and equip")]
    public GameObject itemPrefab;

    [Header("Pickup Settings")]
    public bool destroyOnPickup = true;
    public string playerTag = "Player";
    
    [Header("Startup Protection")]
    [Tooltip("Delay before pickup becomes active (prevents instant pickup on spawn)")]
    public float startupDelay = 0.5f;

    private bool canPickup = false;

    private void Start()
    {
        // Validate prefab assignment
        if (itemPrefab == null)
        {
            Debug.LogError($"[ItemPickup] {gameObject.name} has no Item Prefab assigned!", this);
        }
        else
        {
            Debug.Log($"[ItemPickup] {gameObject.name} configured with prefab: {itemPrefab.name}");
        }

        // Enable pickup after a short delay
        Invoke(nameof(EnablePickup), startupDelay);
    }

    private void EnablePickup()
    {
        canPickup = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't allow pickup until startup delay has passed
        if (!canPickup)
            return;

        Debug.Log($"[ItemPickup] {gameObject.name} trigger entered by: {other.gameObject.name} with tag: {other.tag}");

        if (!other.CompareTag(playerTag))
        {
            Debug.Log($"[ItemPickup] Tag mismatch. Expected '{playerTag}', got '{other.tag}'");
            return;
        }

        // Search for UseItemController on the object or its parents
        UseItemController itemController = other.GetComponentInParent<UseItemController>();
        if (itemController == null)
        {
            Debug.LogWarning($"[ItemPickup] No UseItemController found on {other.gameObject.name} or its parents");
            return;
        }

        Debug.Log($"[ItemPickup] Found UseItemController on {itemController.gameObject.name}");

        // Check if player already has an item equipped
        if (itemController.ActiveItem != null)
        {
            Debug.Log($"[ItemPickup] Player already has an item equipped: {itemController.ActiveItem.ItemName}. Cannot pick up {gameObject.name}.");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError($"[ItemPickup] {gameObject.name} - Item Prefab is not assigned!");
            return;
        }

        Debug.Log($"[ItemPickup] {gameObject.name} instantiating prefab: {itemPrefab.name}");

        // Instantiate item as child of the object with UseItemController
        GameObject itemInstance = Instantiate(itemPrefab, itemController.transform);
        Item item = itemInstance.GetComponent<Item>();

        if (item != null)
        {
            Debug.Log($"[ItemPickup] {gameObject.name} successfully equipped item: {item.GetType().Name} - {item.ItemName}");

            // Disable all renderers if any
            Renderer[] renderers = itemInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }

            itemController.ActiveItem = item;

            if (destroyOnPickup)
                Destroy(gameObject);
        }
        else
        {
            Debug.LogError($"[ItemPickup] Item prefab '{itemPrefab.name}' does not have an Item component!");
            Destroy(itemInstance);
        }
    }
}