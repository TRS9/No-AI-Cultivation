using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.UI
{
    public class DialogueUI : MonoBehaviour
    {
        private const string PanelId = "Dialogue";
        private const int MaxChoices = 4;

        [Header("Settings")]
        [SerializeField] private float typewriterSpeed = 0.03f;

        private VisualElement _dialoguePanel;
        private VisualElement _portraitImage;
        private Label _nameLabel;
        private Label _textLabel;
        private VisualElement _choicesContainer;

        private DialogueNode _currentNode;
        private NPCData _currentNPC;
        private Coroutine _typewriterCoroutine;
        private bool _isTyping;
        private string _fullText;

        public void InitializeUI(VisualElement root)
        {
            BuildUI(root);
            Hide();

            GameDataEvents.OnDialogueRequested -= HandleDialogueRequested;
            GameDataEvents.OnDialogueRequested += HandleDialogueRequested;
        }

        private void OnDisable()
        {
            GameDataEvents.OnDialogueRequested -= HandleDialogueRequested;
        }

        private void HandleDialogueRequested(NPCData npc)
        {
            if (npc == null) return;
            StartDialogue(npc, npc.startNode);
        }

        private void Update()
        {
            if (_dialoguePanel == null || _dialoguePanel.style.display == DisplayStyle.None)
                return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame)
            {
                if (_isTyping)
                {
                    SkipTypewriter();
                }
                else if (_currentNode != null &&
                         (_currentNode.choices == null || _currentNode.choices.Length == 0))
                {
                    EndDialogue();
                }
            }
        }

        /// <summary>
        /// Call to open the dialogue panel with a specific NPC and starting node.
        /// </summary>
        public void StartDialogue(NPCData npcData, DialogueNode startNode)
        {
            if (npcData == null || startNode == null) return;

            _currentNPC = npcData;
            Show();
            GameStateManager.Instance?.OpenPanel(PanelId);
            GameEvents.RaiseDialogueStateChanged(true);
            ShowNode(startNode);
        }

        private void ShowNode(DialogueNode node)
        {
            _currentNode = node;

            if (node == null)
            {
                EndDialogue();
                return;
            }

            // Portrait
            if (_portraitImage != null)
            {
                if (_currentNPC != null && _currentNPC.portrait != null)
                {
                    _portraitImage.style.backgroundImage = new StyleBackground(_currentNPC.portrait);
                    _portraitImage.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _portraitImage.style.display = DisplayStyle.None;
                }
            }

            // NPC name
            if (_nameLabel != null)
                _nameLabel.text = _currentNPC != null ? _currentNPC.npcName : string.Empty;

            // Start typewriter
            _fullText = node.dialogueText ?? string.Empty;
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine());
        }

        private IEnumerator TypewriterRoutine()
        {
            _isTyping = true;
            _textLabel.text = string.Empty;
            _choicesContainer.style.display = DisplayStyle.None;

            var sb = new System.Text.StringBuilder(_fullText.Length);
            for (int i = 0; i < _fullText.Length; i++)
            {
                sb.Append(_fullText[i]);
                _textLabel.text = sb.ToString();
                yield return new WaitForSecondsRealtime(typewriterSpeed);
            }

            _isTyping = false;
            ShowChoices();
        }

        private void SkipTypewriter()
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _textLabel.text = _fullText;
            _isTyping = false;
            ShowChoices();
        }

        private void ShowChoices()
        {
            _choicesContainer.Clear();

            if (_currentNode == null || _currentNode.choices == null ||
                _currentNode.choices.Length == 0)
            {
                // No choices — pressing Space/E will close
                _choicesContainer.style.display = DisplayStyle.None;
                return;
            }

            int count = Mathf.Min(_currentNode.choices.Length, MaxChoices);
            for (int i = 0; i < count; i++)
            {
                var choice = _currentNode.choices[i];
                var btn = new Button(() => OnChoiceSelected(choice.nextNode))
                {
                    text = $"> {choice.choiceText}"
                };
                btn.AddToClassList("dialogue-choice-btn");
                _choicesContainer.Add(btn);
            }

            _choicesContainer.style.display = DisplayStyle.Flex;
        }

        private void OnChoiceSelected(DialogueNode nextNode)
        {
            ShowNode(nextNode);
        }

        private void EndDialogue()
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _currentNode = null;
            _currentNPC = null;
            _isTyping = false;

            Hide();
            GameStateManager.Instance?.ClosePanel(PanelId);
            GameEvents.RaiseDialogueStateChanged(false);
        }

        private void Show()
        {
            if (_dialoguePanel != null)
                _dialoguePanel.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            if (_dialoguePanel != null)
                _dialoguePanel.style.display = DisplayStyle.None;
        }

        // ------------------------------------------------------------------ //
        //  Programmatic UI Construction
        // ------------------------------------------------------------------ //

        private void BuildUI(VisualElement root)
        {
            _dialoguePanel = new VisualElement { name = "DialoguePanel" };
            _dialoguePanel.AddToClassList("dialogue-panel");
            _dialoguePanel.style.position = Position.Absolute;
            _dialoguePanel.style.bottom = 32;
            _dialoguePanel.style.left = new StyleLength(new Length(10, LengthUnit.Percent));
            _dialoguePanel.style.right = new StyleLength(new Length(10, LengthUnit.Percent));
            _dialoguePanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            _dialoguePanel.style.borderTopLeftRadius = 8;
            _dialoguePanel.style.borderTopRightRadius = 8;
            _dialoguePanel.style.borderBottomLeftRadius = 8;
            _dialoguePanel.style.borderBottomRightRadius = 8;
            _dialoguePanel.style.paddingTop = 12;
            _dialoguePanel.style.paddingBottom = 12;
            _dialoguePanel.style.paddingLeft = 16;
            _dialoguePanel.style.paddingRight = 16;

            // Top section: portrait + text column
            var topSection = new VisualElement { name = "DialogueTop" };
            topSection.style.flexDirection = FlexDirection.Row;
            topSection.style.marginBottom = 8;

            _portraitImage = new VisualElement { name = "Portrait" };
            _portraitImage.AddToClassList("dialogue-portrait");
            _portraitImage.style.width = 80;
            _portraitImage.style.height = 80;
            _portraitImage.style.marginRight = 12;
            _portraitImage.style.flexShrink = 0;
            _portraitImage.style.borderTopLeftRadius = 4;
            _portraitImage.style.borderTopRightRadius = 4;
            _portraitImage.style.borderBottomLeftRadius = 4;
            _portraitImage.style.borderBottomRightRadius = 4;
            _portraitImage.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

            var textColumn = new VisualElement { name = "DialogueTextColumn" };
            textColumn.style.flexGrow = 1;

            _nameLabel = new Label { name = "NPCName" };
            _nameLabel.AddToClassList("dialogue-name");
            _nameLabel.style.fontSize = 18;
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _nameLabel.style.color = new Color(0.9f, 0.8f, 0.5f);
            _nameLabel.style.marginBottom = 4;

            _textLabel = new Label { name = "DialogueText" };
            _textLabel.AddToClassList("dialogue-text");
            _textLabel.style.fontSize = 14;
            _textLabel.style.color = Color.white;
            _textLabel.style.whiteSpace = WhiteSpace.Normal;

            textColumn.Add(_nameLabel);
            textColumn.Add(_textLabel);

            topSection.Add(_portraitImage);
            topSection.Add(textColumn);

            // Choices section
            _choicesContainer = new VisualElement { name = "ChoicesContainer" };
            _choicesContainer.AddToClassList("dialogue-choices");
            _choicesContainer.style.borderTopWidth = 1;
            _choicesContainer.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            _choicesContainer.style.paddingTop = 8;

            _dialoguePanel.Add(topSection);
            _dialoguePanel.Add(_choicesContainer);

            root.Add(_dialoguePanel);
        }
    }
}
