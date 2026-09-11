# Game review — 11 September 2026

The project is a systems prototype with a clear factory/cultivation concept. It has substantial implementation, but the authored content and scene integration do not yet support a complete player journey. The older “finished” labels in GAMEPLAN.md and Assets/_Project/TODO.md describe code availability more accurately than playable completion.

This review used the source, serialized assets, the running Unity 6000.3.8f1 editor, EditMode tests, and scripted Play Mode checks. It is not a frame-time benchmark or a full manual gameplay/balance pass.

## Current state

| Area | Evidence and assessment |
| --- | --- |
| Movement, inventory, cultivation | Present on the Universe player. The main inventory, catalogue, hotbar, and machine inspection panels initialize in Play Mode. |
| Factory | 14 MachineData assets with prototype prefabs. Every prefab is currently visuals/collider only; runtime setup supplies behavior. Production, extraction, storage, pipes, splitters, mergers, and Qi networking have code. |
| Recipes | Five recipe assets, but only two valid manual recipes. SmeltCopper and SmeltIron have no outputs; BrewQiPill has null ingredient references. The playable database originally contains only the two manual recipes. There is currently no authored, valid factory recipe chain. |
| Power | QiConduit code exists, but no corresponding MachineData/prefab asset was found. Universe starts with zero conduits. Processing machines consume Qi and need a connection, so power setup is a blocker for an ordinary factory playthrough. |
| Exploration | Universe, Grotto, and MinorRealm are enabled in build settings. MinorRealm has generation and resource-spawning code. A live Universe → Grotto trip left one persistent player but **zero active cameras**. |
| Combat | Player combat, health, enemy AI, and loot code exist, with eight EnemyData assets. The Universe player has neither PlayerCombatController nor HealthSystem. MinorRealmGenerator does not spawn enemies or build a NavMesh. Combat is not integrated into this player journey. |
| Dialogue | NPCInteractor and DialogueUI exist, but there are zero NPCData and zero DialogueNode assets. |
| Progression | Quest/tutorial/shop/win-condition implementations and a main-menu scene were not found. Four PillData assets exist, but the escape objective is not connected to a production chain. |
| Audio | SoundEventTrigger is in Universe, but all nine inspected sound-clip references are empty. |

## Corrections applied

- Machines retain the recipe whose inputs were paid for. Selecting a different recipe affects the next batch rather than converting the active batch into a different output.
- Completed batches wait for enough output capacity instead of silently dropping overflow. Waiting batches stop drawing Qi and show an output-full status.
- Factory and manual crafting reject incomplete recipes and check the combined quantity of repeated ingredient rows. Factory processing also checks machine compatibility.
- Active machine recipe, elapsed time, and duration are serialized and restored without charging ingredients twice. Old saves without these fields retain their previous idle behavior; previously lost batches cannot be reconstructed.
- MachineWiring now creates the required component for visual-only prefabs on both placement and loading. The duplicate placement-only implementation was removed. Existing MachineGuid components are reused, and duplicate saved GUID entries are skipped during restoration.
- Portals capture the outgoing world before changing its scene/realm identity. This preserves factory state when traveling. Loading also restores transition flags before restoring a scene, regenerates a different saved minor-realm seed, and clears stale saved return points.
- Save writes replace the old file directly after writing the temporary file, eliminating the delete-before-move gap. A failed write keeps the previous save.
- Splitters respect storage item filters.
- Machine inventory totals are cached, capacity arithmetic avoids integer overflow, and the exposed item view is read-only so outside writes cannot invalidate the cache. Qi connectivity reuses its queue and registered-machine view.
- Removed two obsolete BuildMenuController components with missing scripts from Universe. The current MachineCatalogueController and HotbarController remain in place.
- Replaced the ore vein prefab's 837,936-triangle MeshCollider with a bounds-based BoxCollider, inherited by its three Universe instances. Visual meshes remain unchanged. Collision is now approximate; inspect the rock edges during playtesting. Rendering cost still needs profiling and lower-detail meshes.
- Placement rejects a missing real prefab even when a ghost prefab exists, preventing a failed placement after paying its cost.

## Verification results

- **74/74 EditMode tests passed**, including 26 added regression cases. The XML result is in `Logs/game-audit-editmode.xml`.
- Universe starts in Play Mode with its principal UI panels available, one active camera, and no initial runtime errors or warnings in the inspected console.
- A scripted **Universe → Grotto → Universe** round trip restored exactly one temporary factory machine, seven buffered inputs, and its paid batch at 2/5 seconds. This used a temporary valid recipe because the authored factory recipes are incomplete. The temporary recipe was removed afterward.
- The editor reports **zero missing scripts** in Universe after cleanup.
- The original user save was restored after runtime verification and checked against its original SHA-256 hash.
- The runtime trip also exposed the Grotto camera gap and terrain/input warnings described below. Full interactive travel, combat, final builds, minor-realm seed reloads, and performance measurements remain unverified.

## Next development steps, in order

### 1. Make travel preserve a usable player session

Create an explicit lifetime arrangement for the player, cameras, input, UI, and shared gameplay services. Grotto currently loses the camera because the camera roots live in Universe while only the player survives travel. Ensure scene-local build grids are refreshed rather than carrying occupied cells into another scene.

Acceptance: Universe → Grotto → Universe and Universe → MinorRealm → Universe retain one active player, a working camera, usable input, inventory/build UI, correct spawn/return positions, and exactly one save coordinator. Repeat each trip twice and test loading in every destination. Resolve the observed duplicate-player input-pairing warning and Grotto terrain/tree warnings as part of this work.

### 2. Finish one complete factory chain

Author the missing outputs for SmeltCopper/SmeltIron and the missing inputs for BrewQiPill; create any required intermediate ItemData assets. Add valid recipes to RecipeDatabase and all participating items to SaveManager's lookup list. Create a QiConduit prefab/data asset and include it in the catalogue and save lookup. Keep the first unlock and build costs reachable from a fresh game.

Target a 10–15 minute slice: gather resources → build extractor/furnace/storage → connect Qi and a pipe → produce a useful pill → consume it → reach the first breakthrough. A fresh player must be able to do this without Inspector edits or injected items.

### 3. Finish logistics and persistence edge cases

Add in-game splitter/merger endpoint selection and serialize those connections. Add and bind a BuildMode/Remove action; the current input asset and PlacementController reference do not supply it. Define dismantling behavior for stored items and paid batches, including refunds. Bring extractor/splitter/merger power behavior into agreement with their nonzero MachineData Qi costs; QiNetwork currently tracks only BaseMachine.

Extend persistence to health and temporary pill effects, and replace rename-sensitive machine/recipe names with stable IDs plus a save-version migration policy. Test loaded full buffers, depleted veins, power loss, and same-scene/different-seed reloads. Test save-write failure feedback rather than assuming a logged error is visible to the player.

### 4. Add a small combat encounter

Wire HealthSystem, PlayerCombatController, attack/dodge actions, layers, and HUD on the player. Add one enemy prefab, suitable navigation, and one spawn location. Verify dodge/movement interaction, death/respawn, and loot collection. Make one useful factory ingredient come from this encounter.

### 5. Add guidance and a visible short-term objective

Create one NPC and a short dialogue explaining the first production goal. Implement a small persistent objective sequence for gathering, building, producing, and breakthrough. Add a main menu and clear new-game/continue behavior. Defer merchants, companions, and a large quest framework until this sequence is enjoyable.

### 6. Measure and balance the slice

Profile representative factories at roughly 10, 50, and 100 machines, plus a generated realm. Record CPU frame time, GC allocation, physics cost, draw calls, and triangle counts. Replace dense rock/character meshes with practical game meshes and LODs based on those measurements. Tune Qi consumption against meditation income and pill value so the player can explore while production remains useful.

The current changes reduce identifiable work; they do not establish an FPS improvement. Add audio and production/power feedback during this pass, then have another person play from a clean save before expanding the machine roster or endgame.
