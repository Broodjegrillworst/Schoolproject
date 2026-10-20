using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private string menuSceneName = "MainMenu";

    public GameObject PausePanel => pausePanel;

    private bool isPaused;
    private bool gameOver;

    private void Start()
    {
        Time.timeScale = 1f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        if ((keyboard == null && gamepad == null) || gameOver)
        {
            return;
        }

        bool pausePressed = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.startButton.wasPressedThisFrame);
        if (pausePressed)
        {
            if (isPaused) Resume(); else Pause();
        }

        if (!isPaused)
        {
            return;
        }

        bool restartPressed = (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.buttonWest.wasPressedThisFrame);
        bool menuPressed = (keyboard != null && keyboard.mKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
        bool resumePressed = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;

        if (restartPressed) RestartLevel();
        else if (menuPressed) ReturnToMenu();
        else if (resumePressed) Resume();
    }

    public void Pause()
    {
        if (gameOver) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        SelectFirstButton(pausePanel);
    }

    public void Resume()
    {
        if (gameOver) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void RestartLevel()
    {
        if (gameOver)
            GameSession.PrepareForLevelRetry();
        Time.timeScale = 1f;
        ScreenTransition2D.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        ScreenTransition2D.LoadScene(menuSceneName);
    }

    public void SetGameOver()
    {
        gameOver = true;
        isPaused = false;
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public static void SelectFirstButton(GameObject root)
    {
        if (root == null || EventSystem.current == null)
            return;

        Button[] buttons = root.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || !buttons[i].gameObject.activeInHierarchy || !buttons[i].interactable)
                continue;

            EventSystem.current.SetSelectedGameObject(buttons[i].gameObject);
            return;
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }
}
