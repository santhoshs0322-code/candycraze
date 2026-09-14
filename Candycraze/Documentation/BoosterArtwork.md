# Booster artwork and gameplay update

Five transparent original booster sprites were created using the imagegen skill with the built-in image-generation tool. Production assets: `Assets/Resources/CandySprites/Booster_*.png`. Unity imports them at 512px. Source originals remain in the Codex generated-images folder.

## Gameplay rules

- Four horizontally: vertical striped candy; four vertically: horizontal striped candy.
- Five in a straight line: color bomb. L/T/cross without a straight five: wrapped candy.
- New power placement prefers the dragged candy's destination in the matched group, then the other swapped candy. It subsequently falls with gravity.
- Stripes/wrapped candies require a matching move or another power to activate. Color bombs can swap with any normal candy. Color bombs do not join normal color matches.
- Stripe + stripe: one row and column through the dragged candy's destination.
- Stripe + wrapped: three rows and three columns.
- Wrapped: 3 x 3 blast, refill, second 3 x 3 blast. Wrapped + wrapped: 5 x 5 twice.
- Color bomb + stripe/wrapped: convert the partner's color into those powers, then activate them. Stripes receive randomized directions.
- Color bomb + color bomb: whole board.
- Every accepted swap consumes one move; rejected swaps and boosters consume none. No free color bombs are injected when starting/shuffling.
- Each profile receives one of each booster once; grant marker persists to the cloud. Guest reset starts a fresh guest profile as requested. Existing inventory is preserved and receives one additional use.

All existing level objectives remain active and are shown separately with icons and remaining counts. This is a CandyCraze ruleset for the supported three power types, not a claim to replicate every Candy Crush mechanic. References: [King special candies](https://candycrush.zendesk.com/hc/en-us/articles/211939685-Creating-and-combining-Special-Candies), [King stripe directions](https://candycrush.zendesk.com/hc/en-us/articles/13939175958941-Learn-all-about-the-Striped-Candy).

## Exact generation prompts

Verification: 28 Unity EditMode tests passed, the isolated Play Mode harness exercised all five boosters and the stripe pair through real board coroutines, and Android C# compilation passed. The server test verifies the starter-grant flag survives cloud serialization after inventory is consumed. No phone was connected, so no physical-device or Play release test was performed. Updated previews are in `Documentation/Previews/game.png`, `power-guide.png` and `combo-board.png`.

Backend deployment must include `Server/profile.js` from this update to preserve `StarterBoostersGranted` in cloud saves. Build a new Android version to deliver the gameplay and artwork updates to installed users.

### Booster_Hammer.png

Use case: stylized-concept. Asset type: original CandyCraze mobile match-3 booster icon. A single centered isolated glossy 3D candy-toy object on genuinely transparent alpha background, generous 12% padding, square composition. Rich pink, purple, aqua and gold, broad soft highlights, rounded edges, clear readable silhouette at 64px. No scene, no button background, no borders, no watermark, no floating particles. Subject: a strawberry pink lollipop hammer, chunky striped cylindrical candy hammer head, short gold handle, diagonal playful angle. No text.

### Booster_Blast.png

Use case: stylized-concept. Asset type: original CandyCraze mobile match-3 booster icon. A single centered isolated glossy 3D candy-toy object on genuinely transparent alpha background, generous 12% padding, square composition. Rich pink, purple, aqua and gold, broad soft highlights, rounded edges, clear readable silhouette at 64px. No scene, no button background, no borders, no watermark, no floating particles. Subject: a glossy golden four-direction cross arrow bursting from a pink candy center, bold equal upward downward left right arrow heads. No text.

### Booster_Shuffle.png

Use case: stylized-concept. Asset type: original CandyCraze mobile match-3 booster icon. A single centered isolated glossy 3D candy-toy object on genuinely transparent alpha background, generous 12% padding, square composition. Rich pink, purple, aqua and gold, broad soft highlights, rounded edges, clear readable silhouette at 64px. No scene, no button background, no borders, no watermark, no floating particles. Subject: two chunky curved aqua and purple arrows forming a circular shuffle symbol around two tiny candy beads. No text.

### Booster_Moves.png

Use case: stylized-concept. Asset type: original CandyCraze mobile match-3 booster icon. A single centered isolated glossy 3D candy-toy object on genuinely transparent alpha background, generous 12% padding, square composition. Rich pink, purple, aqua and gold, broad soft highlights, rounded edges, clear readable silhouette at 64px. No scene, no button background, no borders, no watermark, no floating particles. Subject: a plump pink heart-shaped candy token with gold rim, large clearly readable white text '+5' on the center. No other text.

### Booster_Color.png

Use case: stylized-concept. Asset type: original CandyCraze mobile match-3 booster icon. A single centered isolated glossy 3D candy-toy object on genuinely transparent alpha background, generous 12% padding, square composition. Rich pink, purple, aqua and gold, broad soft highlights, rounded edges, clear readable silhouette at 64px. No scene, no button background, no borders, no watermark, no floating particles. Subject: a glossy chocolate sphere coated with rainbow candy sprinkles with a gold magic wand diagonally behind it, bold compact silhouette. No text.
