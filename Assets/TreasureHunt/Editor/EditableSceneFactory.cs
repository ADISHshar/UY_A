using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreasureHunt.Editor
{
    public static class EditableSceneFactory
    {
        static readonly Color Ink=new Color(.025f,.055f,.075f,.97f);
        static readonly Color Teal=new Color(.2f,.85f,.76f);
        static readonly Color Gold=new Color(.98f,.79f,.43f);
        static readonly Color White=new Color(.83f,.9f,.91f);
        static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        [MenuItem("Treasure Hunt/Generate Editable Scene")]
        public static void GenerateInCurrentScene()
        {
            int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Generate editable Treasure Hunt level");
            var game=Object.FindAnyObjectByType<HuntGame>();
            if(game==null) { var root=new GameObject("Treasure Hunt"); Undo.RegisterCreatedObjectUndo(root,"Create game"); game=root.AddComponent<HuntGame>(); }
            GenerateLevel(game,game.Settings!=null?game.Settings:MazeDesigner.LoadSettings());
            Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject=game.gameObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static void GenerateSavedScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TreasureHunt.unity");
            GenerateInCurrentScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            ValidateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("EDITABLE SCENE SAVED: maze geometry, pickups, traps, guards, sight meshes, explorer, camera, lights, Canvas panels, buttons and EventSystem.");
        }

        public static void GenerateLevel(HuntGame game,MazeSettings settings)
        {
            Undo.RecordObject(game,"Configure editable level"); game.Settings=settings;
            // Only explicit regeneration replaces maze geometry. UI and lighting edits survive it.
            foreach(var old in Object.FindObjectsByType<MazeBuilder>()) Undo.DestroyObjectImmediate(old.gameObject);
            var oldGuards=game.transform.Find("Sentinels"); if(oldGuards!=null) Undo.DestroyObjectImmediate(oldGuards.gameObject);
            var mazeRoot=Child("Maze",game.transform); game.Maze=mazeRoot.AddComponent<MazeBuilder>(); game.Maze.settings=settings; game.Maze.Generate(false);
            GroupGeometry(game.Maze);
            PitFloorSetup.Prepare(game.Maze);
            var waypoints=Child("Patrol Waypoints",mazeRoot.transform).transform;
            foreach(var p in game.Maze.PatrolPoints)
            {
                var point=Child("Waypoint "+game.Maze.PatrolWaypoints.Count,waypoints).transform;
                point.position=p; game.Maze.PatrolWaypoints.Add(point);
            }
            if(game.Player==null) CreatePlayer(game);
            Undo.RecordObject(game.Player.transform,"Place explorer at maze entrance");
            game.Player.transform.SetPositionAndRotation(game.Maze.Spawn,game.Maze.SpawnPoint.rotation); game.Player.game=game;
            if(game.transform.Find("Moonlight")==null)
            {
                var sun=Child("Moonlight",game.transform); sun.transform.rotation=Quaternion.Euler(65,-35,0);
                var light=sun.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.15f; light.color=new Color(.65f,.83f,1); light.shadows=LightShadows.Soft;
                RenderSettings.ambientLight=new Color(.38f,.49f,.55f); RenderSettings.fog=true;
                RenderSettings.fogColor=new Color(.025f,.05f,.075f); RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.014f;
            }
            Physics.SyncTransforms(); CreateGuards(game);
            if(game.UI==null) CreateUI(game);
            GameplayFeatureSetup.Configure(game);
            if(game.GetComponent<AudioSource>()==null) game.gameObject.AddComponent<AudioSource>();
            PersistAssets(game.gameObject);
            EditorUtility.SetDirty(game); EditorUtility.SetDirty(game.Maze); EditorUtility.SetDirty(game.Player);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene); AssetDatabase.SaveAssets();
        }

        static GameObject Child(string name,Transform parent)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); Undo.RegisterCreatedObjectUndo(go,"Create "+name); return go;
        }
        static void GroupGeometry(MazeBuilder maze)
        {
            var originals=new List<Transform>(); foreach(Transform child in maze.transform) originals.Add(child);
            var walls=Child("Walls",maze.transform).transform; var floors=Child("Floors",maze.transform).transform;
            var traps=Child("Traps",maze.transform).transform; var items=Child("Treasure and Key",maze.transform).transform;
            foreach(var child in originals)
            {
                Transform parent=child.name.StartsWith("Wall")||child.name=="Maze wall"?walls:
                    child.name.StartsWith("Foundation")||child.name=="Floor inlay"?floors:
                    child.name=="Pulse trap"?traps:child==maze.SpawnPoint||child==maze.ChallengeRoot?maze.transform:items;
                child.SetParent(parent,true);
            }
        }
        static void CreatePlayer(HuntGame game)
        {
            var root=Child("Explorer",game.transform);
            var controller=root.AddComponent<CharacterController>(); controller.height=1.8f; controller.center=Vector3.up*.9f; controller.radius=.3f;
            game.Player=root.AddComponent<HuntPlayer>();
            var cam=Child("Explorer Camera",root.transform); cam.transform.localPosition=Vector3.up*1.6f;
            game.Player.view=cam.AddComponent<Camera>(); cam.tag="MainCamera"; cam.AddComponent<AudioListener>();
            var camera=game.Player.view; camera.fieldOfView=76; camera.nearClipPlane=.06f; camera.farClipPlane=180;
            camera.backgroundColor=new Color(.025f,.05f,.075f); camera.clearFlags=CameraClearFlags.SolidColor;
            var light=Child("Explorer Lantern",cam.transform).AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(1,.73f,.42f); light.range=10; light.intensity=2;
        }
        public static void CreateGuards(HuntGame game)
        {
            var root=game.transform.Find("Sentinels")??Child("Sentinels",game.transform).transform;
            var existing=game.GetComponentsInChildren<HuntGuard>(true); int count=existing.Length;
            foreach(var p in game.Maze.PatrolPoints)
            {
                if(count>=game.Settings.guards) break;
                if(p.magnitude<game.Settings.cellSize*4 || Vector3.Distance(p,game.Maze.KeyPosition)<game.Settings.cellSize*2 || Vector3.Distance(p,game.Maze.TreasurePosition)<game.Settings.cellSize*2) continue;
                bool occupied=false; foreach(var old in root.GetComponentsInChildren<HuntGuard>()) if(Vector3.Distance(old.transform.position,p)<game.Settings.cellSize) occupied=true;
                if(occupied) continue;
                var guard=Child("Sentinel "+(count+1),root); guard.transform.position=p;
                var body=GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name="Body (replace with your model)"; body.transform.SetParent(guard.transform,false);
                body.transform.localPosition=Vector3.up; body.transform.localScale=new Vector3(.65f,1,.65f);
                body.GetComponent<Renderer>().sharedMaterial=game.Maze.Danger; Object.DestroyImmediate(body.GetComponent<Collider>());
                var agent=guard.AddComponent<NavMeshAgent>(); agent.enabled=false; agent.speed=1.45f; agent.angularSpeed=180; agent.radius=.3f; agent.height=1.8f;
                var brain=guard.AddComponent<HuntGuard>(); brain.game=game; brain.patrolIndex=count*13; brain.CreateSightPreview(); count++;
            }
        }

        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(position.x,-position.y); rect.sizeDelta=size; return rect;
        }
        static GameObject Panel(string name,Transform parent,bool background=true)
        {
            var rect=Rect(name,parent,Vector2.zero,Vector2.zero); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            if(background) { var image=rect.gameObject.AddComponent<Image>(); image.color=Ink; image.raycastTarget=false; }
            return rect.gameObject;
        }
        static Text Label(string name,Transform parent,string text,float x,float y,float w,float h,int size,Color color)
        {
            var rect=Rect(name,parent,new Vector2(x,y),new Vector2(w,h)); var label=rect.gameObject.AddComponent<Text>();
            label.font=Font; label.text=text; label.fontSize=size; label.color=color; label.raycastTarget=false;
            label.horizontalOverflow=HorizontalWrapMode.Wrap; label.verticalOverflow=VerticalWrapMode.Overflow; return label;
        }
        static void Button(string name,Transform parent,string caption,float x,float y,float width,UnityAction action)
        {
            var rect=Rect(name,parent,new Vector2(x,y),new Vector2(width,50));
            var image=rect.gameObject.AddComponent<Image>(); image.color=new Color(.12f,.25f,.28f);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.highlightedColor=new Color(.65f,1,.9f); colors.pressedColor=Gold; button.colors=colors;
            var text=Label("Label",rect,caption,0,0,width,50,18,White); text.alignment=TextAnchor.MiddleCenter;
            UnityEventTools.AddPersistentListener(button.onClick,action);
        }
        static GameObject Screen(string name,Transform canvas,string heading,string description)
        {
            var panel=Panel(name,canvas);
            Label("Eyebrow",panel.transform,"AN EXPEDITION INTO THE FORGOTTEN RUINS",100,74,1000,30,14,Teal);
            Label("Title",panel.transform,heading,96,120,1100,80,56,Gold).fontStyle=FontStyle.Bold;
            Label("Description",panel.transform,description,100,216,1000,80,20,White);
            return panel;
        }
        static void CreateUI(HuntGame game)
        {
            var root=Child("UI Canvas",game.transform); var canvas=root.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720); scaler.matchWidthOrHeight=.5f; root.AddComponent<GraphicRaycaster>();
            var ui=root.AddComponent<HuntUI>(); game.UI=ui;
            ui.MainMenu=Screen("Main Menu",root.transform,"TREASURE HUNT","Find the sun key. Outsmart the sentinels.\nReach the hidden vault before the ruins take you.");
            ui.PauseMenu=Screen("Pause Menu",root.transform,"EXPEDITION PAUSED","Take a breath. The ruins can wait.");
            const string controls="WASD / ARROWS   Move       MOUSE   Look       SHIFT   Sprint\n\nAvoid visible enemy sight. One sighting ends the hunt.\nTraps pulse red; cross during their dark phase.";
            Label("Controls",ui.MainMenu.transform,controls,100,324,1000,130,20,White);
            Label("Controls",ui.PauseMenu.transform,controls,100,324,1000,130,20,White);
            Button("Begin Button",ui.MainMenu.transform,"BEGIN EXPEDITION",100,500,310,game.Resume);
            Button("Quit Button",ui.MainMenu.transform,"QUIT",430,500,230,game.QuitGame);
            Button("Resume Button",ui.PauseMenu.transform,"RESUME",100,500,310,game.Resume);
            Button("Main Menu Button",ui.PauseMenu.transform,"MAIN MENU",430,500,230,game.MainMenu);
            ui.Victory=Screen("Victory Screen",root.transform,"TREASURE RECOVERED","The lost treasure is yours.");
            ui.GameOver=Screen("Game Over Screen",root.transform,"EXPEDITION LOST","");
            ui.DefeatReason=ui.GameOver.transform.Find("Description").GetComponent<Text>();
            ui.VictoryTime=Label("Time and Best",ui.Victory.transform,"EXPEDITION TIME   00:00.0",100,324,1000,120,20,White);
            ui.DefeatTime=Label("Time",ui.GameOver.transform,"EXPEDITION TIME   00:00.0",100,324,1000,40,20,White);
            Label("Tip",ui.GameOver.transform,"Study the patrols. Time your next crossing.",100,380,1000,50,20,White);
            foreach(var screen in new[]{ui.Victory,ui.GameOver})
            {
                Button("Retry Button",screen.transform,"TRY AGAIN",100,500,310,game.Restart);
                Button("Main Menu Button",screen.transform,"MAIN MENU",430,500,230,game.MainMenu);
            }
            ui.HUD=Panel("HUD",root.transform,false);
            var card=Rect("Status Background",ui.HUD.transform,new Vector2(24,22),new Vector2(1232,86)); card.gameObject.AddComponent<Image>().color=Ink;
            Label("Game Name",ui.HUD.transform,"TREASURE HUNT   /   THE SUNKEN VAULT",46,34,650,25,14,Teal);
            ui.Objective=Label("Objective",ui.HUD.transform,"OBJECTIVE  /  Find the sun key",46,64,760,30,20,White);
            ui.HealthAndTime=Label("Health and Timer",ui.HUD.transform,"HEARTS  |||     00:00.0",850,36,370,28,20,White);
            Label("Shortcuts",ui.HUD.transform,"ESC  Pause   /   SHIFT  Sprint",960,73,270,25,14,White);
            var crosshair=Label("Crosshair",ui.HUD.transform,"+",0,0,25,25,20,White); var cr=crosshair.rectTransform; cr.anchorMin=cr.anchorMax=new Vector2(.5f,.5f); cr.pivot=new Vector2(.5f,.5f); crosshair.alignment=TextAnchor.MiddleCenter;
            ui.Notice=Label("Notice",ui.HUD.transform,"Red sight cones mean danger. Cross traps when they turn dark.",36,666,1180,30,14,White);
            var footer=Panel("Shared Footer",root.transform,false); ui.Seed=Label("Seed",footer.transform,"PROCEDURAL MAZE    /    SEED "+game.Settings.seed,100,640,1080,30,14,Teal);
            // Footer belongs to menu screens only; HUD has its own notice.
            footer.AddComponent<CanvasGroup>().blocksRaycasts=false;
            ui.Seed.gameObject.SetActive(true);
            var overlay=Panel("Damage Flash",root.transform,false); ui.DamageOverlay=overlay.AddComponent<Image>(); ui.DamageOverlay.color=Color.clear; ui.DamageOverlay.raycastTarget=false;
            ui.HUD.SetActive(false); ui.PauseMenu.SetActive(false); ui.Victory.SetActive(false); ui.GameOver.SetActive(false);
            var events=Object.FindAnyObjectByType<EventSystem>();
            if(events==null) { var go=Child("EventSystem",game.transform); go.AddComponent<EventSystem>(); go.AddComponent<InputSystemUIInputModule>(); }
        }

        public static void PersistAssets(GameObject root)
        {
            const string folder="Assets/TreasureHunt/Generated";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/TreasureHunt","Generated");
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material=renderer.sharedMaterial;
                if(material==null || AssetDatabase.Contains(material)) continue;
                string name="Material_"+ColorUtility.ToHtmlStringRGBA(material.color)+(material.IsKeywordEnabled("_EMISSION")?"_Glow":"");
                string path=folder+"/"+name+".mat";
                var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(saved!=null) renderer.sharedMaterial=saved;
                else AssetDatabase.CreateAsset(material,path);
            }
            // Ensure the gameplay material references also use saved assets.
            var maze=root.GetComponentInChildren<MazeBuilder>();
            maze.Stone=SaveMaterial(maze.Stone,folder); maze.Gold=SaveMaterial(maze.Gold,folder);
            maze.Teal=SaveMaterial(maze.Teal,folder); maze.Danger=SaveMaterial(maze.Danger,folder);
            foreach(var mesh in root.GetComponentsInChildren<MeshFilter>(true))
                if(mesh.sharedMesh!=null && !AssetDatabase.Contains(mesh.sharedMesh))
                    AssetDatabase.CreateAsset(mesh.sharedMesh,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+mesh.sharedMesh.name+".asset"));
        }
        static Material SaveMaterial(Material material,string folder)
        {
            if(AssetDatabase.Contains(material)) return material;
            string name="Material_"+ColorUtility.ToHtmlStringRGBA(material.color)+(material.IsKeywordEnabled("_EMISSION")?"_Glow":"");
            string path=folder+"/"+name+".mat"; var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(saved!=null) return saved; AssetDatabase.CreateAsset(material,path); return material;
        }
        public static void ValidateScene()
        {
            var game=Object.FindAnyObjectByType<HuntGame>();
            if(game==null || game.Maze==null || game.Player==null || game.UI==null) throw new System.Exception("Missing authored scene references");
            if(game.Audio==null || game.Audio.bank==null || game.UI.InventoryLabels==null || game.UI.InventoryLabels.Length!=5)
                throw new System.Exception("Run Treasure Hunt > Upgrade Gameplay Features to add audio and inventory.");
            if(game.Maze.Doors.Count<2 || game.Maze.Doors.Count>5 || game.Maze.Keys.Count!=game.Maze.Doors.Count)
                throw new System.Exception("Expected 2-5 matching colored doors and keys.");
            if(game.Player.robotModel==null || game.Player.followCamera==null) throw new System.Exception("The blue robot and third-person camera are not assigned.");
            foreach(var pit in game.Maze.Pits)
            {
                bool active=pit.cover.gameObject.activeSelf; pit.cover.gameObject.SetActive(false); Physics.SyncTransforms();
                bool blocked=Physics.Raycast(pit.transform.position+Vector3.up*.4f,Vector3.down,.9f,~0,QueryTriggerInteraction.Ignore);
                pit.cover.gameObject.SetActive(active);
                if(blocked) throw new System.Exception("A floor collider still blocks the hole under "+pit.name);
            }
            var robotShader=Shader.Find("Treasure Hunt/Robot Palette");
            if(robotShader==null || ShaderUtil.ShaderHasError(robotShader)) throw new System.Exception("Robot palette shader failed to compile.");
            foreach(var panel in new[]{game.UI.MainMenu,game.UI.PauseMenu})
            {
                bool hasResume=false;
                foreach(var button in panel.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
                        hasResume|=button.onClick.GetPersistentTarget(i)==game && button.onClick.GetPersistentMethodName(i)==nameof(HuntGame.Resume);
                if(!hasResume) throw new System.Exception("Missing start/resume action on "+panel.name);
            }
            foreach(var trap in game.Maze.TrapVisuals) if(trap==null) throw new System.Exception("Unassigned trap reference");
            foreach(var guard in Object.FindObjectsByType<HuntGuard>())
                if(guard.sightVisual==null || !AssetDatabase.Contains(guard.sightVisual.sharedMesh)) throw new System.Exception("Unsaved guard visual");
            Debug.Log("EDITABLE SCENE VALIDATION PASSED: serialized gameplay references, persistent start/resume actions, traps and saved guard meshes.");
        }
    }
}
