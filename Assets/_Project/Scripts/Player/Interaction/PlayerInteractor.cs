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
            // Interact with the closest interactable, not the first overlap hit —
            // overlap results come back in arbitrary order.
            IInteractable best = null;
            float bestSqrDist = float.MaxValue;

            for (int i = 0; i < _nearbyCount; i++)
            {
                var col = _nearbyColliders[i];
                if (col == null) continue;

                var interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;

                float sqrDist = (col.transform.position - transform.position).sqrMagnitude;
                if (sqrDist < bestSqrDist)
                {
                    bestSqrDist = sqrDist;
                    best = interactable;
                }
            }

            best?.Interact(gameObject);
        }
    }
}
