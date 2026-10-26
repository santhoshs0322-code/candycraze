# Monster Merge on candycraze/dev

Render service `srv-dact4emk1f9s739tdopg` remains linked to `santhoshs0322-code/candycraze`, branch `dev`, repository root, root `Dockerfile`. The image now runs this folder; no auth_server repository is needed. Candycraze/Server and Unity application sources are preserved. The production CandyCraze endpoint at 13.206.105.80 is not changed by this commit; do not deploy this dev Dockerfile to that production service.

Required Render environment:
- MONGODB_URI: existing cluster connection secret. All collections in this service use MONSTER_MONGODB_DATABASE (default monstermerge), regardless of the URI's default database or the old MONGODB_DATABASE=candycraze setting.
- MONSTER_GOOGLE_CLIENT_ID and MONSTER_GOOGLE_CLIENT_SECRET: Google OAuth WEB client credentials for Monster Merge browser login.
- JWT_SECRET: a stable random secret at least 32 characters long.
- PUBLIC_URL=https://candycraze.onrender.com (also provided by Docker).
- MONSTER_MONGODB_DATABASE=monstermerge (also provided by Docker).

In that Google OAuth client, register https://candycraze.onrender.com/auth/google/callback as an authorized redirect URI.

For Google Play purchases, set MONSTER_PLAY_ENABLED=true, MONSTER_PLAY_SERVICE_ACCOUNT_EMAIL and MONSTER_PLAY_SERVICE_ACCOUNT_PRIVATE_KEY. Existing CandyCraze Play credentials/enable flags are not automatically reused. These credentials must have access to Monster Merge's Play package com.yourstudio.monstermerge and products starter, adventurer, champion, legend. Keep billing disabled until those settings match the actual Play app.

Updating render.yaml does not guarantee existing dashboard environment variables change: review them on the existing service. Never commit secret values.

Expected startup: Monster Merge test service listening on <port>.
GET /health returns service=monstermerge and apps=[monstermerge]. GET /payments/health reports google-play. POST /auth/monster/start with JSON {} returns loginId, pollToken and authorizationUrl. Do not log or share the returned polling secret. Cloud saves use /auth/monster/profile; purchases use /api/billing/account, /wallet and /verify.

Validation: npm run check and npm run test:monster-play from this folder. Tests use temporary local MongoDB (MONGOD_BINARY can override its executable location) and fake Google verification; they do not charge money.

## OAuth credential separation
Monster Merge reads only MONSTER_GOOGLE_CLIENT_ID and MONSTER_GOOGLE_CLIENT_SECRET. It does not fall back to CandyCraze's GOOGLE_CLIENT_ID or GOOGLE_CLIENT_SECRET. The Android OAuth client is configured with Monster Merge's package and signing SHA-1 and has no client secret. For the planned native Play Games flow, the server credentials must be the associated Web application client used for server authorization codes. This environment-name change does not itself replace the current Unity browser login; native client and server integration is still required.
