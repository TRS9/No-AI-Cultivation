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

        public GameObject interactPromptUI;

        private Collider[] _nearbyColliders = System.Array.Empty<Collider>();

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
            if (interactPromptUI != null)
                interactPromptUI.SetActive(_nearbyColliders.Length > 0);
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
