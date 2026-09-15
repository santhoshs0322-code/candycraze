# Play Store sign-in and one-time products

## Sign-in after installing from Google Play

The reported message came from Play Games authentication, before the server authorization code or cloud-save request. The previous message combined cancellation and internal errors. The updated app distinguishes these and logs the returned status. The exact underlying Google error is logged by the Play Games plugin as `Authentication failed` in Android Logcat.

In Play Console, open Play Games Services > Setup and management > Configuration:

1. Check the Android credential is for package `com.gamixtv.Candycraze` (including case), in game project `144163133862`.
2. Open App integrity / App signing and copy the SHA-1 under **App signing key certificate**, not the upload key. Check it against the SHA-1 on the Android OAuth client linked to this Play Games credential.
3. The previously supplied fingerprint was `99:A4:79:B7:35:EB:FC:70:20:FD:CF:7E:A6:16:BE:D6:1C:53:D7:6D`. Use it only if it still matches the app signing certificate shown in Console. Add a separate credential for another signing certificate when necessary; preserve credentials for existing supported builds.
4. Publish the Play Games configuration changes. If the game configuration is unpublished, add the signing-in account as a Play Games tester; membership in a closed app testing track alone is not sufficient.
5. For cloud saves, add/check the Game server credential using the Web application OAuth client `144163133862-78cv89qoiv4stk446c606586ro04o49q.apps.googleusercontent.com`, which is currently configured in Unity. This must belong to the same game project. Set the deployed server's GOOGLE_CLIENT_ID to this ID and GOOGLE_CLIENT_SECRET to its corresponding secret. Keep the secret only on the server.

These Console settings cannot be verified or changed from this local repository. If authentication still fails, capture the plugin's `Authentication failed` Logcat line from the installed Play build; do not share authorization codes or tokens.

Official guidance: https://codelabs.developers.google.com/pgs-workshop-setup-unity

## Unblock one-time product creation

The Android manifest declares `com.android.vending.BILLING`, and the custom main Gradle template includes `com.android.billingclient:billing:8.0.0`. The library supplies its version metadata through manifest merging; do not add a fake version marker manually. The previous permission-only build was reported as AIDL by Play Console. Build a new AAB with a version code greater than 39 (and greater than any already uploaded code), then upload it. The already-uploaded build does not change from a Git update.

After Google processes that build, go to Monetize with Play > Products > One-time products. Create each product, add a Buy purchase option, set regional availability and pricing, then activate it. The product ID must exactly match the eventual checkout catalog. Decide final IDs before activating products: the current IAPProductIDs still use `com.yourcompany` placeholders.

Planned packs from ShopManager:

| Pack | Grant | Suggested final product ID |
| --- | --- | --- |
| Small | 500 crystals | coins_small |
| Medium | 1,200 crystals | coins_medium |
| Large | 2,800 crystals | coins_large |

Important: real checkout is not implemented. Packages/manifest.json has no Unity Purchasing dependency, and IAPManager is explicitly a stub that fails purchases on mobile. Adding permission and creating products does not implement payment processing. Keep paid sales disabled until a supported billing integration, store prices, pending/cancelled purchase handling, purchase verification, durable duplicate-safe grants, consumption/acknowledgement, and recovery are implemented. Validate payments with Play license testers before offering them publicly.

Official product setup: https://support.google.com/googleplay/android-developer/answer/16430488

## Version 39 symbol warnings

Release minification is disabled in this project, so there is no R8/ProGuard mapping file to upload. The deobfuscation warning does not block this non-obfuscated build.

For native symbols, use CandyCraze > Build Release AAB, which enables Public Android symbols, or set Create symbols.zip to Public in Android Build Settings before building normally. Upload the symbols archive generated alongside that exact AAB to its version's native debug symbols section in App Bundle Explorer. Do not use a symbols archive from version 34, 35, or 36 for version 39 or a later build. Symbols help diagnose crashes; they do not fix crashes themselves.

Billing version policy: https://developer.android.com/google/play/billing/deprecation-faq
