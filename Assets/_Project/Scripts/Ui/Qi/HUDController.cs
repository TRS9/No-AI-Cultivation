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
        private Label _realmLabel;
        private Button _breakthroughBtn;
        private Label _meditationBonusLabel;
        private Label _interactPrompt;
        private IVisualElementScheduledItem _meditationFadeSchedule;

        public void InitializeUI(VisualElement root)
        {
            var hud = root.Q<VisualElement>("HUD");
            _qiCircle = root.Q<LiquidCircle>("QiCircle");
            _realmLabel = root.Q<Label>("RealmLabel");
            _breakthroughBtn = root.Q<Button>("BreakthroughBtn");
            _meditationBonusLabel = root.Q<Label>("MeditationBonusLabel");
            _interactPrompt = root.Q<Label>("InteractPrompt");

            _breakthroughBtn?.RegisterCallback<ClickEvent>(e => GameEvents.RaiseAttemptBreakthrough());

            if (hudData != null)
            {
                hudData.ResetState();
                hudData.Subscribe();

                hud.dataSource = hudData;

                root.Q<ProgressBar>("StaminaBar")?.SetBinding("value", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(HUDDataSource.StaminaPercent)),
                    bindingMode = BindingMode.ToTarget
                });

                root.Q<Label>("QiLabel")?.SetBinding("text", new DataBinding
                {
                    dataSourcePath = new PropertyPath(nameof(HUDDataSource.QiLabel)),
                    bindingMode = BindingMode.ToTarget
                });

                hudData.PropertyChanged += OnPropertyChanged;
            }
        }

        private void OnDisable()
        {
            if (hudData == null) return;
            hudData.Unsubscribe();
            hudData.PropertyChanged -= OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HUDDataSource.QiProgress):
                    if (_qiCircle != null) _qiCircle.progress = hudData.QiProgress;
                    break;

                case nameof(HUDDataSource.BreakthroughReady):
                    _breakthroughBtn?.EnableInClassList("breakthrough-btn--ready", hudData.BreakthroughReady);
                    break;

                case nameof(HUDDataSource.RealmName):
                case nameof(HUDDataSource.SubStage):
                    if (_realmLabel != null)
                        _realmLabel.text = $"{hudData.RealmName} {hudData.SubStage}".Trim();
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
                _meditationBonusLabel.schedule.Execute(() => hudData.HideMeditationBonus()).ExecuteLater(800);
            });
            fadeOut.ExecuteLater(500);
            _meditationFadeSchedule = fadeOut;
        }
    }
}
