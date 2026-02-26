using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemUIController : MonoBehaviour
{
    public UseItemController itemController;

    [Header("UI References")]
    public GameObject itemDisplayContainer;   // Parent container
    public TextMeshProUGUI itemNameText;
    public Image itemIconImage;

    void Start()
    {
        if (itemController != null)
        {
            itemController.OnItemChanged += UpdateUI;
            UpdateUI(itemController.ActiveItem);
        }
    }

    void UpdateUI(Item item)
    {
        // No item equipped
        if (item == null)
        {
            if (itemDisplayContainer != null)
                itemDisplayContainer.SetActive(false);

            return;
        }

        // Item equipped
        if (itemDisplayContainer != null)
            itemDisplayContainer.SetActive(true);

        if (itemNameText != null)
            itemNameText.text = item.ItemName;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = item.ItemIcon;
            itemIconImage.enabled = item.ItemIcon != null;
        }
    }
}