using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using CultivationGame.Core;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        private VisualElement _panel;

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("PausePanel");

            _panel?.Q<Button>("ResumeBtn")?.RegisterCallback<ClickEvent>(e => OnResume());
            _panel?.Q<Button>("SaveBtn")?.RegisterCallback<ClickEvent>(e => OnSave());
            _panel?.Q<Button>("LoadBtn")?.RegisterCallback<ClickEvent>(e => OnLoad());
            _panel?.Q<Button>("NewGameBtn")?.RegisterCallback<ClickEvent>(e => OnNewGame());
            _panel?.Q<Button>("QuitBtn")?.RegisterCallback<ClickEvent>(e => OnQuit());

            GameEvents.OnPauseStateChanged += OnPauseStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPauseStateChanged -= OnPauseStateChanged;
        }

        private void OnPauseStateChanged(bool isPaused)
        {
            if (_panel != null)
                _panel.style.display = isPaused ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnResume() => GameStateManager.Instance?.Resume();

        private void OnSave()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Save();
                GameStateManager.Instance?.Resume();
            }
        }

        private void OnLoad()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Load();
                GameStateManager.Instance?.Resume();
            }
        }

        private void OnNewGame()
        {
            SaveSystem.DeleteSave();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
