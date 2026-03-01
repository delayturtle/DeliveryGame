using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Cinemachine;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCameraBase introCamera;
    public CinemachineVirtualCameraBase gameplayCamera;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeOutDuration = 0.25f;   // Faster fade to black (hides blend)
    public float fadeInDuration = 1f;       // Slower fade back in

    [Header("Input")]
    public PlayerInput playerInput;
    public string gameplayActionMap = "Player";
    public string cutsceneActionMap = "Dialogue";

    [Header("UI")]
    public GameObject gameplayUIRoot;

    [Header("Optional TV Audio Effect")]
    public AudioLowPassFilter tvAudioFilter;
    public float tvLowPassFrequency = 800f;

    private bool cutsceneActive = false;
    private bool isTransitioning = false;

    void Start()
    {
        StartCoroutine(RunCutscene());
    }

    IEnumerator RunCutscene()
    {
        cutsceneActive = true;
        isTransitioning = false;

        // Disable gameplay UI
        if (gameplayUIRoot != null)
            gameplayUIRoot.SetActive(false);

        // Switch to Dialogue action map (movement disabled, skip still works)
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(cutsceneActionMap);

        // Activate intro camera
        introCamera.Priority = 20;
        gameplayCamera.Priority = 10;

        // Enable TV audio filter
        if (tvAudioFilter != null)
        {
            tvAudioFilter.enabled = true;
            tvAudioFilter.cutoffFrequency = tvLowPassFrequency;
        }

        // Start fully black
        SetFadeAlpha(1f);

        yield return FadeFromBlack();

        // Subscribe safely
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueFinished += OnDialogueFinished;
    }

    void OnDialogueFinished()
    {
        if (!cutsceneActive || isTransitioning)
            return;

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueFinished -= OnDialogueFinished;

        StartCoroutine(EndCutscene());
    }

    IEnumerator EndCutscene()
    {
        isTransitioning = true;
        cutsceneActive = false;

        // FAST fade to black to hide Cinemachine blend
        yield return FadeToBlack();

        // Guarantee fully black before switching cameras
        SetFadeAlpha(1f);

        // Switch cameras AFTER black
        introCamera.Priority = 10;
        gameplayCamera.Priority = 20;

        // Wait one frame so Cinemachine Brain processes blend
        yield return new WaitForEndOfFrame();

        // Disable TV audio effect
        if (tvAudioFilter != null)
            tvAudioFilter.enabled = false;

        // Fade back in
        yield return FadeFromBlack();

        // Re-enable gameplay UI
        if (gameplayUIRoot != null)
            gameplayUIRoot.SetActive(true);

        // Switch back to gameplay action map
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap(gameplayActionMap);

        isTransitioning = false;
    }

    IEnumerator FadeToBlack()
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeOutDuration);
            c.a = normalized;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    IEnumerator FadeFromBlack()
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeInDuration);
            c.a = 1f - normalized;
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
    }

    void SetFadeAlpha(float value)
    {
        if (fadeImage == null) return;

        Color c = fadeImage.color;
        c.a = Mathf.Clamp01(value);
        fadeImage.color = c;
    }
}