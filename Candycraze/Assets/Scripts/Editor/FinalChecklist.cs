// ============================================================
// FinalChecklist.cs  (EDITOR ONLY)
// CandyCraze → Run Final Checklist
// Validates the project is ready for Google Play submission.
// ============================================================
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Text;

namespace CandyCraze.Editor
{
    public static class FinalChecklist
    {
        [MenuItem("CandyCraze/Run Final Checklist")]
        public static void RunChecklist()
        {
            var pass = new StringBuilder();
            var warn = new StringBuilder();
            var fail = new StringBuilder();

            // ── Package ID ───────────────────────────────────
            string pkg = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (!string.IsNullOrEmpty(pkg) && !pkg.Contains("com.unity"))
                pass.AppendLine($"✓ Package ID: {pkg}");
            else
                fail.AppendLine($"✗ Package ID not set (current: {pkg})");

            // ── Version ──────────────────────────────────────
            if (!string.IsNullOrEmpty(PlayerSettings.bundleVersion))
                pass.AppendLine($"✓ Version: {PlayerSettings.bundleVersion}");
            else
                fail.AppendLine("✗ Version not set");

            // ── Backend ──────────────────────────────────────
            var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
            if (backend == ScriptingImplementation.IL2CPP)
                pass.AppendLine("✓ IL2CPP scripting backend");
            else
                fail.AppendLine($"✗ Scripting backend is {backend} — must be IL2CPP");

            // ── ARM64 ────────────────────────────────────────
            var arch = PlayerSettings.Android.targetArchitectures;
            if ((arch & AndroidArchitecture.ARM64) != 0)
                pass.AppendLine("✓ ARM64 enabled");
            else
                fail.AppendLine("✗ ARM64 not enabled — required by Google Play");

            // ── Min SDK ──────────────────────────────────────
            if ((int)PlayerSettings.Android.minSdkVersion >= 24)
                pass.AppendLine($"✓ Min SDK: {PlayerSettings.Android.minSdkVersion}");
            else
                warn.AppendLine($"⚠ Min SDK {PlayerSettings.Android.minSdkVersion} — recommend API 24+");

            // ── Portrait ─────────────────────────────────────
            if (PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait)
                pass.AppendLine("✓ Portrait orientation");
            else
                warn.AppendLine("⚠ Orientation is not Portrait");

            // ── AAB ──────────────────────────────────────────
            if (EditorUserBuildSettings.buildAppBundle)
                pass.AppendLine("✓ Building AAB (Google Play format)");
            else
                warn.AppendLine("⚠ Building APK — switch to AAB for Google Play");

            // ── Scenes ───────────────────────────────────────
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length >= 4)
                pass.AppendLine($"✓ {scenes.Length} scenes in Build Settings");
            else
                fail.AppendLine($"✗ Only {scenes.Length} scenes — need 4 (Bootstrap,MainMenu,LevelMap,Game)");

            // ── GameConfig ───────────────────────────────────
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
            if (config != null)
                pass.AppendLine($"✓ GameConfig found ({config.TotalLevels} levels, {config.GemDefinitions?.Length ?? 0} gems)");
            else
                fail.AppendLine("✗ GameConfig.asset not found in Resources/");

            // ── Keystore ─────────────────────────────────────
            if (!string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
                pass.AppendLine("✓ Keystore configured");
            else
                warn.AppendLine("⚠ Keystore not set — required for release build");

            // ── Splash ───────────────────────────────────────
            if (PlayerSettings.SplashScreen.show)
                pass.AppendLine("✓ Splash screen enabled");
            else
                warn.AppendLine("⚠ Splash screen disabled");

            // ── Build report ─────────────────────────────────
            var report = new StringBuilder();
            report.AppendLine("═══ CANDYCRAZE RELEASE CHECKLIST ═══\n");

            if (pass.Length  > 0) { report.AppendLine("PASSED:"); report.AppendLine(pass.ToString()); }
            if (warn.Length  > 0) { report.AppendLine("WARNINGS:"); report.AppendLine(warn.ToString()); }
            if (fail.Length  > 0) { report.AppendLine("FAILED:"); report.AppendLine(fail.ToString()); }

            int failCount = fail.Length > 0 ? fail.ToString().Split('✗').Length - 1 : 0;
            int warnCount = warn.Length > 0 ? warn.ToString().Split('⚠').Length - 1 : 0;

            string title  = failCount > 0 ? "❌ Not Ready" : warnCount > 0 ? "⚠ Almost Ready" : "✅ Ready!";

            Debug.Log(report.ToString());

            EditorUtility.DisplayDialog(
                $"Checklist — {title}",
                report.ToString(),
                "OK");
        }
    }
}
#endif
