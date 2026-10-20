using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TreasureHunt.Editor
{
    public static class GameplayFeatureSetup
    {
        public static void UpgradeAndBuild()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TreasureHunt.unity");
            UpgradeCurrent(); MazeDesigner.BuildWindows();
        }
        [MenuItem("Treasure Hunt/Upgrade Gameplay Features")]
        public static void UpgradeCurrent()
        {
            var game=Object.FindAnyObjectByType<HuntGame>();
            if(game==null || game.Maze==null) { EditableSceneFactory.GenerateInCurrentScene(); return; }
            var maze=game.Maze;
            PitFloorSetup.RestoreOldPits(maze);
            // Preserve the edited maze, lighting and UI; replace only the old challenge set.
            if(maze.ChallengeRoot!=null) Undo.DestroyObjectImmediate(maze.ChallengeRoot.gameObject);
            if(maze.KeyVisual!=null) Undo.DestroyObjectImmediate(maze.KeyVisual.gameObject);
            if(maze.Door!=null) Undo.DestroyObjectImmediate(maze.Door.gameObject);
            foreach(var trap in maze.TrapVisuals) if(trap!=null) Undo.DestroyObjectImmediate(trap.gameObject);
            maze.TrapVisuals.Clear(); maze.TrapPositions.Clear();
            maze.Doors.Clear(); maze.Keys.Clear(); maze.Spikes.Clear(); maze.Lasers.Clear(); maze.Crushers.Clear(); maze.Pits.Clear();
            maze.RestoreSceneData(); MazeChallenges.Generate(maze);
            PitFloorSetup.Prepare(maze);
            EditableSceneFactory.CreateGuards(game); Configure(game); EditableSceneFactory.PersistAssets(game.gameObject);
            EditorUtility.SetDirty(maze); EditorUtility.SetDirty(game); EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene); AssetDatabase.SaveAssets();
            Selection.activeGameObject=maze.ChallengeRoot.gameObject;
            Debug.Log("GAMEPLAY UPGRADE SAVED: silent laser bar walls, real collapsing shafts, blue third-person robot and red animated sentinels. Existing maze and UI edits preserved.");
        }
        public static void Configure(HuntGame game)
        {
            if(game.Audio==null)
            {
                var root=new GameObject("Audio"); root.transform.SetParent(game.transform,false); game.Audio=root.AddComponent<HuntAudio>();
                game.Audio.interfaceSource=Source("Interface and Feedback",root.transform,false,1);
                game.Audio.ambienceSource=Source("Ruins Ambience",root.transform,false,.075f);
                game.Audio.spatialSources=new AudioSource[16];
                for(int i=0;i<16;i++) game.Audio.spatialSources[i]=Source("Spatial Effect "+(i+1),root.transform,true,1);
            }
            game.Audio.bank=LoadAudioBank();
            EditorUtility.SetDirty(game.Settings);
            if(game.Player.footsteps==null) game.Player.footsteps=Source("Player Footsteps",game.Player.transform,false,1);
            foreach(var guard in game.GetComponentsInChildren<HuntGuard>(true))
                if(guard.footsteps==null) guard.footsteps=Source("Heavy Stone Footsteps",guard.transform,true,1);
            foreach(var item in game.GetComponentsInChildren<HuntWorldObject>(true)) { item.game=game; EditorUtility.SetDirty(item); }
            if(game.UI.InventoryLabels==null || game.UI.InventoryLabels.Length!=5) CreateInventory(game.UI);
            RobotSceneSetup.Configure(game);
            UpdateDefaultText(game.UI);
            EditorUtility.SetDirty(game.Audio); EditorUtility.SetDirty(game.UI); EditorUtility.SetDirty(game.Player);
        }
        static AudioSource Source(string name,Transform parent,bool spatial,float volume)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false); var source=go.AddComponent<AudioSource>();
            source.playOnAwake=false; source.spatialBlend=spatial?1:0; source.volume=volume;
            source.minDistance=1.4f; source.maxDistance=18; source.dopplerLevel=0; source.rolloffMode=AudioRolloffMode.Logarithmic; return source;
        }
        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/TreasureHunt/Audio/"+name+".wav");
        static HuntAudioBank LoadAudioBank()
        {
            const string path="Assets/TreasureHunt/Resources/AudioBank.asset";
            var bank=AssetDatabase.LoadAssetAtPath<HuntAudioBank>(path);
            if(bank==null) { bank=ScriptableObject.CreateInstance<HuntAudioBank>(); AssetDatabase.CreateAsset(bank,path); }
            if(bank.playerSteps==null || bank.playerSteps.Length==0) bank.playerSteps=new[]{Clip("PlayerStoneStep1"),Clip("PlayerStoneStep2"),Clip("PlayerStoneStep3")};
            if(bank.guardSteps==null || bank.guardSteps.Length==0) bank.guardSteps=new[]{Clip("SentinelHeavyStep1"),Clip("SentinelHeavyStep2"),Clip("SentinelHeavyStep3")};
            if(bank.pickup==null) bank.pickup=Clip("KeyPickup"); if(bank.unlock==null) bank.unlock=Clip("DoorUnlock");
            if(bank.locked==null) bank.locked=Clip("DoorLocked"); if(bank.damage==null) bank.damage=Clip("Damage");
            if(bank.victory==null) bank.victory=Clip("TreasureVictory"); if(bank.defeat==null) bank.defeat=Clip("Defeat");
            if(bank.alert==null) bank.alert=Clip("SentinelAlert"); if(bank.spikes==null) bank.spikes=Clip("SpikeMechanism");
            if(bank.laser==null) bank.laser=Clip("LaserPulse"); if(bank.pressureClick==null) bank.pressureClick=Clip("PressureClick");
            if(bank.crusherWindup==null) bank.crusherWindup=Clip("CrusherWindup"); if(bank.crusherImpact==null) bank.crusherImpact=Clip("CrusherImpact");
            if(bank.ambience==null) bank.ambience=Clip("RuinsAmbience");
            if(bank.pickup==null || bank.playerSteps[0]==null || bank.guardSteps[0]==null) throw new System.Exception("Audio files have not imported yet. Refresh the project and retry.");
            EditorUtility.SetDirty(bank); return bank;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(position.x,-position.y); rect.sizeDelta=size; return rect;
        }
        static Image Graphic(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var image=Rect(name,parent,position,size).gameObject.AddComponent<Image>(); image.raycastTarget=false; return image;
        }
        static void CreateInventory(HuntUI ui)
        {
            var bar=Rect("Inventory Bar",ui.HUD.transform,Vector2.zero,new Vector2(620,66));
            bar.anchorMin=bar.anchorMax=new Vector2(.5f,0); bar.pivot=new Vector2(.5f,0); bar.anchoredPosition=new Vector2(0,24);
            var background=bar.gameObject.AddComponent<Image>(); background.color=new Color(.025f,.055f,.075f,.94f); background.raycastTarget=false;
            var layout=bar.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.childAlignment=TextAnchor.MiddleCenter; layout.spacing=8;
            layout.childControlWidth=layout.childControlHeight=false; layout.childForceExpandWidth=layout.childForceExpandHeight=false;
            ui.InventoryLabels=new Text[5]; ui.InventoryIcons=new Image[5];
            for(int i=0;i<5;i++)
            {
                var slot=Rect(((KeyKind)i)+" Key Slot",bar,Vector2.zero,new Vector2(112,54));
                var image=slot.gameObject.AddComponent<Image>(); image.color=new Color(.065f,.13f,.15f); image.raycastTarget=false;
                var icon=Graphic("Key Icon",slot,new Vector2(10,12),new Vector2(13,13)); ui.InventoryIcons[i]=icon;
                Graphic("Stem",icon.transform,new Vector2(5,11),new Vector2(4,18));
                Graphic("Tooth",icon.transform,new Vector2(7,22),new Vector2(9,4));
                var text=Rect("Held Item",slot,new Vector2(32,5),new Vector2(78,44)).gameObject.AddComponent<Text>();
                text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize=13; text.alignment=TextAnchor.MiddleCenter; text.text="EMPTY"; text.raycastTarget=false; ui.InventoryLabels[i]=text;
            }
            var notice=ui.Notice.rectTransform; notice.anchoredPosition=new Vector2(36,-590); notice.sizeDelta=new Vector2(1180,34);
        }
        static void UpdateDefaultText(HuntUI ui)
        {
            foreach(var panel in new[]{ui.MainMenu,ui.PauseMenu})
            foreach(var text in panel.GetComponentsInChildren<Text>(true))
            {
                if(text.text.Contains("Find the sun key.")) text.text="Collect colored keys. Outsmart the sentinels.\nUnlock the doors and reach the hidden vault.";
                if(text.text.Contains("Traps pulse red;")) text.text="WASD / ARROWS  Move    MOUSE  Look    SHIFT  Sprint\n\nSprint footsteps alert nearby guards. Walk to stay quiet.\nTime spikes and lasers. Leave a clicking tile immediately.";
                if(text.text.Contains("Time spikes and lasers.")) text.text="WASD / ARROWS  Move    MOUSE  Orbit    SHIFT  Sprint\n\nFollow the opening through moving laser bars.\nWalk near robots. Step off clicking floors before they collapse.";
            }
            var crosshair=ui.HUD.transform.Find("Crosshair"); if(crosshair!=null) crosshair.gameObject.SetActive(false);
        }
    }
}
