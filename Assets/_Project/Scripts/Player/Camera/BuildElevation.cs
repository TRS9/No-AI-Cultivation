using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.Player
{
    public class BuildElevation : MonoBehaviour
    {
        [SerializeField] private float layerHeight = 3f;
        [SerializeField] private float baseY;
        [SerializeField] private int maxLayers = 10;

        [Header("Input")]
        [SerializeField] private InputActionReference layerUpAction;
        [SerializeField] private InputActionReference layerDownAction;

        private int _currentLayer;

        public int CurrentLayer => _currentLayer;
        public float CurrentWorldY => baseY + _currentLayer * layerHeight;

        private void OnEnable()
        {
            if (layerUpAction != null)
                layerUpAction.action.performed += OnLayerUp;
            if (layerDownAction != null)
                layerDownAction.action.performed += OnLayerDown;
        }

        private void OnDisable()
        {
            if (layerUpAction != null)
                layerUpAction.action.performed -= OnLayerUp;
            if (layerDownAction != null)
                layerDownAction.action.performed -= OnLayerDown;
        }

        private void OnLayerUp(InputAction.CallbackContext ctx)
        {
            SetLayer(_currentLayer + 1);
        }

        private void OnLayerDown(InputAction.CallbackContext ctx)
        {
            SetLayer(_currentLayer - 1);
        }

        private void SetLayer(int layer)
        {
            int clamped = Mathf.Clamp(layer, 0, maxLayers);
            if (clamped == _currentLayer) return;

            _currentLayer = clamped;
            GameEvents.RaiseBuildLayerChanged(_currentLayer, CurrentWorldY);
        }
    }
}
