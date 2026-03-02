using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoadTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Exact scene name as shown in Build Settings")]
    public string sceneToLoad;

    [Header("Trigger Settings")]
    [Tooltip("Tag that is allowed to activate this trigger")]
    public string activatingTag = "Player";

    [Tooltip("Optional delay before loading scene")]
    public float loadDelay = 0f;

    [Tooltip("If true, trigger can only activate once")]
    public bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered)
            return;

        if (!other.CompareTag(activatingTag))
            return;

        hasTriggered = true;

        if (loadDelay > 0f)
        {
            StartCoroutine(LoadAfterDelay());
        }
        else
        {
            LoadScene();
        }
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(loadDelay);
        LoadScene();
    }

    void LoadScene()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning("SceneLoadTrigger: No scene name assigned.");
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}