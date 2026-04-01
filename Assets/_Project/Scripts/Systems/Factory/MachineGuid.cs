using UnityEngine;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Persistent unique identifier for a placed machine instance.
    /// Added at runtime by PlacementController (new machines) or SaveManager (loaded machines).
    /// </summary>
    public class MachineGuid : MonoBehaviour
    {
        [SerializeField] private string guid;

        public string Guid => guid;

        public void AssignNewGuid()
        {
            guid = System.Guid.NewGuid().ToString();
        }

        public void SetGuid(string existingGuid)
        {
            guid = existingGuid;
        }
    }
}
