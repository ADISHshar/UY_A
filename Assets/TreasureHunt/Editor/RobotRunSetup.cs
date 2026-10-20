using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace TreasureHunt.Editor
{
    [InitializeOnLoad]
    public static class RobotRunSetup
    {
        const string Root="Assets/TreasureHunt/Generated/";
        const string Model="Assets/TreasureHunt/Resources/robot-police-unit-animated/source/Robot Police walking.fbx";
        const string Version="Robot Run Flight v2";
        static RobotRunSetup() { EditorApplication.update+=EnsureRun; }
        static void EnsureRun()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update-=EnsureRun;
            var run=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"RobotRun.anim");
            if(run==null || run.name!=Version) Build();
        }
        [MenuItem("Treasure Hunt/Create Robot Running Animation")]
        public static void Build()
        {
            var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"RobotWalk.anim");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"RobotLocomotion.controller");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            if(walk==null || model==null || controller==null) throw new InvalidOperationException("Robot locomotion assets are missing.");
            var sample=UnityEngine.Object.Instantiate(model); sample.hideFlags=HideFlags.HideAndDontSave;
            var clip=new AnimationClip {name=Version,frameRate=60};
            try
            {
                var bones=sample.GetComponentsInChildren<Transform>().Where(t=>t!=sample.transform).ToArray();
                Func<string,Transform> find=name=>bones.First(t=>t.name=="CC_Base_"+name);
                var paths=bones.Select(t=>AnimationUtility.CalculateTransformPath(t,sample.transform)).ToArray();
                var restRotation=bones.Select(t=>t.localRotation).ToArray();
                var restPosition=bones.Select(t=>t.localPosition).ToArray();
                var hip=find("Hip"); var spine=find("Spine01");
                var thighs=new[]{find("L_Thigh"),find("R_Thigh")};
                var calves=new[]{find("L_Calf"),find("R_Calf")};
                var feet=new[]{find("L_Foot"),find("R_Foot")};
                var arms=new[]{find("L_Upperarm"),find("R_Upperarm")};
                var elbows=new[]{find("L_Forearm"),find("R_Forearm")};
                var footPosition=feet.Select(t=>t.position).ToArray();
                var footRotation=feet.Select(t=>t.rotation).ToArray();
                float height=Mathf.Abs(find("Head").position.y-feet.Min(t=>t.position.y))*1.1f;
                var rotations=bones.Select(t=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()}).ToArray();
                var positions=bones.Select(t=>new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()}).ToArray();
                const int frames=48;
                for(int frame=0;frame<=frames;frame++)
                {
                    float phase=frame==frames?0:(float)frame/frames;
                    float time=RobotAnimation.RunCycleSeconds*frame/frames;
                    for(int b=0;b<bones.Length;b++) { bones[b].localPosition=restPosition[b]; bones[b].localRotation=restRotation[b]; }
                    hip.position+=sample.transform.up*height*(-.065f+.025f*(1-Mathf.Cos(phase*Mathf.PI*4)));
                    spine.rotation=Quaternion.AngleAxis(12,sample.transform.right)*spine.rotation;
                    for(int side=0;side<2;side++)
                    {
                        float legPhase=Mathf.Repeat(phase+side*.5f,1);
                        // A short planted stance and a longer lifted recovery create real flight phases.
                        float stride=height*.40f,z,lift;
                        if(legPhase<.34f) { z=Mathf.Lerp(stride*.5f,-stride*.5f,legPhase/.34f); lift=0; }
                        else { float u=(legPhase-.34f)/.66f; z=Mathf.Lerp(-stride*.5f,stride*.5f,u); lift=Mathf.Sin(u*Mathf.PI)*height*.18f; }
                        Vector3 target=footPosition[side]+sample.transform.forward*z+sample.transform.up*lift;
                        SolveLeg(thighs[side],calves[side],feet[side],target,sample.transform.forward);
                        feet[side].rotation=footRotation[side];
                        Vector3 down=-sample.transform.up+sample.transform.right*(side==0?-.12f:.12f);
                        arms[side].rotation=Quaternion.FromToRotation(elbows[side].position-arms[side].position,down)*arms[side].rotation;
                        float swing=Mathf.Sin(legPhase*Mathf.PI*2)*48;
                        arms[side].rotation=Quaternion.AngleAxis(swing,sample.transform.right)*arms[side].rotation;
                        elbows[side].rotation=Quaternion.AngleAxis(-65,sample.transform.right)*elbows[side].rotation;
                    }
                    for(int b=0;b<bones.Length;b++)
                    {
                        Quaternion q=bones[b].localRotation;
                        if(frame>0)
                        {
                            var previous=new Quaternion(rotations[b][0].keys[frame-1].value,rotations[b][1].keys[frame-1].value,rotations[b][2].keys[frame-1].value,rotations[b][3].keys[frame-1].value);
                            if(Quaternion.Dot(previous,q)<0) q=new Quaternion(-q.x,-q.y,-q.z,-q.w);
                        }
                        for(int axis=0;axis<4;axis++) rotations[b][axis].AddKey(time,q[axis]);
                        for(int axis=0;axis<3;axis++) positions[b][axis].AddKey(time,bones[b].localPosition[axis]);
                    }
                }
                for(int b=0;b<bones.Length;b++)
                {
                    for(int axis=0;axis<4;axis++) clip.SetCurve(paths[b],typeof(Transform),"m_LocalRotation."+"xyzw"[axis],rotations[b][axis]);
                    for(int axis=0;axis<3;axis++) clip.SetCurve(paths[b],typeof(Transform),"m_LocalPosition."+"xyz"[axis],positions[b][axis]);
                }
                clip.EnsureQuaternionContinuity();
                var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=true; settings.loopBlend=false;
                AnimationUtility.SetAnimationClipSettings(clip,settings);
                AnimationUtility.SetAnimationEvents(clip,new[]{Contact(.005f,0),Contact(RobotAnimation.RunCycleSeconds*.5f+.005f,1)});
                var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"RobotRun.anim");
                if(saved==null) { AssetDatabase.CreateAsset(clip,Root+"RobotRun.anim"); saved=clip; }
                else { EditorUtility.CopySerialized(clip,saved); EditorUtility.SetDirty(saved); }
                AddWalkContacts(walk,sample,feet);
                var machine=controller.layers[0].stateMachine;
                var idle=machine.states.First(s=>s.state.name=="Idle").state;
                var walking=machine.states.First(s=>s.state.name=="Walk").state;
                var run=machine.states.FirstOrDefault(s=>s.state.name=="Run").state;
                if(!controller.parameters.Any(p=>p.name=="Running")) controller.AddParameter("Running",AnimatorControllerParameterType.Bool);
                if(run==null) run=machine.AddState("Run",new Vector3(540,200));
                foreach(var state in new[]{idle,walking,run}) foreach(var t in state.transitions.ToArray()) state.RemoveTransition(t);
                Transition(idle,walking,true,false); Transition(idle,run,true,true);
                Transition(walking,idle,false,false); Transition(walking,run,true,true);
                Transition(run,walking,true,false); Transition(run,idle,false,false);
                run.motion=saved; run.speedParameter="Playback"; run.speedParameterActive=true;
                EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
                Debug.Log("ROBOT RUN V2 SAVED: true running flight, fixed alternating contacts; walk servo events calibrated from foot height.");
            }
            finally { UnityEngine.Object.DestroyImmediate(sample); if(!AssetDatabase.Contains(clip)) UnityEngine.Object.DestroyImmediate(clip); }
        }
        static void SolveLeg(Transform thigh,Transform calf,Transform foot,Vector3 target,Vector3 pole)
        {
            Vector3 start=thigh.position;
            float upper=Vector3.Distance(start,calf.position),lower=Vector3.Distance(calf.position,foot.position);
            Vector3 direction=(target-start).normalized;
            float distance=Mathf.Clamp(Vector3.Distance(start,target),Mathf.Abs(upper-lower)+.0001f,(upper+lower)*.995f);
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;
            Vector3 knee=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            thigh.rotation=Quaternion.FromToRotation(calf.position-start,knee-start)*thigh.rotation;
            calf.rotation=Quaternion.FromToRotation(foot.position-calf.position,target-calf.position)*calf.rotation;
        }
        static AnimationEvent Contact(float time,int foot) => new AnimationEvent {time=time,functionName="FootContact",intParameter=foot};
        static void AddWalkContacts(AnimationClip walk,GameObject sample,Transform[] feet)
        {
            const int count=240; var heights=new float[2,count];
            for(int i=0;i<count;i++) { walk.SampleAnimation(sample,walk.length*i/count); for(int foot=0;foot<2;foot++) heights[foot,i]=feet[foot].position.y; }
            var events=new AnimationEvent[2];
            for(int foot=0;foot<2;foot++)
            {
                float low=float.PositiveInfinity,high=float.NegativeInfinity;
                for(int i=0;i<count;i++) { low=Mathf.Min(low,heights[foot,i]); high=Mathf.Max(high,heights[foot,i]); }
                float contact=low+(high-low)*.12f; int frame=0;
                for(int i=0;i<count;i++) if(heights[foot,i]<=contact && heights[foot,(i+count-1)%count]>contact) { frame=i; break; }
                events[foot]=Contact(Mathf.Max(.005f,walk.length*frame/count),foot);
            }
            AnimationUtility.SetAnimationEvents(walk,events.OrderBy(e=>e.time).ToArray()); EditorUtility.SetDirty(walk);
        }
        static void Transition(AnimatorState from,AnimatorState to,bool moving,bool running)
        {
            var transition=from.AddTransition(to); transition.hasExitTime=false; transition.duration=.10f;
            transition.AddCondition(moving?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Moving");
            if(moving) transition.AddCondition(running?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Running");
        }
    }
}
