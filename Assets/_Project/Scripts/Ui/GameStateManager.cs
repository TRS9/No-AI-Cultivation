using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [SerializeField] [Tooltip("Input action that toggles the pause menu.")] private InputActionReference pauseAction;
        [SerializeField] [Tooltip("Input action that opens or closes the inventory panel.")] private InputActionReference toggleInventoryAction;
        [SerializeField] [Tooltip("Name of the Player input action map, used to disable player controls when panels are open.")] private string playerMapName = "Player";

        public bool IsPaused { get; private set; }

        private readonly HashSet<string> _openPanels = new();

        // The Player map may already be disabled by other systems (Spirit Sense).
        // Capture its state before blocking input and restore THAT state afterwards
        // instead of force-enabling, so we never re-enable movement mid-meditation.
        private bool _playerMapWasEnabled = true;
        private bool _inputStateCaptured;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (pauseAction != null)
                pauseAction.action.performed += OnPauseInput;
            if (toggleInventoryAction != null)
                toggleInventoryAction.action.performed += OnInventoryInput;
        }

        private void OnDisable()
        {
            if (pauseAction != null)
                pauseAction.action.performed -= OnPauseInput;
            if (toggleInventoryAction != null)
                toggleInventoryAction.action.performed -= OnInventoryInput;
        }

        private void OnPauseInput(InputAction.CallbackContext ctx)
        {
            if (IsPaused) Resume();
            else Pause();
        }

        private void OnInventoryInput(InputAction.CallbackContext ctx)
        {
            if (IsPaused) return;

            if (IsPanelOpen("Inventory"))
                ClosePanel("Inventory");
            else
                OpenPanel("Inventory");
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            CapturePlayerInputState();
            SetPlayerInputEnabled(false);
            SetCursorFree();
            GameEvents.RaisePauseStateChanged(true);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            CloseAllPanels();
            RestorePlayerInputState();
            SetCursorLocked();
            GameEvents.RaisePauseStateChanged(false);
        }

        public void OpenPanel(string panelId)
        {
            // Close other exclusive panels
            var toClose = new List<string>(_openPanels);
            foreach (var id in toClose)
                ClosePanel(id);

            _openPanels.Add(panelId);
            SetCursorFree();
            CapturePlayerInputState();
            SetPlayerInputEnabled(false);
            GameEvents.RaisePanelStateChanged(panelId, true);
        }

        public void ClosePanel(string panelId)
        {
            if (!_openPanels.Remove(panelId)) return;
            GameEvents.RaisePanelStateChanged(panelId, false);

            if (_openPanels.Count == 0 && !IsPaused)
            {
                SetCursorLocked();
                RestorePlayerInputState();
            }
        }

        private void CapturePlayerInputState()
        {
            if (_inputStateCaptured) return;
            var map = ResolvePlayerMap();
            _playerMapWasEnabled = map == null || map.enabled;
            _inputStateCaptured = true;
        }

        private void RestorePlayerInputState()
        {
            if (!_inputStateCaptured) return;
            _inputStateCaptured = false;
            if (_playerMapWasEnabled)
                SetPlayerInputEnabled(true);
        }

        public bool IsPanelOpen(string panelId) => _openPanels.Contains(panelId);

        public void CloseAllPanels()
        {
            var toClose = new List<string>(_openPanels);
            foreach (var id in toClose)
                ClosePanel(id);
        }

        private void SetCursorFree()
        {
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }

        private void SetCursorLocked()
        {
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }

        private InputActionMap ResolvePlayerMap()
        {
            if (pauseAction == null) return null;
            return pauseAction.action.actionMap?.asset?.FindActionMap(playerMapName);
        }

        private void SetPlayerInputEnabled(bool enabled)
        {
            var actionMap = ResolvePlayerMap();
            if (actionMap == null) return;
            if (enabled) actionMap.Enable();
            else actionMap.Disable();
        }
    }
}
