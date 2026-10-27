# Cartoon redesign

The dragons now use larger round heads, big white eyes and rounded pupils, cream belly markings, shorter horns, fewer teeth/claws and bouncier squash-and-stretch animation. A resource-loaded URP toon shader creates flat color bands and dark edge shading without realistic metallic reflections. Species details and evolution mechanics remain attached to the animated rig. Gameplay remains 3D with a 2D cartoon appearance.

All GameUI pages share the new sky/cream/lavender theme, rounded cards, bold buttons and dark text. Home, collection, dragon details, shop, coin confirmation, Google sign-in, daily rewards, powers, upgrades, Settings, pause, results and development screens inherit these components. The gameplay HUD has matching cream cards. Settings has separate audio and motion cards, percentage labels, larger slider handles and 30/60 FPS selection.

Animated vector dragon portraits replace old realistic portrait images in menus and the HUD. They blink and flap, use the selected dragon's colors, and show stage decorations without requiring regenerated PNG files.

Validation: Unity Roslyn compilation passed for runtime, editor and Android-specific C# source. The Unity Editor license remains inactive, so shader compilation, actual screen rendering, touch/layout review and APK/AAB creation could not be run. Review all screens at normal and tall portrait ratios after activating the license. Existing build files and screenshots predate this redesign.

## All-ages refinement

Permanent stages now change proportions as well as accessories: hatchlings have larger heads and smaller wings; mature dragons gain wider shoulders, taller torsos, broader wings and longer tails. Stage changes use stored rest scales so repeated upgrades do not compound scaling. Adult movement keeps playful expressions with less idle sway and bounce. Vector portraits reflect the same maturity progression, while menu wording and secondary button colors are more understated.

Runtime, editor and Android C# compilation passed after this refinement. Visual review remains blocked by the inactive Unity Editor license.

## Feature-graphic visual direction

The feature graphic now guides the in-game UI: brighter sky/turquoise scenery, clean warm-white panels, gold-to-cream heading fills with plum outlines, and teal/gold actions. Home uses the matching transparent CinderHero artwork, the two-line Monster Merge title and AI Evolution subtitle. Selected non-starter dragons retain their own animated vector portrait rather than showing the wrong character.

Headers use Bree Serif; body copy uses Lato. These are bundled open-licensed fonts, not a claim that the generated banner lettering came from an exact reusable font. License texts ship beside the fonts. Sources:
- https://github.com/google/fonts/tree/main/ofl/breeserif
- https://github.com/google/fonts/tree/main/ofl/lato

Visible copy was refreshed across Home, dragon collection/details, shop, coin confirmation, account/sign-in, daily treasure, Settings, pause/results and gameplay status. Payment amounts, verification requirements and pending-purchase instructions remain explicit. In-game dragon eyes now include golden irises; Cinderclaw's wing membranes use the banner's teal.

Hero asset: Assets/MonsterMerge/Resources/MenuArt/CinderHero.png. Generated with the built-in image tool from the feature graphic; the prompts are in the adjacent GENERATION.txt. The exported asset has alpha transparency (verified).

Validation: runtime/editor/Android C# compilation passed with Unity's installed compiler. Both TTF file signatures were verified. Actual Unity import, shader/font rendering, phone-sized layout and APK testing remain outstanding; the last Unity build attempt reported no active Editor license. Existing screenshots and builds are not evidence of this new UI.

## Species upgrades and next-adventure flow

All eight species now have their own three upgrade feature sets in the live animated rig: ember/flame, pearl/coral, venom/leaf, basalt/granite, ice/crystal, thunder/bolt, reactor/mechanical, and moonstone/eclipse. Menu portraits add matching shoulder, wing and crown motifs per tier. Dragon details and next-form previews describe the actual species features instead of a generic armor/crown label.

Victory keeps the result and reward screen. Next Adventure opens a current-versus-next form preview showing the upgraded power and exact coin price. Upgrading requires the existing Google sign-in and a fresh check of account, selection, stage and price. Insufficient coins disable upgrading; Skip remains available. Maximum-stage dragons can continue directly. Upgrade and skip actions guard against repeated navigation; skipping never spends coins.

Every new run and retry begins in Preparing with movement and steering disabled. A 3-2-1 overlay starts the runner only after completion. Backgrounding or losing focus restarts the countdown at three on return, preventing a hidden countdown from starting the run.

Runtime, editor and Android C# compilation passed. Existing gameplay validation was extended to assert countdown immobility and skip preserving coin/stage values, and updated to wait for the new run start. Those Unity play-mode checks and visual checks have not run; the last Editor launch failed license activation. No new APK was generated.


## Power transformation and progression update
- Removed temporary elemental back crests, floating symbols, turbines and box armor. Element color now blends across existing dragon skin. Power smoothly changes overall size, wing span and tail length, with a short glow and restrained pulse. Species armor uses rounded surfaces.
- Stage 1 permits levels 1?10; Stage 2 permits 1?20; Stage 3 permits 1?30; Stage 4 permits all unlocked levels. Starting a run checks both unlocked progress and the selected dragon stage.
- Victory offers Buy Coins immediately. Next Adventure previews the permanent upgrade, with coin packs and replay choices. Skip continues only when the selected dragon meets the next level requirement.
- Home and upgrade screens offer paginated replay selection. Replays earn completion coins and preserve the highest unlocked level. Retry repeats the failed level. All starts retain the 3?2?1 countdown.
- Validation: Editor/runtime/Android C# compilation passed. Validation/Progression/ProgressionCheck.cs compiles the actual RunRules with minimal standalone Unity data/math substitutes; all 3 stage-boundary checks and 10 full-power starter boss simulations passed. Combat simulation assumes full starting health and avoids route hazards; it does not substitute for a complete gameplay test.
- Unity Play Mode and visual review remain pending because the available Editor license was inactive. No new APK/AAB was produced by these checks.


## Individual power-up animations
Each +50 power burst now uses the selected dragon's species animation, independent of its current elemental gate color:
- Cinderclaw: rising roar, open jaw and wide wings (0.95s).
- Coralith: swimming wave through wings and tail (1.30s).
- Viperthorn: low coil, head feint and curled tail (1.10s).
- Cragback: grounded brace with compressed body and folded wings (1.20s).
- Glacierveil: gentle lift and symmetrical wing fan (1.40s).
- Voltwing: quick alternating bank and wing beats (0.75s).
- Ironforge: two charging beats followed by wing deployment (1.15s).
- Nyx Eclipse: crescent body sweep, asymmetric wings and trailing tail (1.35s).
These poses blend back to the existing run/flight animation and affect only the rendered rig. Loss of power and defeat cancel the burst. No extra primitive attachments are spawned. Runtime, Editor and Android C# compilation passed; visual Play Mode review remains pending.


## Elemental body markings
Each species now has a curved skin marking on its rear torso and flanks: flame, water drop, venom leaf, mountain, snowflake, lightning bolt, mechanical gear/core, or crescent. Thin mesh strokes conform to the belly sphere and inherit its animation and permanent growth. Frost and lightning dragons use dark ink for contrast; the other species use warm ivory. Markings identify the selected species and remain stable when temporary gates change combat element. No floating ornaments are added. C# compilation passed; placement and readability still require visual review in Unity.


## Hold-to-move controls
The runner waits after the countdown until a touch begins on the play area. Holding advances it; horizontal dragging steers it. Release/cancel stops forward and sideways movement immediately and clears pending lane drift. Desktop testing uses a held left mouse button. UI-originated touches do not start movement, and the owning finger is tracked during multi-touch. Pause, focus loss and app suspension clear the hold. The HUD shows a hold/drag hint while waiting; locomotion animation follows actual movement. Boss combat remains automatic. Editor-only validation input supports automated hold/release checks and is absent from Android builds. Runtime, Editor and Android C# compilation passed; Play Mode hold/release checks were added but cannot run with the inactive Unity license.


## Five-second boss encounters
Every end-of-level boss encounter resolves within five active seconds. Both sides deal triple damage, with the player first striking at 0.65 seconds and the boss at 1.1 seconds. Victory or defeat can happen sooner; a surviving boss at the deadline causes defeat. The HUD shows remaining seconds. The timer and attack schedule use active encounter elapsed time, so pause does not consume time or skip attacks. A rewarded boss revive begins a fresh five-second attempt with existing remaining boss health. Editor/runtime/Android compilation passed. The standalone full-power starter balance simulation covers all ten early bosses under this deadline; Unity Play Mode review remains pending.


## Loss rewards and slower coin earnings
Defeat banks collected run coins and persists the save; its result screen shows coins saved. A per-run banked total prevents duplicate payouts across revival, repeated loss and a later victory. Winning adds only the unbanked pickups and completion bonus. Track pickups pay 1 coin instead of 5; completion bonuses pay one quarter of the authored level reward (20 and 23 for the first two levels). Existing wallet balances, purchased packs, upgrade prices and combat power are unchanged. Runtime/Editor/Android compilation and standalone reward arithmetic checks passed. Play Mode tests for loss persistence and revival payouts were added but remain pending the Unity license.


Native Play Games client and backend implemented; source changes exported to candycraze/dev under MonsterMergeClient and MonsterMergeServer. Numeric Play Games project ID remains required before building. See MonsterMergeClient/README.md in that repo.

## Menu dragon portraits and evolution flow
Home, collection, shop and upgrade previews now use the detailed anime fantasy dragon atlas at Resources/Dragons/AnimeDragonAtlas.png. Each species has a distinct portrait. On upgrade, the game shows the previous and new stage, the dragon's unique visual change and power, then lets the player start the next level with the existing 3-2-1 countdown.

## Level challenges
Every level receives its own deterministic challenge layout and title, including coin weaves, spike slaloms, meteor dodges, rotor runs, mutation gates, fire crossings, moving walls, power pickups, rockfalls and mixed storm courses. Replaying a level keeps its challenge pattern consistent.
