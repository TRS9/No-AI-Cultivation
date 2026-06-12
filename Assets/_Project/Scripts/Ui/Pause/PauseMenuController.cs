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
        private Slider _masterSlider;
        private Slider _sfxSlider;
        private Slider _musicSlider;

        public void InitializeUI(VisualElement root)
        {
            _panel = root.Q<VisualElement>("PausePanel");

            _panel?.Q<Button>("ResumeBtn")?.RegisterCallback<ClickEvent>(e => OnResume());
            _panel?.Q<Button>("SaveBtn")?.RegisterCallback<ClickEvent>(e => OnSave());
            _panel?.Q<Button>("LoadBtn")?.RegisterCallback<ClickEvent>(e => OnLoad());
            _panel?.Q<Button>("NewGameBtn")?.RegisterCallback<ClickEvent>(e => OnNewGame());
            _panel?.Q<Button>("QuitBtn")?.RegisterCallback<ClickEvent>(e => OnQuit());

            _masterSlider = _panel?.Q<Slider>("MasterVolumeSlider");
            _sfxSlider = _panel?.Q<Slider>("SFXVolumeSlider");
            _musicSlider = _panel?.Q<Slider>("MusicVolumeSlider");

            if (_masterSlider != null)
                _masterSlider.RegisterValueChangedCallback(e => SoundManager.Instance?.SetMasterVolume(e.newValue));
            if (_sfxSlider != null)
                _sfxSlider.RegisterValueChangedCallback(e => SoundManager.Instance?.SetSFXVolume(e.newValue));
            if (_musicSlider != null)
                _musicSlider.RegisterValueChangedCallback(e => SoundManager.Instance?.SetMusicVolume(e.newValue));

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

            if (isPaused)
                SyncVolumeSliders();
        }

        private void SyncVolumeSliders()
        {
            if (SoundManager.Instance == null) return;
            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SFXVolume);
            if (_musicSlider != null) _musicSlider.SetValueWithoutNotify(SoundManager.Instance.MusicVolume);
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
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.NewGame();
                return;
            }

            // Fallback when no SaveManager exists (e.g. testing a sub-scene directly)
            SaveSystem.DeleteSave();
            WorldState.Clear();
            CultivationBuffs.ResetAll();
            SceneTransitionData.ResetAll();
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
