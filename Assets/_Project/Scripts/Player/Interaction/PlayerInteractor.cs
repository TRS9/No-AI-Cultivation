using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        public float interactionRadius = 2f;
        public LayerMask interactableLayer;
        public InputActionReference interactAction;

        [Tooltip("How often (in seconds) the nearby-interactable check runs. Lower = more responsive, higher = cheaper.")]
        public float checkInterval = 0.15f;

        private const int MaxNearby = 10;
        private readonly Collider[] _nearbyColliders = new Collider[MaxNearby];
        private int _nearbyCount;
        private bool _promptVisible;
        private float _nextCheckTime;

        private void OnEnable()
        {
            if (interactAction != null)
                interactAction.action.performed += AttemptInteraction;
        }

        private void OnDisable()
        {
            if (interactAction != null)
                interactAction.action.performed -= AttemptInteraction;
        }

        private void Update()
        {
            if (Time.time < _nextCheckTime) return;
            _nextCheckTime = Time.time + checkInterval;

            _nearbyCount = Physics.OverlapSphereNonAlloc(transform.position, interactionRadius, _nearbyColliders, interactableLayer);
            bool visible = _nearbyCount > 0;

            if (visible != _promptVisible)
            {
                _promptVisible = visible;
                GameEvents.RaiseInteractPromptChanged(visible);
            }
        }

        private void AttemptInteraction(InputAction.CallbackContext context)
        {
            for (int i = 0; i < _nearbyCount; i++)
            {
                var interactable = _nearbyColliders[i].GetComponent<IInteractable>();
                if (interactable != null) { interactable.Interact(gameObject); break; }
            }
        }
    }
}
