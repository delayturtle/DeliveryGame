using UnityEngine;
using UnityEngine.InputSystem;

public class UseItemController : MonoBehaviour
{
    [Header("Item State")]
    [SerializeField] private Item activeItem = null;

    [Header("References")]
    [SerializeField] private Transform playerCamera;

    private InputAction useAction;

    // Event for UI and other systems
    public System.Action<Item> OnItemChanged;

    public Item ActiveItem
    {
        get => activeItem;
        set
        {
            if (activeItem == value)
                return;

            activeItem = value;
            OnItemChanged?.Invoke(activeItem);
        }
    }

    public bool IsEquipped => activeItem != null;

    void Awake()
    {
        useAction = InputSystem.actions["UseItem"];
    }

    void Start()
    {
        // Auto-assign camera if not set
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        // Fire initial state for UI
        OnItemChanged?.Invoke(activeItem);
    }

    void Update()
    {
        if (!IsEquipped || useAction == null)
            return;

        // Button Pressed (Use)
        if (useAction.WasPressedThisFrame())
        {
            UseActiveItem();
        }

        // Button Released (Stop Using)
        if (useAction.WasReleasedThisFrame())
        {
            activeItem?.StopUsingItem();
        }
    }

    void UseActiveItem()
    {
        if (activeItem == null)
            return;

        if (playerCamera == null)
        {
            Debug.LogWarning("[UseItemController] No playerCamera assigned.");
            return;
        }

        Vector3 lookDirection = playerCamera.forward;

        activeItem.UseItem(lookDirection);

        // If item destroyed itself inside UseItem(),
        // ActiveItem may already be null.
        if (activeItem == null)
        {
            OnItemChanged?.Invoke(null);
        }
    }

    /// <summary>
    /// Safely clears the active item (can be called externally).
    /// </summary>
    public void ClearActiveItem()
    {
        if (activeItem == null)
            return;

        activeItem = null;
        OnItemChanged?.Invoke(null);
    }
}