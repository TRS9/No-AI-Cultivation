# Prison Realm: Alchemy Factory — Vollständige Aufgabenliste

Letzte Aktualisierung: 2026-03-30
Analyse-Status: Alle Scripts und Data-Assets manuell geprüft.

---

## 1. KRITISCH: Scene & Prefab Setup (Unity-Editor, manuell)

Diese Aufgaben müssen **im Unity-Editor** erledigt werden und können nicht von Agents gemacht werden.

### 1.1 Universe.unity — Kaputte Script-Referenzen reparieren

- [ ] **"Missing Script" entfernen**: `InventoryManager` existiert nicht mehr (umbenannt zu `InventoryController`). Im Inspector das verwaiste Component auf dem betroffenen GameObject löschen (Rechtsklick → Remove Component)
- [ ] Prüfen ob nach dem Öffnen von Unity weitere "Missing Script"-Warnungen in der Console erscheinen

### 1.2 Camera-System neu verkabeln (Issue #76)

Das Camera-System wurde durch parallele Copilot-Agents beeinträchtigt. Die `.meta`-GUIDs für CameraSystem, SpiritSenseCamera und BuildElevation wurden wiederhergestellt, aber die **Inspector-Referenzen** müssen trotzdem manuell gesetzt werden:

- [ ] **CameraSystem Inspector**: `playerStats` Referenz auf das Player-GameObject mit PlayerStats-Component ziehen
- [ ] **CameraSystem Inspector**: `BuildToggle` InputActionReference auf `Player/BuildToggle` setzen (aus InputSystem_Actions Asset)
- [ ] **CameraSystem Inspector**: Prüfen ob `_inputAxisController` (CinemachineInputAxisController) automatisch gefunden wird
- [ ] **SpiritSenseCamera Inspector**: Alle InputActionReference-Felder prüfen (Pan, Zoom, Orbit)
- [ ] Camera-Wechsel testen: 3rd Person ↔ Spirit Sense (G-Taste) — nahtloser Übergang?

### 1.3 PlacementController & Build-System

- [ ] **PlacementController Inspector**: Input-Referenzen prüfen (Place, Cancel, Rotate, Remove)
- [ ] **BuildGrid Inspector**: Cell Size = 2, Terrain Layer korrekt gesetzt
- [ ] **BuildMenuDataSource Asset**: Alle 11 MachineData-Assets in `availableMachines` Array eingetragen?
- [ ] Cursor-Handling in 3rd Person Build-Modus testen (Cursor sichtbar + Kamera-Look einschränken)
- [ ] Build-Menu UI-Styling für 3rd Person Overlay optimieren

### 1.4 Weitere Scene-Setup Prüfungen

- [ ] **Player GameObject**: Hat PlayerStats, PlayerMovement, PlayerInteractor, PlayerInventory, PlayerCombatController?
- [ ] **Canvas**: PlayerStatsUI, StaminaUI, InventoryController, HUDController korrekt verkabelt?
- [ ] **GameManager**: Existiert als Singleton (DontDestroyOnLoad)?
- [ ] **EventSystem**: Vorhanden mit InputSystemUIInputModule?
- [ ] **BuildSystem GameObject**: BuildGrid + PlacementController vorhanden?
- [ ] **UIManager**: BuildMenuController + BuildMenuDataSource referenziert?
- [ ] **SoundManager**: Singleton in Scene? SoundEventTrigger auf passendem GameObject?
- [ ] **BreakthroughSystem**: Auf GameObject mit PlayerStats-Zugriff?
- [ ] **GameStateManager**: Singleton vorhanden?

### 1.5 Prefab-Prüfung

Jeder Maschinen-Typ braucht ein Prefab mit der richtigen Component:

- [ ] Furnace Prefab → `BaseMachine` Component
- [ ] Crusher Prefab → `BaseMachine` Component
- [ ] Mixer Prefab → `BaseMachine` Component
- [ ] Distiller Prefab → `BaseMachine` Component
- [ ] Condenser Prefab → `BaseMachine` Component
- [ ] PillPress Prefab → `BaseMachine` Component
- [ ] Storage Prefab → `StorageContainer` Component
- [ ] ResourceExtractor Prefab → `ResourceExtractor` Component
- [ ] SpiritPipe Prefab → `SpiritPipe` Component
- [ ] Splitter Prefab → `Splitter` Component
- [ ] Merger Prefab → `Merger` Component
- [ ] Jedes MachineData-Asset hat sein Prefab-Feld gesetzt

### 1.6 UI-Components auf UIManager-GameObject setzen

Alle UI-Scripts die `InitializeUI(VisualElement root)` implementieren, werden via `BroadcastMessage` vom `UIManager` aufgerufen. Sie müssen als **Components auf demselben GameObject** wie `UIManager` sitzen:

- [ ] `BuildMenuController`
- [ ] `DialogueUI`
- [ ] `MachineInspectUI`
- [ ] `MachineUIController` (evtl. Duplikat von MachineInspectUI — prüfen)
- [ ] `FactoryDashboardUI`
- [ ] `PipeConnectionUI`
- [ ] `BreakthroughController`
- [ ] `HealthBarUI`
- [ ] `EnemyHealthBarUI`

### 1.7 Weitere Singletons/Systems auf GameObjects

- [ ] `PillBuffSystem` — braucht eigenes GameObject oder auf Player
- [ ] `SoundEventTrigger` — braucht eigenes GameObject (subscribed auf Events)
- [ ] `PipeConnectionVisualizer` — braucht eigenes GameObject
- [ ] `BreakthroughSystem` — braucht eigenes GameObject mit PlayerStats-Zugriff

### 1.8 OreVeins in Overworld platzieren

- [ ] OreVein GameObjects mit MeshRenderer + Collider + OreVein-Component erstellen
- [ ] OreVeinData-Assets zuweisen (Spirit Stone Ore, Iron Ore, Crystal, Jade)
- [ ] Veins testen: Extraction, Depletion, Respawn

---

## 2. GitHub Issues (Copilot-Agent Tasks)

### Phase 7: NPC & Quest System

| Issue | Titel | Status | Hängt von | Anmerkung |
|-------|-------|--------|-----------|-----------|
| #102 | QuestData ScriptableObject & QuestSystem | ❌ OFFEN | — | Kein QuestData.cs, kein QuestManager.cs |
| #103 | NPCController MonoBehaviour | ❌ OFFEN | NPCData (existiert ✅) | NPCData.cs + DialogueNode.cs vorhanden, aber kein NPCController.cs MonoBehaviour |
| #104 | Quest-Journal UI | ❌ OFFEN | #102 | Warten auf #102 |
| #105 | Händler-System: ShopData & UI | ❌ OFFEN | NPCData (existiert ✅) | Kein ShopData.cs, kein ShopUI |

### Phase 9: Story & Endgame

| Issue | Titel | Status | Hängt von | Anmerkung |
|-------|-------|--------|-----------|-----------|
| #106 | StoryData, StoryProgressSystem & Win-Condition | ❌ OFFEN | #102 | Kein StoryData.cs, kein Win-Condition |
| #112 | Realm-Breaking Pill Produktionskette | ❌ OFFEN | — | Kein Rezept-Asset für Realm-Breaking Pill, keine Produktionskette |

### Systems & Scenes

| Issue | Titel | Status | Hängt von | Anmerkung |
|-------|-------|--------|-----------|-----------|
| #107 | SceneTransitionManager | ⚠️ TEILWEISE | — | Portal.cs, RealmPortal.cs, SceneEntryPoint.cs, SceneTransitionData.cs existieren — aber kein zentraler SceneTransitionManager |
| #108 | Tutorial-System | ❌ OFFEN | — | Kein TutorialSystem.cs |
| #109 | Hauptmenü-Scene | ❌ OFFEN | — | Keine MainMenu.unity (Szenen: Universe, Grotto, MinorRealm, test) |
| #110 | Kompass-HUD | ❌ OFFEN | — | Kein CompassHUD.cs |
| #111 | Inventar-Tooltips & Rechtsklick-Kontextmenü | ❌ OFFEN | — | Nicht implementiert |

### Tests

| Issue | Titel | Status | Anmerkung |
|-------|-------|--------|-----------|
| #114 | Unit Tests: QiNetwork BFS | ❌ OFFEN | Kein QiNetworkTests.cs (MachineInventoryTests vorhanden) |
| #115 | Unit Tests: BaseMachine Pipeline | ⚠️ TEILWEISE | MachineInventoryTests.cs deckt Inventory-Teil ab, aber kein vollständiger BaseMachine-Pipeline-Test |
| #116 | Unit Tests: SaveSystem | ❌ OFFEN | Kein SaveSystemTests.cs |

### Qualität

| Issue | Titel | Status | Anmerkung |
|-------|-------|--------|-----------|
| #113 | Game Balance Pass | ❌ OFFEN | Zuletzt (nach allem Content) |
| #117 | Code-Qualität Review | ❌ OFFEN | Siehe Sektion 4 |

### Empfohlene Agent-Reihenfolge

**Welle 1** (alle parallel): #102, #103, #105, #107, #108, #109, #110, #111, #112, #114, #116, #117
**Welle 2** (nach #102 fertig): #104, #106
**Welle 3** (nach #115-Partial ausbauen): vollständige #115
**Welle 4** (zum Schluss): #113

---

## 3. Kleinere technische TODOs (noch kein GitHub Issue)

- [ ] **Save/Load für Splitter/Merger-Verbindungen**: Verbindungen werden aktuell nicht in SaveData serialisiert — auf Scene-Reload gehen Pipe-Verbindungen verloren
- [ ] **Visual Pipes**: Line Renderer oder Mesh-Generierung zwischen verbundenen Maschinen zur visuellen Darstellung der Logistics-Netzwerke
- [ ] **Throughput Upgrades**: `itemsPerTransfer` erhöhen / `transferInterval` senken via Upgrade-System (Post-Core-Loop Feature)
- [ ] **OnPlayerDodge Sound fehlt**: Event `GameEvents.OnPlayerDodge` existiert, wird aber nirgends subscribed — `SoundEventTrigger.cs` sollte einen Handler bekommen
- [ ] **NavMesh baken**: Für NPCController (Phase 7) und EnemyAI brauchen alle begehbaren Flächen ein NavMesh

---

## 4. Bekannte Code-Probleme

### 4.1 Duplikate / Redundanz prüfen

- [ ] `MachineUIController.cs` vs `MachineInspectUI.cs` — mögliche Duplikation der Maschinen-UI. Klären welches das aktive ist, anderes löschen
- [ ] `GameManager.cs` vs `GameStateManager.cs` — beide tracken Pause-State separat. `GameManager` wird nirgends aktiv genutzt, `GameStateManager` schon. Wahrscheinlich kann `GameManager` gelöscht oder konsolidiert werden
- [ ] `UIManager.cs` vs `GameStateManager.cs` — beide managen UI-State. Klären ob beide nötig sind oder konsolidiert werden können
- [ ] `HUDController.cs` + `HUDDataSource.cs` — HUD hat zwei Files, funktioniert aber. Architektur-Review ob das sauber ist

### 4.2 Assembly-Konfiguration

- [ ] `Game.Systems.asmdef` — wurde von einem Copilot-Agent modifiziert (Commit 345dfef). Prüfen ob die Referenzen noch stimmen
- [ ] Prüfen ob alle neuen Scripts (SoundManager, BreakthroughSystem, etc.) im richtigen Assembly liegen

---

## 5. Implementierte Features (Referenz)

### Core Factory (vollständig ✅)
- `ItemData`, `PillData`, `EssenceData`, `RawMaterialData`, `OreVeinData`
- `RecipeData` + `RecipeDatabase` (mit Lookup-Methoden)
- MachineData Assets (14 Assets: AlchemyDistiller, BasicFurnace, Condenser, Crusher, Distiller, Furnace, Merger, Mixer, PillPress, ResourceExtractor, SpiritCrusher, SpiritPipe, Splitter, Storage)
- `BaseMachine` (Input/Output Inventories, Recipe Processing, Power Check)
- `MachineInventory` (Dictionary-basiert, Stack-Limits)
- `SpiritPipe`, `Splitter`, `Merger` (Logistics-Routing)
- `QiNetwork` + `QiConduit` (BFS-Konnektivität, Dirty-Flag Optimierung)
- `OreVein` + `ResourceExtractor` (Auto-Mining, Depletion, Respawn)
- OreVein Assets: CopperVein, IronVein, SpiritStoneVein
- `StorageContainer`
- `BuildGrid` + `PlacementController` (Ghost Preview, Rotation, Inventory-Kosten)
- `PipeConnectionUI` + `PipeConnectionVisualizer` (Click-to-Connect)
- `MachineInspectUI` (Echtzeit-Status, Recipe-Wechsel)
- `FactoryDashboardUI` (Produktion/Minute, Bottleneck-Detection)
- `RecipeDatabaseWindow` Editor-Tool (Validierung + Balance-Analyse)

### Player & Combat (vollständig ✅)
- `PlayerMovement`, `PlayerStats`, `PlayerInventory`, `PlayerInteractor`, `PlayerPersistence`
- `CameraSystem` (3rd Person ↔ Spirit Sense, Build-Mode entkoppelt)
- `SpiritSenseCamera` + `BuildElevation`
- `PlayerCombatController` (Attack, Dodge, Abilities)
- `EnemyAI` (NavMeshAgent: Patrol → Detect → Chase → Attack)
- `HealthSystem`, `EnemyLoot`, `LootSystem`
- 8 EnemyData Assets: AshWolf, CorruptCultivator, PrisonGuard, RealmBoss, SpectralGuard, SpiritBeastT1, SpiritBeastT2, StoneSerpent

### Cultivation (vollständig ✅)
- `PillBuffSystem` (Konsum, temporäre Buffs, Buff-Events)
- `BreakthroughSystem` + `BreakthroughController` (Chance-Mechanik, UI)
- 5 Pill Assets: BasicPill, BreakthroughPill, BrewQiPill, QiGatheringPill, WarriorPill
- Umfangreiche RealmDefinition Assets (QiRefinement I-IX, FoundationEstablishment, GoldenCore, NascentSoul, OriginReturning, ... bis DaoSource)
- 5 Essence Assets (Earth, Fire, Iron, Lightning, Water)
- 7 RawMaterial Assets (CopperOre, CrystalShard, IronOre, JadeFragment, SpiritGrass, SpiritHerb, SpiritStone)

### NPC & Dialogue (Daten vorhanden, keine Runtime-Logik ⚠️)
- `NPCData` ScriptableObject (Daten-Template vorhanden)
- `DialogueNode` ScriptableObject (Dialogue-Baum-Knoten)
- `DialogueUI` (UI-Component mit Typewriter-Effekt)
- **Fehlt**: `NPCController` MonoBehaviour, `DialogueSystem` Controller, NPC-Assets

### Audio (vollständig ✅)
- `SoundManager` (Singleton, AudioSource-Pool)
- `SoundEventTrigger` (subscribed auf GameEvents)
- `SoundClip` ScriptableObject

### Save & Scene (vollständig ✅)
- `SaveSystem` (atomares JSON: write-to-temp, rename)
- `WorldState` (aggregierter Spielzustand)
- `SceneTransitionData` (static Übergabe-Klasse)
- `Portal`, `RealmPortal`, `SceneEntryPoint` (Scene-Übergänge funktional)
- 4 Szenen: Universe (Overworld), Grotto (Workshop), MinorRealm, test

### Crafting (vollständig ✅)
- `CraftingSystem` (Kern-Logik)
- `CraftingStation` (Interaktable in Welt)
- `CraftingController` UI + `CraftingDataSource`
- 5 Recipe Assets + RecipeDatabase Asset

### UI & HUD (vollständig ✅)
- `HUDController` + `HUDDataSource` (Qi/HP/Stamina Bars)
- `BuildMenuController` + `BuildMenuDataSource`
- `InventoryController` + `InventoryDataSource`
- `PauseMenuController`
- `GameStateManager` (Panel-Management, Cursor-Locking)
- `UIManager` (BroadcastMessage zu UI-Components)
- `MachineUIController` + `MachineInspectUI`
- `HealthBarUI` + `EnemyHealthBarUI`
- `BreakthroughController`

### Editor Tools (vollständig ✅)
- `RecipeDatabaseWindow` (Validierung + Balance-Analyse)
- `EssencePainterWindow` (Biome-Painted Essence Spawning)
- `CsvImportWindow` (Bulk-Import von Items via CSV)
- `MinorRealmConfigEditor` (Custom Inspector)

### Unit Tests (teilweise ✅)
- `MachineInventoryTests` (MachineInventory Coverage)
- `RecipeDataValidationTests` (RecipeData-Felder + Constraints)
- `RecipeDatabaseLookupTests` (GetRecipesForMachine, GetRecipesForItem)
- `LootSystemTests` (Drop-Table Logik)
- **Fehlen**: QiNetworkTests, BaseMachinePipelineTests, SaveSystemTests
