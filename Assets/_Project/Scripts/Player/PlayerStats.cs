using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;
using CultivationGame.Systems;
using UnityEngine.InputSystem;

namespace CultivationGame.Player
{
    public class PlayerStats : MonoBehaviour, IQiReceiver
    {
        [Header("Current Status")]
        public RealmDefinition currentRealm;
        public double currentQi;

        [Header("Cultivation")]
        public bool isMeditating;
        public float meditationQiRate = 1f;
        [SerializeField] public float meditationEssenceMultiplier = 1.2f;

        [Header("Input References")]
        public InputActionReference meditate;

        public double MaxQi => currentRealm != null ? currentRealm.qiCapacity : 10;
        public string RealmName => currentRealm != null ? currentRealm.realmName : "Unknown";
        public string SubStage => currentRealm != null ? currentRealm.subStage.ToString() : "";

        private void Start()
        {
            GameEvents.RaiseQiChanged(currentQi, MaxQi);
            GameEvents.RaiseRealmChanged(RealmName, SubStage);
        }

        private void Update()
        {
            if (meditate != null && meditate.action.WasPressedThisFrame())
            {
                ToggleMeditation();
            }
            if (isMeditating && currentRealm != null)
            {
                float speedMult = PillBuffSystem.Instance != null ? PillBuffSystem.Instance.MeditationRateMultiplier : 1f;
                AddQi(meditationQiRate * speedMult * Time.deltaTime);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnAttemptBreakthrough += AttemptBreakthrough;
            GameEvents.OnAddQi += AddQi;
        }

        private void OnDisable()
        {
            GameEvents.OnAttemptBreakthrough -= AttemptBreakthrough;
            GameEvents.OnAddQi -= AddQi;
        }

        public void ToggleMeditation()
        {
            isMeditating = !isMeditating;
            GameEvents.RaiseMeditationToggled(isMeditating);
        }

        public void AddQi(double amount)
        {
            if (currentRealm == null) return;
            currentQi = currentQi + amount;
            // currentQi = System.Math.Min(currentQi + amount, currentRealm.qiCapacity);
            GameEvents.RaiseQiChanged(currentQi, MaxQi);

            if (currentQi >= currentRealm.qiCapacity)
            {
                if (currentRealm.qiCapacity * 1.2 <= currentQi)
                {
                    AttemptBreakthrough();
                }
                else
                {
                    GameEvents.RaiseQiMax();
                }
            }
        }

        public void AttemptBreakthrough()
        {
            if (currentRealm == null || currentRealm.nextRealm == null) return;

            if (currentQi < currentRealm.qiCapacity)
            {
                Debug.Log("Nicht genug Qi für einen Durchbruch!");
                return;
            }

            float pillBonus = PillBuffSystem.Instance != null ? PillBuffSystem.Instance.BreakthroughBonus : 0f;
            float roll = Random.Range(0f, 1f);
            if (roll <= currentRealm.breakthroughSuccessRate + pillBonus)
                PerformSuccess();
            else
                PerformFailure();

            GameEvents.RaiseAfterRealmBreakthrough();
        }

        private void PerformSuccess()
        {
            currentRealm = currentRealm.nextRealm;
            currentQi = 0;
            Debug.Log($"Durchbruch geschafft! Neuer Rang: {currentRealm.realmName}");
            GameEvents.RaiseQiChanged(currentQi, MaxQi);
            GameEvents.RaiseRealmChanged(RealmName, SubStage);
        }

        private void PerformFailure()
        {
            currentQi *= 0.9;
            Debug.Log("Durchbruch fehlgeschlagen! Qi-Stabilität verloren.");
            GameEvents.RaiseQiChanged(currentQi, MaxQi);
        }
    }
}
