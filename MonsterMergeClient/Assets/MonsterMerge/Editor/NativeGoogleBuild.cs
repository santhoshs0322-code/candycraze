using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace MonsterMerge.Editor
{
    public sealed class NativeGoogleBuild : IPreprocessBuildWithReport
    {
        [Serializable] sealed class Configuration { public string gameServicesProjectId; }
        public int callbackOrder => -100;
        public void OnPreprocessBuild(BuildReport report)
        {
            if(report.summary.platform!=BuildTarget.Android)return;
            var config=JsonUtility.FromJson<Configuration>(File.ReadAllText("Assets/MonsterMerge/Resources/NativeGoogleConfiguration.json"));
            if(config==null||!Regex.IsMatch(config.gameServicesProjectId??"",@"^[1-9][0-9]{4,20}$"))
                throw new BuildFailedException("Set Monster Merge's numeric Play Games Services project ID in Assets/MonsterMerge/Resources/NativeGoogleConfiguration.json before building native sign-in.");
            File.WriteAllText("Assets/Plugins/Android/MonsterGames.androidlib/res/values/strings.xml",
                "<resources><string name=\"monster_play_games_id\" translatable=\"false\">"+config.gameServicesProjectId+"</string></resources>");
            if(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)!="com.Gamixtv.monstermerge")
                throw new BuildFailedException("Monster Merge's Play Console package is com.Gamixtv.monstermerge. Check Android Player Settings.");
        }
    }
}
