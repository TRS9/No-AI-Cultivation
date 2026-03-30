using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;
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
        public float meditationEssenceMultiplier = 1.2f;

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
                float speedMult = CultivationBuffs.MeditationRateMultiplier;
                AddQi(meditationQiRate * speedMult * Time.deltaTime);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnAddQi += AddQi;
        }

        private void OnDisable()
        {
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
            currentQi = System.Math.Min(currentQi + amount, currentRealm.qiCapacity);
            GameEvents.RaiseQiChanged(currentQi, MaxQi);

            if (currentQi >= currentRealm.qiCapacity)
            {
                GameEvents.RaiseQiMax();
            }
        }
    }
}
