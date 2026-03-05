# No AI Cultivation — Project Structure

## Assembly Architecture

```
Game.Core  (foundation — no game assembly dependencies)
  ↑
Game.Data  (depends on: Core)
  ↑
Game.Systems  (depends on: Core, Data)
  ↑
Game.Player  (depends on: Core, Data, Unity.InputSystem)
  ↑
Game.UI  (depends on: Core, Data, Systems, Player, Unity.TextMeshPro, Unity.InputSystem)
  ↑
Game.Editor  (depends on: everything — Editor only)
```

---

## Folder Structure

```
Assets/_Project/
│
├── Animations/              Reserved for animation clips and controllers
├── Audio/                   Reserved for music, SFX, ambient audio
├── Data/                    ScriptableObject asset instances (designer-editable)
│   ├── Essence/             EssenceData assets (FireEssence, WaterEssence, etc.)
│   └── Realm/               RealmDefinition assets (00_Mortal, etc.)
├── Input/                   Unity Input System action maps (.inputactions)
├── Materials/               Physics materials, rendering materials
├── Models/                  Reserved for 3D models and meshes
├── Prefab/                  Prefab assets
│   └── UI/                  UI-specific prefabs (InventorySlot, etc.)
├── Scenes/                  Unity scene files
├── Scripts/                 All C# source code (see below)
├── Sprites/                 2D art assets
│   ├── Biomes/              Biome/terrain sprites
│   ├── Buildings/           Reserved for building sprites
│   ├── Characters/          Player and NPC sprites
│   ├── Effects/             Reserved for VFX sprites
│   ├── InnerWorld/          Inner world tile sprites
│   ├── POIs/                Reserved for point-of-interest sprites
│   └── UI/                  Reserved for UI icons and elements
```

---

## Scripts Folder Structure

```
Scripts/
├── Core/                    Foundation types shared by all assemblies
├── Data/                    ScriptableObject class definitions
│   └── DataTemplates/       SO templates for game data
├── Editor/                  Editor-only tools and inspectors
├── Player/                  Player-specific logic
│   ├── Interaction/         Player interaction with world objects
│   ├── Inventory/           Player inventory storage
│   └── Movement/            Player movement and physics
├── Systems/                 Game systems and world logic
│   ├── Combat/              Reserved for combat system
│   ├── Cultivation/         Reserved for cultivation/meditation system
│   ├── Dao/                 Reserved for dao comprehension system
│   ├── InnerWorld/          Reserved for inner world system
│   ├── Interaction/         World-side interactable objects
│   ├── Save/                Reserved for save/load system
│   └── Sect/                Reserved for sect management system
└── Ui/                      All UI display and controllers
    └── Inventory/           Inventory panel UI
```

---

## Script Reference

### Core (namespace: CultivationGame.Core)

| Script | Type | Purpose |
|--------|------|---------|
| **GameEnums.cs** | Enums | All shared enumerations: GameState, RealmSubStage, DaoType, DaoCategory, BiomeType, POIType, TerrainType, InnerWorldTileType, BuildingTerrain, BuildingRarity, SectRank, BuffType |
| **GameEvents.cs** | Static class | Lightweight event bus for decoupled communication. Events: OnQiChanged, OnRealmChanged, OnStaminaChanged, OnInventoryChanged |
| **GameManager.cs** | MonoBehaviour (Singleton) | Holds global GameState. Persists across scenes via DontDestroyOnLoad. Access via GameManager.Instance |
| **IInteractable.cs** | Interface | Contract for any world object the player can interact with. Single method: Interact(GameObject user) |
| **IQiReceiver.cs** | Interface | Contract for anything that can receive Qi. Single method: AddQi(double amount). Implemented by PlayerStats |
| **IInventory.cs** | Interface | Contract for anything that stores items. Single method: AddItem(ScriptableObject item). Implemented by PlayerInventory |

### Data (namespace: CultivationGame.Data)

| Script | Type | Purpose |
|--------|------|---------|
| **EssenceData.cs** | ScriptableObject | Data template for collectible spirit essences. Fields: essenceName, description, qiValue, essenceColor, icon, collectionEffect |
| **RealmDefinition.cs** | ScriptableObject | Data template for cultivation realms. Fields: realmName, realmIndex, subStage, description, qiCapacity, baseQiRate, breakthroughSuccessRate, nextRealm, baseCombatPower |

### Player (namespace: CultivationGame.Player)

| Script | Type | Purpose |
|--------|------|---------|
| **PlayerStats.cs** | MonoBehaviour | Tracks player cultivation state (current realm, Qi). Handles breakthrough attempts (success/failure with probability). Implements IQiReceiver. Fires GameEvents for Qi and realm changes |
| **PlayerMovement.cs** | MonoBehaviour | Camera-relative movement with Rigidbody physics. Sprint system with stamina drain/regen and delay. Jump with ground detection via raycast. Fires GameEvents for stamina changes |
| **PlayerInteractor.cs** | MonoBehaviour | Detects nearby IInteractable objects via OverlapSphere. Shows/hides interaction prompt UI. Triggers interaction on input action |
| **PlayerInventory.cs** | MonoBehaviour | Dictionary-based item storage (EssenceData → count). Implements IInventory. Fires GameEvents.OnInventoryChanged when items change |

### Systems (namespace: CultivationGame.Systems)

| Script | Type | Purpose |
|--------|------|---------|
| **SpiritEssence.cs** | MonoBehaviour | World-placed collectible essence. Implements IInteractable. On interact: grants Qi via IQiReceiver, adds to inventory via IInventory, then destroys itself. Colors its renderer from EssenceData on Start |

### UI (namespace: CultivationGame.UI)

| Script | Type | Purpose |
|--------|------|---------|
| **PlayerStatsUI.cs** | MonoBehaviour | Listens to GameEvents.OnQiChanged and OnRealmChanged. Updates TextMeshPro fields for Qi counter and realm display. Place on Canvas |
| **StaminaUI.cs** | MonoBehaviour | Listens to GameEvents.OnStaminaChanged. Updates a UI Slider for the stamina bar. Place on Canvas |
| **InventoryManager.cs** | MonoBehaviour | Controls inventory panel toggle (open/close). Switches between Player and UI input maps. Manages cursor lock state. Refreshes display on open |
| **InventoryDisplay.cs** | MonoBehaviour | Renders inventory contents by managing slot prefab instances. Uses simple object pooling (reuses slots, hides extras instead of destroy/recreate) |
| **InventorySlotDisplay.cs** | MonoBehaviour | Individual inventory slot UI element. Displays essence icon (sprite + color) and item count via TextMeshPro |

---

## Key Data Flow

```
SpiritEssence (world object)
    → IQiReceiver.AddQi()        → PlayerStats fires OnQiChanged      → PlayerStatsUI updates text
    → IInventory.AddItem()        → PlayerInventory fires OnInventoryChanged
    → Destroy self

PlayerMovement (every FixedUpdate)
    → fires OnStaminaChanged      → StaminaUI updates slider

PlayerStats.AttemptBreakthrough()
    → success: advance realm      → fires OnRealmChanged               → PlayerStatsUI updates text
    → failure: lose 10% Qi        → fires OnQiChanged                  → PlayerStatsUI updates text

InventoryManager (toggle input)
    → open panel                  → InventoryDisplay.RefreshDisplay()  → InventorySlotDisplay.Setup()
    → close panel                 → re-enable player input
```

---

## Scene Setup Checklist

1. **Player GameObject**: PlayerStats, PlayerMovement, PlayerInteractor, PlayerInventory components
2. **Canvas**: PlayerStatsUI (wire qiText + realmText), StaminaUI (wire staminaSlider), InventoryManager (wire panel + display + playerInventory), InventoryDisplay (wire slotContainer + slotPrefab)
3. **GameManager**: Empty GameObject with GameManager component (persists across scenes)
4. **EventSystem**: Required for UI input
