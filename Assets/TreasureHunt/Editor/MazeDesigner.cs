using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreasureHunt.Editor
{
    public sealed class MazeDesigner : EditorWindow
    {
        MazeSettings settings;
        SerializedObject serializedSettings;
        [MenuItem("Treasure Hunt/Maze Designer")]
        public static void Open() => GetWindow<MazeDesigner>("Maze Designer");
        void OnEnable() { settings=LoadSettings(); }
        public static MazeSettings LoadSettings()
        {
            const string folder="Assets/TreasureHunt/Resources";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/TreasureHunt","Resources");
            var asset=AssetDatabase.LoadAssetAtPath<MazeSettings>(folder+"/MazeSettings.asset");
            if(asset==null) { asset=CreateInstance<MazeSettings>(); AssetDatabase.CreateAsset(asset,folder+"/MazeSettings.asset"); AssetDatabase.SaveAssets(); }
            return asset;
        }
        void OnGUI()
        {
            EditorGUILayout.Space(12); EditorGUILayout.LabelField("TREASURE HUNT / MAZE DESIGNER",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Set the maze dimensions and difficulty, then generate a complete level in the scene. The seed reproduces the same maze. Geometry, guards, explorer and Canvas are saved in the hierarchy. Regenerating replaces the maze and guards; existing UI and lighting are preserved.",MessageType.Info);
            settings=(MazeSettings)EditorGUILayout.ObjectField("Level settings",settings,typeof(MazeSettings),false);
            if(settings==null) return;
            serializedSettings=new SerializedObject(settings); serializedSettings.Update();
            var property=serializedSettings.GetIterator(); bool next=true;
            while(property.NextVisible(next)) { next=false; if(property.name!="m_Script") EditorGUILayout.PropertyField(property,true); }
            serializedSettings.ApplyModifiedProperties();
            EditorGUILayout.Space(12);
            if(GUILayout.Button("NEW RANDOM SEED",GUILayout.Height(32))) { Undo.RecordObject(settings,"Random maze seed"); settings.seed=Guid.NewGuid().GetHashCode()&int.MaxValue; EditorUtility.SetDirty(settings); }
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            if(GUILayout.Button("GENERATE COMPLETE EDITABLE LEVEL",GUILayout.Height(48)))
            {
                var game=FindAnyObjectByType<HuntGame>();
                if(game==null) { var root=new GameObject("Treasure Hunt"); Undo.RegisterCreatedObjectUndo(root,"Create game"); game=root.AddComponent<HuntGame>(); }
                int group=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Regenerate editable maze");
                EditableSceneFactory.GenerateLevel(game,settings);
                Undo.CollapseUndoOperations(group); Selection.activeGameObject=game.Maze.gameObject;
                if(SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        // Run with -executeMethod TreasureHunt.Editor.MazeDesigner.PrepareAndValidate.
        public static void PrepareAndValidate()
        {
            for(int seed=0;seed<100;seed++)
            {
                var layout=new MazeLayout(5+seed%27,5+(seed*7)%27,seed);
                var seen=new HashSet<Vector2Int>(); var queue=new Queue<Vector2Int>(); queue.Enqueue(Vector2Int.zero); seen.Add(Vector2Int.zero);
                int edges=0;
                while(queue.Count>0)
                {
                    var cell=queue.Dequeue();
                    for(int d=0;d<4;d++) if(layout.Open[cell.x,cell.y,d])
                    {
                        edges++; var n=cell+MazeLayout.Directions[d];
                        if(n.x<0||n.y<0||n.x>=layout.Width||n.y>=layout.Height||!layout.Open[n.x,n.y,(d+2)%4]) throw new Exception("Invalid maze passage");
                        if(seen.Add(n)) queue.Enqueue(n);
                    }
                }
                int expected=layout.Width*layout.Height;
                if(seen.Count!=expected||edges/2!=expected-1||layout.Route.Count<3) throw new Exception("Maze connectivity failed");
                for(int i=1;i<layout.Route.Count;i++)
                {
                    var delta=layout.Route[i]-layout.Route[i-1]; int d=Array.IndexOf(MazeLayout.Directions,delta);
                    if(d<0||!layout.Open[layout.Route[i-1].x,layout.Route[i-1].y,d]) throw new Exception("Broken treasure path");
                }
                for(int count=2;count<=5;count++)
                {
                    int previous=0;
                    for(int stage=0;stage<count;stage++)
                    {
                        MazeChallenges.Stage(layout.Route.Count,count,stage,out int keyIndex,out int doorIndex);
                        if(keyIndex<previous || keyIndex>=doorIndex || doorIndex>=layout.Route.Count) throw new Exception("A colored key is locked behind its own door");
                        previous=doorIndex;
                    }
                }
            }
            // Builds consume the saved level; they never regenerate a designer's edits.
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/TreasureHunt.unity");
            var game=FindAnyObjectByType<HuntGame>();
            if(game==null || game.Maze==null || game.UI==null || game.Player==null)
            {
                EditableSceneFactory.GenerateInCurrentScene();
                EditorSceneManager.SaveScene(scene);
            }
            EditableSceneFactory.ValidateScene();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/TreasureHunt.unity",true)};
            if(AssetDatabase.LoadAssetAtPath<Material>("Assets/TreasureHunt/Resources/HuntMaterial.mat")==null)
            {
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",Color.white);
                AssetDatabase.CreateAsset(material,"Assets/TreasureHunt/Resources/HuntMaterial.mat");
            }
            if(AssetDatabase.LoadAssetAtPath<Material>("Assets/TreasureHunt/Resources/HuntStone.mat")==null)
                AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")),"Assets/TreasureHunt/Resources/HuntStone.mat");
            AssetDatabase.SaveAssets();
            Debug.Log("TREASURE HUNT VALIDATION PASSED: 100 seeds, all cells connected, reciprocal passages, valid treasure routes. Playable scene prepared.");
        }
        public static void BuildWindows()
        {
            PrepareAndValidate();
            PlayerSettings.companyName="Independent"; PlayerSettings.productName="Treasure Hunt";
            PlayerSettings.defaultScreenWidth=1280; PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var result=BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,"Builds/TreasureHunt/TreasureHunt.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows build failed: "+result.summary.result);
        }
    }
}
