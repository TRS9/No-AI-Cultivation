using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;

namespace CultivationGame.UI
{
    /// <summary>
    /// Factory Dashboard that shows an overview of all machines:
    /// production rates per output type, machine status counts,
    /// and bottleneck warnings. Only visible in Spirit-Sense mode.
    /// </summary>
    public class FactoryDashboardUI : MonoBehaviour
    {
        [Header("Update Rate")]
        [Tooltip("How often the dashboard recalculates (seconds).")]
        [SerializeField] private float updateInterval = 0.5f;

        private VisualElement _panel;
        private VisualElement _productionContainer;
        private Label _statusLabel;
        private VisualElement _bottleneckContainer;

        private float _updateTimer;
        private bool _isSpiritSense;

        // ------------------------------------------------------------------ //
        //  Lifecycle
        // ------------------------------------------------------------------ //

        public void InitializeUI(VisualElement root)
        {
            BuildPanel(root);
            _panel.style.display = DisplayStyle.None;
        }

        private void OnEnable()
        {
            GameEvents.OnMeditationToggled += HandleMeditationToggled;
        }

        private void OnDisable()
        {
            GameEvents.OnMeditationToggled -= HandleMeditationToggled;
        }

        private void Update()
        {
            if (!_isSpiritSense || _panel == null) return;

            _updateTimer += Time.deltaTime;
            if (_updateTimer >= updateInterval)
            {
                _updateTimer = 0f;
                RefreshDashboard();
            }
        }

        // ------------------------------------------------------------------ //
        //  Spirit-Sense gate
        // ------------------------------------------------------------------ //

        private void HandleMeditationToggled(bool isMeditating)
        {
            _isSpiritSense = isMeditating;

            if (_panel == null) return;

            if (isMeditating)
            {
                _panel.style.display = DisplayStyle.Flex;
                _updateTimer = 0f;
                RefreshDashboard();
            }
            else
            {
                _panel.style.display = DisplayStyle.None;
            }
        }

        // ------------------------------------------------------------------ //
        //  UI Construction
        // ------------------------------------------------------------------ //

        private void BuildPanel(VisualElement root)
        {
            _panel = new VisualElement();
            _panel.name = "FactoryDashboardPanel";
            _panel.AddToClassList("factory-dashboard");

            // Inline styles for a semi-transparent overlay panel
            _panel.style.position = Position.Absolute;
            _panel.style.top = 10;
            _panel.style.right = 10;
            _panel.style.width = 380;
            _panel.style.paddingTop = 12;
            _panel.style.paddingBottom = 12;
            _panel.style.paddingLeft = 14;
            _panel.style.paddingRight = 14;
            _panel.style.backgroundColor = new Color(0.05f, 0.05f, 0.12f, 0.88f);
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

            // Title
            var title = new Label("Factory Dashboard");
            title.style.fontSize = 16;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.7f, 0.85f, 1f);
            title.style.marginBottom = 8;
            _panel.Add(title);

            // --- Production section ---
            var prodHeader = new Label("Production / Minute:");
            prodHeader.style.fontSize = 12;
            prodHeader.style.color = new Color(0.6f, 0.8f, 0.6f);
            prodHeader.style.marginBottom = 4;
            prodHeader.style.marginTop = 4;
            _panel.Add(prodHeader);

            _productionContainer = new VisualElement();
            _productionContainer.name = "ProductionRates";
            _panel.Add(_productionContainer);

            // --- Machine status section ---
            var statusHeader = new Label("Machine Status:");
            statusHeader.style.fontSize = 12;
            statusHeader.style.color = new Color(0.6f, 0.8f, 0.6f);
            statusHeader.style.marginBottom = 4;
            statusHeader.style.marginTop = 10;
            _panel.Add(statusHeader);

            _statusLabel = new Label();
            _statusLabel.style.fontSize = 12;
            _statusLabel.style.color = Color.white;
            _statusLabel.style.marginBottom = 4;
            _panel.Add(_statusLabel);

            // --- Bottleneck section ---
            var bottleneckHeader = new Label("Bottlenecks:");
            bottleneckHeader.style.fontSize = 12;
            bottleneckHeader.style.color = new Color(1f, 0.7f, 0.3f);
            bottleneckHeader.style.marginBottom = 4;
            bottleneckHeader.style.marginTop = 10;
            _panel.Add(bottleneckHeader);

            _bottleneckContainer = new VisualElement();
            _bottleneckContainer.name = "Bottlenecks";
            _panel.Add(_bottleneckContainer);

            root.Add(_panel);
        }

        // ------------------------------------------------------------------ //
        //  Dashboard Refresh
        // ------------------------------------------------------------------ //

        private void RefreshDashboard()
        {
            var machines = GetAllMachines();
            if (machines == null || machines.Count == 0)
            {
                _productionContainer.Clear();
                _statusLabel.text = "No machines placed.";
                _bottleneckContainer.Clear();
                return;
            }

            RefreshProductionRates(machines);
            RefreshMachineStatus(machines);
            RefreshBottlenecks(machines);
        }

        // ------------------------------------------------------------------ //
        //  Machine discovery
        // ------------------------------------------------------------------ //

        private IReadOnlyList<BaseMachine> GetAllMachines()
        {
            if (QiNetwork.Instance != null)
                return QiNetwork.Instance.RegisteredMachines;

            return null;
        }

        // ------------------------------------------------------------------ //
        //  Production rates  (items / minute per output type)
        // ------------------------------------------------------------------ //

        private void RefreshProductionRates(IReadOnlyList<BaseMachine> machines)
        {
            _productionContainer.Clear();

            // Accumulate theoretical output per item across all active machines
            var ratesPerItem = new Dictionary<string, float>(); // itemName → items/min

            foreach (var machine in machines)
            {
                if (machine == null) continue;
                if (machine.CurrentRecipe == null) continue;
                if (!machine.IsPowered) continue;
                if (machine.MachineData == null) continue;

                float speed = machine.MachineData.processingSpeed > 0f
                    ? machine.MachineData.processingSpeed : 1f;
                float duration = machine.CurrentRecipe.craftingDuration / speed;

                if (duration <= 0f) continue;

                float cyclesPerMinute = 60f / duration;

                foreach (var output in machine.CurrentRecipe.outputs)
                {
                    if (output.item == null) continue;
                    string itemName = output.item.name;

                    if (!ratesPerItem.ContainsKey(itemName))
                        ratesPerItem[itemName] = 0f;

                    ratesPerItem[itemName] += output.amount * cyclesPerMinute;
                }
            }

            if (ratesPerItem.Count == 0)
            {
                var noneLabel = new Label("  (no active production)");
                noneLabel.style.fontSize = 11;
                noneLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
                _productionContainer.Add(noneLabel);
                return;
            }

            foreach (var kv in ratesPerItem)
            {
                var row = BuildProductionRow(kv.Key, kv.Value);
                _productionContainer.Add(row);
            }
        }

        private VisualElement BuildProductionRow(string itemName, float perMinute)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;

            var nameLabel = new Label($"  {itemName}:");
            nameLabel.style.fontSize = 12;
            nameLabel.style.color = Color.white;
            nameLabel.style.width = 140;
            row.Add(nameLabel);

            var rateLabel = new Label($"{perMinute:F1}/min");
            rateLabel.style.fontSize = 12;
            rateLabel.style.color = new Color(0.4f, 1f, 0.4f);
            rateLabel.style.width = 70;
            row.Add(rateLabel);

            // Simple bar visualisation
            var barBg = new VisualElement();
            barBg.style.width = 100;
            barBg.style.height = 10;
            barBg.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            barBg.style.borderTopLeftRadius = 3;
            barBg.style.borderTopRightRadius = 3;
            barBg.style.borderBottomLeftRadius = 3;
            barBg.style.borderBottomRightRadius = 3;

            float fillPercent = Mathf.Clamp01(perMinute / 60f); // 60/min = full bar
            var barFill = new VisualElement();
            barFill.style.width = Length.Percent(fillPercent * 100f);
            barFill.style.height = Length.Percent(100f);
            barFill.style.backgroundColor = new Color(0.3f, 0.7f, 1f);
            barFill.style.borderTopLeftRadius = 3;
            barFill.style.borderTopRightRadius = 3;
            barFill.style.borderBottomLeftRadius = 3;
            barFill.style.borderBottomRightRadius = 3;
            barBg.Add(barFill);
            row.Add(barBg);

            return row;
        }

        // ------------------------------------------------------------------ //
        //  Machine status counts
        // ------------------------------------------------------------------ //

        private void RefreshMachineStatus(IReadOnlyList<BaseMachine> machines)
        {
            int active = 0;
            int stalled = 0;
            int unpowered = 0;
            int idle = 0;

            foreach (var machine in machines)
            {
                if (machine == null) continue;
                var state = ClassifyMachine(machine);
                switch (state)
                {
                    case MachineState.Active:    active++;    break;
                    case MachineState.Stalled:   stalled++;   break;
                    case MachineState.Unpowered: unpowered++; break;
                    default:                     idle++;      break;
                }
            }

            _statusLabel.text =
                $"\u2705 {active} Active    " +
                $"\u26A0\uFE0F {stalled} Stalled    " +
                $"\u274C {unpowered} Unpowered    " +
                $"\u23F8 {idle} Idle";
        }

        // ------------------------------------------------------------------ //
        //  Bottleneck warnings
        // ------------------------------------------------------------------ //

        private void RefreshBottlenecks(IReadOnlyList<BaseMachine> machines)
        {
            _bottleneckContainer.Clear();
            bool any = false;

            for (int i = 0; i < machines.Count; i++)
            {
                var machine = machines[i];
                if (machine == null) continue;

                var state = ClassifyMachine(machine);
                if (state == MachineState.Active || state == MachineState.Idle)
                    continue;

                string machineName = machine.MachineData != null
                    ? machine.MachineData.machineName : "Machine";
                string machineId = $"{machineName} #{i + 1}";

                string icon;
                string reason;

                if (state == MachineState.Unpowered)
                {
                    icon = "\u274C";
                    reason = "No Qi (network not connected)";
                }
                else // Stalled
                {
                    icon = "\u26A0\uFE0F";
                    reason = IsOutputFull(machine)
                        ? "Output full (pipe blocked)"
                        : "Processing stalled (power lost)";
                }

                var warning = new Label($"{icon} {machineId}: {reason}");
                warning.style.fontSize = 11;
                warning.style.color = state == MachineState.Unpowered
                    ? new Color(1f, 0.4f, 0.4f)
                    : new Color(1f, 0.8f, 0.3f);
                warning.style.marginBottom = 2;
                _bottleneckContainer.Add(warning);
                any = true;
            }

            if (!any)
            {
                var noneLabel = new Label("  No bottlenecks detected.");
                noneLabel.style.fontSize = 11;
                noneLabel.style.color = new Color(0.4f, 0.8f, 0.4f);
                _bottleneckContainer.Add(noneLabel);
            }
        }

        // ------------------------------------------------------------------ //
        //  Machine classification helpers
        // ------------------------------------------------------------------ //

        private enum MachineState { Active, Stalled, Unpowered, Idle }

        private MachineState ClassifyMachine(BaseMachine machine)
        {
            if (machine.CurrentRecipe == null || machine.MachineData == null)
                return MachineState.Idle;

            bool needsPower = machine.MachineData.qiConsumptionRate > 0f;

            // Currently processing and powered → active
            if (machine.IsProcessing && machine.IsPowered)
                return MachineState.Active;

            // Currently processing but lost power mid-cycle → stalled
            if (machine.IsProcessing && !machine.IsPowered)
                return MachineState.Stalled;

            // Not processing and unpowered → unpowered
            if (needsPower && !machine.IsPowered)
                return MachineState.Unpowered;

            // Not processing, powered, has recipe — check why it can't start
            if (HasRequiredInputs(machine) && IsOutputFull(machine))
                return MachineState.Stalled; // output blocked

            return MachineState.Idle;
        }

        private bool HasRequiredInputs(BaseMachine machine)
        {
            if (machine.CurrentRecipe == null || machine.InputInventory == null) return false;

            foreach (var input in machine.CurrentRecipe.inputs)
            {
                if (input.item == null) continue;
                if (!machine.InputInventory.HasItem(input.item, input.amount))
                    return false;
            }
            return true;
        }

        private bool IsOutputFull(BaseMachine machine)
        {
            if (machine.CurrentRecipe == null || machine.OutputInventory == null) return false;

            int totalOutput = 0;
            foreach (var output in machine.CurrentRecipe.outputs)
                totalOutput += output.amount;

            return !machine.OutputInventory.HasSpace(totalOutput);
        }
    }
}
