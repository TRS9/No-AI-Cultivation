using UnityEngine;
using UnityEngine.SceneManagement;
using CultivationGame.Core;

namespace CultivationGame.Systems
{
    public class Portal : MonoBehaviour, IInteractable
    {
        [SerializeField] private string destinationScene;
        [SerializeField] private bool isExitPortal;

        // World-space position where the player will appear in the destination scene.
        // Set this in the Inspector to the grotto entrance spawn coordinates.
        [SerializeField] private Vector3 destinationSpawnPosition;
        [SerializeField] private float destinationSpawnRotationY;

        public void SetAsExitPortal() => isExitPortal = true;

        public void Interact(GameObject user)
        {
            if (isExitPortal)
            {
                if (!SceneTransitionData.HasPendingReturn) return;
                SceneManager.LoadScene(SceneTransitionData.ReturnScene);
            }
            else
            {
                SceneTransitionData.SetReturn(
                    SceneManager.GetActiveScene().name,
                    user.transform.position,
                    user.transform.eulerAngles.y);
                SceneTransitionData.SetDestination(destinationSpawnPosition, destinationSpawnRotationY);
                SceneManager.LoadScene(destinationScene);
            }
        }
    }
}
