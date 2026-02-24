using UnityEngine;
using TMPro;

public class ItemNameUIController : MonoBehaviour
{
    public UseItemController itemController;
    public TextMeshProUGUI itemNameText;

    void Start()
    {
        if (itemController != null)
        {
            itemController.OnItemChanged += UpdateItemName;
            UpdateItemName(itemController.ActiveItem);
        }
    }

    void UpdateItemName(Item item)
    {
        if (item == null)
        {
            itemNameText.text = "";
            return;
        }

        itemNameText.text = item.ItemName;
    }
}