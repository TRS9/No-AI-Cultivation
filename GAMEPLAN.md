# Prison Realm: Alchemy Factory — Entwicklungsplan

## Konzept-Zusammenfassung
Moderner Chemiker wird in eine Kultivierungswelt reinkarniert, in ein Gefängnis-Realm verbannt.
Zusammen mit anderen Gefangenen revolutioniert er die Alchemie, um mit Realm-Breaking Pills auszubrechen.
**60% Factory / 40% Cultivation-Combat-Exploration.**
Inspiriert von: Satisfactory, Arknights Endfield, Xianxia-Novels (Martial Peak etc.)

---

## Bestandsaufnahme: Was bereits existiert

| System | Status | Anpassung nötig? |
|--------|--------|-------------------|
| Player Movement + Stamina | Fertig | Fliegen später hinzufügen |
| Minor Realm Generierung | Fertig | Biome-Configs für Materialien anpassen |
| Essence Collection | Fertig | ~~Wird zu allgemeinem Item-Pickup erweitert~~ ✅ Erledigt |
| Inventar (EssenceData-basiert) | ✅ Umgebaut | ~~Muss auf generisches Item-System umgebaut werden~~ ✅ Nutzt jetzt ItemData |
| Cultivation / Breakthrough | Fertig | ~~Pillen-Integration hinzufügen~~ ✅ PillBuffSystem implementiert |
| Save/Load | ✅ Erweitert | ~~Muss um Factory-Daten erweitert werden~~ ✅ Building, Pipe, Machine, OreVein SaveEntries |
| Scene Management / Portale | Fertig | Prison Realm = Universe, Grotto = Workshop |
| UI (Qi, Stamina, Inventory) | Fertig | Factory-UI hinzufügen (Build Menu fertig) |
| GameEvents System | ✅ Erweitert | ~~Neue Events für Factory hinzufügen~~ ✅ GameDataEvents.cs mit allen Factory-Events |
| Terrain Generation | Fertig | Bleibt für Minor Realms |
| **Generisches Item-System** | ✅ **Fertig** | ItemData, RawMaterialData, PillData, EssenceData |
| **Rezept/Crafting-System** | ✅ **Fertig** | RecipeData, RecipeDatabase, CraftingSystem |
| **Pill-System** | ✅ **Fertig** | PillData, PillBuffSystem, CultivationBuffs |
| **Building/Placement** | ✅ **Fertig** | BuildGrid, PlacementController, BuildMenu |
| **Maschinen-Logik** | ✅ **Fertig** | BaseMachine, MachineInventory, IMachineConnectable |
| **Transport (Spirit Pipes)** | ✅ **Fertig** | SpiritPipe (IMachineConnectable), StorageContainer |
| **Ore Veins** | ✅ **Fertig** | OreVein, OreVeinData, ResourceExtractor |
| **Maschinen-UI** | ✅ **Fertig** | MachineUIController (Rezept, Slots, Progress) |
| **Machine-Removal** | ✅ **Fertig** | PlacementController.OnRemove |
| **Qi-Netzwerk** | ✅ **Fertig** | QiNetwork, QiConduit (Satisfactory-Strom) |

---

## DESIGN-ENTSCHEIDUNGEN (vor Implementierung zu treffen)

### DE-1: Factory-Platzierung — Grid oder Freeform?
- **Grid (empfohlen):** Einfacher zu implementieren, sauberer Look, Satisfactory-Feeling.
  Snap-to-Grid mit fester Zellgröße (z.B. 2x2m). Rotation in 90°-Schritten.
- **Freeform:** Mehr Freiheit, aber Collision-Management wird komplex.
- **Empfehlung:** Grid. Passt besser zum "Ingenieur optimiert Produktion"-Fantasy.

### DE-2: Transport-System — Conveyor Belts oder Spirit Pipes?
- **Conveyor Belts (Satisfactory-Style):** Visuell klar, Spieler sieht Items fließen.
- **Spirit Pipes (Xianxia-Flavor):** Gleiche Mechanik, anderer Look. Qi fließt sichtbar.
- **Manual Only (Frühes Spiel):** Spieler trägt Items selbst zwischen Maschinen.
- **Empfehlung:** Start mit Manual → Spirit Pipes als Upgrade freischalten. Gibt Progression.

### DE-3: Rezept-Komplexität
- **Flach (3 Stufen):** Rohmaterial → Zwischenprodukt → Pille. Einfach, schnell spielbar.
- **Tief (5+ Stufen):** Mehrere Zwischenprodukte, Nebenprodukte, Abfall. Satisfactory-Level.
- **Empfehlung:** Start mit 3 Stufen, auf 5 erweitern wenn das Grundsystem steht.

### DE-4: Qi-Maschinen-Interaktion ✅ ENTSCHIEDEN
- **Qi-Netzwerk (wie Satisfactory-Strom):** Alles läuft über Qi. Spieler meditiert → füllt Qi-Pool → QiConduits (Leitungen) verteilen Qi → Maschinen in Reichweite sind "powered".
- Kein `FuelType`, keine Spirit Stones als Treibstoff, keine Multiple-Fuel-Logik.
- `MachineData.qiConsumptionRate` = Qi/Sekunde während Produktion.
- Natürliche Progression: höherer Realm = mehr Qi-Kapazität = mehr Maschinen betreibbar.

### DE-5: Combat-Tiefe
- **Minimal (empfohlen für V1):** Einfaches Action-Combat. Angriff, Ausweichen, 2-3 Techniken.
  Genug um Minor Realms gefährlich zu machen, ohne ein ganzes Combat-System zu bauen.
- **Mittel:** Combo-System, Elementar-Reaktionen, Waffen-Typen.
- **Empfehlung:** Minimal starten. Combat ist nicht der USP — die Factory ist es.

### DE-6: NPC-Anzahl
- **Kern-NPCs (empfohlen):** 5-8 Charaktere mit eigener Persönlichkeit und Funktion.
  - Der Alchemist (Rezept-Lehrer)
  - Der Schmied (Maschinen-Bauer/Upgrader)
  - Der Anführer (Quest-Geber, Story)
  - 2-3 Cultivatoren (Ressourcen-Helfer, Kampf-Begleitung)
  - 1-2 Mysterien-Charaktere (Story-Twists)
- **Empfehlung:** Qualität über Quantität. 6 gut geschriebene NPCs > 20 generische.

---

## PHASEN-PLAN

---

### PHASE 0: Refactoring — Generisches Item-System ✅ IMPLEMENTIERT
**Priorität: HÖCHSTE — ohne das geht nichts weiter**
**Geschätzter Umfang: Das erste was gebaut werden muss**

Das aktuelle System kennt nur `EssenceData`. Die Factory braucht ein allgemeines Item-System.

#### Schritt 0.1: ItemData ScriptableObject erstellen
```
ItemData (ScriptableObject) — Ersetzt/erweitert EssenceData
├── itemName (string)
├── description (string)
├── icon (Sprite)
├── itemType (enum: RawMaterial, Essence, Intermediate, Pill, Tool, Fuel)
├── stackSize (int, default 99)
├── qiValue (double) — nur für Essenzen/Pillen relevant
├── rarity (enum: Common, Uncommon, Rare, Epic, Legendary)
└── collectionEffect (GameObject)
```

#### Schritt 0.2: EssenceData von ItemData erben lassen
- `EssenceData : ItemData` mit zusätzlichem `essenceColor`-Feld
- Alle bestehenden EssenceData-Assets bleiben kompatibel
- Bestehender Code (SpiritEssence, EssenceSpawner) funktioniert weiterhin

#### Schritt 0.3: PlayerInventory auf ItemData umstellen
- `Dictionary<EssenceData, int>` → `Dictionary<ItemData, int>`
- `AddItem(ScriptableObject)` → `AddItem(ItemData, int amount = 1)`
- IInventory-Interface anpassen

#### Schritt 0.4: SaveData erweitern
- `inventoryEntries` muss Item-Typ-ID speichern, nicht nur Essence-ID
- Neues Feld: `itemId` statt `essenceId` (backwards-compatible mit Migration)

#### Schritt 0.5: InventoryDisplay anpassen
- Muss verschiedene Item-Typen anzeigen können
- Icon-basiert (funktioniert bereits, nur Dictionary-Key ändert sich)

**Dateien die geändert werden:**
- `Core/IInventory.cs`
- `Data/DataTemplates/EssenceData.cs` (wird zu Subclass)
- Neues File: `Data/DataTemplates/ItemData.cs`
- `Player/Inventory/PlayerInventory.cs`
- `Data/SaveData.cs`
- `Ui/Save/SaveManager.cs`
- `Ui/Inventory/InventoryDisplay.cs`
- `Ui/Inventory/InventorySlotDisplay.cs`

---

### PHASE 1: Rezept- und Crafting-System ✅ IMPLEMENTIERT
**Priorität: HOCH — Kern der Factory-Mechanik**

#### Schritt 1.1: RecipeData ScriptableObject
```
RecipeData (ScriptableObject)
├── recipeName (string)
├── description (string)
├── inputs (RecipeSlot[])  — { ItemData item, int amount }
├── outputs (RecipeSlot[])  — { ItemData item, int amount }
├── processingTime (float, Sekunden)
├── requiredMachine (MachineType enum)
├── requiredCultivationRealm (RealmDefinition) — Mindest-Realm zum Freischalten
├── successRate (float, 0-1) — kann durch Cultivation verbessert werden
└── qiCost (double) — Qi-Verbrauch pro Craft
```

#### Schritt 1.2: RecipeDatabase
- ScriptableObject das alle Rezepte hält
- Lookup-Methoden: `GetRecipesForMachine(MachineType)`, `GetRecipesForItem(ItemData)`
- Unlocking-Logik: welche Rezepte sind freigeschaltet basierend auf Realm + Story

#### Schritt 1.3: Manuelles Crafting (ohne Maschinen)
- Einfaches Crafting-UI: Spieler wählt Rezept, Items werden verbraucht, Output erscheint
- Das ist der "Early Game"-Crafting-Loop bevor Maschinen existieren
- Funktioniert am Crafting Table in der Settlement-Area

**Neue Dateien:**
- `Data/DataTemplates/RecipeData.cs`
- `Data/RecipeDatabase.cs`
- `Systems/Crafting/CraftingSystem.cs`
- `Ui/Crafting/CraftingUI.cs`
- `Ui/Crafting/RecipeSlotUI.cs`

---

### PHASE 2: Pill-System und Cultivation-Integration ✅ IMPLEMENTIERT
**Priorität: HOCH — verbindet Factory mit bestehendem Cultivation**

#### Schritt 2.1: PillData (erbt von ItemData)
```
PillData : ItemData
├── pillTier (int, 1-5)
├── qiBoost (double) — sofortiger Qi-Gewinn bei Einnahme
├── cultivationSpeedMultiplier (float) — temporärer Meditation-Bonus
├── breakthroughBonus (float) — erhöht Breakthrough-Erfolgsrate
├── buffDuration (float, Sekunden)
├── sideEffects (string) — flavor text
└── maxDailyUses (int) — Toleranz-Mechanik (Xianxia-typisch)
```

#### Schritt 2.2: Pill-Einnahme in PlayerStats integrieren
- Neue Methode: `ConsumePill(PillData pill)`
- Temporäre Buffs: Speed, Qi-Rate, Breakthrough-Chance
- Toleranz-System: Wiederholte Pillen-Einnahme hat abnehmenden Effekt
- Event: `GameEvents.OnPillConsumed`

#### Schritt 2.3: Pill-Hierarchie definieren (Content)
```
Tier 1: Qi Gathering Pill        — Basis-Qi-Boost, billig herzustellen
Tier 2: Meridian Cleansing Pill  — Erhöht Meditation-Rate temporär
Tier 3: Foundation Pill           — Erhöht Breakthrough-Chance
Tier 4: Spirit Condensation Pill  — Großer Qi-Boost + Cultivation Speed
Tier 5: Realm-Breaking Pill       — Das Endziel. Erfordert massive Ressourcen.
```

**Dateien:**
- Neues File: `Data/DataTemplates/PillData.cs`
- `Player/PlayerStats.cs` (erweitern)
- `Core/GameEvents.cs` (neue Events)
- Neues File: `Systems/Pill/PillBuffSystem.cs`

---

### PHASE 3: Building & Placement System ✅ IMPLEMENTIERT
**Priorität: HOCH — ohne Maschinen keine Factory**

#### Schritt 3.1: MachineData ScriptableObject
```
MachineData (ScriptableObject)
├── machineName (string)
├── machineType (MachineType enum: Furnace, Crusher, Mixer, Distiller, Condenser, Conveyor, Storage)
├── description (string)
├── prefab (GameObject)
├── ghostPrefab (GameObject) — transparente Vorschau beim Platzieren
├── gridSize (Vector2Int) — z.B. 2x2, 1x1, 3x2
├── buildCost (RecipeSlot[]) — Items zum Bauen
├── requiredCultivationRealm (RealmDefinition)
├── processingSpeed (float) — Multiplikator
├── inputSlots (int)
├── outputSlots (int)
└── qiConsumptionRate (float) — Qi/s während Produktion (gespeist über QiConduits)
```

#### Schritt 3.2: Grid-System
- `BuildGrid` Komponente auf dem Terrain
- Zellen-basiert (z.B. 2m x 2m)
- Occupation-Tracking: welche Zellen sind belegt
- Terrain-Höhen-Snapping: Maschinen passen sich dem Boden an
- Keine Platzierung auf Wasser oder zu steilem Gelände

#### Schritt 3.3: Placement-Controller
- Build-Modus Toggle (z.B. B-Taste)
- Ghost-Preview folgt dem Cursor (Raycast auf Grid)
- Grün = platzierbar, Rot = blockiert
- Rotation mit R-Taste (90°-Schritte)
- Klick = Platzieren (Items aus Inventar abziehen)
- Rechtsklick = Abbrechen / Maschine abbauen

#### Schritt 3.4: Save/Load für Gebäude
- `BuildingsSaveData`: Liste aller platzierten Maschinen mit Position, Rotation, Typ, Inventar-Inhalt
- In SaveData integrieren

**Dateien:**
- `Data/DataTemplates/MachineData.cs`
- `Systems/Building/BuildGrid.cs`
- `Systems/Building/PlacementController.cs`
- `Systems/Building/BuildingSaveData.cs`
- `Ui/Building/BuildMenuUI.cs`

---

### PHASE 4: Maschinen-Logik und Produktion ✅ IMPLEMENTIERT
**Priorität: HOCH — das Herz der Factory**

#### Schritt 4.1: BaseMachine Komponente
```
BaseMachine : MonoBehaviour
├── machineData (MachineData)
├── currentRecipe (RecipeData)
├── inputInventory (Dictionary<ItemData, int>)
├── outputInventory (Dictionary<ItemData, int>)
├── processingTimer (float)
├── isProcessing (bool)
├── fuelLevel (float)
│
├── TryStartProcessing() — prüft Inputs, startet Timer
├── Update() — Timer runterzählen, Output erzeugen wenn fertig
├── AddInput(ItemData, int) — Item in Maschine legen
├── RemoveOutput(ItemData, int) — Item aus Maschine nehmen
└── SetRecipe(RecipeData) — Rezept auswählen
```

#### Schritt 4.2: Machine-Interaktion
- Spieler interagiert mit Maschine (IInteractable)
- Öffnet Maschinen-UI: Input-Slots, Output-Slots, Rezept-Auswahl, Fortschrittsbalken
- Drag & Drop oder Click-to-Transfer zwischen Spieler-Inventar und Maschine

#### Schritt 4.3: Spezifische Maschinen
```
Furnace     — Erhitzt Rohmaterialien (Erze schmelzen, Kräuter trocknen)
Crusher     — Zerkleinert Materialien (Pulver herstellen)
Mixer       — Kombiniert Flüssigkeiten/Pulver
Distiller   — Extrahiert Essenzen aus Pflanzen
Condenser   — Kondensiert Qi in physische Form (Spirit Stones)
PillPress   — Formt finale Pillen aus Zwischenprodukten
```

#### Schritt 4.4: Maschinen-UI
- Einheitliches MachineUI-Prefab das sich an MachineData anpasst
- Input-Slots links, Output-Slots rechts, Fortschritt in der Mitte
- Rezept-Dropdown oder Rezept-Browser

**Dateien:**
- `Systems/Factory/BaseMachine.cs` ✅
- `Systems/Factory/MachineInventory.cs` ✅
- `Systems/Factory/Splitter.cs` ✅ (neu — verteilt Items 1→2, Round-Robin)
- `Systems/Factory/Merger.cs` ✅ (neu — kombiniert Streams 2→1)
- `Ui/Factory/MachineUIController.cs` ✅

---

### PHASE 5: Transport und Logistik ✅ IMPLEMENTIERT
**Priorität: MITTEL — kommt nachdem einzelne Maschinen funktionieren**

#### Schritt 5.1: Manueller Transport (schon implizit da)
- Spieler nimmt Items aus Maschine A, legt sie in Maschine B
- Das ist der Early-Game-Loop

#### Schritt 5.2: Spirit Pipes (Automatisierung)
- Platzierbar wie Maschinen auf dem Grid
- Verbinden Output einer Maschine mit Input einer anderen
- Visuelle Darstellung: leuchtende Rohre mit fließenden Partikeln
- Durchsatz-Limit pro Pipe (upgradebar)

#### Schritt 5.3: Storage Container
- Puffer-Lager zwischen Produktionsschritten
- Verschiedene Größen (Small: 100 Items, Medium: 500, Large: 2000)
- Filter: nur bestimmte Items akzeptieren

#### Schritt 5.4: Splitter / Merger
- Splitter: 1 Input → 2 Outputs (verteilt Items)
- Merger: 2 Inputs → 1 Output (kombiniert Ströme)
- Nötig für komplexe Produktionsketten

#### Schritt 5.5: Qi-Netzwerk (Satisfactory-Strom) ✅ IMPLEMENTIERT
- **QiConduit**: Platzierbare Qi-Leitung mit connectionRadius (Conduit↔Conduit) und machineRadius (Conduit→Maschine)
- **QiNetwork**: Singleton, BFS-Connectivity vom Qi-Source-Punkt, aggregierter Qi-Verbrauch pro Frame
- Maschinen brauchen Qi-Verbindung → `IsPowered` muss true sein
- `MachineData.qiConsumptionRate` = Qi/s während Produktion
- Kein `successRate` mehr — Produktion gelingt immer (Factory = Präzision)

**Dateien:**
- `Systems/Factory/SpiritPipe.cs`
- `Systems/Factory/StorageContainer.cs`
- `Systems/Factory/Splitter.cs`
- `Systems/Factory/Merger.cs`

---

### PHASE 6: Ressourcen und Minor Realm Anpassung ⚠️ TEILWEISE IMPLEMENTIERT
**Priorität: MITTEL — Content für die Factory**

#### Schritt 6.1: Neue Ressourcen-Typen definieren
```
Kräuter:     Spirit Grass, Flame Lotus, Frost Root, Thunder Vine
Mineralien:  Spirit Stone Ore, Iron Essence, Crystal Shard
Flüssig:     Spirit Water, Magma Dew, Void Essence
Spezial:     Beast Core, Ancient Fragment (für späte Rezepte)
```

#### Schritt 6.2: Biome-spezifische Ressourcen
- SpiritForest → Kräuter (Spirit Grass, Frost Root)
- StoneMountain → Mineralien (Spirit Stone Ore, Iron Essence)
- CelestialPeak → Seltene Materialien (Crystal Shard)
- NebulaBeach → Flüssigkeiten (Spirit Water)
- BarrenWastes → Beast Cores (gefährlich, aber wertvoll)

#### Schritt 6.3: Resource Nodes im Prison Realm (Overworld)
- Feste Ressourcen-Vorkommen in der Hauptwelt (nicht prozedural)
- Regenerieren über Zeit (wie EssenceSpawner, bereits implementiert)
- Manche nur mit höherem Cultivation-Level erreichbar

#### Schritt 6.4: Minor Realm Anpassung
- MinorRealmConfig bekommt `availableResources (ItemData[])` Feld
- Ressourcen-Spawning statt nur Essence-Spawning
- Höherer Seed = seltenere Ressourcen = gefährlicher

**Dateien die geändert werden:**
- `Data/MinorRealmConfig.cs` (erweitern)
- `Systems/Realm/MinorRealmGenerator.cs` (Ressourcen-Spawning)
- Neue ItemData-Assets für jede Ressource

---

### PHASE 7: NPC-System und Community
**Priorität: MITTEL — Story und Immersion**

#### Schritt 7.1: Basis-NPC-System
```
NPCData (ScriptableObject)
├── npcName (string)
├── role (enum: Alchemist, Smith, Leader, Cultivator, Merchant, Mysterious)
├── portrait (Sprite)
├── dialogueLines (DialogueLine[])
│   ├── text (string)
│   ├── condition (enum: None, QuestComplete, RealmReached, ItemOwned)
│   ├── conditionValue (string)
│   └── responses (string[])
└── relationshipLevel (int)
```

#### Schritt 7.2: Dialogue-System
- Einfaches Textbox-UI mit Portrait
- Branching basierend auf Conditions (Quest-Status, Realm, Items im Inventar)
- NPCs erinnern sich an Story-Fortschritt

#### Schritt 7.3: Kern-NPCs implementieren
```
1. Master Chen (Alchemist)  — Lehrt Rezepte, gibt Crafting-Quests
2. Iron Liu (Schmied)       — Baut/upgraded Maschinen, gibt Building-Quests
3. Elder Wei (Anführer)     — Hauptquest-Geber, Story-Exposition
4. Mei (Cultivatorin)       — Kampf-Tutorial, Begleitung in Minor Realms
5. Ghost (Mysteriös)        — Story-Twist, späte Game-Quests
6. Old Zhang (Händler)      — Tauscht Items, hat seltene Materialien
```

#### Schritt 7.4: Quest-System (einfach)
```
QuestData (ScriptableObject)
├── questName (string)
├── description (string)
├── questGiver (NPCData)
├── objectives (QuestObjective[])
│   ├── type (enum: CollectItem, CraftItem, ReachRealm, DefeatEnemy, BuildMachine)
│   ├── targetId (string)
│   └── requiredAmount (int)
├── rewards (RecipeSlot[]) — Item-Belohnungen
├── unlocksRecipe (RecipeData) — Rezept als Belohnung
├── unlocksNextQuest (QuestData)
└── dialogueOnComplete (DialogueLine[])
```

**Dateien:**
- `Data/DataTemplates/NPCData.cs`
- `Data/DataTemplates/QuestData.cs`
- `Systems/NPC/NPCController.cs`
- `Systems/NPC/DialogueSystem.cs`
- `Systems/Quest/QuestManager.cs`
- `Ui/Dialogue/DialogueUI.cs`
- `Ui/Quest/QuestTrackerUI.cs`

---

### PHASE 8: Combat (Minimal Viable)
**Priorität: NIEDRIG-MITTEL — nach Factory-Kern**

#### Schritt 8.1: Basis-Combat
- Einfacher Angriff (Mausklick)
- Ausweichen (Dodge-Roll, Shift)
- HP-System (getrennt von Qi)
- Damage basierend auf Cultivation Realm + Equipment

#### Schritt 8.2: Enemy-System
- `EnemyData` ScriptableObject (HP, Damage, Speed, Loot-Table)
- Einfache AI: Patrol → Detect → Chase → Attack
- Spawning in Minor Realms und gefährlichen Overworld-Zonen

#### Schritt 8.3: Loot-System
- Gegner droppen Materialien (Beast Cores, seltene Kräuter)
- Drop-Tables in EnemyData definiert
- Verbindung zur Factory: seltene Combat-Drops als Crafting-Material

#### Schritt 8.4: Pill-basierte Kampf-Boosts
- Pillen vor dem Kampf einnehmbar
- Temporäre Buffs: Damage+, Speed+, Defense+
- Verbindet Factory-Output direkt mit Combat-Gameplay

**Dateien:**
- `Systems/Combat/CombatController.cs`
- `Systems/Combat/HealthSystem.cs`
- `Systems/Combat/EnemyAI.cs`
- `Data/DataTemplates/EnemyData.cs`
- `Ui/Combat/HealthBarUI.cs`

---

### PHASE 9: Progression, Balancing, Story-Integration
**Priorität: NIEDRIG — Endphase, nach allen Kern-Systemen**

#### Schritt 9.1: Hauptquest-Linie
```
Act 1: Ankommen, Überlebens-Basics, erste einfache Pillen
Act 2: Factory aufbauen, Produktion optimieren, Community stärken
Act 3: Seltene Materialien aus gefährlichen Realms holen
Act 4: Realm-Breaking Pill Massenproduktion
Act 5: Der Ausbruch (Finale)
```

#### Schritt 9.2: Freischalt-Kette
```
Realm 1 (Qi Gathering) → Manuelles Crafting, Basis-Rezepte
Realm 2 (Foundation)    → Furnace + Crusher freigeschaltet
Realm 3 (Core)          → Mixer + Distiller, Spirit Pipes
Realm 4 (Nascent Soul)  → PillPress, Condenser, komplexe Rezepte
Realm 5 (Dao)           → Alle Maschinen, Realm-Breaking Pill Rezept
```

#### Schritt 9.3: Win-Condition
- X Realm-Breaking Pills produziert (z.B. 10)
- Jede Pille benötigt massive Ressourcen → natürlicher Endgame-Grind
- Finale Cutscene/Sequenz

---

## EMPFOHLENE REIHENFOLGE (Step-by-Step)

```
SCHRITT  1: [Phase 0] Generisches Item-System          ✅ FERTIG
SCHRITT  2: [Phase 1] Rezept/Crafting-System            ✅ FERTIG
SCHRITT  3: [Phase 2] Pill-System + Cultivation-Hook    ✅ FERTIG
SCHRITT  4: [Phase 6] Ressourcen-Definitionen (Content) ✅ FERTIG (OreVeins)
SCHRITT  5: [Phase 3] Grid + Placement-System           ✅ FERTIG
SCHRITT  6: [Phase 4] Erste Maschine (Furnace)          ✅ FERTIG (BaseMachine)
SCHRITT  7: [Phase 4] Maschinen-UI                      ✅ FERTIG
SCHRITT  8: [Phase 4] Weitere Maschinen                 ✅ FERTIG (Splitter/Merger)
SCHRITT  9: [Phase 5] Spirit Pipes + Storage            ✅ FERTIG
SCHRITT 10: [Phase 7] NPC-Basis + Dialogue              ⬜ OFFEN
SCHRITT 11: [Phase 7] Quest-System                      ⬜ OFFEN
SCHRITT 12: [Phase 8] Combat (Minimal)                  ✅ FERTIG
SCHRITT 13: [Phase 6] Minor Realm Ressourcen-Anpassung  ⬜ OFFEN
SCHRITT 14: [Phase 9] Story-Quests + Freischalt-Kette   ⬜ OFFEN
SCHRITT 15: [Phase 9] Balancing + Win-Condition         ⬜ OFFEN
```

---

## ARCHITEKTUR-PRINZIPIEN

1. **ScriptableObjects für alles:** Items, Rezepte, Maschinen, NPCs, Quests — alles als Assets.
   Ermöglicht Content-Erstellung ohne Code-Änderungen.

2. **Event-Driven (bestehend):** GameEvents wird erweitert, nicht ersetzt.
   Neue Events: OnItemCrafted, OnMachinePlaced, OnPillConsumed, OnQuestCompleted.

3. **Interface-basiert (bestehend):** IInteractable wird von Maschinen implementiert.
   Neues Interface: IProcessable für Maschinen-Logik.

4. **Composition over Inheritance:** BaseMachine als Kern, spezifisches Verhalten via Komponenten.

5. **Save-System inkrementell erweitern:** Jede Phase fügt ihre Daten zu SaveData hinzu.
   Backward-Compatibility durch Default-Werte bei fehlenden Feldern.

---

## KAMERA & BUILD-MODUS

### Build-Modus Entkopplung ✅ IMPLEMENTIERT
- Build-Modus ist **unabhängig von der Kameraperspektive** — funktioniert in 3rd Person UND Spirit Sense
- **Shift-Taste** toggelt Build-Modus in jeder Perspektive
- `GameEvents.OnBuildModeToggled(bool)` — neues Event, ersetzt die alte Kopplung an `OnMeditationToggled`
- BuildMode-InputMap wird additiv aktiviert (Place, Cancel, Rotate funktionieren auch in 3rd Person)
- Bei Spirit Sense Eintritt: Build-Modus wird automatisch aktiviert
- Bei Spirit Sense Austritt: Build-Modus wird automatisch deaktiviert

### Kamera-Transition Fix ✅ IMPLEMENTIERT
- Cinemachine-Position/Rotation wird **vor dem Deaktivieren** gespeichert
- Beim Rückwechsel (Spirit Sense → 3rd Person): Animation-Ziel ist die gespeicherte Position
- Kamera-Transform wird vor Cinemachine-Reaktivierung gesetzt → nahtloser Übergang

### Realm-basierte Spirit Sense Reichweite ✅ IMPLEMENTIERT
- `RealmDefinition.spiritSenseRange` — maximale Zoom-Distanz pro Realm
- `SpiritSenseCamera.SetMaxZoom(float)` — wird bei Spirit Sense Eintritt gesetzt
- Höherer Realm = größere Übersicht = strategischer Vorteil beim Factory-Bau

**Dateien:**
- `Scripts/Core/GameEvents.cs` — OnBuildModeToggled Event
- `Scripts/Data/DataTemplates/RealmDefinition.cs` — spiritSenseRange Feld
- `Scripts/Player/Camera/CameraSystem.cs` — Build-Toggle, Kamera-Fix, Realm-Distanz
- `Scripts/Player/Camera/SpiritSenseCamera.cs` — SetMaxZoom()
- `Scripts/Ui/Building/BuildMenuController.cs` — hört auf OnBuildModeToggled
- `Input/InputSystem_Actions.inputactions` — BuildToggle im Player-Map

---

## TODOs

→ Siehe `Assets/_Project/TODO.md`
