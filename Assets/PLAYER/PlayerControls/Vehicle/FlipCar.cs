using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class FlipCar : MonoBehaviour
{
    [Header("Flip Settings")]
    public GameObject flipObject;
    public Transform basePos;
    public Transform flipPos;
    public float flipDuration = 1f;

    [Header("Animation")]
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private InputAction flipAction;
    private bool isFlipping = false;

    void Start()
    {
        // Get the Flip input action
        flipAction = InputSystem.actions.FindAction("Flip");
        
        if (flipAction == null)
        {
            Debug.LogWarning("[FlipCar] 'Flip' input action not found. Make sure it's defined in your Input Actions.");
        }

        // Ensure flipObject starts at basePos
        if (flipObject != null && basePos != null)
        {
            flipObject.transform.position = basePos.position;
            flipObject.transform.rotation = basePos.rotation;
        }
    }

    void Update()
    {
        // Check if Flip input was pressed
        if (flipAction != null && flipAction.WasPressedThisFrame() && !isFlipping)
        {
            StartCoroutine(FlipSequence());
        }
    }

    private IEnumerator FlipSequence()
    {
        if (flipObject == null || basePos == null || flipPos == null)
        {
            Debug.LogError("[FlipCar] Missing required references!");
            yield break;
        }

        isFlipping = true;

        // Move from basePos to flipPos
        yield return StartCoroutine(MoveObject(basePos, flipPos, flipDuration));

        // Move back from flipPos to basePos
        yield return StartCoroutine(MoveObject(flipPos, basePos, flipDuration));

        isFlipping = false;
    }

    private IEnumerator MoveObject(Transform fromPos, Transform toPos, float duration)
    {
        float elapsed = 0f;

        Vector3 startPosition = fromPos.position;
        Quaternion startRotation = fromPos.rotation;
        Vector3 endPosition = toPos.position;
        Quaternion endRotation = toPos.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = flipCurve.Evaluate(t);

            // Lerp position and rotation
            flipObject.transform.position = Vector3.Lerp(startPosition, endPosition, curvedT);
            flipObject.transform.rotation = Quaternion.Slerp(startRotation, endRotation, curvedT);

            yield return null;
        }

        // Ensure final position is exact
        flipObject.transform.position = endPosition;
        flipObject.transform.rotation = endRotation;
    }
}
