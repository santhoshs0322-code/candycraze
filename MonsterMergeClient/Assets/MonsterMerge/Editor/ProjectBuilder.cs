using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MonsterMerge.Editor
{
    [InitializeOnLoad]
    public static class ProjectBuilder
    {
        public const string Root = "Assets/MonsterMerge";
        public const string CatalogPath = Root + "/Data/GameCatalog.asset";
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static ProjectBuilder() { EditorApplication.delayCall += Install; }
        static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath)) return;
            foreach (var scene in LoadedScenes()) if (scene.isDirty) { Debug.Log("Monster Merge: save your scene, then choose Monster Merge > Generate Project."); return; }
            Generate();
        }
        static IEnumerable<Scene> LoadedScenes() { for(int i = 0; i < SceneManager.sceneCount; i++) yield return SceneManager.GetSceneAt(i); }
        [MenuItem("Monster Merge/Generate Project")]
        public static void Generate()
        {
            if (AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath)) { Debug.Log("Monster Merge assets already exist. Existing designer edits are preserved."); return; }
            var setup = EditorSceneManager.GetSceneManagerSetup();
            foreach(string folder in new[]{"Data/Monsters","Data/Bosses","Data/Levels","Materials","Prefabs/Monsters","Prefabs/Bosses","Prefabs/Track","Scenes","Art/Icons","Audio"}) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            try
            {
                var catalog = ScriptableObject.CreateInstance<GameCatalog>();
                catalog.baseMaterial = Material("White",Color.white);
                catalog.elements = new[] {
                    ElementConfig(Element.Normal,"",new Color(.4f,.94f,.58f),1), ElementConfig(Element.Fire,"Fire",new Color(1,.35f,.12f),1.3f),
                    ElementConfig(Element.Ice,"Frost",new Color(.25f,.8f,1),1.25f), ElementConfig(Element.Lightning,"Thunder",new Color(1,.85f,.2f),1.35f),
                    ElementConfig(Element.Machine,"Cyber",new Color(.58f,.7f,.77f),1.4f), ElementConfig(Element.Shadow,"Shadow",new Color(.65f,.3f,.85f),1.45f)
                };
                var monsters = new List<MonsterDefinition>();
                monsters.Add(Monster("dragon","Dragon",0,new Color(.34f,.85f,.47f),0));
                monsters.Add(Monster("shark","Shark",180,new Color(.25f,.68f,.9f),1));
                monsters.Add(Monster("trex","T-Rex",260,new Color(.9f,.56f,.22f),2));
                monsters.Add(Monster("panda","Panda",300,new Color(.9f,.94f,.93f),3));
                monsters.Add(Monster("slime","Slime",220,new Color(.9f,.34f,.62f),4));
                monsters.Add(Monster("sharkdragon","Shark Dragon",0,new Color(.23f,.8f,.7f),0));
                monsters.Add(Monster("cyberrex","Cyber Rex",0,new Color(.56f,.67f,.75f),2,MonsterMerge.Element.Machine));
                monsters.Add(Monster("ninjapanda","Ninja Panda",0,new Color(.6f,.34f,.78f),3,MonsterMerge.Element.Shadow));
                catalog.monsters = monsters.ToArray();
                catalog.recipes = new[] {
                    new MutationRecipe { ingredientA="dragon", ingredientB="shark", resultId="sharkdragon" },
                    new MutationRecipe { ingredientA="trex", ingredientB="Machine", resultId="cyberrex" },
                    new MutationRecipe { ingredientA="panda", ingredientB="Shadow", resultId="ninjapanda" }
                };
                var bosses = new[] { Boss("Stone Golem",0,new Color(.52f,.6f,.56f)), Boss("Giant Robot",1,new Color(.57f,.65f,.76f)), Boss("Shadow Beast",2,new Color(.42f,.22f,.57f)) };
                BuildTrack(catalog);
                catalog.levels = new LevelDefinition[10];
                for(int i=0;i<10;i++)
                {
                    var level = ScriptableObject.CreateInstance<LevelDefinition>(); level.number = i+1;
                    level.length = 210 + i*7; level.completionCoins = 80+i*15;
                    level.boss = bosses[(i/3)%bosses.Length]; level.bossMultiplier = 1+i*.085f;
                    var segments = new List<SegmentDefinition> {
                        Segment(SegmentKind.Dna,14,0), Segment(SegmentKind.Coins,29,0), Segment(SegmentKind.Dna,44,0),
                        Segment(SegmentKind.Gate,70,0), Segment(SegmentKind.Dna,87,-1.7f), Segment(SegmentKind.Coins,87,1.7f),
                        Segment(SegmentKind.Spikes,110,1.9f), Segment(SegmentKind.Dna,121,0), Segment(SegmentKind.Coins,139,0),
                        Segment(SegmentKind.Dna,166,0), Segment(SegmentKind.Coins,185,-1.8f)
                    };
                    if(i>=3) segments.Add(Segment(SegmentKind.FireZone,153,-1.8f));
                    if(i>=5) { var gate = Segment(SegmentKind.Gate,157,0); gate.left = MonsterMerge.Element.Lightning; gate.right = MonsterMerge.Element.Machine; segments.Add(gate); }
                    if(i>=6) segments.Add(Segment(SegmentKind.MovingBlock,199,0));
                    if(i>=7) segments.Add(Segment(SegmentKind.Choice,211,-1.8f));
                    if(i>=8) segments.Add(Segment(SegmentKind.RotatingBar,230,0));
                    if(i>=9)
                    {
                        segments.Add(Segment(SegmentKind.FallingObject,245,-1.8f));
                        var gate = Segment(SegmentKind.Gate,258,0); gate.left = MonsterMerge.Element.Shadow; gate.right = MonsterMerge.Element.Fire; segments.Add(gate);
                    }
                    level.segments = segments.ToArray(); TrackLayout.Space(level); Save(level,"Data/Levels/Level"+(i+1).ToString("00")+".asset"); catalog.levels[i] = level;
                }
                CreateAudio(catalog);
                Save(catalog,"Data/GameCatalog.asset");
                CreateScene("Bootstrap",catalog); CreateScene("Home",catalog); CreateScene("Gameplay",catalog);
                Configure(); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
                Debug.Log("Monster Merge generated: 3 scenes, 10 levels, 8 monsters, 3 bosses, prefabs and URP materials.");
            }
            finally { if(setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Root+"/Scenes/Bootstrap.unity");
        }
        static ElementDefinition ElementConfig(MonsterMerge.Element element,string prefix,Color color,float attack)
            => new ElementDefinition { element=element,prefix=prefix,color=color,attackMultiplier=attack };
        static SegmentDefinition Segment(SegmentKind kind,float z,float x) => new SegmentDefinition { kind=kind,z=z,lane=x };
        static void Save(UnityEngine.Object obj,string path) { AssetDatabase.CreateAsset(obj,Root+"/"+path); }
        static Material Material(string name,Color color)
        {
            if(materials.TryGetValue(name,out var existing) && existing) return existing;
            string path = Root+"/Materials/"+name+".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")); material.name = name;
                material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",.22f); material.enableInstancing = true; AssetDatabase.CreateAsset(material,path);
            }
            materials[name] = material; return material;
        }
        static GameObject Part(string name,Transform parent,PrimitiveType shape,Vector3 position,Vector3 size,Material material)
        {
            var obj = GameObject.CreatePrimitive(shape); obj.name = name; obj.transform.SetParent(parent,false);
            obj.transform.localPosition = position; obj.transform.localScale = size; UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        static GameObject Prefab(GameObject root,string path)
        { var prefab = PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+path+".prefab"); UnityEngine.Object.DestroyImmediate(root); return prefab; }
        static MonsterDefinition Monster(string id,string title,int cost,Color color,int style,MonsterMerge.Element element = MonsterMerge.Element.Normal)
        {
            var monster = ScriptableObject.CreateInstance<MonsterDefinition>(); monster.id=id; monster.displayName=title; monster.unlockCost=cost;
            monster.color=color; monster.startingElement=element;
            monster.evolutions = new[] {
                new EvolutionStage { title="Baby", requiredDna=0,scale=.8f },
                new EvolutionStage { title="", requiredDna=20,scale=1,attackBonus=4 },
                new EvolutionStage { title="Armored", requiredDna=60,scale=1.15f,attackBonus=8 },
                new EvolutionStage { title="Legendary", requiredDna=110,scale=1.3f,attackBonus=14 }
            };
            var root = new GameObject(title); var visual = root.AddComponent<MonsterVisual>();
            var rig = new GameObject("Body rig").transform; rig.SetParent(root.transform); visual.animatedRoot=rig;
            var body = Material(id,color); var white=Material("Ivory",new Color(.97f,.99f,.93f)); var black=Material("Ink",new Color(.035f,.08f,.1f));
            var accent=Material("Coral",new Color(1,.4f,.34f)); var gold=Material("Gold",new Color(1,.78f,.19f));
            var bodyParts = new List<Renderer>();
            bodyParts.Add(Part("Belly",rig,PrimitiveType.Sphere,new Vector3(0,.78f,0),new Vector3(.95f,1.1f,.8f),body).GetComponent<Renderer>());
            bodyParts.Add(Part("Big head",rig,PrimitiveType.Sphere,new Vector3(0,1.55f,.22f),new Vector3(1.35f,1.15f,1.05f),body).GetComponent<Renderer>());
            Part("Muzzle",rig,PrimitiveType.Sphere,new Vector3(0,1.27f,.65f),new Vector3(.85f,.47f,.5f),white);
            for(int side=-1;side<=1;side+=2)
            {
                Part("Eye",rig,PrimitiveType.Sphere,new Vector3(side*.37f,1.7f,.665f),new Vector3(.43f,.49f,.23f),style==3?black:white);
                Part("Pupil",rig,PrimitiveType.Sphere,new Vector3(side*.36f,1.7f,.78f),new Vector3(.21f,.28f,.12f),style==3?white:black);
                Part("Eye glint",rig,PrimitiveType.Sphere,new Vector3(side*.36f-.035f,1.77f,.835f),new Vector3(.07f,.08f,.04f),white);
                Part("Foot",rig,PrimitiveType.Sphere,new Vector3(side*.38f,.22f,.25f),new Vector3(.43f,.35f,.62f),style==3?black:body);
                Part("Arm",rig,PrimitiveType.Capsule,new Vector3(side*.54f,.8f,.13f),new Vector3(.25f,.3f,.25f),style==3?black:body);
                if(style==3) Part("Ear",rig,PrimitiveType.Sphere,new Vector3(side*.53f,2.02f,.1f),Vector3.one*.45f,black);
                else if(style==0 || style==2) Part("Horn",rig,PrimitiveType.Capsule,new Vector3(side*.46f,2.08f,.07f),new Vector3(.17f,.3f,.16f),gold).transform.localRotation=Quaternion.Euler(0,0,-side*23);
            }
            Part("Smile",rig,PrimitiveType.Cube,new Vector3(0,1.21f,.9f),new Vector3(.29f,.035f,.025f),black);
            if(style!=3 && style!=4)
            {
                var tail=Part("Tail",rig,PrimitiveType.Capsule,new Vector3(0,.7f,-.64f),new Vector3(.3f,.57f,.3f),body);
                tail.transform.localRotation=Quaternion.Euler(60,0,0); bodyParts.Add(tail.GetComponent<Renderer>());
            }
            if(style==1 || id=="sharkdragon") Part("Dorsal fin",rig,PrimitiveType.Cube,new Vector3(0,1.25f,-.42f),new Vector3(.18f,.7f,.65f),body).transform.localRotation=Quaternion.Euler(25,0,0);
            if(style==4) { rig.localScale=new Vector3(1.1f,.8f,1.1f); Part("Slime tuft",rig,PrimitiveType.Sphere,new Vector3(.1f,2.08f,.1f),new Vector3(.42f,.5f,.4f),body); }
            visual.wings=new GameObject("Evolved wings"); visual.wings.transform.SetParent(rig,false);
            for(int side=-1;side<=1;side+=2)
            {
                var wing=Part("Wing",visual.wings.transform,PrimitiveType.Cube,new Vector3(side*.86f,1.17f,-.24f),new Vector3(.88f,.5f,.12f),accent);
                wing.transform.localRotation=Quaternion.Euler(0,side*20,side*28);
            }
            visual.armor=new GameObject("Armor"); visual.armor.transform.SetParent(rig,false);
            Part("Chestplate",visual.armor.transform,PrimitiveType.Cube,new Vector3(0,.87f,.42f),new Vector3(.6f,.48f,.16f),gold);
            visual.crown=new GameObject("Legendary crown"); visual.crown.transform.SetParent(rig,false);
            for(int i=-1;i<=1;i++) Part("Crown crystal",visual.crown.transform,PrimitiveType.Cube,new Vector3(i*.23f,2.22f,.22f),new Vector3(.16f,.38f,.16f),gold).transform.localRotation=Quaternion.Euler(0,0,i*20);
            visual.bodyRenderers=bodyParts.ToArray(); visual.armor.SetActive(false); visual.crown.SetActive(false);
            monster.prefab=Prefab(root,"Monsters/"+title); Save(monster,"Data/Monsters/"+title+".asset");
            return monster;
        }
        static BossDefinition Boss(string title,int style,Color color)
        {
            var boss=ScriptableObject.CreateInstance<BossDefinition>(); boss.displayName=title; boss.health=110+style*25; boss.attack=9+style*2;
            var root=new GameObject(title); var visual=root.AddComponent<MonsterVisual>(); var rig=new GameObject("Boss rig").transform; rig.SetParent(root.transform); visual.animatedRoot=rig;
            var material=Material(title,color); var eyes=Material("Boss eyes",new Color(1,.3f,.18f));
            var parts=new List<Renderer>();
            parts.Add(Part("Torso",rig,style==2?PrimitiveType.Sphere:PrimitiveType.Cube,new Vector3(0,1.3f,0),new Vector3(1.8f,1.8f,1.05f),material).GetComponent<Renderer>());
            parts.Add(Part("Head",rig,PrimitiveType.Cube,new Vector3(0,2.65f,.05f),new Vector3(1.5f,1.2f,1.2f),material).GetComponent<Renderer>());
            for(int side=-1;side<=1;side+=2)
            {
                Part("Eye",rig,PrimitiveType.Cube,new Vector3(side*.38f,2.75f,.67f),new Vector3(.3f,.16f,.07f),eyes);
                parts.Add(Part("Fist",rig,PrimitiveType.Sphere,new Vector3(side*1.3f,1.15f,.12f),Vector3.one*.85f,material).GetComponent<Renderer>());
                Part("Foot",rig,PrimitiveType.Cube,new Vector3(side*.6f,.26f,.12f),new Vector3(.65f,.52f,.8f),material);
            }
            visual.bodyRenderers=parts.ToArray(); boss.prefab=Prefab(root,"Bosses/"+title); Save(boss,"Data/Bosses/"+title+".asset"); return boss;
        }
        static void BuildTrack(GameCatalog catalog)
        {
            var teal=Material("Track",new Color(.16f,.53f,.5f)); var white=Material("Ivory",new Color(.97f,.99f,.93f));
            var track=new GameObject("Straight segment");
            Part("Surface",track.transform,PrimitiveType.Cube,Vector3.zero,new Vector3(6.5f,.6f,15),teal);
            for(int side=-1;side<=1;side+=2) Part("Edge",track.transform,PrimitiveType.Cube,new Vector3(side*3.2f,.34f,0),new Vector3(.13f,.12f,15),white);
            for(int z=-6;z<=6;z+=3) Part("Center marking",track.transform,PrimitiveType.Cube,new Vector3(0,.306f,z),new Vector3(.06f,.01f,.6f),white);
            catalog.trackPrefab=Prefab(track,"Track/Straight");
            var dna=new GameObject("DNA crystal");
            Part("Crystal",dna.transform,PrimitiveType.Cube,Vector3.zero,Vector3.one*.4f,Material("DNA",new Color(.15f,.95f,1))).transform.localRotation=Quaternion.Euler(45,0,45);
            catalog.dnaPrefab=Prefab(dna,"Track/DNA");
            var coin=new GameObject("Coin");
            Part("Coin",coin.transform,PrimitiveType.Cylinder,Vector3.zero,new Vector3(.55f,.08f,.55f),Material("Gold",Color.yellow)).transform.localRotation=Quaternion.Euler(90,0,0);
            catalog.coinPrefab=Prefab(coin,"Track/Coin");
            var obstacle=new GameObject("Obstacle");
            Part("Hazard",obstacle.transform,PrimitiveType.Cube,new Vector3(0,.5f,0),Vector3.one,Material("Hazard",new Color(1,.3f,.38f))).transform.localRotation=Quaternion.Euler(0,15,0);
            catalog.obstaclePrefab=Prefab(obstacle,"Track/Obstacle");
            var gate=new GameObject("Mutation gate"); var mutation=gate.AddComponent<MutationGate>();
            for(int side=-1;side<=1;side+=2)
            {
                var mat=Material(side<0?"Fire gate":"Ice gate",side<0?new Color(1,.36f,.15f):new Color(.24f,.76f,1));
                var panel=Part("Header",gate.transform,PrimitiveType.Cube,new Vector3(side*1.55f,3.05f,0),new Vector3(2.85f,.75f,.2f),mat).GetComponent<Renderer>();
                Part("Outer pillar",gate.transform,PrimitiveType.Cube,new Vector3(side*3,1.5f,0),new Vector3(.15f,3,.15f),mat);
                var labelObject=new GameObject("Element label"); labelObject.transform.SetParent(gate.transform,false);
                labelObject.transform.localPosition=new Vector3(side*1.55f,3.05f,-.14f); labelObject.transform.localRotation=Quaternion.identity;
                var label=labelObject.AddComponent<TextMesh>(); label.text=side<0?"FIRE":"ICE"; label.fontSize=64; label.characterSize=.075f;
                label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center; label.color=Color.white;
                if(side<0) { mutation.leftPanel=panel; mutation.leftLabel=label; } else { mutation.rightPanel=panel; mutation.rightLabel=label; }
            }
            Part("Center pillar",gate.transform,PrimitiveType.Cube,new Vector3(0,1.5f,0),new Vector3(.13f,3,.13f),white);
            catalog.gatePrefab=Prefab(gate,"Track/MutationGate");
        }
        static void CreateAudio(GameCatalog catalog)
        {
            catalog.click=Tone("Click",660,.07f); catalog.coin=Tone("Coin",1100,.13f); catalog.dna=Tone("DNA",820,.16f);
            catalog.evolution=Tone("Evolution",440,.55f); catalog.damage=Tone("Damage",140,.15f); catalog.attack=Tone("Attack",220,.1f);
            catalog.victory=Tone("Victory",740,.8f); catalog.defeat=Tone("Defeat",160,.55f);
        }
        static AudioClip Tone(string name,float frequency,float duration)
        {
            string path=Root+"/Audio/"+name+".wav"; const int rate=22050; int samples=(int)(rate*duration);
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36+samples*2); writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate*2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples*2);
                for(int i=0;i<samples;i++) { float t=(float)i/rate; float envelope=Mathf.Min(1,t*100)*(1-(float)i/samples); writer.Write((short)(Mathf.Sin(t*frequency*Mathf.PI*2)*envelope*5000)); }
            }
            AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static void CreateScene(string name,GameCatalog catalog)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            if(name=="Bootstrap") new GameObject("Game services").AddComponent<GameApp>().catalog=catalog;
            else
            {
                var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener),typeof(FollowCamera)); camera.tag="MainCamera";
                var lens=camera.GetComponent<Camera>(); lens.clearFlags=CameraClearFlags.SolidColor; lens.backgroundColor=new Color(.34f,.68f,.76f); lens.farClipPlane=170;
                camera.transform.position=new Vector3(0,6,-10); camera.transform.LookAt(new Vector3(0,1,4));
                var light=new GameObject("Sun").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.2f; light.shadows=LightShadows.Soft;
                light.transform.rotation=Quaternion.Euler(45,-30,0);
                RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.68f,.77f,.79f);
                RenderSettings.fog=true; RenderSettings.fogColor=lens.backgroundColor; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=65; RenderSettings.fogEndDistance=150;
                if(name=="Home")
                {
                    new GameObject("Home").AddComponent<HomeController>();
                    Part("Display island",null,PrimitiveType.Cylinder,new Vector3(0,-.25f,0),new Vector3(4,.2f,4),Material("Track",Color.cyan));
                    for(int i=0;i<8;i++) Part("Floating island",null,PrimitiveType.Cube,new Vector3((i%2==0?-1:1)*(5+i),-2-i*.4f,-5-i*3),new Vector3(3,1.5f,3),Material("Track",Color.cyan));
                }
                else new GameObject("Gameplay").AddComponent<GameplayController>();
            }
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/"+name+".unity");
        }
        [MenuItem("Monster Merge/Configure Android Settings")]
        public static void Configure()
        {
            PlayerSettings.companyName="YourStudio"; PlayerSettings.productName="Monster Merge AI Evolution";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.Gamixtv.monstermerge");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultScreenWidth=540; PlayerSettings.defaultScreenHeight=960;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed; PlayerSettings.runInBackground=true;
            PlayerSettings.bundleVersion="0.1.0"; PlayerSettings.Android.bundleVersionCode=1;
            var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/URP-Performant.asset");
            if(pipeline) { GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline; }
            EditorBuildSettings.scenes=new[] {
                new EditorBuildSettingsScene(Root+"/Scenes/Bootstrap.unity",true),
                new EditorBuildSettingsScene(Root+"/Scenes/Home.unity",true),
                new EditorBuildSettingsScene(Root+"/Scenes/Gameplay.unity",true)
            };
        }
        [MenuItem("Monster Merge/Open Home")]
        static void OpenHome() { if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Root+"/Scenes/Home.unity"); }
    }
}
