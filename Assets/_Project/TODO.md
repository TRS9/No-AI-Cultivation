# Prison Realm: Alchemy Factory — Vollständige Aufgabenliste

Letzte Aktualisierung: 2025-07-22
Analyse-Status: Deep Analysis + Code Review Pass abgeschlossen — Alle Scripts, Data-Assets, UXML/USS, Assemblies, MD-Dokumentation geprüft.

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
- [ ] **GameManager**: ~~Existiert als Singleton (DontDestroyOnLoad)?~~ **GELÖSCHT** — GameManager.cs war toter Code (`SetState()` nie aufgerufen). `GameStateManager` übernimmt alle Aufgaben.
- [ ] **EventSystem**: Vorhanden mit InputSystemUIInputModule?
- [ ] **BuildSystem GameObject**: BuildGrid + PlacementController vorhanden?
- [ ] **UIManager**: MachineCatalogueController + HotbarController + HotbarDataSource referenziert?
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

- [ ] `MachineCatalogueController`
- [ ] `HotbarController`
- [ ] `DialogueUI`
- [ ] `MachineInspectUI`
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

- [x] ~~`MachineUIController.cs` vs `MachineInspectUI.cs`~~ — **GELÖST**: `MachineUIController.cs` gelöscht (Duplikat). Transfer-Feature nach `MachineInspectUI.cs` migriert ("Zuführen" Button + `RemoveItem` in `PlayerInventory` ergänzt).
- [x] ~~`GameManager.cs` vs `GameStateManager.cs`~~ — **GELÖST**: `GameManager.cs` gelöscht (toter Code, `SetState()` nie aufgerufen). `GameStateManager` bleibt als einziger State-Manager.
- [x] ~~`UIManager.cs` vs `GameStateManager.cs`~~ — **Kein Duplikat**, verschiedene Aufgaben: `UIManager` = Init (`BroadcastMessage`), `GameStateManager` = Runtime Panel-State.
- [x] ~~`HUDController.cs` + `HUDDataSource.cs`~~ — **Saubere MVC-Architektur**, kein Handlungsbedarf.

### 4.2 Assembly-Konfiguration

- [ ] `Game.Systems.asmdef` — wurde von einem Copilot-Agent modifiziert (Commit 345dfef). Prüfen ob die Referenzen noch stimmen
- [ ] Prüfen ob alle neuen Scripts (SoundManager, BreakthroughSystem, etc.) im richtigen Assembly liegen

---

## 7. Factory Game Loop Audit (2025-07-21)

### Complete Loop Verified

| Step | Mechanism | Status |
|------|-----------|--------|
| Player opens Build Menu | Shift key → `GameEvents.OnBuildModeToggled` → `MachineCatalogueController` shows panel | ✅ |
| Player selects machine | Clicks slot → `placementController.StartPlacement(machine)` | ✅ |
| Ghost preview follows cursor | `PlacementController.UpdateGhostPosition` snaps to BuildGrid | ✅ |
| Cost check | `HasBuildResources()` checks `PlayerInventory.HasItem()` | ✅ |
| Place machine (left-click) | `DeductBuildResources()` → Instantiate prefab → `SetMachineData()` → OccupyCells | ✅ |
| Inspect any machine (E key) | `PlayerInteractor` → `IInteractable.Interact()` → `GameDataEvents.OnMachineInteracted` → `MachineInspectUI` | ✅ (fixed) |
| Connect pipe | Interact with SpiritPipe → `PipeConnectionUI` opens → click source → click destination → `pipe.Connect()` | ✅ |
| Resource extraction | `ResourceExtractor` finds nearby `OreVein` → `TryExtract()` → fills `OutputInventory` | ✅ |
| Item transport | `SpiritPipe.TransferItems()` ticks every `transferInterval` | ✅ |
| Processing | `BaseMachine.TryStartProcessing()` checks recipe + input → progress bar → `CompleteProcessing()` produces output | ✅ |
| Qi power | `QiNetwork` BFS from `QiConduit` → sets `BaseMachine.IsPowered` | ✅ |
| Dashboard | `FactoryDashboardUI` polls `QiNetwork.GetAllMachines()` each `updateInterval` | ✅ |

### Bugs Fixed in This Pass

| Severity | File | Fix |
|----------|------|-----|
| 🔴 Critical | `MachineInspectUI` | `OnMachineInteracted` now uses `IMachineConnectable` — all 5 machine types open the inspect panel. Non-BaseMachine types hide progress/recipe/time/dropdown. |
| 🔴 Critical | `PlacementController` | `DeductBuildResources` now uses `playerInventory.RemoveItem()` instead of mutating the backing dictionary directly. `InventoryChanged` event now fires via `RemoveItem`. |
| 🔴 Critical | `SpiritPipe` | `TransferItems()` now checks `StorageContainer.AcceptsItem()` before transferring. Storage item filters now work. |
| 🔴 Critical | `PlayerInteractor` | Added `if (interactAction != null)` guard in `OnEnable`/`OnDisable`. No longer crashes if field is not wired in Inspector. |
| 🟡 Medium | `ResourceExtractor` | `SetMachineData()` now guards `data.processingSpeed > 0f` before dividing. No longer produces infinite extraction interval. |
| 🟡 Medium | `MachineCatalogueController` | `InitializeUI` now unsubscribes before re-subscribing. No double-registration on enable/disable cycles. |
| 🟡 Medium | `MachineInspectUI` | Same double-subscription fix applied. |
| 🟡 Medium | `PipeConnectionUI` | Same double-subscription fix applied. |
| 🟡 Medium | `MachineInspectUI` | `SubscribeToMachine` / `UnsubscribeFromMachine` now guard against double-subscribing when `InputInventory == OutputInventory` (StorageContainer). |

### Missing Features (require new implementation)

- [ ] **Splitter destination setup UI** — There is no in-game way to call `Splitter.SetDestinations(destA, destB)`. Splitter works for pipe-IN (via its InputInventory) but its two output destinations must be wired manually or via a new "Click to set destination" mode similar to PipeConnectionUI. **Blocker for Splitter use in-game.**
- [ ] **Merger source setup UI** — Same issue: `Merger.SetSources(sourceA, sourceB)` has no UI. Merger's OutputInventory can be pipe-connected normally, but its two inputs cannot be configured in-game. **Blocker for Merger use in-game.**
- [ ] **QiConduit placement auto-registration** — `QiConduit.Start()` registers with `QiNetwork`. But the Qi network connectivity (BFS) only considers conduit-to-conduit adjacency. The player has no visual feedback for which machines are in range of a conduit. Consider adding a range circle visualizer (like `PipeConnectionVisualizer`).

### Inspector Wiring Required (before play-testing)

| Component | Required Fields |
|-----------|----------------|
| `PlacementController` | buildGrid, playerInventory, terrainLayer, buildCamera, ghostValidMaterial, ghostInvalidMaterial, placeAction, cancelAction, rotateAction, removeAction, machineLayer |
| `MachineCatalogueController` | HotbarDataSource asset, placementController |
| `HotbarController` | HotbarDataSource asset |
| `BuildMenuDataSource` | availableMachines[] — all 11 MachineData assets |
| `MachineInspectUI` | recipeDatabase, playerInventory |
| `PlayerInteractor` | interactAction (Player/Interact), interactableLayer, interactionRadius |
| `BaseMachine` prefabs | machineData (each prefab's own MachineData asset), inputCapacity, outputCapacity |
| `OreVein` GameObjects | veinData (OreVeinData asset) |
| `QiNetwork` | qiSourcePoint (Transform of Qi source object) |



### Gelöschte Scripts
- **GameManager.cs** — Toter Code. `SetState()` wurde von keinem Script aufgerufen. `GameStateManager` übernimmt alle Pause/Panel-Funktionen.
- **MachineUIController.cs** — Duplikat von `MachineInspectUI.cs`. Beide subscribten `GameDataEvents.OnMachineInteracted`. Transfer-Feature wurde nach `MachineInspectUI` migriert.

### Behobene Bugs
- **PlacementController.cs** — `OnPlace()` fehlte `Splitter` und `Merger` Typ-Checks. `SetMachineData()` wurde für diese Maschinentypen nie aufgerufen → Platzierte Splitter/Merger waren funktionslos.
- **PlayerStats.cs** — `AddQi()` clampte `currentQi` nicht auf `MaxQi`. Qi konnte unbegrenzt ansteigen. Jetzt: `Math.Min(currentQi + amount, qiCapacity)`. Redundantes `[SerializeField]` auf public Field entfernt.
- **PlayerInteractor.cs** — `Physics.OverlapSphere()` allokierte jeden Frame ein neues Array (GC-Druck). Ersetzt durch `Physics.OverlapSphereNonAlloc()` mit vorab-allokiertem `Collider[10]` Buffer.

### Neue UXML/USS Panels
Vier UI-Controller referenzierten UXML-Elemente die nicht existierten → Panels waren zur Runtime unsichtbar/kaputt:
- **MachineInspect.uxml + .uss** — Inspect-Panel mit Header, Status, Progress, Recipe-Dropdown, Input/Output Slots, Transfer-Buttons
- **Dialogue.uxml + .uss** — Dialogue-Overlay mit Portrait, Speaker, Typewriter-Text, Choice-Buttons
- **FactoryDashboard.uxml + .uss** — Dashboard mit Qi-Netzwerk-Status, Maschinenlist, Bottleneck-Warnung
- **PipeConnection.uxml + .uss** — Pipe-Connection HUD mit Source/Dest Labels, Cancel-Button

### MainScreen.uxml aktualisiert
4 neue `<ui:Template>` + `<ui:Instance>` Einträge: MachineInspect, Dialogue, FactoryDashboard, PipeConnection

### Neue Features
- **PlayerInventory.RemoveItem()** — Generische Item-Entfernung (vorher nur via `UsePill`/`UseEssence` möglich). Benötigt für Machine-Transfer.
- **MachineInspectUI „Zuführen" Button** — Transferiert Rezept-Inputs vom Spieler-Inventar in die Maschine. Wired auf `InspectTransferIn` UXML-Button.

---

## 5. Implementierte Features (Referenz)
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
- `MachineInspectUI` (Echtzeit-Status, Recipe-Wechsel, Leeren + Zuführen)
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
- `MachineCatalogueController` + `HotbarController` + `HotbarDataSource`
- `InventoryController` + `InventoryDataSource`
- `PauseMenuController`
- `GameStateManager` (Panel-Management, Cursor-Locking)
- `UIManager` (BroadcastMessage zu UI-Components)
- ~~`MachineUIController`~~ → `MachineInspectUI` (Duplikat gelöscht, Features migriert)
- `HealthBarUI` + `EnemyHealthBarUI`
- `BreakthroughController`

### Editor Tools (vollständig ✅)
- `RecipeDatabaseWindow` (Validierung + Balance-Analyse)
- `EssencePainterWindow` (Biome-Painted Essence Spawning)
- `CsvImportWindow` (Bulk-Import von Items via CSV)
- `MinorRealmConfigEditor` (Custom Inspector)
- `InspectorAutoSetup` (Auto-Wiring Inspector Utility)
- `RealmProgressionLinker` (Realm-Progressions-Ketten-Linker)

### Unit Tests (teilweise ✅)
- `MachineInventoryTests` (MachineInventory Coverage)
- `RecipeDataValidationTests` (RecipeData-Felder + Constraints)
- `RecipeDatabaseLookupTests` (GetRecipesForMachine, GetRecipesForItem)
- `LootSystemTests` (Drop-Table Logik)
- `RecipeTestHelper` (Test-Hilfsfunktionen)
- **Fehlen**: QiNetworkTests, BaseMachinePipelineTests, SaveSystemTests

---

## 8. Code-Qualität Zusammenfassung (2025-07-22)

### Geprüft und in Ordnung ✅
- **Namespaces**: Alle 97 .cs-Dateien nutzen korrekte Namespaces gemäß Assembly
- **Assembly-Abhängigkeiten**: Korrekte Kette: Core → Data → Player → Systems → UI → Editor
- **Interface-Implementierungen**: Alle Maschinen implementieren IInteractable + IMachineConnectable
- **Event-Management**: Alle Subscriptions werden korrekt in OnDisable/OnDestroy aufgeräumt
- **Null-Safety**: Durchgehend gute Null-Checks (PlayerInteractor, BaseMachine, OreVein, etc.)
- **Kein toter Code**: Alle Methoden werden referenziert oder sind Unity-Lifecycle-Methoden
- **Keine fehlenden Typen**: Alle referenzierten Typen existieren

### Gelöschte Dateien (nicht mehr im Projekt)
- ~~`GameManager.cs`~~ — Ersetzt durch `GameStateManager`
- ~~`MachineUIController.cs`~~ — Ersetzt durch `MachineInspectUI`
- ~~`BuildMenuController.cs`~~ — Ersetzt durch `MachineCatalogueController` + `HotbarController`
- ~~`PlayerStatsUI.cs`~~ — Ersetzt durch `HUDController` + `HUDDataSource`
- ~~`StaminaUI.cs`~~ — Integriert in `HUDController`
- ~~`InventoryManager.cs`~~ — Ersetzt durch `InventoryController`
- ~~`InventoryDisplay.cs`~~ — Integriert in `InventoryController`
- ~~`InventorySlotDisplay.cs`~~ — Integriert in `InventoryController`
