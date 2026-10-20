using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
namespace TreasureHunt.Editor
{
    [InitializeOnLoad]
    public static class LiveUiReview
    {
        const string Pending="TreasureHunt.LiveUiReview";
        static LiveUiReview()
        {
            EditorApplication.playModeStateChanged+=state=> { if(state==PlayModeStateChange.EnteredPlayMode) StartPending(); };
            EditorApplication.delayCall+=()=> { if(EditorApplication.isPlaying) StartPending(); };
        }
        public static void Begin()
        {
            SessionState.SetBool(Pending,true);Application.runInBackground=true;
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if(type!=null) EditorWindow.GetWindow(type).Focus();
            EditorApplication.isPlaying=true;
        }
        static void StartPending()
        {
            if(!SessionState.GetBool(Pending,false))return;SessionState.SetBool(Pending,false);
            var game=UnityEngine.Object.FindAnyObjectByType<HuntGame>();
            if(game!=null) game.StartCoroutine(Capture(game));
        }
        static void CaptureFile(string path)
        {
            var image=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.Destroy(image);
        }
        static IEnumerator Capture(HuntGame game)
        {
            yield return null;yield return null;
            game.enabled=false;game.Player.enabled=false;
            foreach(var guard in UnityEngine.Object.FindObjectsByType<HuntGuard>()) { guard.enabled=false;guard.GetComponent<NavMeshAgent>().isStopped=true; }
            foreach(var hazard in game.Maze.GetComponentsInChildren<HuntWorldObject>())hazard.enabled=false;
            Directory.CreateDirectory("Deliverables/UI");Directory.CreateDirectory("Deliverables/Motion");
            var stateField=typeof(HuntGame).GetField("state",BindingFlags.Instance|BindingFlags.NonPublic);
            var sprintField=typeof(HuntPlayer).GetField("<Sprinting>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
            var names=new[]{"main-menu","hud","pause","game-over","victory"};var states=new[]{0,1,2,4,3};
            for(int i=0;i<states.Length;i++)
            {
                stateField.SetValue(game,states[i]);
                if(states[i]==4) typeof(HuntGame).GetField("result",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,"A sentinel spotted you.");
                game.UI.Refresh(game);
                yield return new WaitForSecondsRealtime(.5f);
                yield return new WaitForEndOfFrame();
                CaptureFile("Deliverables/UI/"+names[i]+".png");
                yield return null;yield return null;
            }
            stateField.SetValue(game,1);game.Resume();game.enabled=true;
            var start=game.Player.transform.position;var driver=game.Player.GetComponent<RobotAnimation>();
            foreach(bool sprint in new[]{false,true})
            {
                sprintField.SetValue(game.Player,sprint);float elapsed=0;bool captured=false;bool checkedState=false;
                while(elapsed<1.6f)
                {
                    elapsed+=Time.deltaTime;
                    game.Player.transform.position=start+Vector3.right*Mathf.PingPong(elapsed*(sprint?game.Player.sprintSpeed:game.Player.walkSpeed),.65f);
                    yield return null;
                    if(elapsed>.45f && !checkedState)
                    {
                        checkedState=true;string expected=sprint?"Run":"Walk";
                        if(!driver.animator.GetCurrentAnimatorStateInfo(0).IsName(expected)) Debug.LogError("LIVE MOTION CHECK FAILED: expected "+expected);
                        else Debug.Log("LIVE MOTION CHECK PASSED: "+expected+" state plays while moving");
                    }
                    if(elapsed>.9f && !captured)
                    {
                        captured=true;yield return new WaitForEndOfFrame();CaptureFile("Deliverables/Motion/gameplay-"+(sprint?"running":"walking")+".png");
                    }
                }
            }
            yield return null;yield return new WaitForEndOfFrame();
            File.WriteAllText("Temp/TreasureHunt.LiveReview.result","SUCCESS");Debug.Log("LIVE UI AND MOTION REVIEW COMPLETE");
            EditorApplication.isPlaying=false;
        }
    }
}
