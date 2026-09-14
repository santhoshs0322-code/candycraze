// Prevent accidentally uploading a debug-signed or misconfigured Android build.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CandyCraze.Editor
{
    public sealed class GooglePlayReleaseValidator : IPreprocessBuildWithReport
    {
        private const string ExpectedPackageName = "com.gamixtv.Candycraze";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                return;

            if (!EditorUserBuildSettings.buildAppBundle)
                throw new BuildFailedException(
                    "Google Play releases must be built as an Android App Bundle (.aab), not an APK.");

            string packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (!string.Equals(packageName, ExpectedPackageName, StringComparison.Ordinal))
                throw new BuildFailedException(
                    $"Android package name must be '{ExpectedPackageName}' for this Play Games project. Current value: '{packageName}'.");

            if (!PlayerSettings.Android.useCustomKeystore)
                throw new BuildFailedException(
                    "Custom Keystore is disabled. A Play upload must not use Unity's Android Debug certificate.");

            string keyStore = PlayerSettings.Android.keystoreName;
            if (string.IsNullOrWhiteSpace(keyStore))
                throw new BuildFailedException("No Android keystore is selected.");

            string resolvedKeyStore = keyStore.StartsWith("{inproject}: ", StringComparison.Ordinal)
                ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, keyStore.Substring("{inproject}: ".Length))
                : keyStore;
            if (!File.Exists(resolvedKeyStore))
                throw new BuildFailedException($"Android keystore was not found: {resolvedKeyStore}");

            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName))
                throw new BuildFailedException("No Android key alias is selected.");

            // Unity otherwise falls back to its debug certificate when signing
            // information is incomplete. Passwords are kept by Unity outside
            // ProjectSettings, so this check does not expose them in source control.
            if (string.IsNullOrWhiteSpace(PlayerSettings.Android.keystorePass) ||
                string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasPass))
                throw new BuildFailedException(
                    "Keystore or key-alias password is missing. Enter both in Player Settings > Publishing Settings before building.");

            if (PlayerSettings.Android.bundleVersionCode < 1)
                throw new BuildFailedException("Android version code must be at least 1.");
        }
    }
}
#endif
