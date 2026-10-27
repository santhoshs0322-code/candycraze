using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace MonsterMerge.Editor
{
    public sealed class AppIconBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        [MenuItem("Monster Merge/Apply Dragon App Icon")]
        public static void Apply()
        {
            const string path="Assets/MonsterMerge/Art/AppIcon.png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(!icon)throw new BuildFailedException("Monster Merge app icon is missing: "+path);
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{icon});
            foreach(var kind in new[]{AndroidPlatformIconKind.Legacy,AndroidPlatformIconKind.Round,AndroidPlatformIconKind.Adaptive})
            {
                var slots=PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android,kind);
                foreach(var slot in slots)
                {
                    if(kind==AndroidPlatformIconKind.Adaptive)slot.SetTextures(new[]{icon,icon});
                    else slot.SetTexture(icon);
                }
                PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android,kind,slots);
            }
            AssetDatabase.SaveAssets();
        }
        public void OnPreprocessBuild(BuildReport report)
        {
            if(report.summary.platform==BuildTarget.Android)Apply();
        }
    }
}
