using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseGame : MonoBehaviour
{
    [Header("Pause Menu")]
    [Tooltip("The pause menu GameObject to show/hide")]
    public GameObject pauseMenuGameObject;

    private InputAction pauseAction;
    private bool isPaused = false;

    void Start()
    {
        // Find the pause input action
        pauseAction = InputSystem.actions.FindAction("PauseButton");
        
        if (pauseAction == null)
        {
            Debug.LogWarning("[PauseGame] 'PauseButton' input action not found!");
        }

        // Ensure pause menu starts disabled
        if (pauseMenuGameObject != null)
        {
            pauseMenuGameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (pauseAction != null && pauseAction.WasPressedThisFrame())
        {
            TogglePause();
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    void Pause()
    {
        if (pauseMenuGameObject != null)
        {
            pauseMenuGameObject.SetActive(true);
        }

        Time.timeScale = 0f;
        Debug.Log("[PauseGame] Game paused");
    }

    void Resume()
    {
        if (pauseMenuGameObject != null)
        {
            pauseMenuGameObject.SetActive(false);
        }

        Time.timeScale = 1f;
        Debug.Log("[PauseGame] Game resumed");
    }

    // Public method to resume from UI button
    public void ResumeFromButton()
    {
        if (isPaused)
        {
            isPaused = false;
            Resume();
        }
    }

    // Public method to load a scene by name (for Unity Events)
    public void LoadScene(string sceneName)
    {
        // Reset time scale before loading scene
        Time.timeScale = 1f;
        
        Debug.Log($"[PauseGame] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
