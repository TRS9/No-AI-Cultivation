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

        private Collider[] _nearbyColliders = System.Array.Empty<Collider>();
        private bool _promptVisible;

        private void OnEnable()
        {
            interactAction.action.performed += AttemptInteraction;
        }

        private void OnDisable()
        {
            interactAction.action.performed -= AttemptInteraction;
        }

        private void Update()
        {
            _nearbyColliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);
            bool visible = _nearbyColliders.Length > 0;

            if (visible != _promptVisible)
            {
                _promptVisible = visible;
                GameEvents.RaiseInteractPromptChanged(visible);
            }
        }

        private void AttemptInteraction(InputAction.CallbackContext context)
        {
            foreach (Collider hit in _nearbyColliders)
            {
                var interactable = hit.GetComponent<IInteractable>();
                if (interactable != null) { interactable.Interact(gameObject); break; }
            }
        }
    }
}
