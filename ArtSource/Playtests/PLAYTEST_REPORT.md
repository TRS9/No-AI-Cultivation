# Movement, character and building playtest ? 11 September 2026

## Verified changes

- Ground movement uses explicit acceleration and braking. Walk/sprint are 1.8/4.8 m/s, with clip cadence matched to the authored foot stride. Air steering is limited; releasing input slows the body instead of letting it coast indefinitely.
- Jump impulse changed from 10 to 6, with stronger falling gravity. The real-scene recordings measured 4.997 m / 1.942 s airborne before and 1.774 m / 1.024 s after. These are sampled measurements, not exact analytic bounds.
- Jump, Fall and Land are separate states. Descent remains in Fall until contact; moving landings blend promptly into locomotion. Buffered jump input and coyote time remain available. Holding Jump does not repeatedly launch the character.
- Uphill motion no longer mistakes positive vertical velocity for a new jump. Grounded foot IK aligns feet near contact.
- Added Meditate, Craft, Attack, Dodge, Hurt and Death actions. The upper-body action layer fades out when empty, fixing retained arm poses. Health and combat references are wired; the live HUD showed 100/100 HP.
- Consolidated 11 material slots to one palette material per visible LOD. Three detail levels contain approximately 10.1k / 5.0k / 2.2k triangles. Grass is shorter and less dense.
- Catalogue items can be clicked to begin placement or dragged to hotbar slots. Slots without icons display machine names. Preview materials are dedicated translucent green/red. Placement revalidates resources/grid at the input event, clears stale validity after a missed terrain ray, and cancels when leaving build mode.

## Tests and direct inspection

- Existing Edit Mode suite: **74 passed, 0 failed** (`Tests-EditMode.xml`).
- New Play Mode suite: **4 passed, 0 failed** (`Tests-PlayMode.xml`): jump height/landing/no held-key bounce; stopping within 0.35 m after sprint release; remaining grounded while sprinting up a 12-degree slope; meditation/death movement blocking.
- The movement fixture uses the same zero-friction capsule behavior as the scene's Slippery material. Its initial ordinary-friction setup was corrected after diagnostics showed friction absorbing the motor acceleration.
- Imported controller validation checks all action states, sustained falling, in-place root position, finite skinned vertices, one material per skin and zero Humanoid retarget/import warnings. Pose captures are in `../PrisonAlchemist/Unity_*.png`.
- Live Blender viewport inspection covered locomotion and meditation deformations. Unity live inspection confirmed correct idle arms after removing the empty override layer's weight.
- **Built a Furnace:** catalogue click ? R rotation ? LMB placement at (338,1,272), yaw 90 degrees. Verified BaseMachine, collider, persistent MachineGuid, occupied 2?2 cells and cleared ghost. `building.txt` contains the runtime result. This exercised placement, not a complete powered production chain.
- Final live checks confirmed G enters Meditate and blocks movement, paced catalogue dragging assigns Furnace to slot 1, the slot name is readable, digit 1 selects the green preview, and exiting build mode removes the ghost. A fast automation drag initially coalesced into a click at its destination; separate input frames verified the real drag flow.
- After exiting, the restored save and pre-test backup had identical SHA-256 hashes; Play Mode was off, the scene was saved, and the temporary hotbar assignment was cleared.
- Playtest helpers back up and restore the player's persistent save when exiting Play Mode. Test machines are temporary.

## Remaining priorities

1. Build a small traversal course with stairs, ledges, downhill transitions and steep inclines. The flat-ground and 12-degree uphill tests do not establish robust stair stepping or arbitrary terrain traversal.
2. Add placement obstruction, terrain-slope and distance rules. Current grid checks prevent machine-cell overlap, but tree/terrain geometry is not a complete placement blocker. Add machine icons and explicit cost/power feedback, then test a full extractor ? processing ? storage chain.
3. Align attack damage with the animation contact frame, add turn-in-place clips and tune controller response through a longer hands-on session. The character and machine visuals remain an early stylized art pass.
