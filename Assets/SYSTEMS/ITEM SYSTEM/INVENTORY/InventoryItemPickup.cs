using UnityEngine;

public class InventoryItemPickup : MonoBehaviour
{
    [Header("Item Configuration")]
    [Tooltip("Item prefab to instantiate and equip")]
    public GameObject itemPrefab;

    [Header("Pickup Settings")]
    public bool destroyOnPickup = true;
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ItemPickup] Trigger entered by: {other.gameObject.name} with tag: {other.tag}");

        if (!other.CompareTag(playerTag))
        {
            Debug.Log($"[ItemPickup] Tag mismatch. Expected '{playerTag}', got '{other.tag}'");
            return;
        }

        UseItemController itemController = other.GetComponent<UseItemController>();
        if (itemController == null)
        {
            Debug.LogWarning($"[ItemPickup] No UseItemController found on {other.gameObject.name}");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("[ItemPickup] Item Prefab is not assigned!");
            return;
        }

        // Instantiate item as child of player (invisible, persistent)
        GameObject itemInstance = Instantiate(itemPrefab, other.transform);
        Item item = itemInstance.GetComponent<Item>();

        if (item != null)
        {
            Debug.Log($"[ItemPickup] Successfully equipped item: {item.ItemName}");

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