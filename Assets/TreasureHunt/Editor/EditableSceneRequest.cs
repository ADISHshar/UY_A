using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreasureHunt.Editor
{
    // Allows a one-shot local generation request while this project is already open.
    [InitializeOnLoad]
    public static class EditableSceneRequest
    {
        const string Request="Temp/TreasureHunt.Editable.request";
        static double nextCheck;
        static EditableSceneRequest() { EditorApplication.update+=Check; }
        static void Check()
        {
            if(EditorApplication.timeSinceStartup<nextCheck) return;
            nextCheck=EditorApplication.timeSinceStartup+1;
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            string action=File.ReadAllText(Request).Trim(); File.Delete(Request);
            try
            {
                if(SceneManager.GetActiveScene().path!="Assets/Scenes/TreasureHunt.unity")
                {
                    if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new Exception("Generation cancelled to preserve the current scene.");
                    EditorSceneManager.OpenScene("Assets/Scenes/TreasureHunt.unity");
                }
                var game=UnityEngine.Object.FindAnyObjectByType<HuntGame>();
                if(game==null || game.Maze==null || game.Player==null || game.UI==null) EditableSceneFactory.GenerateInCurrentScene();
                if(action=="ui-live-review") { UiPanelSetup.Apply(); LiveUiReview.Begin(); }
                if(action=="ui-motion-review") { UiPanelSetup.Apply(); MotionReviewSetup.Capture(); }
                if(action=="ui-motion-refresh") { RobotRunSetup.Build(); UiPanelSetup.Apply(); }
                if(action=="three-enemies") EnemyCountSetup.Apply();
                if(action=="tile-respawn") TileRespawnSetup.Apply();
                if(action=="robot-grounding") RobotGroundingSetup.Apply();
                if(action=="wall-spike-fix") WallSpikeRepair.Apply();
                if(action=="hazard-revision") HazardRevisionSetup.Build();
                if(action=="upgrade-build" || action=="upgrade") GameplayFeatureSetup.UpgradeCurrent();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                if(action=="build" || action=="upgrade-build" || action=="wall-spike-fix" || action=="robot-grounding" || action=="tile-respawn") MazeDesigner.BuildWindows(); else EditableSceneFactory.ValidateScene();
                File.WriteAllText("Temp/TreasureHunt.Editable.result","SUCCESS");
            }
            catch(Exception error)
            {
                Debug.LogException(error); File.WriteAllText("Temp/TreasureHunt.Editable.result",error.ToString());
            }
        }
    }
}
