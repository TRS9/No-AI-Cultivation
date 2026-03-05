using UnityEngine;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Place one instance of this in every scene that can be entered via a Portal.
    /// Runs in Start (after SaveManager.Awake) and teleports the player to the
    /// position stored by SceneTransitionData when entering through a portal.
    /// Does nothing when loading from a save file (SaveManager handles that path).
    /// </summary>
    public class SceneEntryPoint : MonoBehaviour
    {
        private void Start()
        {
            if (!SceneTransitionData.HasPendingDestination && !SceneTransitionData.HasPendingReturn)
                return;

            var player = GameObject.FindWithTag("Player");
            if (player == null) return;

            Vector3 pos;
            float rotY;

            if (SceneTransitionData.HasPendingDestination)
            {
                pos = SceneTransitionData.DestinationPosition;
                rotY = SceneTransitionData.DestinationRotationY;
                SceneTransitionData.ClearDestination();
            }
            else
            {
                pos = SceneTransitionData.ReturnPosition;
                rotY = SceneTransitionData.ReturnRotationY;
                SceneTransitionData.ClearReturn();
            }

            player.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotY, 0f));

            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = pos;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
