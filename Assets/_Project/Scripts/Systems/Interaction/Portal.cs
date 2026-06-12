using UnityEngine;
using UnityEngine.SceneManagement;
using CultivationGame.Core;

namespace CultivationGame.Systems
{
    public class Portal : MonoBehaviour, IInteractable
    {
        [SerializeField] [Tooltip("Name of the scene to load when the player interacts with this portal.")] private string destinationScene;
        [SerializeField] [Tooltip("When true, this portal returns the player to their previous scene and position.")] private bool isExitPortal;

        // World-space position where the player will appear in the destination scene.
        // Set this in the Inspector to the grotto entrance spawn coordinates.
        [SerializeField] [Tooltip("World-space position where the player spawns in the destination scene.")] private Vector3 destinationSpawnPosition;
        [SerializeField] [Tooltip("Yaw (Y-axis rotation) applied to the player upon entering the destination scene.")] private float destinationSpawnRotationY;

        public void SetAsExitPortal() => isExitPortal = true;

        public void Interact(GameObject user)
        {
            if (isExitPortal)
            {
                if (!SceneTransitionData.HasPendingReturn) return;
                // Leaving the minor realm — clear the flag so saves made afterwards
                // don't carry stale realm data.
                SceneTransitionData.IsMinorRealm = false;
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
