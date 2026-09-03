// ============================================================
// AndroidBuildSetup.cs  (EDITOR ONLY)
// CandyCraze → Setup Android Build
// Configures all Android Player Settings automatically.
// ============================================================
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEditor.Build;

namespace CandyCraze.Editor
{
    public static class AndroidBuildSetup
    {
        [MenuItem("CandyCraze/Setup Android Build Settings")]
        public static void SetupAndroid()
        {
            // ── Switch Platform ───────────────────────────────
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android, BuildTarget.Android);

            // ── Player Settings ───────────────────────────────
            PlayerSettings.companyName  = "YourCompany";
            PlayerSettings.productName  = "CandyCraze";

            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.Android, "com.yourcompany.candycraze");

            PlayerSettings.bundleVersion     = "2.0.1";
            // NOTE: version code is auto-incremented by Build Release AAB —
            // don't hard-set it here or it would reset the counter backward.
            // Ensure it's at least 5 (codes 1-4 already used on Play).
            if (PlayerSettings.Android.bundleVersionCode < 5)
                PlayerSettings.Android.bundleVersionCode = 5;

            // ── Screen ───────────────────────────────────────
            PlayerSettings.defaultInterfaceOrientation =
                UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait          = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown= false;
            PlayerSettings.allowedAutorotateToLandscapeLeft     = false;
            PlayerSettings.allowedAutorotateToLandscapeRight    = false;

            // ── Aspect ratio — support 16:9 through tall 21.6:9 phones ─
            // Without a high max aspect, tall phones (20:9, 21:9) get
            // letterboxed with black bars. 2.4 lets the game fill any
            // modern phone screen edge-to-edge. (16:9 and wider is the
            // supported floor by default, so no min needs setting.)
            PlayerSettings.Android.maxAspectRatio = 2.4f;      // up to ~21.6:9

            // ── Android API ──────────────────────────────────
            PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel24; // Android 7
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33; // Android 13

            // ── IL2CPP + ARM64 only (iQOO and modern Android requirement) ─
            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // ── Graphics ─────────────────────────────────────
            PlayerSettings.Android.blitType = AndroidBlitType.Auto;
            PlayerSettings.gpuSkinning      = true;

            // ── Quality ──────────────────────────────────────
            QualitySettings.vSyncCount  = 0;
            Application.targetFrameRate = 60;

            // ── Splash Screen ────────────────────────────────
            PlayerSettings.SplashScreen.show          = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            // ── Internet Access ──────────────────────────────
            PlayerSettings.Android.forceInternetPermission = false;

            // ── Build type ───────────────────────────────────
            EditorUserBuildSettings.buildAppBundle = true; // AAB for Google Play

            // ── Native debug symbols (fixes the "no debug symbols" warning) ─
            // Bundles a symbols.zip alongside the AAB so Play Console can
            // symbolicate native (IL2CPP) crashes/ANRs.
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;

            Debug.Log("[AndroidBuildSetup] ✓ Android settings configured.");

            EditorUtility.DisplayDialog(
                "Android Build Ready",
                "Android Player Settings configured!\n\n" +
                "Package: com.yourcompany.candycraze\n" +
                "Version: 2.0.1 (code auto-increments per build)\n" +
                "Min SDK: Android 7 (API 24)\n" +
                "Target SDK: Android 13 (API 33)\n" +
                "Backend: IL2CPP\n" +
                "Arch: ARM64 + ARMv7\n" +
                "Output: AAB (Google Play)\n\n" +
                "⚠ Next: Set up your Keystore in\n" +
                "Player Settings → Publishing Settings\n\n" +
                "See RELEASE_BUILD.md for full instructions.",
                "Got it");
        }

        [MenuItem("CandyCraze/Build Release AAB")]
        public static void BuildReleaseAAB()
        {
            // ── Auto-increment version code so every upload is unique ─────
            // Google Play rejects reused codes; this guarantees a fresh one
            // on every build without manual edits.
            int newCode = PlayerSettings.Android.bundleVersionCode + 1;
            PlayerSettings.Android.bundleVersionCode = newCode;
            Debug.Log($"[AndroidBuildSetup] Auto-incremented version code to {newCode}.");

            // Verify keystore is set
            if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            {
                EditorUtility.DisplayDialog(
                    "Keystore Missing",
                    "Please set up your keystore first:\n\n" +
                    "Player Settings → Publishing Settings → Keystore\n\n" +
                    "See RELEASE_BUILD.md for instructions.",
                    "OK");
                return;
            }

            // ── CRITICAL: force App Bundle (AAB) output ──────────────
            // Without this, Unity builds a plain APK and just names it
            // ".aab", which Play Console rejects with a vague upload error.
            EditorUserBuildSettings.buildAppBundle = true;
            // Make sure we're actually on the Android build target.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, BuildTarget.Android);

            string outputPath = "Builds/CandyCraze.aab";

            // Ensure output folder
            if (!System.IO.Directory.Exists("Builds"))
                System.IO.Directory.CreateDirectory("Builds");

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[]
                {
                    "Assets/Scenes/Bootstrap.unity",
                    "Assets/Scenes/MainMenu.unity",
                    "Assets/Scenes/LevelMap.unity",
                    "Assets/Scenes/Game.unity",
                },
                locationPathName = outputPath,
                target           = BuildTarget.Android,
                options          = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorUtility.DisplayDialog(
                    "Build Succeeded!",
                    $"AAB built successfully!\n\n" +
                    $"Version code: {PlayerSettings.Android.bundleVersionCode}\n" +
                    $"Output: {outputPath}\n" +
                    $"Size: {summary.totalSize / 1048576:F1} MB\n\n" +
                    "Upload this file to Google Play Console\n" +
                    "under Internal Testing → Create Release.",
                    "Done");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Build Failed",
                    $"Build failed with {summary.totalErrors} error(s).\n\n" +
                    "Check the Console for details.",
                    "OK");
            }
        }
    }
}
#endif
