using UnityEngine;
using UnityEngine.InputSystem;

public class UseItemController : MonoBehaviour
{
    [SerializeField]
    private Item activeItem = null;

    private InputAction useAction;

    public Item ActiveItem
    {
        get { return activeItem; }
        set { activeItem = value; }
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
            activeItem.UseItem();
        }
    }
}
