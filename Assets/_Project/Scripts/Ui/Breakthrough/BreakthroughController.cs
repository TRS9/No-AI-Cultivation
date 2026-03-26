using UnityEngine;
using UnityEngine.UIElements;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class BreakthroughController : MonoBehaviour
    {
        private VisualElement _overlay;
        private VisualElement _panel;
        private Label _resultLabel;
        private IVisualElementScheduledItem _hideSchedule;

        public void InitializeUI(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("BreakthroughOverlay");
            _panel = root.Q<VisualElement>("BreakthroughPanel");
            _resultLabel = root.Q<Label>("BreakthroughResultLabel");

            _overlay?.Q<Button>("ConfirmBtn")?.RegisterCallback<ClickEvent>(OnConfirm);
            _overlay?.Q<Button>("CancelBtn")?.RegisterCallback<ClickEvent>(OnCancel);

            GameEvents.OnBreakthroughConfirmRequested += ShowConfirmation;
            GameEvents.OnRealmBreakthrough += ShowResult;
        }

        private void OnDisable()
        {
            GameEvents.OnBreakthroughConfirmRequested -= ShowConfirmation;
            GameEvents.OnRealmBreakthrough -= ShowResult;
        }

        private void ShowConfirmation()
        {
            if (_overlay == null) return;
            _overlay.style.display = DisplayStyle.Flex;
            _panel.style.display = DisplayStyle.Flex;
            if (_resultLabel != null) _resultLabel.style.opacity = 0f;
        }

        private void OnConfirm(ClickEvent evt)
        {
            if (_panel != null) _panel.style.display = DisplayStyle.None;
            GameEvents.RaiseAttemptBreakthrough();
        }

        private void OnCancel(ClickEvent evt)
        {
            Hide();
        }

        private void ShowResult(bool success, string realmName)
        {
            if (_resultLabel == null || _overlay == null) return;

            _overlay.style.display = DisplayStyle.Flex;
            _panel.style.display = DisplayStyle.None;

            _resultLabel.RemoveFromClassList("breakthrough-result--success");
            _resultLabel.RemoveFromClassList("breakthrough-result--failure");

            if (success)
            {
                _resultLabel.text = $"BREAKTHROUGH!\n{realmName}";
                _resultLabel.AddToClassList("breakthrough-result--success");
            }
            else
            {
                _resultLabel.text = "BREAKTHROUGH FAILED\nQi Destabilized";
                _resultLabel.AddToClassList("breakthrough-result--failure");
            }

            _resultLabel.style.opacity = 1f;

            _hideSchedule?.Pause();
            _hideSchedule = _resultLabel.schedule.Execute(() =>
            {
                _resultLabel.style.opacity = 0f;
                var finalize = _resultLabel.schedule.Execute(Hide);
                finalize.ExecuteLater(800);
            });
            _hideSchedule.ExecuteLater(1500);
        }

        private void Hide()
        {
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }
    }
}
