using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace TreasureHunt.Editor
{
    public static class MotionReviewSetup
    {
        public static void Capture() => EditorReviewRunner.Enqueue(CaptureFrames());
        static IEnumerator CaptureFrames()
        {
            var game=UnityEngine.Object.FindAnyObjectByType<HuntGame>();
            var driver=game.Player.GetComponent<RobotAnimation>();
            var scene=EditorSceneManager.NewPreviewScene();
            var clone=UnityEngine.Object.Instantiate(game.Player.robotModel.gameObject);SceneManager.MoveGameObjectToScene(clone,scene);clone.transform.position=Vector3.zero;clone.transform.rotation=Quaternion.identity;
            var animator=clone.GetComponentInChildren<Animator>();animator.enabled=false;
            var run=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TreasureHunt/Generated/RobotRun.anim");
            var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TreasureHunt/Generated/RobotWalk.anim");
            var soles=driver.soles.Select(s=>new RobotAnimation.SoleProbe {bone=animator.transform.Find(AnimationUtility.CalculateTransformPath(s.bone,driver.animator.transform)),points=s.points}).ToArray();
            var cameraObject=new GameObject("Motion Review Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.GetComponent<Camera>();camera.scene=scene;
            camera.transform.position=new Vector3(3.5f,1.8f,3);camera.transform.LookAt(new Vector3(0,.9f,0));camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.06f,.08f);camera.fieldOfView=32;
            var lightObject=new GameObject("Key Light",typeof(Light));SceneManager.MoveGameObjectToScene(lightObject,scene);lightObject.GetComponent<Light>().type=LightType.Directional;lightObject.GetComponent<Light>().intensity=2.5f;lightObject.transform.rotation=Quaternion.Euler(40,-45,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);SceneManager.MoveGameObjectToScene(floor,scene);var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.12f,.20f,.22f);floor.GetComponent<Renderer>().sharedMaterial=material;
            var texture=new RenderTexture(960,720,24);camera.targetTexture=texture;Directory.CreateDirectory("Deliverables/Motion");
            try
            {
                run.SampleAnimation(animator.gameObject,0);
                float initial=soles.Min(s=>s.points.Min(p=>s.bone.TransformPoint(p).y));clone.transform.position-=Vector3.up*initial;
                var phases=new[]{0f,.16f,.40f,.50f,.66f,.90f};
                foreach(float phase in phases)
                {
                    run.SampleAnimation(animator.gameObject,run.length*phase);
                    foreach(var renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.updateWhenOffscreen=true;
                    yield return null; yield return null;
                    camera.Render();
                    var previous=RenderTexture.active;RenderTexture.active=texture;var png=new Texture2D(960,720,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,960,720),0,0);png.Apply();File.WriteAllBytes("Deliverables/Motion/run-"+Mathf.RoundToInt(phase*100).ToString("00")+".png",png.EncodeToPNG());UnityEngine.Object.DestroyImmediate(png);RenderTexture.active=previous;
                    Debug.Log("RUN POSE "+phase+": sole heights "+string.Join(", ",soles.Select(s=>s.points.Min(p=>s.bone.TransformPoint(p).y).ToString("F3"))));
                }
                foreach(var clip in new[]{walk,run})
                {
                    var events=AnimationUtility.GetAnimationEvents(clip);
                    if(events.Length!=2 || events.Select(e=>e.intParameter).Distinct().Count()!=2 || events.Any(e=>e.functionName!="FootContact")) throw new Exception("Expected one left and one right contact per cycle: "+clip.name);
                    Debug.Log("MOTION CHECK PASSED: two alternating contact events for "+clip.name+": "+string.Join(", ",events.Select(e=>e.time.ToString("F3"))));
                }
            }
            finally {camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(material);EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
