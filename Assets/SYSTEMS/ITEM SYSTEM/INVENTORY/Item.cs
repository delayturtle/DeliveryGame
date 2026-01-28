using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [SerializeField]
    private string itemName;

    public string ItemName
    {
        get { return itemName; }
    }

    // Called when the item is used
    public abstract void UseItem();

    // Called when the item use is released/ended
    public virtual void StopUsingItem()
    {
        // Optional override for items that need cleanup
    }
}
