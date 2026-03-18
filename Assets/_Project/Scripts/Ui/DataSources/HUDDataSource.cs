using System;
using System.ComponentModel; // <-- DAS ist der neue Standard
using UnityEngine;
using Unity.Properties;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    [CreateAssetMenu(menuName = "Cultivation/UI/HUD Data Source")]
    // 1. Interface austauschen
    public class HUDDataSource : ScriptableObject, INotifyPropertyChanged
    {
        // 2. Standard C# Event nutzen
        public event PropertyChangedEventHandler PropertyChanged;

        private float _staminaPercent = 100f;
        private float _qiProgress;
        private string _qiLabel = "Qi: 0/0";
        private bool _breakthroughReady;
        private string _realmName = "";
        private string _subStage = "";
        private bool _interactPromptVisible;
        private string _meditationBonusText = "";
        private bool _meditationBonusVisible;

        [CreateProperty]
        public float StaminaPercent
        {
            get => _staminaPercent;
            private set
            {
                if (Mathf.Approximately(_staminaPercent, value)) return;
                _staminaPercent = value;
                Notify(nameof(StaminaPercent));
            }
        }

        [CreateProperty]
        public float QiProgress
        {
            get => _qiProgress;
            private set
            {
                if (Mathf.Approximately(_qiProgress, value)) return;
                _qiProgress = value;
                Notify(nameof(QiProgress));
            }
        }

        [CreateProperty]
        public string QiLabel
        {
            get => _qiLabel;
            private set
            {
                if (_qiLabel == value) return;
                _qiLabel = value;
                Notify(nameof(QiLabel));
            }
        }

        [CreateProperty]
        public bool BreakthroughReady
        {
            get => _breakthroughReady;
            private set
            {
                if (_breakthroughReady == value) return;
                _breakthroughReady = value;
                Notify(nameof(BreakthroughReady));
            }
        }

        [CreateProperty]
        public string RealmName
        {
            get => _realmName;
            private set
            {
                if (_realmName == value) return;
                _realmName = value;
                Notify(nameof(RealmName));
            }
        }

        [CreateProperty]
        public string SubStage
        {
            get => _subStage;
            private set
            {
                if (_subStage == value) return;
                _subStage = value;
                Notify(nameof(SubStage));
            }
        }

        [CreateProperty]
        public bool InteractPromptVisible
        {
            get => _interactPromptVisible;
            private set
            {
                if (_interactPromptVisible == value) return;
                _interactPromptVisible = value;
                Notify(nameof(InteractPromptVisible));
            }
        }

        [CreateProperty]
        public string MeditationBonusText
        {
            get => _meditationBonusText;
            private set
            {
                if (_meditationBonusText == value) return;
                _meditationBonusText = value;
                Notify(nameof(MeditationBonusText));
            }
        }

        [CreateProperty]
        public bool MeditationBonusVisible
        {
            get => _meditationBonusVisible;
            private set
            {
                if (_meditationBonusVisible == value) return;
                _meditationBonusVisible = value;
                Notify(nameof(MeditationBonusVisible));
            }
        }

        // 3. Notify Methode anpassen
        private void Notify(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Subscribe()
        {
            GameEvents.OnStaminaChanged += HandleStamina;
            GameEvents.OnQiChanged += HandleQi;
            GameEvents.OnQiMax += HandleQiMax;
            GameEvents.OnAfterRealmBreakthrough += HandleBreakthrough;
            GameEvents.OnRealmChanged += HandleRealmChanged;
            GameEvents.OnMeditationBonusApplied += HandleMeditationBonus;
            GameEvents.OnInteractPromptChanged += HandleInteractPrompt;
        }

        public void Unsubscribe()
        {
            GameEvents.OnStaminaChanged -= HandleStamina;
            GameEvents.OnQiChanged -= HandleQi;
            GameEvents.OnQiMax -= HandleQiMax;
            GameEvents.OnAfterRealmBreakthrough -= HandleBreakthrough;
            GameEvents.OnRealmChanged -= HandleRealmChanged;
            GameEvents.OnMeditationBonusApplied -= HandleMeditationBonus;
            GameEvents.OnInteractPromptChanged -= HandleInteractPrompt;
        }

        public void ResetState()
        {
            _staminaPercent = 100f;
            _qiProgress = 0f;
            _qiLabel = "Qi: 0/0";
            _breakthroughReady = false;
            _realmName = "";
            _subStage = "";
            _interactPromptVisible = false;
            _meditationBonusVisible = false;
        }

        private void HandleStamina(float current, float max)
        {
            StaminaPercent = max > 0 ? (current / max) * 100f : 0f;
        }

        private void HandleQi(double currentQi, double maxQi)
        {
            QiProgress = maxQi > 0 ? (float)(currentQi / maxQi) : 0f;
            QiLabel = $"Qi: {currentQi:F0}/{maxQi:F0}";
        }

        private void HandleQiMax()
        {
            BreakthroughReady = true;
        }

        private void HandleBreakthrough()
        {
            BreakthroughReady = false;
        }

        private void HandleRealmChanged(string realmName, string subStage)
        {
            RealmName = realmName;
            SubStage = subStage;
            QiProgress = 0f;
            QiLabel = "Qi: 0/0";
        }

        private void HandleMeditationBonus(float multiplier)
        {
            MeditationBonusText = $"x{multiplier:F1} Meditation Bonus!";
            MeditationBonusVisible = true;
        }

        public void HideMeditationBonus()
        {
            MeditationBonusVisible = false;
        }

        private void HandleInteractPrompt(bool visible)
        {
            InteractPromptVisible = visible;
        }
    }
}