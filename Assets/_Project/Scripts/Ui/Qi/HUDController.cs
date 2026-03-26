using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private HUDDataSource hudData;

        private LiquidCircle _qiCircle;
        private Label _realmNameLabel;
        private Label _subStageLabel;
        private Button _breakthroughBtn;
        private Label _meditationBonusLabel;
        private Label _interactPrompt;
        private ProgressBar _healthBar;
        private Label _healthLabel;
        private VisualElement _buffContainer;
        private IVisualElementScheduledItem _meditationFadeSchedule;
        private IVisualElementScheduledItem _breakthroughBlinkSchedule;
        private bool _blinkState;

        // Track buff UI elements by key
        private readonly Dictionary<string, VisualElement> _buffElements = new();
        private IVisualElementScheduledItem _buffTimerSchedule;

        public void InitializeUI(VisualElement root)
        {
            var hud = root.Q<VisualElement>("HUD");
            _qiCircle = root.Q<LiquidCircle>("QiCircle");
            _realmNameLabel = root.Q<Label>("RealmNameLabel");
            _subStageLabel = root.Q<Label>("SubStageLabel");
            _breakthroughBtn = root.Q<Button>("BreakthroughBtn");
            _meditationBonusLabel = root.Q<Label>("MeditationBonusLabel");
            _interactPrompt = root.Q<Label>("InteractPrompt");
            _healthBar = root.Q<ProgressBar>("HealthBar");
            _healthLabel = root.Q<Label>("HealthLabel");
            _buffContainer = root.Q<VisualElement>("BuffContainer");

            _breakthroughBtn?.RegisterCallback<ClickEvent>(OnBreakthroughClicked);

            if (hudData != null)
            {
                hudData.ResetState();
                hudData.Subscribe();

                // Declarative bindings via UI Toolkit data binding
                hud.dataSource = hudData;

                var staminaBar = root.Q<ProgressBar>("StaminaBar");
                staminaBar?.SetBinding("value", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(HUDDataSource.StaminaPercent)),
                    bindingMode = BindingMode.ToTarget
                });

                _healthBar?.SetBinding("value", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(HUDDataSource.HealthPercent)),
                    bindingMode = BindingMode.ToTarget
                });

                var qiLabel = root.Q<Label>("QiLabel");
                qiLabel?.SetBinding("text", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(HUDDataSource.QiLabel)),
                    bindingMode = BindingMode.ToTarget
                });

                _healthLabel?.SetBinding("text", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(HUDDataSource.HealthLabel)),
                    bindingMode = BindingMode.ToTarget
                });

                // Manual bindings for properties that need logic
                hudData.PropertyChanged += OnPropertyChanged;
                hudData.OnBuffAdded += HandleBuffAdded;
                hudData.OnBuffRemoved += HandleBuffRemoved;
            }

            StartBuffTimerUpdates(root);
        }

        private void OnDisable()
        {
            if (hudData != null)
            {
                hudData.Unsubscribe();
                hudData.PropertyChanged -= OnPropertyChanged;
                hudData.OnBuffAdded -= HandleBuffAdded;
                hudData.OnBuffRemoved -= HandleBuffRemoved;
            }
            _breakthroughBlinkSchedule?.Pause();
            _buffTimerSchedule?.Pause();
        }

        private void OnBreakthroughClicked(ClickEvent evt) => GameEvents.RaiseAttemptBreakthrough();

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HUDDataSource.QiProgress):
                    if (_qiCircle != null) _qiCircle.progress = hudData.QiProgress;
                    break;
                case nameof(HUDDataSource.BreakthroughReady):
                    if (_breakthroughBtn != null)
                    {
                        _breakthroughBtn.EnableInClassList("breakthrough-btn--ready", hudData.BreakthroughReady);
                        UpdateBreakthroughBlink(hudData.BreakthroughReady);
                    }
                    break;
                case nameof(HUDDataSource.RealmName):
                    if (_realmNameLabel != null)
                        _realmNameLabel.text = hudData.RealmName;
                    break;
                case nameof(HUDDataSource.SubStage):
                    if (_subStageLabel != null)
                        _subStageLabel.text = hudData.SubStage;
                    break;
                case nameof(HUDDataSource.InteractPromptVisible):
                    if (_interactPrompt != null)
                        _interactPrompt.style.display = hudData.InteractPromptVisible
                            ? DisplayStyle.Flex : DisplayStyle.None;
                    break;
                case nameof(HUDDataSource.MeditationBonusVisible):
                    HandleMeditationBonus();
                    break;
            }
        }

        private void HandleMeditationBonus()
        {
            if (_meditationBonusLabel == null || !hudData.MeditationBonusVisible) return;

            _meditationBonusLabel.text = hudData.MeditationBonusText;
            _meditationBonusLabel.style.opacity = 1f;
            _meditationFadeSchedule?.Pause();

            var fadeOut = _meditationBonusLabel.schedule.Execute(() =>
            {
                _meditationBonusLabel.style.opacity = 0f;
                var hide = _meditationBonusLabel.schedule.Execute(() => hudData.HideMeditationBonus());
                hide.ExecuteLater(800);
            });
            fadeOut.ExecuteLater(500);
            _meditationFadeSchedule = fadeOut;
        }

        // --- Breakthrough Blink ---
        private void UpdateBreakthroughBlink(bool ready)
        {
            _breakthroughBlinkSchedule?.Pause();
            if (!ready || _breakthroughBtn == null)
            {
                _breakthroughBtn?.RemoveFromClassList("breakthrough-blink-on");
                return;
            }

            _blinkState = false;
            _breakthroughBlinkSchedule = _breakthroughBtn.schedule.Execute(() =>
            {
                _blinkState = !_blinkState;
                _breakthroughBtn.EnableInClassList("breakthrough-blink-on", _blinkState);
            }).Every(600);
        }

        // --- Buff Icons ---
        private void HandleBuffAdded(ActiveBuff buff)
        {
            if (_buffContainer == null) return;

            string key = $"{buff.BuffName}_{buff.PillName}";
            var icon = new VisualElement();
            icon.AddToClassList("buff-icon");

            var nameLabel = new Label(buff.PillName);
            nameLabel.AddToClassList("buff-icon__name");
            icon.Add(nameLabel);

            var timerLabel = new Label(buff.FormattedTime);
            timerLabel.AddToClassList("buff-icon__timer");
            timerLabel.name = $"BuffTimer_{key}";
            icon.Add(timerLabel);

            _buffContainer.Add(icon);
            _buffElements[key] = icon;
        }

        private void HandleBuffRemoved(string buffName, string pillName)
        {
            string key = $"{buffName}_{pillName}";
            if (_buffElements.TryGetValue(key, out var icon))
            {
                icon.RemoveFromHierarchy();
                _buffElements.Remove(key);
            }
        }

        private void StartBuffTimerUpdates(VisualElement root)
        {
            _buffTimerSchedule = root.schedule.Execute(() =>
            {
                if (hudData == null) return;
                foreach (var buff in hudData.ActiveBuffs)
                {
                    string key = $"{buff.BuffName}_{buff.PillName}";
                    if (_buffElements.TryGetValue(key, out var icon))
                    {
                        var timerLabel = icon.Q<Label>($"BuffTimer_{key}");
                        if (timerLabel != null)
                            timerLabel.text = buff.FormattedTime;
                    }
                }
            }).Every(500);
        }
    }
}
