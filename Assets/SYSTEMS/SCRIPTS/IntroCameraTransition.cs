using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System.Collections;

public class IntroCameraTransition : MonoBehaviour
{
    public CinemachineVirtualCameraBase tvCamera;
    public CinemachineVirtualCameraBase playerCamera;

    public Image fadeImage;
    public float fadeDuration = 1f;

    void Start()
    {
        // Ensure TV cam starts active
        tvCamera.Priority = 20;
        playerCamera.Priority = 10;

        StartCoroutine(FadeFromBlack());
    }

    public void SwitchToPlayerCamera()
    {
        StartCoroutine(SwitchRoutine());
    }

    IEnumerator SwitchRoutine()
    {
        yield return StartCoroutine(FadeToBlack());

        tvCamera.Priority = 10;
        playerCamera.Priority = 20;

        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(FadeFromBlack());
    }

    IEnumerator FadeToBlack()
    {
        float t = 0;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
    }

    IEnumerator FadeFromBlack()
    {
        float t = 0;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1, 0, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
    }
}