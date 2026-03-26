# No AI Cultivation — Project Documentation

## Project Overview

**Prison Realm: Alchemy Factory** — A Xianxia cultivation idle-factory game.
A modern chemist reincarnated into a cultivation world is banished to a Prison Realm.
Together with other prisoners, he revolutionizes alchemy to craft Realm-Breaking Pills and escape.

**60% Factory / 40% Cultivation-Combat-Exploration.**
Inspired by: Satisfactory, Arknights Endfield, Xianxia Novels (Martial Peak etc.)

---

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
│   ├── Machines/            MachineData assets (Furnace, Extractor, etc.)
│   ├── Realm/               RealmDefinition assets (00_Mortal, etc.)
│   └── UI/                  UI data sources (BuildMenuDataSource, etc.)
├── Input/                   Unity Input System action maps (.inputactions)
├── Materials/               Physics materials, rendering materials, ghost materials
├── Models/                  Reserved for 3D models and meshes
├── Prefab/                  Prefab assets
│   ├── CraftingStations/    Machine prefabs (Furnace, Crusher, etc.)
│   └── UI/                  UI-specific prefabs (InventorySlot, etc.)
├── Scenes/                  Unity scene files
├── Scripts/                 All C# source code (see below)
├── Sprites/                 2D art assets
│   ├── Biomes/              Biome/terrain sprites
│   ├── Buildings/           Building and machine sprites
│   ├── Characters/          Player and NPC sprites
│   ├── Effects/             Reserved for VFX sprites
│   ├── InnerWorld/          Inner world tile sprites
│   ├── POIs/                Reserved for point-of-interest sprites
│   └── UI/                  UI icons and elements
```

---

## Scripts Folder Structure

```
Scripts/
├── Core/                        Foundation types shared by all assemblies
│   ├── CultivationBuffs.cs          Pill buff state tracking
│   ├── GameEnums.cs                 All shared enums (incl. MachineType)
│   ├── GameEvents.cs                Core event bus (player state events)
│   ├── GameManager.cs               Singleton game state manager
│   ├── IInteractable.cs             Interaction interface
│   └── IQiReceiver.cs               Qi receiver interface
│
├── Data/                        ScriptableObject class definitions
│   ├── DataTemplates/
│   │   ├── EssenceData.cs           Spirit essence data (extends ItemData)
│   │   ├── ItemData.cs              Abstract base for all items
│   │   ├── MachineData.cs           Machine configuration data
│   │   ├── OreVeinData.cs           Ore vein configuration data
│   │   ├── PillData.cs              Pill item data (extends ItemData)
│   │   ├── RawMaterialData.cs       Raw material data (extends ItemData)
│   │   ├── RealmDefinition.cs       Cultivation realm definitions
│   │   ├── RecipeData.cs            Crafting recipe definitions
│   │   └── RecipeDatabase.cs        Recipe lookup database
│   ├── GameDataEvents.cs            Data-layer event bus (factory events)
│   ├── IInventory.cs                Inventory interface
│   ├── MinorRealmConfig.cs          Minor realm generation config
│   └── SaveData.cs                  Save/load data structures
│
├── Editor/                      Editor-only tools and inspectors
│
├── Player/                      Player-specific logic
│   ├── Camera/                  Camera control scripts
│   ├── Interaction/
│   │   └── PlayerInteractor.cs      Interaction detection and prompts
│   ├── Inventory/
│   │   └── PlayerInventory.cs       Dictionary-based item storage (ItemData)
│   ├── Movement/                Player movement and physics
│   ├── PlayerPersistence.cs         Player data persistence
│   └── PlayerStats.cs               Cultivation state, Qi, breakthroughs
│
├── Systems/                     Game systems and world logic
│   ├── Building/                Grid-based building system
│   │   ├── BuildGrid.cs             Grid management and occupation tracking
│   │   └── PlacementController.cs   Ghost preview, placement, rotation
│   ├── Crafting/                Recipe-based crafting
│   │   └── CraftingSystem.cs        Manual crafting logic
│   ├── Essence/
│   │   └── EssenceSpawner.cs        Spirit essence world spawning
│   ├── Factory/                 Machine production pipeline
│   │   ├── BaseMachine.cs           Core machine: input/output, timer processing
│   │   ├── IMachineConnectable.cs   Interface for pipe-connectable machines
│   │   ├── MachineInventory.cs      Shared item buffer with capacity limits
│   │   ├── Merger.cs                2-input / 1-output routing node (2→1 combiner)
│   │   ├── OreVein.cs               World resource node (depletion + respawn)
│   │   ├── QiConduit.cs             Qi power pole (conduit chain)
│   │   ├── QiNetwork.cs             Qi power grid manager (BFS, singleton)
│   │   ├── ResourceExtractor.cs     Auto-mines nearby OreVeins
│   │   ├── Splitter.cs              1-input / 2-output routing node (round-robin)
│   │   ├── SpiritPipe.cs            Transport: connects machine outputs to inputs
│   │   └── StorageContainer.cs      Buffer storage between production steps
│   ├── Interaction/             World-side interactable objects
│   │   ├── CraftingStation.cs       Manual crafting table interaction
│   │   ├── Portal.cs                Scene transition portal
│   │   ├── RealmPortal.cs           Realm-specific portal
│   │   ├── SceneEntryPoint.cs       Scene entry spawn point
│   │   └── SpiritEssence.cs         Collectible essence world object
│   ├── Items/                   Reserved for item system extensions
│   ├── Pill/                    Pill consumption and buffs
│   │   └── PillBuffSystem.cs        Pill buff application and tracking
│   ├── Realm/                   Procedural realm generation
│   │   ├── BiomeZoneMap.cs          Biome zone mapping
│   │   ├── HeightmapBuilder.cs      Terrain heightmap generation
│   │   └── MinorRealmGenerator.cs   Minor realm procedural generation
│   └── Save/                    Save/load system
│       ├── SaveSystem.cs            Core save/load logic
│       ├── SceneTransitionData.cs   Scene transition state
│       └── WorldState.cs            Persistent world state tracking
│
└── Ui/                          All UI display and controllers
    ├── Building/                Build mode UI
    │   ├── BuildMenuController.cs   Build menu panel controller
    │   └── BuildMenuDataSource.cs   Available machines data source
    ├── Crafting/                Crafting UI
    │   └── CraftingController.cs    Crafting panel controller
    ├── DataSources/             UI data binding sources
    ├── Inventory/               Inventory panel UI
    ├── Pause/                   Pause menu UI
    ├── Qi/                      Qi display UI
    ├── Save/                    Save/load UI
    ├── GameStateManager.cs          Game state UI management
    └── UIManager.cs                 Central UI manager
```

---

## Script Reference

### Core (namespace: CultivationGame.Core)

| Script | Type | Purpose |
|--------|------|---------|
| **GameEnums.cs** | Enums | All shared enumerations: GameState, RealmSubStage, DaoType, DaoCategory, BiomeType, POIType, TerrainType, InnerWorldTileType, BuildingTerrain, BuildingRarity, SectRank, BuffType, **MachineType** (Furnace, Crusher, Mixer, Distiller, Condenser, PillPress, Storage, SpiritPipe, ResourceExtractor, Splitter, Merger) |
| **GameEvents.cs** | Static class | Core event bus for player state. Events: OnQiChanged, OnRealmChanged, OnStaminaChanged, OnInventoryChanged, OnMeditationToggled, **OnBuildModeToggled**, OnBuildLayerChanged, OnPauseStateChanged, OnPanelStateChanged |
| **GameManager.cs** | MonoBehaviour (Singleton) | Holds global GameState. Persists across scenes via DontDestroyOnLoad. Access via GameManager.Instance |
| **IInteractable.cs** | Interface | Contract for any world object the player can interact with. Single method: Interact(GameObject user) |
| **IQiReceiver.cs** | Interface | Contract for anything that can receive Qi. Single method: AddQi(double amount). Implemented by PlayerStats |
| **CultivationBuffs.cs** | Static class | Tracks active pill buff state: cultivationSpeedMultiplier, breakthroughBonus. Reset and apply methods for PillBuffSystem |

### Data (namespace: CultivationGame.Data)

| Script | Type | Purpose |
|--------|------|---------|
| **ItemData.cs** | ScriptableObject (abstract) | Base class for all items. Fields: itemName, description, icon, stackSize, rarity. Subclassed by EssenceData, RawMaterialData, PillData |
| **EssenceData.cs** | ScriptableObject | Spirit essence data (extends ItemData). Additional fields: qiValue, essenceColor, collectionEffect |
| **RawMaterialData.cs** | ScriptableObject | Raw material data (extends ItemData). Used for ores, herbs, minerals |
| **PillData.cs** | ScriptableObject | Pill item data (extends ItemData). Fields: pillTier, qiBoost, cultivationSpeedMultiplier, breakthroughBonus, buffDuration, maxDailyUses |
| **MachineData.cs** | ScriptableObject | Machine configuration. Fields: machineName, machineType, prefab, ghostPrefab, gridSize, buildCost, icon, processingSpeed, inputSlots, outputSlots, **qiConsumptionRate** |
| **OreVeinData.cs** | ScriptableObject | Ore vein configuration. Fields: resource (RawMaterialData), totalYield, yieldPerExtraction, respawnTimeSeconds |
| **RealmDefinition.cs** | ScriptableObject | Cultivation realm definitions. Fields: realmName, realmIndex, qiCapacity, baseQiRate, breakthroughSuccessRate, **spiritSenseRange**, nextRealm |
| **RecipeData.cs** | ScriptableObject | Crafting recipe. Fields: recipeName, inputs (RecipeSlot[]), outputs (RecipeSlot[]), processingTime, requiredMachine, craftingDuration |
| **RecipeDatabase.cs** | ScriptableObject | Recipe lookup database. Methods: GetRecipesForMachine(MachineType), GetRecipesForItem(ItemData) |
| **GameDataEvents.cs** | Static class | Data-layer event bus. Events: OnCraftingStarted, OnCraftingCompleted, OnCraftingFailed, OnPillConsumed, OnPillEffectsApplied, OnMachinePlaced, OnMachineRemoved, OnBuildModeGhostStarted, OnBuildModeGhostCancelled, OnMachineInteracted, OnMachineProcessingCompleted, OnPipeConnected, OnPipeDisconnected, OnPipeInteracted, **OnQiNetworkChanged**, OnResourceExtracted |
| **IInventory.cs** | Interface | Contract for anything that stores items. Method: AddItem(ItemData, int amount). Implemented by PlayerInventory, MachineInventory |
| **MinorRealmConfig.cs** | ScriptableObject | Minor realm generation configuration |
| **SaveData.cs** | Serializable classes | Save/load data structures. Includes: InventorySaveEntry, BuildingSaveEntry, PipeConnectionSaveEntry, MachineInventorySaveEntry, OreVeinSaveEntry |

### Player (namespace: CultivationGame.Player)

| Script | Type | Purpose |
|--------|------|---------|
| **CameraSystem.cs** | MonoBehaviour | Manages camera transitions between Cinemachine (3rd person) and SpiritSenseCamera (overhead). Build mode toggle (Shift), saves/restores Cinemachine state for smooth transitions. Realm-based Spirit Sense zoom range |
| **SpiritSenseCamera.cs** | MonoBehaviour | Overhead camera for build/meditation mode. Pan (WASD), orbit (middle mouse), zoom (scroll). SetMaxZoom(float) for realm-based range scaling |
| **PlayerStats.cs** | MonoBehaviour | Tracks player cultivation state (current realm, Qi). Handles breakthrough attempts. Implements IQiReceiver. Fires GameEvents for Qi and realm changes |
| **PlayerMovement.cs** | MonoBehaviour | Camera-relative movement with Rigidbody physics. Sprint system with stamina drain/regen. Jump with ground detection. Fires GameEvents for stamina changes |
| **PlayerInteractor.cs** | MonoBehaviour | Detects nearby IInteractable objects via OverlapSphere. Shows/hides interaction prompt UI. Triggers interaction on input action |
| **PlayerInventory.cs** | MonoBehaviour | Dictionary-based item storage (Dictionary<ItemData, int>). Implements IInventory. Fires GameEvents.OnInventoryChanged when items change |
| **PlayerPersistence.cs** | MonoBehaviour | Handles player data persistence across scenes |

### Systems (namespace: CultivationGame.Systems)

| Script | Type | Purpose |
|--------|------|---------|
| **BuildGrid.cs** | MonoBehaviour | Grid management for building placement. Cell-based (configurable size). Tracks occupied cells. Terrain height snapping |
| **PlacementController.cs** | MonoBehaviour | Build mode controller. Ghost preview, grid snapping, rotation (90° steps), placement validation, inventory cost deduction. Wires MachineData to BaseMachine/ResourceExtractor/StorageContainer on placement |
| **CraftingSystem.cs** | MonoBehaviour | Manual crafting logic. Checks recipe requirements, consumes inputs, produces outputs. Works with CraftingStation interaction |
| **EssenceSpawner.cs** | MonoBehaviour | Spirit essence world spawning with respawn timers |
| **IMachineConnectable.cs** | Interface | Contract for machine connectivity (SpiritPipe). Properties: InputInventory, OutputInventory, MachineData. Implemented by BaseMachine, ResourceExtractor, StorageContainer, QiConduit |
| **BaseMachine.cs** | MonoBehaviour | Core machine component. Input/output MachineInventory, timer-based recipe processing. IsPowered set by QiNetwork. Methods: TryStartProcessing(), AddInput(), RemoveOutput(), SetRecipe() |
| **QiNetwork.cs** | MonoBehaviour (Singleton) | Qi power grid manager. BFS connectivity from source through QiConduits. Sets IsPowered on machines in range. Consumes Qi from player pool per frame |
| **QiConduit.cs** | MonoBehaviour | Qi power pole. connectionRadius (conduit↔conduit), machineRadius (conduit→machine). Placed on grid. IsConnected set by QiNetwork |
| **MachineInventory.cs** | Class | Shared item buffer (Dictionary<ItemData, int>) with capacity limits. Used by BaseMachine, ResourceExtractor, StorageContainer |
| **SpiritPipe.cs** | MonoBehaviour | Transport system. Connects output of one machine to input of another. Configurable transferInterval and itemsPerTransfer. Optional item filter |
| **Splitter.cs** | MonoBehaviour | 1-input / 2-output routing node. Round-robin distribution from its InputInventory buffer to two destination machines. Implements IMachineConnectable |
| **Merger.cs** | MonoBehaviour | 2-input / 1-output routing node. Alternately pulls from two source machines into its OutputInventory buffer. Implements IMachineConnectable |
| **OreVein.cs** | MonoBehaviour | World resource node with finite yield, depletion, and respawn. Uses WorldState for persistence. Visual dimming when depleted |
| **ResourceExtractor.cs** | MonoBehaviour | Machine that auto-mines nearby OreVeins (Physics.OverlapSphere, 4m radius). Extraction on timer, outputs to MachineInventory |
| **StorageContainer.cs** | MonoBehaviour | Buffer storage between production steps. Single MachineInventory for input+output |
| **PillBuffSystem.cs** | MonoBehaviour | Pill consumption and buff application. Applies temporary buffs to CultivationBuffs. Handles buff duration and expiry |
| **CraftingStation.cs** | MonoBehaviour | World-placed crafting table. Implements IInteractable. Opens crafting UI on interaction |
| **Portal.cs** | MonoBehaviour | Scene transition portal. Implements IInteractable |
| **RealmPortal.cs** | MonoBehaviour | Realm-specific portal with cultivation requirements |
| **SceneEntryPoint.cs** | MonoBehaviour | Scene entry spawn point for player |
| **SpiritEssence.cs** | MonoBehaviour | World-placed collectible essence. Implements IInteractable. Grants Qi, adds to inventory, destroys self |
| **BiomeZoneMap.cs** | Class | Biome zone mapping for procedural generation |
| **HeightmapBuilder.cs** | Class | Terrain heightmap generation |
| **MinorRealmGenerator.cs** | MonoBehaviour | Minor realm procedural generation |
| **SaveSystem.cs** | MonoBehaviour | Core save/load logic |
| **SceneTransitionData.cs** | ScriptableObject | Scene transition state data |
| **WorldState.cs** | Static class | Persistent world state tracking (spawner respawns, etc.) |

### UI (namespace: CultivationGame.UI)

| Script | Type | Purpose |
|--------|------|---------|
| **BuildMenuController.cs** | MonoBehaviour | Build menu panel controller. Listens to **OnBuildModeToggled** (works in any camera perspective). Displays available machines from BuildMenuDataSource. Triggers PlacementController on selection |
| **BuildMenuDataSource.cs** | ScriptableObject | Data source for build menu. Holds array of available MachineData assets |
| **CraftingController.cs** | MonoBehaviour | Crafting panel UI controller. Displays available recipes, handles crafting interaction |
| **GameStateManager.cs** | MonoBehaviour | Game state UI management (pause, menus) |
| **UIManager.cs** | MonoBehaviour | Central UI manager. Coordinates all UI panels |
| **PlayerStatsUI.cs** | MonoBehaviour | Listens to GameEvents.OnQiChanged and OnRealmChanged. Updates Qi counter and realm display |
| **StaminaUI.cs** | MonoBehaviour | Listens to GameEvents.OnStaminaChanged. Updates stamina bar slider |
| **InventoryManager.cs** | MonoBehaviour | Controls inventory panel toggle. Switches input maps. Manages cursor lock state |
| **InventoryDisplay.cs** | MonoBehaviour | Renders inventory contents with slot prefab pooling |
| **InventorySlotDisplay.cs** | MonoBehaviour | Individual inventory slot: icon + count display |

---

## Event Systems

The project uses a **dual event bus** architecture to maintain clean assembly dependencies:

### GameEvents.cs (Core Assembly)
Lightweight events for **player state** — accessible from all assemblies.

| Event | Trigger |
|-------|---------|
| OnQiChanged | Player Qi amount changes |
| OnRealmChanged | Player advances to new cultivation realm |
| OnStaminaChanged | Player stamina changes (sprint, regen) |
| OnInventoryChanged | Player inventory contents change |
| OnMeditationToggled | Player enters/exits meditation |
| OnBuildModeToggled | Build mode toggled on/off (Shift key, any perspective) |
| OnBuildLayerChanged | Build elevation layer changed |
| OnPauseStateChanged | Game paused/unpaused |
| OnPanelStateChanged | UI panel opened/closed |

### GameDataEvents.cs (Data Assembly)
Data-layer events for **factory and crafting systems** — accessible from Systems and UI.

| Event | Trigger |
|-------|---------|
| OnCraftingStarted | Recipe crafting begins |
| OnCraftingCompleted | Recipe crafting finishes |
| OnPillConsumed | Player consumes a pill |
| OnMachinePlaced | Machine placed on grid |
| OnMachineRemoved | Machine removed from grid |
| OnBuildModeGhostStarted | Ghost placement preview started |
| OnBuildModeGhostCancelled | Ghost placement preview cancelled |
| OnQiNetworkChanged | Qi network demand/supply changed |
| OnMachineInteracted | Player interacts with a machine |
| OnMachineProcessingCompleted | Machine finishes processing a recipe |
| OnPipeConnected | Spirit pipe connection established |
| OnPipeDisconnected | Spirit pipe connection removed |
| OnPipeInteracted | Player interacts with a pipe |
| OnResourceExtracted | Resource extractor mines from ore vein |

---

## Key Data Flows

### Factory Pipeline

```
Player Meditation → Qi Pool
    → QiNetwork (BFS from source through QiConduits)
    → IsPowered = true on machines in conduit range

OreVein (world resource node)
    → ResourceExtractor (auto-mines on timer, Physics.OverlapSphere 4m)
    → SpiritPipe (transfers items at configurable interval)
    → BaseMachine (processes recipe: inputs → outputs on timer, requires IsPowered)
    → SpiritPipe (moves outputs to next stage)
    → StorageContainer (buffer storage)

Splitter (1→2 routing):
    SpiritPipe → Splitter.InputInventory → round-robin to destA.InputInventory / destB.InputInventory

Merger (2→1 routing):
    sourceA.OutputInventory + sourceB.OutputInventory → Merger.OutputInventory → SpiritPipe → next machine
```

### Crafting Flow
```
RecipeData (defines inputs, outputs, processing time)
    + Player Inventory (provides ingredients)
    → CraftingSystem (validates recipe, consumes inputs)
    → Output Items added to Player Inventory
    → fires GameDataEvents.OnCraftingCompleted
```

### Pill Flow
```
PillData (defines tier, qiBoost, buffs, duration)
    → PillBuffSystem.ConsumePill()
    → CultivationBuffs (applies temporary multipliers)
    → PlayerStats (enhanced cultivation speed, breakthrough chance)
    → fires GameDataEvents.OnPillConsumed
```

### Essence Collection (original flow)
```
SpiritEssence (world object)
    → IQiReceiver.AddQi()        → PlayerStats fires OnQiChanged      → PlayerStatsUI updates
    → IInventory.AddItem()        → PlayerInventory fires OnInventoryChanged
    → Destroy self

```

---

## Unity Editor Setup Guide

### Building System Setup

#### 1. Create a "BuildSystem" GameObject in the Scene

1. In the Hierarchy, create an empty GameObject named **BuildSystem**.
2. Add the **BuildGrid** component.
   - Set **Cell Size** to `2` (2 m × 2 m grid cells).
   - Set **Terrain Layer** to the layer mask that contains your terrain/ground colliders.
3. Add the **PlacementController** component.
   - Drag the **BuildGrid** component into the `Build Grid` field.
   - Drag the **PlayerInventory** (on the Player GameObject) into the `Player Inventory` field.
   - Set **Terrain Layer** to the same terrain layer mask.
   - Set **Build Camera** to the Main Camera (or leave empty — it will fall back to `Camera.main`).
   - Assign the **Input Action References** from the BuildMode map:
     - `Place Action` → `BuildMode/Place`
     - `Cancel Action` → `BuildMode/Cancel`
     - `Rotate Action` → `BuildMode/Rotate`

#### 2. Create Ghost Materials

In `Assets/_Project/Materials/` (create the folder if it doesn't exist):

**GhostValid.mat**
- Shader: **Universal Render Pipeline/Lit**
- Surface Type: **Transparent**
- Base Color: **RGBA (0.2, 0.8, 0.2, 0.4)** (semi-transparent green)

**GhostInvalid.mat**
- Shader: **Universal Render Pipeline/Lit**
- Surface Type: **Transparent**
- Base Color: **RGBA (0.8, 0.2, 0.2, 0.4)** (semi-transparent red)

Assign these to the PlacementController:
- `Ghost Valid Material` → GhostValid
- `Ghost Invalid Material` → GhostInvalid

#### 3. Add BuildMenuController to the UI

1. Select the **UIManager** GameObject in the scene.
2. Add the **BuildMenuController** component.
3. Create a **BuildMenuDataSource** asset:
   - Right-click in Project → **Create → Cultivation → UI → Build Menu Data Source**
   - Save it in `Assets/_Project/Data/UI/` (create folder if needed).
4. Wire the BuildMenuController:
   - `Build Menu Data` → the BuildMenuDataSource asset you just created.
   - `Placement Controller` → the PlacementController on the BuildSystem GameObject.

#### 4. Create MachineData Assets

For each machine type (Furnace, Crusher, Mixer, etc.):

1. Right-click in Project → **Create → Cultivation → Machine Data**
2. Save in `Assets/_Project/Data/Machines/` (create folder if needed).
3. Fill in:
   - **Machine Name**: e.g. "Furnace"
   - **Machine Type**: select from the enum
   - **Prefab**: the actual machine prefab (create in `Assets/_Project/Prefab/CraftingStations/`)
   - **Ghost Prefab**: (optional) a dedicated ghost prefab, or leave empty to reuse the main prefab
   - **Grid Size**: e.g. (1, 1) for a 2 m × 2 m machine, (2, 1) for a 4 m × 2 m machine
   - **Build Cost**: array of RecipeIngredient (item + amount)
   - **Icon**: a Sprite for the build menu

#### 5. Populate the BuildMenuDataSource

1. Select the BuildMenuDataSource asset.
2. In the **Available Machines** array, add all the MachineData assets you want
   to appear in the build menu.

#### 6. Create Machine Prefabs

In `Assets/_Project/Prefab/CraftingStations/`:

1. Create a prefab for each machine (e.g. a cube placeholder with a collider).
2. Optionally add the `CraftingStation` component (or a new `PlacedMachine` component)
   so the player can interact with placed machines.
3. Make sure each prefab has at least one **Renderer** (for the ghost material swap)
   and one **Collider** (for interaction raycasting after placement).

#### Controls

| Key | Action | Context |
|-----|--------|---------|
| **Left Shift** | Toggle Build Mode | Any perspective |
| Click a machine in the Build Menu | Start ghost placement | Build Mode |
| **Left Click** | Confirm placement | Build Mode (ghost active) |
| **Right Click** or **Escape** | Cancel placement | Build Mode (ghost active) |
| **R** | Rotate ghost 90° | Build Mode (ghost active) |
| **WASD** | Pan camera | Spirit Sense |
| **Middle Mouse + Drag** | Orbit camera | Spirit Sense |
| **Scroll Wheel** | Zoom (realm-based max) | Spirit Sense |
| **Page Up / Page Down** | Change build layer | Spirit Sense |
| **G** | Toggle meditation / Spirit Sense | 3rd Person |

---

### Factory System Setup

#### 1. Create Ore Resources (RawMaterialData assets)
- **Create → Cultivation → Raw Material Data** for each ore type:
  - Spirit Stone Ore (color: cyan/blue)
  - Iron Essence (color: dark gray)
  - Crystal Shard (color: purple)
  - Jade Fragment (color: green)

#### 2. Create OreVeinData assets
- **Create → Cultivation → Ore Vein Data** for each vein type
- Assign the corresponding RawMaterialData to the `resource` field
- Configure `totalYield` (default 100), `yieldPerExtraction` (default 1), `respawnTimeSeconds` (default 600)

#### 3. Create MachineData assets for new machine types
- **Resource Extractor**: MachineType = `ResourceExtractor`, processingSpeed affects extraction interval
- **Storage Container**: MachineType = `Storage`, inputSlots/outputSlots define pipe connections
- **Spirit Pipe**: MachineType = `SpiritPipe`, gridSize = (1,1)

#### 4. Create Prefabs

Each machine type needs a prefab with the appropriate component:

| Machine | Required Component | Notes |
|---------|-------------------|-------|
| Furnace, Crusher, etc. | `BaseMachine` | Needs recipe assignment |
| Resource Extractor | `ResourceExtractor` | Auto-detects nearby OreVeins (4m radius) |
| Storage Container | `StorageContainer` | Single inventory for input+output |
| Spirit Pipe | `SpiritPipe` | Connect via Interact (UI TBD) |
| Ore Vein | `OreVein` | Needs MeshRenderer + Collider + OreVeinData |

#### 5. Place OreVeins in the Overworld Scene
- Create GameObjects with MeshRenderer + Collider
- Add the `OreVein` component
- Assign an `OreVeinData` asset
- The `uniqueId` auto-generates on first validation

#### 6. Add to Build Menu
- Open the BuildMenuDataSource in the Inspector
- Add new MachineData assets to the `availableMachines` array

#### How the Factory Loop Works
1. **OreVein** sits in the world with a finite resource yield
2. **ResourceExtractor** placed near an OreVein auto-detects it (Physics.OverlapSphere)
3. Extractor mines on a timer, placing resources in its OutputInventory
4. **SpiritPipe** connects Extractor's output to a BaseMachine's input
5. **BaseMachine** (Furnace, Crusher, etc.) checks inputs against its recipe, processes on timer, produces outputs
6. Another SpiritPipe moves outputs to the next machine or a StorageContainer

#### Spirit Pipe Connection
- Place a Spirit Pipe on the grid
- Interact with it to open configuration (UI to be implemented)
- Call `pipe.Connect(sourceMachine, destinationMachine)` to establish the link
- Pipe transfers items at `transferInterval` (default 1s), `itemsPerTransfer` (default 1)
- Optional `filterItem` restricts which items flow through

#### OreVein Respawn
- Follows the EssenceSpawner pattern using WorldState
- When depleted, records timestamp via `WorldState.RecordSpawnerCollection()`
- On scene load, checks `WorldState.GetRemainingRespawn()` to resume respawn timer
- Visual dimming via MaterialPropertyBlock when depleted

---

## Scene Setup Checklist

1. **Player GameObject**: PlayerStats, PlayerMovement, PlayerInteractor, PlayerInventory components
2. **Canvas**: PlayerStatsUI (wire qiText + realmText), StaminaUI (wire staminaSlider), InventoryManager (wire panel + display + playerInventory), InventoryDisplay (wire slotContainer + slotPrefab)
3. **GameManager**: Empty GameObject with GameManager component (persists across scenes)
4. **EventSystem**: Required for UI input
5. **BuildSystem**: Empty GameObject with BuildGrid + PlacementController components (see Building System Setup above)
6. **UIManager**: Add BuildMenuController component, wire BuildMenuDataSource and PlacementController
7. **OreVeins**: Place OreVein GameObjects in the overworld with OreVeinData assigned
8. **Machine Prefabs**: Ensure all machine prefabs are registered in BuildMenuDataSource



---

## Future Work

→ Siehe `Assets/_Project/TODO.md`
