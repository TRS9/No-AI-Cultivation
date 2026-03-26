namespace CultivationGame.Core
{
    public static class GameEvents
    {
        // --- Qi / Cultivation ---
        public delegate void QiChanged(double currentQi, double maxQi);
        public static event QiChanged OnQiChanged;
        public static void RaiseQiChanged(double current, double max)
            => OnQiChanged?.Invoke(current, max);

        public delegate void AddQi(double amount);
        public static event AddQi OnAddQi;
        public static void RaiseAddQi(double amount)
            => OnAddQi?.Invoke(amount);

        public delegate void QiMaxed();
        public static event QiMaxed OnQiMax;
        public static void RaiseQiMax()
            => OnQiMax?.Invoke();

        public delegate void BreakthroughConfirmRequested();
        public static event BreakthroughConfirmRequested OnBreakthroughConfirmRequested;
        public static void RaiseBreakthroughConfirmRequested()
            => OnBreakthroughConfirmRequested?.Invoke();

        public delegate void AttemptBreakthrough();
        public static event AttemptBreakthrough OnAttemptBreakthrough;
        public static void RaiseAttemptBreakthrough()
            => OnAttemptBreakthrough?.Invoke();

        public delegate void RealmBreakthrough(bool success, string realmName);
        public static event RealmBreakthrough OnRealmBreakthrough;
        public static void RaiseRealmBreakthrough(bool success, string realmName)
            => OnRealmBreakthrough?.Invoke(success, realmName);

        public delegate void AfterRealmBreakthrough();
        public static event AfterRealmBreakthrough OnAfterRealmBreakthrough;
        public static void RaiseAfterRealmBreakthrough()
            => OnAfterRealmBreakthrough?.Invoke();

        public delegate void MeditationToggled(bool isMeditating);
        public static event MeditationToggled OnMeditationToggled;
        public static void RaiseMeditationToggled(bool isMeditating)
            => OnMeditationToggled?.Invoke(isMeditating);

        // --- Realm ---
        public delegate void RealmChanged(string realmName, string subStage);
        public static event RealmChanged OnRealmChanged;
        public static void RaiseRealmChanged(string realmName, string subStage)
            => OnRealmChanged?.Invoke(realmName, subStage);

        // --- Stamina ---
        public delegate void StaminaChanged(float current, float max);
        public static event StaminaChanged OnStaminaChanged;
        public static void RaiseStaminaChanged(float current, float max)
            => OnStaminaChanged?.Invoke(current, max);

        // --- Inventory ---
        public delegate void InventoryChanged();
        public static event InventoryChanged OnInventoryChanged;
        public static void RaiseInventoryChanged()
            => OnInventoryChanged?.Invoke();

        public delegate void MeditationBonusApplied(float multiplier);
        public static event MeditationBonusApplied OnMeditationBonusApplied;
        public static void RaiseMeditationBonusApplied(float multiplier)
            => OnMeditationBonusApplied?.Invoke(multiplier);

        // --- Crafting UI ---
        public delegate void CraftingStationInteracted();
        public static event CraftingStationInteracted OnCraftingStationInteracted;
        public static void RaiseCraftingStationInteracted()
            => OnCraftingStationInteracted?.Invoke();

        // --- Interact Prompt ---
        public delegate void InteractPromptChanged(bool visible);
        public static event InteractPromptChanged OnInteractPromptChanged;
        public static void RaiseInteractPromptChanged(bool visible)
            => OnInteractPromptChanged?.Invoke(visible);

        // --- Build Mode ---
        public delegate void BuildModeToggled(bool isBuildMode);
        public static event BuildModeToggled OnBuildModeToggled;
        public static void RaiseBuildModeToggled(bool isBuildMode)
            => OnBuildModeToggled?.Invoke(isBuildMode);

        // --- Build Elevation ---
        public delegate void BuildLayerChanged(int layer, float worldY);
        public static event BuildLayerChanged OnBuildLayerChanged;
        public static void RaiseBuildLayerChanged(int layer, float worldY)
            => OnBuildLayerChanged?.Invoke(layer, worldY);

        // --- Combat (Phase 8) ---
        public delegate void PlayerHealthChanged(float currentHealth, float maxHealth);
        public static event PlayerHealthChanged OnPlayerHealthChanged;
        public static void RaisePlayerHealthChanged(float current, float max)
            => OnPlayerHealthChanged?.Invoke(current, max);

        public delegate void PlayerDied();
        public static event PlayerDied OnPlayerDied;
        public static void RaisePlayerDied()
            => OnPlayerDied?.Invoke();

        public delegate void PlayerAttack();
        public static event PlayerAttack OnPlayerAttack;
        public static void RaisePlayerAttack()
            => OnPlayerAttack?.Invoke();

        public delegate void PlayerDodge();
        public static event PlayerDodge OnPlayerDodge;
        public static void RaisePlayerDodge()
            => OnPlayerDodge?.Invoke();

        // --- Buff Tracking (HUD) ---
        public delegate void BuffStarted(string buffName, string pillName, float duration);
        public static event BuffStarted OnBuffStarted;
        public static void RaiseBuffStarted(string buffName, string pillName, float duration)
            => OnBuffStarted?.Invoke(buffName, pillName, duration);

        public delegate void BuffExpired(string buffName, string pillName);
        public static event BuffExpired OnBuffExpired;
        public static void RaiseBuffExpired(string buffName, string pillName)
            => OnBuffExpired?.Invoke(buffName, pillName);

        // --- Game State ---
        public delegate void PauseStateChanged(bool isPaused);
        public static event PauseStateChanged OnPauseStateChanged;
        public static void RaisePauseStateChanged(bool isPaused)
            => OnPauseStateChanged?.Invoke(isPaused);

        public delegate void PanelStateChanged(string panelId, bool isOpen);
        public static event PanelStateChanged OnPanelStateChanged;
        public static void RaisePanelStateChanged(string panelId, bool isOpen)
            => OnPanelStateChanged?.Invoke(panelId, isOpen);

        // --- Dialogue (Phase 7) ---
        public delegate void DialogueStateChanged(bool isActive);
        public static event DialogueStateChanged OnDialogueStateChanged;
        public static void RaiseDialogueStateChanged(bool isActive)
            => OnDialogueStateChanged?.Invoke(isActive);
    }
}
