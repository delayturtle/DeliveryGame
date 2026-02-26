using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [Header("Item Info")]

    [SerializeField]
    private string itemName = "Item";

    [SerializeField]
    private Sprite itemIcon;   // 👈 For HUD display

    // Public accessors
    public string ItemName
    {
        get { return itemName; }
    }

    public Sprite ItemIcon
    {
        get { return itemIcon; }
    }

    // =====================================================
    // ITEM BEHAVIOR
    // =====================================================

    // Called when the item is used
    // Direction = where the player is aiming
    public abstract void UseItem(Vector3 direction);

    // Optional override for items that require hold/release behavior
    public virtual void StopUsingItem()
    {
        // Optional override for items that need cleanup
    }

    // =====================================================
    // OPTIONAL UTILITY
    // =====================================================

    /// <summary>
    /// Clears this item from the UseItemController safely.
    /// </summary>
    protected void ClearFromController()
    {
        UseItemController controller = GetComponentInParent<UseItemController>();

        if (controller != null && controller.ActiveItem == this)
        {
            controller.ActiveItem = null;
        }
    }
}