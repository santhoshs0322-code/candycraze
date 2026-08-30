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

            PlayerSettings.bundleVersion     = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;

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

            Debug.Log("[AndroidBuildSetup] ✓ Android settings configured.");

            EditorUtility.DisplayDialog(
                "Android Build Ready",
                "Android Player Settings configured!\n\n" +
                "Package: com.yourcompany.candycraze\n" +
                "Version: 1.0.0 (code 1)\n" +
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
