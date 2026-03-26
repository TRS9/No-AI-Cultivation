using UnityEngine;

namespace CultivationGame.Data
{
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public DialogueNode nextNode;
    }

    [CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Cultivation/Dialogue Node")]
    public class DialogueNode : ScriptableObject
    {
        [TextArea(3, 5)]
        public string dialogueText;

        public DialogueChoice[] choices;
    }
}
