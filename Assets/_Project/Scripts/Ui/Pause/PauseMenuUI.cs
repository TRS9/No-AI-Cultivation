using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject pausePanel;

        [Header("Buttons")]
        public Button resumeButton;
        public Button saveButton;
        public Button loadButton;
        public Button newGameButton;
        public Button quitButton;

        [Header("Input")]
        public InputActionReference pauseAction;
        public string playerMapName = "Player";

        private bool isPaused;

        private void Start()
        {
            // Initialzustand
            pausePanel.SetActive(false);

            // Button-Events registrieren
            resumeButton.onClick.AddListener(ResumeGame);
            saveButton.onClick.AddListener(HandleSave);
            loadButton.onClick.AddListener(HandleLoad);
            newGameButton.onClick.AddListener(HandleNewGame);
            quitButton.onClick.AddListener(QuitGame);
        }

        private void OnEnable() => pauseAction.action.performed += TogglePause;
        private void OnDisable() => pauseAction.action.performed -= TogglePause;

        private void TogglePause(InputAction.CallbackContext context)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        public void PauseGame()
        {
            isPaused = true;
            pausePanel.SetActive(true);
            Time.timeScale = 0f; // Stoppt Physik und Animationen

            SetPlayerInputEnabled(false);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void ResumeGame()
        {
            isPaused = false;
            pausePanel.SetActive(false);
            Time.timeScale = 1f;

            SetPlayerInputEnabled(true);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void HandleSave()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Save();
                ResumeGame();
            }
        }

        private void HandleLoad()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Load();
                ResumeGame();
            }
        }

        private void HandleNewGame()
        {
            // Löscht den aktuellen Spielstand
            SaveSystem.DeleteSave();

            // Setzt die Zeit zurück und lädt die aktuelle Szene neu
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void QuitGame()
        {
//#if UNITY_EDITOR
  //          UnityEditor.EditorApplication.isPlaying = false;
//#else
  //          Application.Quit();
//#endif
        }

        private void SetPlayerInputEnabled(bool isEnabled)
        {
            var actionMap = pauseAction.action.actionMap.asset.FindActionMap(playerMapName);
            if (isEnabled) actionMap?.Enable();
            else actionMap?.Disable();
        }
    }
}