#if UNITY_EDITOR
// Run only in an isolated project copy, never in a player's working project.
// unity -batchmode -projectPath <copy> -executeMethod CandyVerification.Begin -logFile <log>
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CandyCraze;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CandyVerification
{
    private static double next;
    private static string Output => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "VerificationResults");
    static CandyVerification()
    {
        next = EditorApplication.timeSinceStartup + 4;
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, stack, type) => {
            if (SessionState.GetBool("CandyVerifyActive", false) && (type == LogType.Exception || type == LogType.Error))
                File.AppendAllText(Path.Combine(Output, "runtime-errors.txt"), message + "\n" + stack + "\n");
        };
    }
    public static void Begin()
    {
        if (PlayerSettings.companyName != "CandyCrazeVerification") throw new Exception("Refusing to run outside isolated verification project.");
        Directory.CreateDirectory(Output);
        SetGameSize();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        SessionState.SetInt("CandyVerifyStage", 1);
        SessionState.SetBool("CandyVerifyActive", true);
        EditorApplication.isPlaying = true;
    }
    private static void SetGameSize()
    {
        var assembly = typeof(Editor).Assembly;
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(single, new[] { Enum.Parse(groupType,"Standalone") });
        var sizeType = assembly.GetType("UnityEditor.GameViewSize");
        var kind = assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, new[] { Enum.Parse(kind,"FixedResolution"), (object)540, 960, "Candy QA" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group,new[] {size});
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
        var viewType = assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,count-1);
    }
    private static void Check(bool condition,string description)
    {
        if (!condition) throw new Exception(description);
        File.AppendAllText(Path.Combine(Output,"checks.txt"), "PASS " + description + "\n");
    }
    private static void Tick()
    {
        if (!SessionState.GetBool("CandyVerifyActive",false) || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 3;
        try
        {
            int stage=SessionState.GetInt("CandyVerifyStage",1);
            if(stage==1)
            {
                var save=SaveManager.Instance;
                Check(save!=null,"Save manager starts with home scene");
                save.ResetToGuest();
                var data=new SaveData {Coins=450,BoosterHammer=2,SoundOn=false};
                data.RecordAttempt(5); data.SetLevelComplete(5,3,12345);
                Check(save.RestoreAccount("qa-A",JsonUtility.ToJson(data),0),"Restore known player");
                Check(save.Data.CurrentLevel==6 && save.Data.Coins==450,"Completed progress and economy restored");
                save.Data.Coins=777; save.Save();
                var accountFile=Directory.GetFiles(Application.persistentDataPath,"player_*.json")
                    .First(path=>File.ReadAllText(path).Contains("\"Coins\":777"));
                File.WriteAllText(accountFile,"interrupted file write");
                Check(save.RestoreAccount("qa-B",null,0),"New player loads");
                Check(save.Data.CurrentLevel==3 && save.Data.Coins==0 && save.Data.LevelEntries.Count==0,"New player never inherits previous user progress");
                Check(save.RestoreAccount("qa-A",JsonUtility.ToJson(data),0) && save.Data.Coins==777,"Unsent account-specific progress survives switching");
                Check(save.Data.Coins==777,"Corrupt primary file recovers the preferences backup");
                Check(!save.RestoreAccount("qa-invalid","[",0) && save.Data.Coins==777,"Invalid cloud payload leaves active progress unchanged");
                int generation=save.SaveGeneration;
                save.Data.PlaySeconds+=.1;
                save.AcknowledgeUpload(1,generation);
                Check(!save.HasPendingSave,"Live timers do not cause infinite cloud upload loops");
                save.Save(); int sent=save.SaveGeneration; save.Save(); save.AcknowledgeUpload(2,sent);
                Check(save.HasPendingSave,"Changes during upload remain queued");
                GoogleAuthManager.Instance.SignOut();
                Check(save.AccountId==null && save.Data.CurrentLevel==3 && save.Data.Coins==0 && save.Data.LevelEntries.Count==0,"Sign-out loads fresh guest defaults");
                Check(save.Data.SoundOn && save.Data.MusicOn,"Sign-out restores default settings");
                Capture("home");
                Click("LET'S PLAY");
            }
            if(stage==2)
            {
                Check(SceneManager.GetActiveScene().name=="LevelMap","Home Play button opens level map");
                Check(Object.FindObjectsOfType<Button>().Any(b=>b.name=="1" && b.interactable),"Default level is playable");
                Check(Object.FindObjectsOfType<Button>().Any(b=>b.name=="4" && !b.interactable),"Future levels remain locked");
                Capture("level-map");
                Click("1");
            }
            if(stage==3)
            {
                Check(SceneManager.GetActiveScene().name=="Game","Level button opens gameplay");
                Check(GameManager.Instance != null && GameManager.Instance.MovesRemaining>0,"Game starts with moves");
                Check(Object.FindObjectsOfType<GemView>().Length>=60,"Candy board is populated");
                Check(Object.FindObjectsOfType<GemView>().All(g=>g.GetComponent<SpriteRenderer>().sprite.name.StartsWith("Candy_") || g.GetComponent<SpriteRenderer>().sprite.name.StartsWith("Power_")),"Board uses new candy artwork");
                Check(SaveManager.Instance.Data.LevelsStarted>0,"Gameplay attempts are tracked");
                Capture("game");
                UIManager.Instance.OnPausePressed();
                Check(GameManager.Instance.State==GameState.Paused,"Pause button pauses gameplay");
            }
            if(stage==4)
            {
                Capture("pause");
                Click("CANDY POWER GUIDE");
                var guide=GameObject.Find("Power candy recipe bookOverlay");
                Check(guide!=null && guide.GetComponentInParent<Canvas>()!=null,"Power guide opens inside the game canvas");
                Capture("power-guide");
                Object.DestroyImmediate(guide);
                UIManager.Instance.OnResumePressed();
                Check(GameManager.Instance.State!=GameState.Paused && Time.timeScale==1,"Resume unpauses gameplay");
                GameManager.Instance.OnGameWon.Invoke();
            }
            if(stage==5)
            {
                Capture("win");
                Check(SaveManager.Instance.Data.GetEntry(1)?.Completed==true,"Victory persists level completion");
                UIManager.Instance.OnQuitToMapPressed();
            }
            if(stage==6)
            {
                Check(SceneManager.GetActiveScene().name=="LevelMap","Victory returns to map");
                Capture("completed-map");
                SessionState.SetBool("CandyVerifyActive",false);
                EditorApplication.Exit(File.Exists(Path.Combine(Output,"runtime-errors.txt"))?2:0);
                return;
            }
            SessionState.SetInt("CandyVerifyStage",stage+1);
        }
        catch(Exception error)
        {
            File.AppendAllText(Path.Combine(Output,"failed.txt"),error.ToString());
            SessionState.SetBool("CandyVerifyActive",false);
            EditorApplication.Exit(1);
        }
    }
    private static void Click(string name)
    {
        var button=Object.FindObjectsOfType<Button>().FirstOrDefault(b=>b.name==name && b.interactable);
        Check(button!=null,"Button available: "+name);
        button.onClick.Invoke();
    }
    private static void Capture(string name)
    {
        Canvas.ForceUpdateCanvases();
        var camera=Camera.main;
        if(camera==null) { camera=new GameObject("QA Camera").AddComponent<Camera>(); camera.orthographic=true; }
        var texture=new RenderTexture(540,960,24);
        camera.targetTexture=texture;
        var canvases=Object.FindObjectsOfType<Canvas>().Where(c=>c.enabled && c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        foreach(var canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1; }
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var prior=RenderTexture.active; RenderTexture.active=texture;
        var image=new Texture2D(540,960,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,540,960),0,0); image.Apply();
        File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());
        RenderTexture.active=prior; camera.targetTexture=null;
        foreach(var canvas in canvases) canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        Object.DestroyImmediate(image); Object.DestroyImmediate(texture);
    }
}
#endif
