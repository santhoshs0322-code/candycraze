# Monster Merge on candycraze/dev

Render service `srv-dact4emk1f9s739tdopg` remains linked to `santhoshs0322-code/candycraze`, branch `dev`, repository root, root `Dockerfile`. The image now runs this folder; no auth_server repository is needed. Candycraze/Server and Unity application sources are preserved. The production CandyCraze endpoint at 13.206.105.80 is not changed by this commit; do not deploy this dev Dockerfile to that production service.

Required Render environment:
- MONGODB_URI: existing cluster connection secret. All collections in this service use MONSTER_MONGODB_DATABASE (default monstermerge), regardless of the URI's default database or the old MONGODB_DATABASE=candycraze setting.
- MONSTER_GOOGLE_CLIENT_ID and MONSTER_GOOGLE_CLIENT_SECRET: Google OAuth WEB client credentials linked as the Monster Merge Play Games server credential.
- JWT_SECRET: a stable random secret at least 32 characters long.
- PUBLIC_URL=https://candycraze.onrender.com (also provided by Docker).
- MONSTER_MONGODB_DATABASE=monstermerge (also provided by Docker).

In that Google OAuth client, register https://candycraze.onrender.com/auth/google/callback as an authorized redirect URI.

For Google Play purchases, set MONSTER_PLAY_ENABLED=true, MONSTER_PLAY_SERVICE_ACCOUNT_EMAIL and MONSTER_PLAY_SERVICE_ACCOUNT_PRIVATE_KEY. Existing CandyCraze Play credentials/enable flags are not automatically reused. These credentials must have access to Monster Merge's Play package com.Gamixtv.monstermerge and products starter, adventurer, champion, legend. Keep billing disabled until those settings match the actual Play app.

Updating render.yaml does not guarantee existing dashboard environment variables change: review them on the existing service. Never commit secret values.

Expected startup: Monster Merge test service listening on <port>.
GET /health returns service=monstermerge and apps=[monstermerge]. GET /payments/health reports google-play. POST /auth/monster/start with JSON {} returns loginId, pollToken and authorizationUrl. Do not log or share the returned polling secret. Cloud saves use /auth/monster/profile; purchases use /api/billing/account, /wallet and /verify.

Validation: npm run check and npm run test:monster-play from this folder. Tests use temporary local MongoDB (MONGOD_BINARY can override its executable location) and fake Google verification; they do not charge money.

## OAuth credential separation
Monster Merge reads only MONSTER_GOOGLE_CLIENT_ID and MONSTER_GOOGLE_CLIENT_SECRET. It does not fall back to CandyCraze's GOOGLE_CLIENT_ID or GOOGLE_CLIENT_SECRET. The Android OAuth client is configured with Monster Merge's package and signing SHA-1 and has no client secret. For the planned native Play Games flow, the server credentials must be the associated Web application client used for server authorization codes. Native client and server integration is now included. See ../MonsterMergeClient/README.md for Play Games setup and the required new Android build.

## Native login endpoints
GET /auth/monster/config exposes only the public server OAuth client ID. POST /auth/monster/play-games accepts authorizationCode, exchanges it server-side, retrieves the Play Games player identity, and returns the app session. Client-supplied player IDs are not trusted. Existing browser endpoints remain for older builds. Native profiles do not automatically merge with prior OpenID profiles. Native login does not require the browser redirect URI; the legacy browser endpoint still does.

## Purchases disabled after successful sign-in
Sign-in does not enable billing. If /payments/health returns paymentsEnabled:false, set MONSTER_PLAY_ENABLED=true in the Render service after configuring MONSTER_PLAY_SERVICE_ACCOUNT_EMAIL and MONSTER_PLAY_SERVICE_ACCOUNT_PRIVATE_KEY with access to Monster Merge's Play package. Activate the starter, adventurer, champion and legend one-time products and their buy options in Play Console. The wallet endpoint reports paymentsEnabled for client UI; it continues to return balances when checkout is disabled. No secret values or dashboard settings are changed by a source push.
