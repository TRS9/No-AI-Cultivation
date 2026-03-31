using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.Player
{
    public class BuildElevation : MonoBehaviour
    {
        [SerializeField] [Tooltip("Vertical distance in world units between build layers.")] private float layerHeight = 3f;
        [SerializeField] [Tooltip("World Y position of build layer 0 (ground level).")] private float baseY;
        [SerializeField] [Tooltip("Maximum number of build layers the player can ascend to.")] private int maxLayers = 10;

        [Header("Input")]
        [SerializeField] [Tooltip("Input action to ascend one build layer.")] private InputActionReference layerUpAction;
        [SerializeField] [Tooltip("Input action to descend one build layer.")] private InputActionReference layerDownAction;

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
