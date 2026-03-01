using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFadeController : MonoBehaviour
{
    public AudioSource musicSource;
    public Image fadeOverlay;
    public float fadeDuration = 2f;

    public void OnButtonPressed()
    {
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float startVolume = musicSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;

            // Fade music
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);

            // Fade screen
            Color c = fadeOverlay.color;
            c.a = Mathf.Lerp(0f, 1f, t);
            fadeOverlay.color = c;

            yield return null;
        }

        musicSource.volume = 0f;
    }
}