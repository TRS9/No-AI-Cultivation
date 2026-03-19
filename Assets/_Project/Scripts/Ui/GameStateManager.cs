using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private InputActionReference toggleInventoryAction;
        [SerializeField] private string playerMapName = "Player";

        public bool IsPaused { get; private set; }

        private readonly HashSet<string> _openPanels = new();

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
            SetPlayerInputEnabled(false);
            SetCursorFree();
            GameEvents.RaisePauseStateChanged(true);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            CloseAllPanels();
            SetPlayerInputEnabled(true);
            SetCursorLocked();
            GameEvents.RaisePauseStateChanged(false);
        }

        public void OpenPanel(string panelId)
        {
            var toClose = new List<string>(_openPanels);
            foreach (var id in toClose)
                ClosePanel(id);

            _openPanels.Add(panelId);
            SetCursorFree();
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
                SetPlayerInputEnabled(true);
            }
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
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void SetCursorLocked()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void SetPlayerInputEnabled(bool enabled)
        {
            if (pauseAction == null) return;
            var actionMap = pauseAction.action.actionMap?.asset?.FindActionMap(playerMapName);
            if (actionMap == null) return;
            if (enabled) actionMap.Enable();
            else actionMap.Disable();
        }
    }
}
