using UnityEngine;

public class MouseLockController : MonoBehaviour
{
    public bool lockOnStart = true;

    void Start()
    {
        if (lockOnStart)
            LockCursor();
    }

    void Update()
    {
        // Press Escape to unlock cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        // Click to re-lock cursor
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
