using UnityEngine;
using UnityEngine.InputSystem;

public class TruckPowerupHandler : MonoBehaviour
{
    public TruckInventory inventory;
    public UseItemController itemController;

    private InputAction grabItemAction;

    void Start()
    {
        grabItemAction = InputSystem.actions.FindAction("GrabItem");
        
        if (grabItemAction == null)
        {
            Debug.LogWarning("[TruckPowerupHandler] 'GrabItem' input action not found!");
        }
    }

    void Update()
    {
        if (grabItemAction != null && grabItemAction.WasPressedThisFrame())
        {
            TryUseRandomPackage();
        }
    }

    void TryUseRandomPackage()
    {
        if (inventory == null || itemController == null)
            return;

        if (itemController.ActiveItem != null)
        {
            Debug.Log("Already holding a powerup.");
            return;
        }

        GameObject package = inventory.ConsumeRandomPackage();

        if (package == null)
        {
            Debug.Log("No packages to consume.");
            return;
        }

        PackagePowerup powerup = package.GetComponent<PackagePowerup>();

        if (powerup == null || powerup.itemPrefab == null)
        {
            Debug.Log("Package has no powerup attached.");
            Destroy(package);
            return;
        }

        // Instantiate powerup item
        GameObject itemInstance = Instantiate(powerup.itemPrefab, itemController.transform);
        Item item = itemInstance.GetComponent<Item>();

        if (item != null)
        {
            Renderer[] renderers = itemInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }

            itemController.ActiveItem = item;
        }
        else
        {
            Debug.LogError("Powerup prefab missing Item component.");
            Destroy(itemInstance);
        }

        Destroy(package);
    }
}