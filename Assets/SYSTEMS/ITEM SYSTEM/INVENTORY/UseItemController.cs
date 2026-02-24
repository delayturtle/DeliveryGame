using UnityEngine;
using UnityEngine.InputSystem;

public class UseItemController : MonoBehaviour
{
    [SerializeField]
    private Item activeItem = null;

    private InputAction useAction;
public System.Action<Item> OnItemChanged;

    public Item ActiveItem
{
    get { return activeItem; }
    set
    {
        activeItem = value;
        OnItemChanged?.Invoke(activeItem);
    }
}

    public bool IsEquipped
    {
        get { return activeItem != null; }
    }

    void Start()
    {
        useAction = InputSystem.actions["UseItem"];
    }

    void Update()
    {
        if (IsEquipped && useAction.WasPressedThisFrame())
        {
            // Unparent the item before using it
            if (activeItem.transform.parent != null)
            {
                activeItem.transform.SetParent(null);
            }

            activeItem.UseItem();
        }
    }
}
