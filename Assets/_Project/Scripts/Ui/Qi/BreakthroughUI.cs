using UnityEngine;
using UnityEngine.UI; 
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class BreakthroughUI : MonoBehaviour
    {
        [Header("UI References")]
        public Button breakthroughButton;

        private void Start()
        {
            if (breakthroughButton != null)
            {
                breakthroughButton.gameObject.SetActive(false);

                breakthroughButton.onClick.AddListener(HandleBreakthroughButton);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnQiMax += HandleQiMaxed;
            GameEvents.OnAfterRealmBreakthrough += HandleBreakthrough;
        }

        private void OnDisable()
        {
            GameEvents.OnQiMax -= HandleQiMaxed;
            GameEvents.OnAfterRealmBreakthrough -= HandleBreakthrough;
        }

        private void HandleQiMaxed()
        {
            if (breakthroughButton != null)
            {
                breakthroughButton.gameObject.SetActive(true);
            }
        }

        private void HandleBreakthrough()
        {
            if (breakthroughButton != null)
            {
                breakthroughButton.gameObject.SetActive(false);
            }
        }

        private void HandleBreakthroughButton()
        {
            GameEvents.RaiseAttemptBreakthrough();
        }
    }
}