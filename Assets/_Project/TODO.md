# Prison Realm: Alchemy Factory — Offene Aufgaben

Letzte Aktualisierung: 2026-03-26
Vollständige Liste aller offenen Punkte. GitHub Issues werden mit Copilot-Agents bearbeitet.
Manuelle TODOs müssen im Unity-Editor erledigt werden.

---

## Manuelle TODOs (Unity-Editor)

*Entspricht GitHub Issue #76*

- [ ] CameraSystem Inspector: `playerStats` Referenz zuweisen
- [ ] CameraSystem Inspector: `BuildToggle` InputActionReference auf `Player/BuildToggle` setzen
- [ ] PlacementController Inspector: Input-Referenzen prüfen (Place, Cancel, Rotate, Remove)
- [ ] Cursor-Handling in 3rd Person Build-Modus testen
- [ ] Build-Menu UI-Styling für 3rd Person Overlay optimieren

---

## GitHub Issues (Copilot-Agent Tasks)

### Phase 7: NPC & Quest System

| Issue | Titel | Hängt von |
|-------|-------|-----------|
| #102 | QuestData ScriptableObject & QuestSystem | — |
| #103 | NPCController MonoBehaviour | NPCData (✅ existiert) |
| #104 | Quest-Journal UI | #102 |
| #105 | Händler-System: ShopData & UI | NPCData |

### Phase 9: Story & Endgame

| Issue | Titel | Hängt von |
|-------|-------|-----------|
| #106 | StoryData, StoryProgressSystem & Win-Condition | #102 |
| #112 | Realm-Breaking Pill Produktionskette | — |

### Systems & Scenes

| Issue | Titel | Hängt von |
|-------|-------|-----------|
| #107 | SceneTransitionManager | — |
| #108 | Tutorial-System | — |
| #109 | Hauptmenü-Scene | — |
| #110 | Kompass-HUD | — |
| #111 | Inventar-Tooltips & Rechtsklick-Kontextmenü | — |

### Tests

| Issue | Titel | Hängt von |
|-------|-------|-----------|
| #114 | Unit Tests: QiNetwork BFS | — |
| #115 | Unit Tests: BaseMachine Pipeline | — |
| #116 | Unit Tests: SaveSystem | — |

### Qualität

| Issue | Titel | Hängt von |
|-------|-------|-----------|
| #113 | Game Balance Pass | alle Content-Assets |
| #117 | Code-Qualität Review | — |

---

## Kleinere technische TODOs (noch kein GitHub Issue)

- [ ] **Save/Load für Splitter/Merger-Verbindungen**: Verbindungen werden aktuell nicht in SaveData serialisiert — auf Scene-Reload gehen Pipe-Verbindungen verloren
- [ ] **Visual Pipes**: Line Renderer oder Mesh-Generierung zwischen verbundenen Maschinen zur visuellen Darstellung der Logistics-Netzwerke
- [ ] **Throughput Upgrades**: `itemsPerTransfer` erhöhen / `transferInterval` senken via Upgrade-System (Post-Core-Loop Feature)

---

## Erledigte Punkte (Referenz)

✅ Phase 6: Minor Realm Ressourcen-Spawning (PR #60, #62)
✅ Phase 8: Combat System (PR #18, #19)
✅ Pipe Connection UI (PR #41)
✅ MachineData Assets alle 11 Typen (PR #40)
✅ 3rd Person Build Cursor (PR #79)
✅ QiNetwork Performance Dirty-Flag (PR #69)
✅ BreakthroughSystem + UI (PR #78)
✅ SoundManager + SoundEventTrigger (PR #84)
✅ HUD: Qi/HP/Stamina + Buff Icons (PR #81)
✅ RecipeDatabase Editor-Tool (PR #82)
✅ LootSystem + EnemyData Assets (PR #80, #89)
✅ MachineInspectUI (PR #77)
✅ FactoryDashboardUI (PR #99)
✅ NPCData + DialogueNode + DialogueUI (PR #90, #92, #59)
✅ Unit Tests: MachineInventory, RecipeData, RecipeDatabase, LootSystem
✅ Doppeltes .asmdef entfernt
✅ 36 fehlende .meta-Dateien generiert
