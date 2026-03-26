using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    /// <summary>
    /// Controls the Pipe Connection panel and the Click-to-Connect workflow.
    /// When a player interacts (E) with a SpiritPipe, this UI opens and allows
    /// them to connect the pipe between a source and destination machine.
    /// </summary>
    public class PipeConnectionUI : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private float maxConnectionRange = 100f;
        [SerializeField] private LayerMask machineLayer = ~0;

        [Header("Highlight Colors")]
        [SerializeField] private Color compatibleHighlight = new Color(0.2f, 1f, 0.2f, 1f);
        [SerializeField] private Color hoverHighlight = new Color(0.4f, 1f, 0.8f, 1f);
        [SerializeField] private Color selectedHighlight = new Color(0.2f, 0.6f, 1f, 1f);

        // UI Elements
        private VisualElement _panel;
        private Label _titleLabel;
        private Label _sourceLabel;
        private Label _destinationLabel;
        private Label _instructionLabel;
        private Button _connectButton;
        private Button _disconnectButton;
        private Button _cancelButton;
        private Button _closeButton;

        // State
        private SpiritPipe _currentPipe;
        private ConnectionState _state = ConnectionState.Idle;
        private IMachineConnectable _selectedSource;
        private MonoBehaviour _selectedSourceMono;
        private Renderer _hoveredRenderer;

        // Highlight tracking
        private readonly Dictionary<Renderer, Color> _originalColors = new();
        private readonly List<Renderer> _highlightedRenderers = new();
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private enum ConnectionState
        {
            Idle,
            SelectingSource,
            SelectingDestination
        }

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        public void InitializeUI(VisualElement root)
        {
            BuildPanel(root);
            GameDataEvents.OnPipeInteracted += OnPipeInteracted;
        }

        private void OnDisable()
        {
            GameDataEvents.OnPipeInteracted -= OnPipeInteracted;
            ExitConnectionMode();
        }

        private void Update()
        {
            if (_state == ConnectionState.Idle) return;
            HandleConnectionMode();
        }

        // ------------------------------------------------------------------ //
        //  UI Construction
        // ------------------------------------------------------------------ //

        private void BuildPanel(VisualElement root)
        {
            _panel = new VisualElement { name = "PipeConnectionPanel" };
            _panel.style.position = Position.Absolute;
            _panel.style.top = 40;
            _panel.style.left = new StyleLength(new Length(50, LengthUnit.Percent));
            _panel.style.translate = new StyleTranslate(
                new Translate(new Length(-50, LengthUnit.Percent), new Length(0)));
            _panel.style.backgroundColor = new Color(0.12f, 0.12f, 0.18f, 0.92f);
            _panel.style.paddingTop = 12;
            _panel.style.paddingBottom = 12;
            _panel.style.paddingLeft = 16;
            _panel.style.paddingRight = 16;
            _panel.style.borderTopLeftRadius = 8;
            _panel.style.borderTopRightRadius = 8;
            _panel.style.borderBottomLeftRadius = 8;
            _panel.style.borderBottomRightRadius = 8;
            _panel.style.borderTopWidth = 1;
            _panel.style.borderBottomWidth = 1;
            _panel.style.borderLeftWidth = 1;
            _panel.style.borderRightWidth = 1;
            _panel.style.borderTopColor = new Color(0.4f, 0.6f, 1f, 0.5f);
            _panel.style.borderBottomColor = new Color(0.4f, 0.6f, 1f, 0.5f);
            _panel.style.borderLeftColor = new Color(0.4f, 0.6f, 1f, 0.5f);
            _panel.style.borderRightColor = new Color(0.4f, 0.6f, 1f, 0.5f);
            _panel.style.minWidth = 280;
            _panel.style.display = DisplayStyle.None;

            // Title
            _titleLabel = new Label("Spirit Pipe");
            _titleLabel.style.fontSize = 16;
            _titleLabel.style.color = Color.white;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _titleLabel.style.marginBottom = 10;
            _titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _panel.Add(_titleLabel);

            // Source info
            _sourceLabel = new Label("Source: None");
            _sourceLabel.style.color = new Color(0.8f, 0.85f, 0.9f);
            _sourceLabel.style.marginBottom = 4;
            _panel.Add(_sourceLabel);

            // Destination info
            _destinationLabel = new Label("Destination: None");
            _destinationLabel.style.color = new Color(0.8f, 0.85f, 0.9f);
            _destinationLabel.style.marginBottom = 10;
            _panel.Add(_destinationLabel);

            // Instruction
            _instructionLabel = new Label();
            _instructionLabel.style.color = new Color(1f, 0.95f, 0.5f);
            _instructionLabel.style.marginBottom = 10;
            _instructionLabel.style.display = DisplayStyle.None;
            _panel.Add(_instructionLabel);

            // Button row
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;

            _connectButton = CreateButton("Connect", OnConnectClicked);
            buttonRow.Add(_connectButton);

            _disconnectButton = CreateButton("Disconnect", OnDisconnectClicked);
            buttonRow.Add(_disconnectButton);

            _cancelButton = CreateButton("Cancel", OnCancelClicked);
            _cancelButton.style.display = DisplayStyle.None;
            buttonRow.Add(_cancelButton);

            _closeButton = CreateButton("Close", Close);
            buttonRow.Add(_closeButton);

            _panel.Add(buttonRow);
            root.Add(_panel);
        }

        private Button CreateButton(string text, System.Action clicked)
        {
            var btn = new Button(clicked) { text = text };
            btn.style.marginLeft = 4;
            btn.style.marginRight = 4;
            btn.style.paddingTop = 4;
            btn.style.paddingBottom = 4;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 12;
            return btn;
        }

        // ------------------------------------------------------------------ //
        //  Event handlers
        // ------------------------------------------------------------------ //

        private void OnPipeInteracted(MonoBehaviour pipe)
        {
            if (pipe is SpiritPipe spiritPipe)
                Open(spiritPipe);
        }

        // ------------------------------------------------------------------ //
        //  Open / Close
        // ------------------------------------------------------------------ //

        public void Open(SpiritPipe pipe)
        {
            if (_panel == null || pipe == null) return;

            // Close if already open for a different pipe
            if (_currentPipe != null && _currentPipe != pipe)
                Close();

            _currentPipe = pipe;
            _state = ConnectionState.Idle;
            _panel.style.display = DisplayStyle.Flex;

            UpdateConnectionInfo();
            UpdateButtonVisibility();

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OpenPanel("PipeConnection");
        }

        public void Close()
        {
            ExitConnectionMode();
            _currentPipe = null;

            if (_panel != null)
                _panel.style.display = DisplayStyle.None;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.ClosePanel("PipeConnection");
        }

        // ------------------------------------------------------------------ //
        //  Button callbacks
        // ------------------------------------------------------------------ //

        private void OnConnectClicked()
        {
            if (_currentPipe == null) return;
            EnterConnectionMode();
        }

        private void OnDisconnectClicked()
        {
            if (_currentPipe == null) return;
            _currentPipe.Disconnect();
            UpdateConnectionInfo();
            UpdateButtonVisibility();
        }

        private void OnCancelClicked()
        {
            ExitConnectionMode();
            UpdateButtonVisibility();
        }

        // ------------------------------------------------------------------ //
        //  Connection mode
        // ------------------------------------------------------------------ //

        private void EnterConnectionMode()
        {
            _state = ConnectionState.SelectingSource;
            _selectedSource = null;
            _selectedSourceMono = null;

            _instructionLabel.text = "Click on a source machine (output)\u2026";
            _instructionLabel.style.display = DisplayStyle.Flex;

            HighlightCompatibleMachines(requireOutput: true);
            UpdateButtonVisibility();
        }

        private void ExitConnectionMode()
        {
            _state = ConnectionState.Idle;
            _selectedSource = null;
            _selectedSourceMono = null;

            ClearAllHighlights();

            if (_instructionLabel != null)
                _instructionLabel.style.display = DisplayStyle.None;
        }

        private void HandleConnectionMode()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Right-click cancels
            if (mouse.rightButton.wasPressedThisFrame)
            {
                ExitConnectionMode();
                UpdateButtonVisibility();
                return;
            }

            // Hover highlight
            UpdateHoverHighlight(mouse);

            // Left-click selects
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (IsMouseOverPanel()) return;
                TrySelectMachine(mouse);
            }
        }

        private void TrySelectMachine(Mouse mouse)
        {
            var camera = Camera.main;
            if (camera == null) return;

            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit, maxConnectionRange, machineLayer)) return;

            var connectable = hit.collider.GetComponentInParent<IMachineConnectable>();
            if (connectable == null) return;

            var mono = connectable as MonoBehaviour;
            if (mono == null) return;

            if (_state == ConnectionState.SelectingSource)
            {
                if (connectable.OutputInventory == null) return;

                _selectedSource = connectable;
                _selectedSourceMono = mono;

                ClearAllHighlights();
                HighlightRenderer(mono, selectedHighlight);
                HighlightCompatibleMachines(requireInput: true);

                _state = ConnectionState.SelectingDestination;
                _instructionLabel.text = "Click on a destination machine (input)\u2026";
            }
            else if (_state == ConnectionState.SelectingDestination)
            {
                if (connectable.InputInventory == null) return;

                // Disconnect the old connection before making a new one
                if (_currentPipe.IsConnected)
                    _currentPipe.Disconnect();

                _currentPipe.Connect(_selectedSource, connectable);

                ExitConnectionMode();
                UpdateConnectionInfo();
                UpdateButtonVisibility();
            }
        }

        // ------------------------------------------------------------------ //
        //  Hover highlight
        // ------------------------------------------------------------------ //

        private void UpdateHoverHighlight(Mouse mouse)
        {
            var camera = Camera.main;
            if (camera == null) return;

            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            Renderer newHovered = null;

            if (Physics.Raycast(ray, out var hit, maxConnectionRange, machineLayer))
            {
                var connectable = hit.collider.GetComponentInParent<IMachineConnectable>();
                if (connectable != null)
                {
                    bool valid = _state == ConnectionState.SelectingSource
                        ? connectable.OutputInventory != null
                        : connectable.InputInventory != null;

                    if (valid)
                    {
                        var mono = connectable as MonoBehaviour;
                        if (mono != null)
                            newHovered = mono.GetComponentInChildren<Renderer>();
                    }
                }
            }

            if (newHovered == _hoveredRenderer) return;

            // Restore previous hover to its base highlight color
            if (_hoveredRenderer != null && _highlightedRenderers.Contains(_hoveredRenderer))
                ApplyHighlightColor(_hoveredRenderer, compatibleHighlight);

            _hoveredRenderer = newHovered;

            // Apply brighter hover color
            if (_hoveredRenderer != null)
                ApplyHighlightColor(_hoveredRenderer, hoverHighlight);
        }

        // ------------------------------------------------------------------ //
        //  Highlighting
        // ------------------------------------------------------------------ //

        private void HighlightCompatibleMachines(bool requireOutput = false, bool requireInput = false)
        {
            foreach (var mono in FindObjectsOfType<MonoBehaviour>())
            {
                if (mono is not IMachineConnectable connectable) continue;

                // Skip the pipe itself
                if (mono == _currentPipe) continue;

                // Skip the already-selected source
                if (_selectedSourceMono != null && mono == _selectedSourceMono) continue;

                bool compatible = true;
                if (requireOutput && connectable.OutputInventory == null) compatible = false;
                if (requireInput && connectable.InputInventory == null) compatible = false;

                if (compatible)
                    HighlightRenderer(mono, compatibleHighlight);
            }
        }

        private void HighlightRenderer(MonoBehaviour target, Color color)
        {
            var renderer = target.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            if (!_originalColors.ContainsKey(renderer))
            {
                var original = renderer.sharedMaterial != null
                    ? renderer.sharedMaterial.color
                    : Color.white;
                _originalColors[renderer] = original;
            }

            if (!_highlightedRenderers.Contains(renderer))
                _highlightedRenderers.Add(renderer);

            ApplyHighlightColor(renderer, color);
        }

        private void ApplyHighlightColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorProperty, color);
            block.SetColor(ColorProperty, color);
            renderer.SetPropertyBlock(block);
        }

        private void ClearAllHighlights()
        {
            foreach (var renderer in _highlightedRenderers)
            {
                if (renderer == null) continue;

                if (_originalColors.TryGetValue(renderer, out var original))
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetColor(BaseColorProperty, original);
                    block.SetColor(ColorProperty, original);
                    renderer.SetPropertyBlock(block);
                }
            }

            _highlightedRenderers.Clear();
            _originalColors.Clear();
            _hoveredRenderer = null;
        }

        // ------------------------------------------------------------------ //
        //  UI Updates
        // ------------------------------------------------------------------ //

        private void UpdateConnectionInfo()
        {
            if (_currentPipe == null) return;

            string sourceName = GetMachineName(_currentPipe.Source);
            string destName = GetMachineName(_currentPipe.Destination);

            if (_sourceLabel != null)
                _sourceLabel.text = $"Source: {sourceName}";
            if (_destinationLabel != null)
                _destinationLabel.text = $"Destination: {destName}";
        }

        private void UpdateButtonVisibility()
        {
            bool isConnected = _currentPipe != null && _currentPipe.IsConnected;
            bool isInConnectionMode = _state != ConnectionState.Idle;

            if (_connectButton != null)
                _connectButton.style.display = !isInConnectionMode
                    ? DisplayStyle.Flex : DisplayStyle.None;

            if (_disconnectButton != null)
                _disconnectButton.style.display = isConnected && !isInConnectionMode
                    ? DisplayStyle.Flex : DisplayStyle.None;

            if (_cancelButton != null)
                _cancelButton.style.display = isInConnectionMode
                    ? DisplayStyle.Flex : DisplayStyle.None;

            if (_closeButton != null)
                _closeButton.style.display = !isInConnectionMode
                    ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private string GetMachineName(IMachineConnectable machine)
        {
            if (machine == null) return "None";

            if (machine.MachineData != null && !string.IsNullOrEmpty(machine.MachineData.machineName))
                return machine.MachineData.machineName;

            if (machine is MonoBehaviour mono)
                return mono.gameObject.name;

            return "Unknown";
        }

        private bool IsMouseOverPanel()
        {
            if (_panel == null || _panel.panel == null) return false;

            var mouse = Mouse.current;
            if (mouse == null) return false;

            var screenPos = mouse.position.ReadValue();
            var panelRect = _panel.worldBound;

            // UI Toolkit uses top-left origin; Input System mouse uses bottom-left
            var uitkY = Screen.height - screenPos.y;
            return panelRect.Contains(new Vector2(screenPos.x, uitkY));
        }
    }
}
