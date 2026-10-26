using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace MonsterMerge.Editor
{
    // Run after configuration checks, before Unity writes the Android manifest.
    public sealed class AutoBuildVersion : IPreprocessBuildWithReport
    {
        public int callbackOrder => 1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.Android)return;
            string previous=PlayerSettings.bundleVersion;
            string next=NextVersion(previous);
            int code=PlayerSettings.Android.bundleVersionCode;
            if(code<0 || code>=2100000000)
                throw new BuildFailedException("Android build number cannot be incremented within Google Play's supported range.");
            PlayerSettings.bundleVersion=next;
            PlayerSettings.Android.bundleVersionCode=code+1;
            // Persist before building: even a later failed attempt must not reuse this number.
            AssetDatabase.SaveAssets();
            Debug.Log("[Build version] "+previous+" ("+code+") -> "+next+" ("+(code+1)+")");
        }
        public static string NextVersion(string current)
        {
            var parts=(current??"").Split('.');
            if(parts.Length!=3 || !int.TryParse(parts[0],out int major) || major<0 ||
                !int.TryParse(parts[1],out int minor) || minor<0 ||
                !int.TryParse(parts[2],out int patch) || patch<0 || patch==int.MaxValue)
                throw new BuildFailedException("Use a numeric Major.Minor.Patch app version, for example 1.0.0, before building.");
            return major+"."+minor+"."+(patch+1);
        }
    }
}
