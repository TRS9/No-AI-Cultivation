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
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);
            bool visible = hitColliders.Length > 0;

            if (visible != _promptVisible)
            {
                _promptVisible = visible;
                GameEvents.RaiseInteractPromptChanged(visible);
            }
        }

        private void AttemptInteraction(InputAction.CallbackContext context)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableLayer);

            foreach (Collider hit in hitColliders)
            {
                IInteractable interactableObject = hit.GetComponent<IInteractable>();

                if (interactableObject != null)
                {
                    interactableObject.Interact(this.gameObject);
                    break;
                }
            }
        }
    }
}
