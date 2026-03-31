using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Player;

namespace CultivationGame.Systems
{
    public class BreakthroughSystem : MonoBehaviour
    {
        [SerializeField] [Tooltip("Reference to the player's stats, used to read current realm and Qi.")] private PlayerStats playerStats;
        [SerializeField] [Tooltip("Fraction of maximum Qi lost on a failed breakthrough attempt (0–1).")] private float failureQiLoss = 0.1f;

        private void OnEnable()
        {
            GameEvents.OnAttemptBreakthrough += AttemptBreakthrough;
        }

        private void OnDisable()
        {
            GameEvents.OnAttemptBreakthrough -= AttemptBreakthrough;
        }

        public void AttemptBreakthrough()
        {
            if (playerStats == null)
            {
                Debug.LogWarning("BreakthroughSystem: PlayerStats reference missing.");
                return;
            }

            var realm = playerStats.currentRealm;
            if (realm == null || realm.nextRealm == null)
            {
                Debug.Log("No further realm available.");
                return;
            }

            if (playerStats.currentQi < realm.qiCapacity)
            {
                Debug.Log("Not enough Qi for a breakthrough!");
                return;
            }

            float pillBonus = CultivationBuffs.BreakthroughBonus;
            float roll = Random.Range(0f, 1f);
            bool success = roll <= realm.breakthroughSuccessRate + pillBonus;

            if (success)
                PerformSuccess();
            else
                PerformFailure();

            GameEvents.RaiseAfterRealmBreakthrough();
        }

        private void PerformSuccess()
        {
            var nextRealm = playerStats.currentRealm.nextRealm;
            playerStats.currentRealm = nextRealm;
            playerStats.currentQi = 0;

            CultivationBuffs.DamageMultiplier = nextRealm.baseDamage / 5f;
            CultivationBuffs.DefenseMultiplier = 1f + nextRealm.baseDefense * 0.1f;

            Debug.Log($"Breakthrough successful! New realm: {nextRealm.realmName}");

            GameEvents.RaiseQiChanged(playerStats.currentQi, playerStats.MaxQi);
            GameEvents.RaiseRealmChanged(playerStats.RealmName, playerStats.SubStage);
            GameEvents.RaiseRealmBreakthrough(true, nextRealm.realmName);
        }

        private void PerformFailure()
        {
            playerStats.currentQi *= 1.0 - failureQiLoss;

            Debug.Log("Breakthrough failed! Qi destabilized.");

            GameEvents.RaiseQiChanged(playerStats.currentQi, playerStats.MaxQi);
            GameEvents.RaiseRealmBreakthrough(false, playerStats.RealmName);
        }
    }
}
