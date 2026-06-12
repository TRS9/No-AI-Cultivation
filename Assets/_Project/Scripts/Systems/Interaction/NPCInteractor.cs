using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Makes an NPC interactable: pressing E starts its dialogue tree.
    /// Place on an NPC GameObject (Interactable layer, with a collider) and
    /// assign the NPCData asset. DialogueUI listens for the raised event.
    /// </summary>
    public class NPCInteractor : MonoBehaviour, IInteractable
    {
        [SerializeField] [Tooltip("NPC identity and dialogue entry point shown when the player interacts.")]
        private NPCData npcData;

        public void Interact(GameObject user)
        {
            if (npcData == null || npcData.startNode == null) return;
            GameDataEvents.RaiseDialogueRequested(npcData);
        }
    }
}
