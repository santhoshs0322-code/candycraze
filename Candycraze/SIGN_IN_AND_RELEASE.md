# CandyCraze account flow and release checklist

## What changed

- Explicit Google Play Games sign-in on the home screen. The button stays busy until server authentication and progress restore both succeed.
- App sign-out immediately returns to a fresh guest profile: levels 1–3 unlocked, no completed levels/stars/coins/boosters, default settings.
- Guest data and account backups use separate files. The last player's account is never loaded as a guest.
- Full-profile autosaves after rewards, completed levels, inventory/settings changes, every 30 seconds of active gameplay, and on app pause.
- Unsent account progress is retained on the device after sign-out. It is retried when that same verified player signs in and the server revision still matches.
- Home, level map and gameplay use the new candy palette, responsive safe areas, rounded cards, button feedback, progress nodes and result dialogs.

Sign-out ends the CandyCraze session, not the device's Google/Play Games account. Play Games v2 does not expose the old SDK sign-out operation. Changing the Play Games account is done through Play Games settings. This app deliberately does not auto-start cloud login on launch.

## Server deployment (required before releasing this client)

The deleted legacy Backend folder has not been restored. The replacement is in `Candycraze/Server` relative to the Git repository root. The root Dockerfile/render.yaml now point to it.

Configure these **runtime environment variables** on the existing Render service:

- `MONGODB_URI`: your MongoDB connection string.
- `MONGODB_DATABASE`: database name, default `candycraze`.
- `GOOGLE_CLIENT_ID`: the **Web application / game-server** OAuth client ID configured in Play Games and Unity.
- `GOOGLE_CLIENT_SECRET`: that web client's secret; server only.
- `PORT`: supplied by hosting, or 3000 locally.

Never put MongoDB credentials or the client secret in Unity, resources, source control, screenshots, or the AAB.

Deploy using the repository-root Dockerfile, or set the service root directory to `Candycraze/Server` and use its Dockerfile. Do not keep the old `node Backend/server.js` override. The Unity CloudSaveManager URL must match the deployed HTTPS service; currently it is `https://candycraze.onrender.com`.

Check `GET /health` after deployment. A healthy HTTP response is only a connectivity check; use a real Play Games tester account to verify Google authorization and MongoDB persistence.

Behind a reverse proxy, configure `TRUSTED_PROXY_CIDRS` with the actual trusted proxy addresses/ranges so the login rate limiter sees individual clients. Do not trust arbitrary forwarded headers. Without this setting, clients behind the same proxy share the 20-attempt/minute limit. For multiple server instances, use an edge/shared rate limiter.

Google verification exchanges a single-use server authorization code with Google's token endpoint and reads the player from `games/v1/players/me`. A player ID sent by the client is never trusted. Google access tokens, refresh tokens and authorization codes are not stored.

## One player, one document

Collection: `players`. The unique MongoDB `_id` is the **server-verified Play Games player ID**. Login uses an atomic upsert, not an insert for every login.

The single document contains:

- Identity: playerId, displayName, provider.
- Server activity: createdAt, updatedAt, lastLoginAt, loginCount, lastLogoutAt, logoutCount, lastAppVersion.
- Sync: revision, saveCount, lastSavedAt, saveHash.
- Session: a SHA-256 hash of an opaque random token and its expiry; the raw token is never stored in MongoDB.
- Nested `save`: schema version, unlocked level, total stars, coins, lives and regeneration time.
- `save.LevelEntries[]`: one entry per level, completed flag, best stars, high score, attempts, wins, last-played timestamp.
- All five booster inventories, daily reward day/timestamp, claimed reward count.
- Sound/music preferences, last played level, started/won/lost counts, moves, boosters used, active play seconds, save timestamp, app version.

Only app/game activity needed for this profile is tracked; no contacts, location, Google email, or unrelated device data.

A newer login replaces the previous session. Saves require both a valid session and the expected revision. Stale writes cannot overwrite another device's newer progress. Identical upload retries are idempotent.

This is client-reported gameplay persistence, not an anti-cheat or purchase-validation system. Do not treat uploaded coin totals as proof of payment.

## Existing saves and recovery

The old shared local file `candycraze_save.json` and old PlayerPrefs save are left untouched. Because they contain no verified owner, they are not automatically attached to a Google account. New guest data uses `guest_v2.json`; account files use a hash of the verified player ID.

The legacy backend in Git used Google Identity `sub` IDs in a `users` collection. Those IDs are **not interchangeable with Play Games player IDs**. No destructive database migration or guessed account merge is performed. If the live service has legacy users/saves, back it up and establish a server-verified ownership mapping before migrating them into `players`. Do not deploy this replacement over valuable legacy data assuming it has already been migrated.

Offline progress is backed up locally. If another device has already advanced the server revision, the server wins on the next sign-in and the unsent device copy is preserved in a `.conflict` recovery file. It is never automatically merged because merging currencies/rewards can duplicate or lose purchases. The original account backup is also retained during atomic replacement.

## Verification

From `Server`:

```sh
npm ci
npm test
npm audit --omit=dev
```

Tests start a temporary real MongoDB process. Google identity responses are mocked in the tests; real OAuth credentials are not needed. The tests cover atomic one-record login (including concurrent login), account isolation, save/restore, session revocation, stale saves, idempotent retries, malformed data, and server-side Google identity verification.

Unity Test Runner: EditMode → `ProgressTests` and `CandyPowerTests` (12 tests total).

The candy-art update also passes Android and Editor C# compilation. Its six normal candy sprites and four power sprites live in `Assets/Resources/CandySprites`; exact image-generation prompts are in `Documentation/CandyArtwork.md`. L/T/cross matches now create wrapped area bombs, and matched/blasted powers chain once each.

Live service recheck on 2026-09-14: `https://candycraze.onrender.com/health` returned HTTP 404 after an earlier timeout. The replacement server defines that route, so confirm the deployed service/version and base URL before treating live sign-in as verified. No Android device was connected for an end-to-end OAuth test.

`Tools/Verification/CandyVerification.cs` is an optional Play Mode verification harness, kept outside Assets. Run it only in an isolated project copy: copy it into that copy's `Assets/Editor`, set the copy's company name to `CandyCrazeVerification`, and use `-executeMethod CandyVerification.Begin`. It uses its own local profile directory, exercises account switching and the home/map/game flows, and writes screenshots/checks into `VerificationResults`. Never run it in the production project.

## Android closed-test acceptance

Play app-signing SHA-1 supplied by the app owner:
`99:A4:79:B7:35:EB:FC:70:20:FD:CF:7E:A6:16:BE:D6:1C:53:D7:6D`.
Register this fingerprint with the Android OAuth credential for `com.gamixtv.Candycraze` in Play Games configuration. This document records the supplied value; it does not register the credential in Google Console.

Unity's current Play Games Web/game-server client ID is
`144163133862-78cv89qoiv4stk446c606586ro04o49q.apps.googleusercontent.com`.
The backend `GOOGLE_CLIENT_ID` and matching client secret must belong to this Web client.
The SHA-1 is an Android credential field, not a Unity Web client ID or a server secret.

Sign-in can also work for a directly installed debug/release APK: link an additional Android credential for the same package with that APK's actual signing SHA-1, and authorize the tester account. A Play-installed APK uses the Play app-signing certificate instead.
See [Google's certificate and tester troubleshooting](https://developer.android.com/games/pgs/android/troubleshooting#check_the_certificate_fingerprint).

1. Deploy/configure the replacement server, with any required legacy migration handled first.
2. In Play Games, verify the Android package is `com.gamixtv.Candycraze`, the **Play app-signing certificate** SHA-1 is registered, and the Web/game-server client matches Unity and the server. The upload-key certificate alone is not sufficient for Play-installed builds.
3. Let Unity reimport and compile. Keep the launcher manifest fix. Build a new signed AAB with a higher version code; this change does not automatically upload or publish anything.
4. Install from the Play closed-test track. As guest, confirm defaults and normal gameplay.
5. Sign in as A, complete a level, change a preference and claim a reward; wait for “Cloud progress is up to date.”
6. Sign out: verify zero completed levels and defaults. Sign in as A again: verify progress, coins, inventory and settings return.
7. Sign in as a different/new account: verify no A data appears.
8. Retry with network loss, cancelled sign-in, rapid repeated taps and app background/resume.
9. Check MongoDB: A still has exactly one player document after repeated sign-ins/saves.

A C# compilation check or Editor preview is not proof that the signed Play-installed AAB and live OAuth credentials are working.
