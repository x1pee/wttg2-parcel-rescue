# Audit — 2026-10-20

Version 1.0.0 was reviewed without a code change. No reproducible defect justifying a new plugin release was found.

The hotkey requires game focus, a fresh press, the normal roaming state, unpaused time and a cooldown. A pickup searches only the game's current shipped-product list. It accepts a delivered, unlocked, visible, non-kinematic box with the correct parent; queued and drone-carried boxes are excluded. It invokes the game's original pickup handler, which removes the box from the delivery list and updates inventory once. Object scans run on demand, not every frame.

The production build passed. The earlier isolated-game test passed 15 checks. This audit additionally exercised the original pickup handler in the combined plugin environment: inventory/list changes, distance rejection, exclusion of the in-flight parent and repeated-pickup protection. This does not prove a physical F6 press in a normal gameplay session or recovery of a box already destroyed by the game.

The used game class signatures match the local pre-localization English backup. An independently installed Steam build has not been tested. Source remains MIT licensed; the original game and third-party dependencies are not distributed.

## Combined Unity verification

2026-10-20: 48/48 checks passed in a separate real Unity process with the neighboring plugins installed. The fixture waited for natural additive-world staging and used a redirected test profile. Original delivered-box pickup, inventory/list changes, distance rejection, in-flight parent exclusion and repeated-pickup protection passed. This is targeted runtime evidence, not a full gameplay session or a retail Steam compatibility claim.
