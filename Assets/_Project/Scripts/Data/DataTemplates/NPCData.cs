using UnityEngine;

namespace CultivationGame.Data
{
    [CreateAssetMenu(fileName = "NewNPC", menuName = "Cultivation/NPC Data")]
    public class NPCData : ScriptableObject
    {
        public string npcName;
        public Sprite portrait;
        public DialogueNode startNode;
    }
}
