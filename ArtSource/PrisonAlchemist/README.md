# Prison Alchemist — character handoff

Created directly in the open Blender 5.0.1 session and integrated into Unity 6000.3.8f1.

## Design reasoning

`GAMEPLAN.md` describes a modern chemist reincarnated into imprisonment in a cultivation realm: alchemy, automation with fellow prisoners, and escape through realm-breaking pills. The existing movement, stamina, meditation, spirit-sense, crafting and Qi-network systems suggest an agile working alchemist rather than a heavily armoured warrior.

The original character uses an ink/jade split work coat, linen crossed lapels and boot wraps, copper fittings, a burgundy sash, reagent vials, a satchel and a practical topknot. The placeholder's appearance was not used as an art reference. This establishes a stylized direction; it is an authored procedural character, not a scanned or downloaded model.

## Preserved gameplay contract

- `Universe.unity` player root: world position `(339, 1, 270)`, unit scale.
- Existing capsule: height **2 m**, radius **0.5 m**, centre `(0,0,0)`, Y axis. Its envelope is 1 × 2 × 1 m.
- Model feet are at its origin; crown is 2 m high in neutral pose. The visual is placed at local `(0,-1,0)` under Player. Swinging limbs can extend outside the capsule, as normal for a humanoid locomotion visual; physics still uses the original capsule.
- Existing `PlayerMovement`, Rigidbody, input references, other player scripts and Cinemachine tracking target are retained.
- Animator parameters remain `Speed` (Float), `IsGrounded` (Bool), `Jump` (Trigger). The original parameters are retained; the controller now separates Jump, Fall and Land and includes gameplay action states.
- The Speed blend tree now uses **0 / 1.8 / 4.8**, matching horizontal velocity, walk speed and sprint speed. Previously its thresholds were 0 / 0.5 / 1.
- All twelve clips are in place. Root motion is disabled; Rigidbody movement and jump impulse own displacement.
- Legacy visual renderers/Animators are disabled, retained for recovery. No old art assets were deleted.

## Delivered files

- `PrisonAlchemist.blend`: editable character, rig, actions and lit studio. The original Blender scene is preserved in this file too.
- `PreCharacterWorkspace.blend`: a copy of the original open Blender workspace before character creation.
- `Assets/_Project/Characters/PrisonAlchemist/PrisonAlchemist.fbx`: mesh, 52-bone rig and twelve actions; -Z Forward / Y Up; metre scale.
- `Assets/_Project/Characters/PrisonAlchemist/PrisonAlchemistVisual.prefab`: reusable visual variant.
- `Materials/` beside the FBX: remapped URP Lit materials.
- `PrisonAlchemist_Preview.png`: lit Blender portrait.
- `Unity_Idle.png`, `Unity_Walk.png`, `Unity_Run.png`, `Unity_Jump.png`: captured imported poses.
- `blender_qa.json`, `unity_validation.txt`: measured validation results.

| Clip | Duration | Loop | Controller use |
|---|---:|---|---|
| Idle | 3.000 s | Yes | Speed 0 |
| Walk | 0.800 s | Yes | Speed 1.8; cadence scaled to stride |
| Run | 0.667 s | Yes | Speed 4.8; cadence scaled to stride |
| Jump | 0.300 s | No | Jump trigger; vertical velocity selects Fall |
| Fall / Land | 1.000 / 0.300 s | Fall only | Ground contact controls recovery |
| Meditate / Craft | 3.000 / 1.000 s | Yes | Meditation and crafting events |
| Attack / Dodge | 0.400 / 0.333 s | No | Combat input triggers |
| Hurt / Death | 0.300 / 1.000 s | No | Health events |

## Rig and QA

The T-pose skeleton has the torso, shoulders, limbs, feet, toes and finger chains; Unity explicitly maps 51 Humanoid bones. The root is the remaining bone. Support rings surround elbows and knees, with normalized skin weights and at most two influences per vertex. The final mesh contains **5,488 Blender vertices / 10,157 triangles**. Unity splits vertices at material, UV and normal boundaries, so its imported count is higher.

Idle, running and jump poses were inspected directly in the live Blender viewport. Corrections addressed sleeve shoulder weighting, overlapping shoulder inserts, coat/trouser clearance, boot wrap intersections and toe-cap fit. Blender samples verify finite deformed vertices, no unweighted vertices, normalized weights and zero horizontal root travel. Walk foot clearance is corrected per frame. Idle breathing varies height by a few millimetres; it does not change the physics capsule.

Unity validation uses the actual controller in a manually evaluated PlayableGraph: speed 0/1.8/4.8 selects Idle/Walk/Run, Jump selects Jump, and grounding returns to Idle. Each pose is baked for finite-vertex checks and preview captures. The final import reports zero animation/retarget warnings. A scene Play Mode smoke test also verified that the active Humanoid is visible through the existing Main Camera; no console errors remained. Play Mode was stopped afterward. The follow-up below adds real input playtests and physics tests; arbitrary stairs and steep terrain still need a dedicated traversal course.

## Editing and rebuilding

The session-only loopback bridge is running on `127.0.0.1:9877`; it does not install a system service. After restarting Blender, run `blender_bridge.py` once in Blender's Python Console to enable the included `client.py`. All bpy jobs execute on Blender's main thread. Closing Blender ends the listener.

For normal art revisions, edit `PrisonAlchemist.blend`, then run `export_character.py` through the live bridge. For a clean procedural rebuild, first open **PreCharacterWorkspace.blend**, then execute **build_pipeline.py** in Blender's Python Console. It creates a separate studio scene. Scripts currently use this project's absolute path; update the source root if moving the project. `render_preview.py` regenerates the portrait separately.

Unity menu: **Tools → Cultivation → Character** provides Configure Import, Install Visual, and Validate/Capture. Configure Import creates the Human avatar, maps bones, trims clip names, sets loops and root locks, and remaps URP materials. Install Visual preserves the existing player root and redirects Animator references. The FBX importer deliberately retains bone transforms for future hand/weapon/vial attachments.

## Gameplay polish and playtest follow-up (11 September 2026)

The live input recording reduced the jump from 4.997 m rise / 1.942 s airborne to 1.774 m / 1.024 s. The controller uses acceleration, stronger ground braking, limited air steering, buffered/coyote jumping and faster descent. Landing no longer resets movement speed or waits for a fixed jump clip to finish. A grounded foot IK pass supports terrain contact. The empty upper-body action layer has zero weight outside actions, avoiding a retained arm pose.

The character now uses one palette material per visible LOD, with approximately 10.1k / 5.0k / 2.2k triangles. Grass height and density are reduced. Editable original source scripts and the original workspace backup remain available.

Building was exercised in Play Mode: B opens build mode, Q opens the catalogue, clicking Furnace selects its preview, R rotates it, and LMB places it. A rotated Furnace was successfully placed with a persistent GUID, BaseMachine component and occupied grid cells. Catalogue dragging assigns hotbar slots, and missing icons now display machine names. Valid/invalid previews use dedicated green/red materials. Test sessions restore the user's pre-test save when Play Mode exits.

Additional controls: G meditation, LMB attack outside build mode, C dodge, Space jump, Shift sprint. Crafting animation follows the existing recipe events. Combat is now wired to the player's HealthSystem; the HUD initializes to 100/100 HP.

Use **Tools > Cultivation > Character > Apply Gameplay Polish** after a rebuild to apply import, visual, controller, LOD, input and terrain settings together. Running Install Visual alone does not apply all follow-up gameplay settings.

Remaining work: a designed traversal course (especially stair stepping and steep slopes), machine icons and finished machine art, terrain obstruction/slope rules for placement, turn-in-place animation, and combat contact timing. No cloth simulation or facial rig is included. See `../Playtests/PLAYTEST_REPORT.md` for verified test results and limits.
