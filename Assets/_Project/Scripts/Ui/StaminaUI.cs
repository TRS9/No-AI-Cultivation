using UnityEngine;
using UnityEngine.UI;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class StaminaUI : MonoBehaviour
    {
        public Slider staminaSlider;

        private void OnEnable()
        {
            GameEvents.OnStaminaChanged += HandleStaminaChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnStaminaChanged -= HandleStaminaChanged;
        }

        private void HandleStaminaChanged(float current, float max)
        {
            if (staminaSlider != null)
            {
                staminaSlider.maxValue = max;
                staminaSlider.value = current;
            }
        }
    }
}
