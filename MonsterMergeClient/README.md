# Monster Merge native sign-in client changes

This folder contains the changed Unity files to overlay onto the existing Monster Merge AI Evolution Unity project, not a complete standalone Unity project. These changes are already applied in the local Monster Merge workspace. The CandyCraze Unity app is not modified.

The new client uses Play Games Services v2 (22.0.0) and a native Java bridge. No browser is opened. It fetches the public Web client ID from /auth/monster/config, obtains a one-time server authorization code, then POSTs it to /auth/monster/play-games. The server verifies the player and issues the existing Monster Merge session format. UI displays the verified gamer name; email is not required.

## Required Play Console setup before building
1. Set up Play Games Services for Monster Merge, using its Google Cloud project.
2. Link an Android OAuth credential for package com.Gamixtv.monstermerge and the correct Play signing SHA-1.
3. Link a Web application OAuth client as the Play Games server credential. Render MONSTER_GOOGLE_CLIENT_ID and MONSTER_GOOGLE_CLIENT_SECRET must match this Web client. No secret is embedded in the app.
4. Put the numeric Play Games Services project/game ID in Assets/MonsterMerge/Resources/NativeGoogleConfiguration.json, field gameServicesProjectId. The configured ID is 474055571663, supplied for Monster Merge. The build hook validates it and writes the Android resource.
5. Enable Play Games test access for testers and release the configuration as appropriate. Build and install the new Monster Merge APK/AAB through its testing track. Merely deploying the backend cannot update an already-installed APK's login behavior.

The build hook writes the Android app-ID resource and checks the package. UI cancel, timeout and late callbacks are handled without opening a browser. Guest play remains available.

Existing browser/OpenID profiles and receipts remain stored; Play Games accounts use a separate player namespace. No unsafe matching by name/email or automatic account migration is performed.

Validation: Java bridge compilation against cached Google Play SDK 22.0.0, Unity runtime/Editor/Android C# compilation, and 15 backend tests passed. The numeric game ID is configured. Full Android build and on-device sign-in still require verification of the linked OAuth clients, Play Games tester access, and an active Unity license.
