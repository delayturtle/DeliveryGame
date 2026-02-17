using UnityEngine;

/// <summary>
/// Attach this to a GameObject that represents a camera spot.
/// Set moveTarget (optional) and lookAtTarget in the inspector.
/// If moveTarget is left empty it will default to the GameObject's transform.
/// </summary>
public class CameraTarget : MonoBehaviour
{
    [Tooltip("Where the camera should move to. If empty, this GameObject's transform will be used.")]
    public Transform moveTarget;

    [Tooltip("What the camera should look at while moving. Optional.")]
    public Transform lookAtTarget;

    void Reset()
    {
        // When component is added in editor, default moveTarget to this transform
        if (moveTarget == null) moveTarget = transform;
    }

    void OnValidate()
    {
        // Keep the default when editing in inspector
        if (moveTarget == null) moveTarget = transform;
    }
}
