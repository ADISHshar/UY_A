using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TreasureHunt.Editor
{
    [InitializeOnLoad]
    public static class RobotIdleSetup
    {
        const string IdlePath="Assets/TreasureHunt/Generated/RobotIdle.anim";
        const string ModelPath="Assets/TreasureHunt/Resources/robot-police-unit-animated/source/Robot Police walking.fbx";
        const string ClipName="Robot Standing Idle";
        static RobotIdleSetup() { EditorApplication.update+=EnsureIdle; }
        static void EnsureIdle()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update-=EnsureIdle;
            var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
            if(saved!=null && saved.name!=ClipName) Build();
        }
        [MenuItem("Treasure Hunt/Create Robot Standing Idle")]
        public static void Build()
        {
            var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if(saved==null || model==null) throw new InvalidOperationException("Robot model and idle clip must exist first.");
            var sample=UnityEngine.Object.Instantiate(model);
            sample.hideFlags=HideFlags.HideAndDontSave;
            var clip=new AnimationClip {name=ClipName,frameRate=30};
            try
            {
                // Use the rig's standing bind pose so both legs stay straight and planted.
                foreach(var side in new[]{"L","R"})
                {
                    var upper=sample.GetComponentsInChildren<Transform>().First(t=>t.name=="CC_Base_"+side+"_Upperarm");
                    var forearm=sample.GetComponentsInChildren<Transform>().First(t=>t.name=="CC_Base_"+side+"_Forearm");
                    Vector3 direction=Vector3.down+sample.transform.right*(side=="L"?-.12f:.12f)+sample.transform.forward*.06f;
                    upper.rotation=Quaternion.FromToRotation(forearm.position-upper.position,direction.normalized)*upper.rotation;
                }
                const float duration=4;
                foreach(var bone in sample.GetComponentsInChildren<Transform>())
                {
                    if(bone==sample.transform) continue;
                    string path=AnimationUtility.CalculateTransformPath(bone,sample.transform);
                    Vector3 position=bone.localPosition,scale=bone.localScale;
                    for(int axis=0;axis<3;axis++)
                    {
                        string suffix=axis==0?"x":axis==1?"y":"z";
                        clip.SetCurve(path,typeof(Transform),"m_LocalPosition."+suffix,AnimationCurve.Constant(0,duration,position[axis]));
                        clip.SetCurve(path,typeof(Transform),"m_LocalScale."+suffix,AnimationCurve.Constant(0,duration,scale[axis]));
                    }
                    var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
                    for(int frame=0;frame<=120;frame++)
                    {
                        float time=frame/30f,wave=Mathf.Sin(time/duration*Mathf.PI*2);
                        Vector3 offset=Vector3.zero;
                        if(bone.name=="CC_Base_Spine02") offset=new Vector3(.7f*wave,0,.4f*wave);
                        if(bone.name=="CC_Base_Head") offset=new Vector3(0,2.5f*wave,0);
                        if(bone.name.EndsWith("_Upperarm")) offset=new Vector3(.8f*wave,0,0);
                        Quaternion rotation=bone.localRotation*Quaternion.Euler(offset);
                        for(int axis=0;axis<4;axis++) curves[axis].AddKey(time,rotation[axis]);
                    }
                    for(int axis=0;axis<4;axis++) clip.SetCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[axis],curves[axis]);
                }
                clip.EnsureQuaternionContinuity();
                var settings=AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime=true; settings.loopBlend=true;
                AnimationUtility.SetAnimationClipSettings(clip,settings);
                EditorUtility.CopySerialized(clip,saved); EditorUtility.SetDirty(saved); AssetDatabase.SaveAssets();
                Debug.Log("ROBOT STANDING IDLE SAVED: four-second loop, planted feet, subtle head and upper-body movement.");
            }
            finally { UnityEngine.Object.DestroyImmediate(sample); UnityEngine.Object.DestroyImmediate(clip); }
        }
    }
}
