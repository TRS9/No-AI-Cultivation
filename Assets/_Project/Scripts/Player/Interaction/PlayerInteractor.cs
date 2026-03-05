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

            if (hitColliders.Length > 0)
            {
                if (interactPromptUI != null)
                {
                    interactPromptUI.SetActive(true);
                }
            }
            else
            {
                if (interactPromptUI != null)
                {
                    interactPromptUI.SetActive(false);
                }
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
