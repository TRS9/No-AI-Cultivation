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

        public delegate void AttemptBreakthrough();
        public static event AttemptBreakthrough OnAttemptBreakthrough;
        public static void RaiseAttemptBreakthrough()
            => OnAttemptBreakthrough?.Invoke();

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

        // --- Game State ---
        public delegate void PauseStateChanged(bool isPaused);
        public static event PauseStateChanged OnPauseStateChanged;
        public static void RaisePauseStateChanged(bool isPaused)
            => OnPauseStateChanged?.Invoke(isPaused);

        public delegate void PanelStateChanged(string panelId, bool isOpen);
        public static event PanelStateChanged OnPanelStateChanged;
        public static void RaisePanelStateChanged(string panelId, bool isOpen)
            => OnPanelStateChanged?.Invoke(panelId, isOpen);
    }
}
