using UnityEngine;

public class CrosshairUIController : MonoBehaviour
{
    public UseItemController itemController;
    public GameObject crosshairUI;

    void Update()
    {
        if (itemController == null || crosshairUI == null)
            return;

        crosshairUI.SetActive(itemController.IsEquipped);
    }
}