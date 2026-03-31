using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data;

namespace CultivationGame.Systems
{
    /// <summary>
    /// Bridges game events to the SoundManager.
    /// Assign SoundClip assets in the Inspector for each event category.
    /// </summary>
    public class SoundEventTrigger : MonoBehaviour
    {
        [Header("Machine Sounds")]
        [SerializeField] [Tooltip("Sound played when a crafting recipe starts processing.")] private SoundClip machineStartClip;
        [SerializeField] [Tooltip("Sound played when a machine finishes processing a recipe.")] private SoundClip machineCompleteClip;
        [SerializeField] [Tooltip("Sound played when a machine stalls due to insufficient power.")] private SoundClip machineStalledClip;

        [Header("Item Sounds")]
        [SerializeField] [Tooltip("Sound played when a resource is extracted or picked up.")] private SoundClip itemPickupClip;

        [Header("Cultivation Sounds")]
        [SerializeField] [Tooltip("Sound played when the player successfully breaks through to the next realm.")] private SoundClip breakthroughClip;

        [Header("Combat Sounds")]
        [SerializeField] [Tooltip("Sound played when the player performs an attack.")] private SoundClip combatHitClip;
        [SerializeField] [Tooltip("Sound played when the player or an enemy dies.")] private SoundClip combatDeathClip;

        [Header("UI Sounds")]
        [SerializeField] [Tooltip("Sound played on UI button clicks.")] private SoundClip uiClickClip;

        [Header("Dialogue Sounds")]
        [SerializeField] [Tooltip("Sound played as each dialogue character types.")] private SoundClip dialogueTypeClip;

        private void OnEnable()
        {
            GameDataEvents.OnCraftingStarted += HandleCraftingStarted;
            GameDataEvents.OnMachineProcessingCompleted += HandleMachineCompleted;
            GameDataEvents.OnMachineStalled += HandleMachineStalled;
            GameDataEvents.OnResourceExtracted += HandleResourceExtracted;
            GameEvents.OnAfterRealmBreakthrough += HandleBreakthrough;
            GameEvents.OnPlayerAttack += HandlePlayerAttack;
            GameEvents.OnPlayerDied += HandlePlayerDied;
            GameDataEvents.OnEnemyDied += HandleEnemyDied;
        }

        private void OnDisable()
        {
            GameDataEvents.OnCraftingStarted -= HandleCraftingStarted;
            GameDataEvents.OnMachineProcessingCompleted -= HandleMachineCompleted;
            GameDataEvents.OnMachineStalled -= HandleMachineStalled;
            GameDataEvents.OnResourceExtracted -= HandleResourceExtracted;
            GameEvents.OnAfterRealmBreakthrough -= HandleBreakthrough;
            GameEvents.OnPlayerAttack -= HandlePlayerAttack;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
            GameDataEvents.OnEnemyDied -= HandleEnemyDied;
        }

        // --- Machine events ---

        private void HandleCraftingStarted(RecipeData recipe)
        {
            Play2D(machineStartClip);
        }

        private void HandleMachineCompleted(MonoBehaviour machine, RecipeData recipe)
        {
            Play3D(machineCompleteClip, machine.transform.position);
        }

        private void HandleMachineStalled(MonoBehaviour machine)
        {
            Play3D(machineStalledClip, machine.transform.position);
        }

        // --- Item events ---

        private void HandleResourceExtracted(ItemData resource, int amount)
        {
            Play2D(itemPickupClip);
        }

        // --- Cultivation events ---

        private void HandleBreakthrough()
        {
            Play2D(breakthroughClip);
        }

        // --- Combat events ---

        private void HandlePlayerAttack()
        {
            Play2D(combatHitClip);
        }

        private void HandlePlayerDied()
        {
            Play2D(combatDeathClip);
        }

        private void HandleEnemyDied(MonoBehaviour enemy)
        {
            Play3D(combatDeathClip, enemy.transform.position);
        }

        // --- Public helpers for UI / Dialogue ---

        public void PlayUIClick()
        {
            Play2D(uiClickClip);
        }

        public void PlayDialogueType()
        {
            Play2D(dialogueTypeClip);
        }

        // --- Internals ---

        private void Play2D(SoundClip clip)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(clip);
        }

        private void Play3D(SoundClip clip, Vector3 position)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(clip, position);
        }
    }
}
