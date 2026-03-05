using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Player;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject inventoryPanel;
        public InventoryDisplay inventoryDisplay;

        [Header("Data References")]
        public PlayerInventory playerInventory;

        [Header("Input")]
        public InputActionReference toggleAction;
        public string playerMapName = "Player";

        private bool isVisible = false;

        private void Start()
        {
            if (inventoryDisplay != null && playerInventory != null)
                inventoryDisplay.playerInventory = playerInventory;
        }

        private void OnEnable()
        {
            toggleAction.action.performed += HandleToggle;
            GameEvents.OnInventoryChanged += UpdateDisplay;
        }

        private void OnDisable()
        {
            toggleAction.action.performed -= HandleToggle;
            GameEvents.OnInventoryChanged -= UpdateDisplay;
        }

        private void HandleToggle(InputAction.CallbackContext context)
        {
            isVisible = !isVisible;
            inventoryPanel.SetActive(isVisible);

            var inputAsset = toggleAction.action.actionMap.asset;
            var playerActionMap = inputAsset.FindActionMap(playerMapName);

            if (isVisible)
            {
                UpdateDisplay();

                playerActionMap?.Disable();

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                playerActionMap?.Enable();
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void UpdateDisplay()
        {
            if (!isVisible || inventoryDisplay == null || playerInventory == null) return;
            inventoryDisplay.RefreshDisplay(playerInventory.GetItems());
        }
    }
}
