# Prison Realm: Alchemy Factory — Vollständige Aufgabenliste

Letzte Aktualisierung: 2026-03-26
Komplett-Übersicht aller offenen Aufgaben als ob das Projekt frisch aufgesetzt wird.

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

### 1.6 OreVeins in Overworld platzieren

- [ ] OreVein GameObjects mit MeshRenderer + Collider + OreVein-Component erstellen
- [ ] OreVeinData-Assets zuweisen (Spirit Stone Ore, Iron Ore, Crystal, Jade)
- [ ] Veins testen: Extraction, Depletion, Respawn

---

## 2. GitHub Issues (Copilot-Agent Tasks)

### Phase 7: NPC & Quest System

| Issue | Titel | Hängt von | Parallel-sicher? |
|-------|-------|-----------|-----------------|
| #102 | QuestData ScriptableObject & QuestSystem | — | ✅ Ja |
| #103 | NPCController MonoBehaviour | NPCData (existiert) | ✅ Ja |
| #104 | Quest-Journal UI | #102 | ❌ Warten |
| #105 | Händler-System: ShopData & UI | NPCData | ✅ Ja |

### Phase 9: Story & Endgame

| Issue | Titel | Hängt von | Parallel-sicher? |
|-------|-------|-----------|-----------------|
| #106 | StoryData, StoryProgressSystem & Win-Condition | #102 | ❌ Warten |
| #112 | Realm-Breaking Pill Produktionskette | — | ✅ Ja |

### Systems & Scenes

| Issue | Titel | Hängt von | Parallel-sicher? |
|-------|-------|-----------|-----------------|
| #107 | SceneTransitionManager | — | ✅ Ja |
| #108 | Tutorial-System | — | ✅ Ja |
| #109 | Hauptmenü-Scene | — | ✅ Ja |
| #110 | Kompass-HUD | — | ✅ Ja |
| #111 | Inventar-Tooltips & Rechtsklick-Kontextmenü | — | ✅ Ja |

### Tests

| Issue | Titel | Hängt von | Parallel-sicher? |
|-------|-------|-----------|-----------------|
| #114 | Unit Tests: QiNetwork BFS | — | ✅ Ja |
| #115 | Unit Tests: BaseMachine Pipeline | — | ✅ Ja |
| #116 | Unit Tests: SaveSystem | — | ✅ Ja |

### Qualität

| Issue | Titel | Hängt von | Parallel-sicher? |
|-------|-------|-----------|-----------------|
| #113 | Game Balance Pass | alle Content-Assets | ❌ Zuletzt |
| #117 | Code-Qualität Review | — | ✅ Ja |

### Empfohlene Agent-Reihenfolge

**Welle 1** (alle parallel): #102, #103, #105, #107, #108, #109, #110, #111, #112, #114, #115, #116, #117
**Welle 2** (nach #102 fertig): #104, #106
**Welle 3** (zum Schluss): #113

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
- [ ] `UIManager.cs` vs `GameStateManager.cs` — beide managen UI-State. Klären ob beide nötig sind oder konsolidiert werden können
- [ ] `HUDController.cs` + `HUDDataSource.cs` — HUD hat zwei Files, funktioniert aber. Architektur-Review ob das sauber ist

### 4.2 Assembly-Konfiguration

- [ ] `Game.Systems.asmdef` — wurde von einem Copilot-Agent modifiziert (Commit 345dfef). Prüfen ob die Referenzen noch stimmen
- [ ] Prüfen ob alle neuen Scripts (SoundManager, BreakthroughSystem, etc.) im richtigen Assembly liegen

---

## 5. Implementierte Features (Referenz)

### Core Factory (vollständig ✅)
- ItemData, PillData, EssenceData, RawMaterialData
- RecipeData + RecipeDatabase (mit Lookup-Methoden)
- MachineData Assets (alle 11 Typen: Furnace, Crusher, Mixer, Distiller, Condenser, PillPress, Storage, ResourceExtractor, SpiritPipe, Splitter, Merger)
- BaseMachine (Input/Output Inventories, Recipe Processing, Power Check)
- MachineInventory (Dictionary-basiert, Stack-Limits)
- SpiritPipe, Splitter, Merger (Logistics-Routing)
- QiNetwork + QiConduit (BFS-Konnektivität, Dirty-Flag Optimierung)
- OreVein + ResourceExtractor (Auto-Mining, Depletion, Respawn)
- StorageContainer
- BuildGrid + PlacementController (Ghost Preview, Rotation, Inventory-Kosten)
- PipeConnectionUI + PipeConnectionVisualizer (Click-to-Connect)
- MachineInspectUI (Echtzeit-Status, Recipe-Wechsel)
- FactoryDashboardUI (Produktion/Minute, Bottleneck-Detection)
- RecipeDatabase Editor-Tool (Validierung + Balance-Analyse)

### Player & Combat (vollständig ✅)
- PlayerMovement, PlayerStats, PlayerInventory, PlayerInteractor
- CameraSystem (3rd Person ↔ Spirit Sense, Build-Mode entkoppelt)
- PlayerCombatController (Attack, Dodge, Abilities)
- EnemyAI (NavMeshAgent: Patrol → Detect → Chase → Attack)
- HealthSystem, EnemyLoot, LootSystem
- 5 EnemyData Assets mit Drop Tables

### Cultivation (vollständig ✅)
- PillBuffSystem (Konsum, temporäre Buffs, Buff-Events)
- BreakthroughSystem + BreakthroughController (Chance-Mechanik, UI)
- RealmDefinition Assets (5 Stufen, spiritSenseRange)

### UI (teilweise ✅)
- HUDController + HUDDataSource (Qi/HP/Stamina Bars, Buff Icons, Breakthrough-Indikator)
- BuildMenuController + BuildMenuDataSource
- InventoryController + InventoryDisplay
- DialogueUI (Typewriter-Effekt, Dynamic Choices)
- PauseMenuController (Volume-Slider)
- GameStateManager (Panel-Management, Cursor-Locking)
- BreakthroughController (Confirmation + Result Panel)
- CraftingController

### Data & Audio (vollständig ✅)
- NPCData, DialogueNode (Dialogue-Baum)
- OreVeinData (4 Typen konfiguriert)
- MinorRealmConfig + RealmResourceEntry (Ressourcen-Spawning in Minor Realms)
- SoundManager + SoundEventTrigger + SoundClip
- SaveSystem (atomares JSON: write-to-temp, rename)
- CSV Import Editor-Tool

### Tests (teilweise ✅)
- MachineInventoryTests (12 Tests)
- RecipeDataValidationTests + RecipeDatabaseLookupTests (22 Tests)
- LootSystemTests
- **Fehlen**: QiNetwork, BaseMachine, SaveSystem Tests

### Sonstige Fixes ✅
- Doppeltes Game.Tests.EditMode.asmdef entfernt
- 36 fehlende .meta-Dateien generiert
- Camera-Script GUIDs wiederhergestellt (CameraSystem, SpiritSenseCamera, BuildElevation)
