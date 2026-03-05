using System.Collections;
using TMPro;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    public class MeditationBonusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float holdTime = 0.5f;
        [SerializeField] private float fadeTime = 0.8f;

        private Coroutine _activeCoroutine;

        private void Awake()
        {
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            gameObject.SetActive(false);
        }

        private void OnEnable() => GameEvents.OnMeditationBonusApplied += Show;
        private void OnDisable() => GameEvents.OnMeditationBonusApplied -= Show;

        private void Show(float multiplier)
        {
            label.text = $"+{(multiplier - 1f) * 100f:F0}% Meditation Bonus";

            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _activeCoroutine = StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            var color = label.color;
            color.a = 1f;
            label.color = color;
            gameObject.SetActive(true);

            yield return new WaitForSeconds(holdTime);

            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                color.a = 1f - elapsed / fadeTime;
                label.color = color;
                yield return null;
            }

            gameObject.SetActive(false);
            _activeCoroutine = null;
        }
    }
}
