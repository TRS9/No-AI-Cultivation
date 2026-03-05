using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class QiSphereUI : MonoBehaviour
    {
        [Header("Visuals")]
        public Image fillImage;
        public TextMeshProUGUI amountText;

        private void OnEnable()
        {
            GameEvents.OnQiChanged += HandleQiUpdate;
            GameEvents.OnRealmChanged += HandleRealmUpdate;
        }

        private void OnDisable()
        {
            GameEvents.OnQiChanged -= HandleQiUpdate;
            GameEvents.OnRealmChanged -= HandleRealmUpdate;
        }

        private void HandleQiUpdate(double currentQi, double maxQi)
        {
            if (maxQi <= 0) return;

            float progress = (float)(currentQi / maxQi);

            if (fillImage != null)
            {
                fillImage.fillAmount = progress;
            }

            if (amountText != null)
            {
                amountText.text = $"{currentQi:F0} / {maxQi:F0}";
            }
        }

        private void HandleRealmUpdate(string realmName, string subStage)
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = 0;
            }

            Debug.Log($"UI: Sphäre für {realmName} {subStage} vorbereitet.");
        }
    }
}