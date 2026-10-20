using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TreasureHunt.Editor
{
    public static class RobotSceneSetup
    {
        const string ModelPath="Assets/TreasureHunt/Resources/robot-police-unit-animated/source/Robot Police walking.fbx";
        const string TextureRoot="Assets/TreasureHunt/Resources/robot-police-unit-animated/textures/";
        const string Generated="Assets/TreasureHunt/Generated/";
        public static void Configure(HuntGame game)
        {
            if(!AssetDatabase.IsValidFolder(Generated.TrimEnd('/'))) AssetDatabase.CreateFolder("Assets/TreasureHunt","Generated");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if(model==null) throw new Exception("The downloaded robot FBX is missing.");
            var controller=Controller();
            if(game.Player.robotModel==null) game.Player.robotModel=Attach(game.Player.gameObject,model,controller,RobotMaterial(false),game);
            EnsureOverrides(game.Player.robotModel,game.Player.GetComponent<RobotAnimation>().animator,controller,RobotMaterial(false));
            foreach(var guard in game.GetComponentsInChildren<HuntGuard>(true))
            {
                var existing=guard.GetComponent<RobotAnimation>();
                if(existing!=null) { EnsureOverrides(existing.animator.transform,existing.animator,controller,RobotMaterial(true)); continue; }
                var body=guard.transform.Find("Body (replace with your model)"); if(body!=null) Undo.DestroyObjectImmediate(body.gameObject);
                Attach(guard.gameObject,model,controller,RobotMaterial(true),game);
            }
            CalibrateGait(game,model);
            RobotGroundingSetup.Configure(game);
            var camera=game.Player.view;
            camera.transform.SetParent(game.transform,true);
            if(game.Player.followCamera==null)
            {
                game.Player.followCamera=camera.gameObject.GetComponent<ThirdPersonCamera>()??camera.gameObject.AddComponent<ThirdPersonCamera>(); camera.fieldOfView=68;
            }
            game.Player.followCamera.target=game.Player; game.Player.followCamera.yaw=game.Player.transform.eulerAngles.y;
            Physics.SyncTransforms(); game.Player.followCamera.Snap();
            EditorUtility.SetDirty(game.Player); EditorUtility.SetDirty(game.Player.followCamera);
        }
        static void CalibrateGait(HuntGame game,GameObject model)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Generated+"RobotWalk.anim");
            var sample=UnityEngine.Object.Instantiate(model); sample.hideFlags=HideFlags.HideAndDontSave;
            try
            {
                sample.transform.position=Vector3.zero; sample.transform.localScale=game.Player.robotModel.localScale;
                var feet=sample.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="CC_Base_L_Foot"||t.name=="CC_Base_R_Foot").ToArray();
                if(feet.Length!=2) throw new Exception("Robot gait calibration requires both foot bones.");
                const int frames=120; var positions=new Vector3[2,frames]; float dt=clip.length/frames;
                for(int i=0;i<frames;i++) { clip.SampleAnimation(sample,i*dt); for(int f=0;f<2;f++) positions[f,i]=feet[f].position; }
                var contactSpeeds=new System.Collections.Generic.List<float>();
                for(int f=0;f<2;f++)
                {
                    float low=float.MaxValue,high=float.MinValue;
                    for(int i=0;i<frames;i++) { low=Mathf.Min(low,positions[f,i].y); high=Mathf.Max(high,positions[f,i].y); }
                    for(int i=1;i<frames;i++)
                    {
                        if(positions[f,i].y>low+(high-low)*.25f) continue;
                        var delta=positions[f,i]-positions[f,i-1]; delta.y=0;
                        if(delta.magnitude/dt>.1f) contactSpeeds.Add(delta.magnitude/dt);
                    }
                }
                contactSpeeds.Sort(); if(contactSpeeds.Count<4) throw new Exception("Insufficient planted-foot samples to calibrate robot gait.");
                float nativeSpeed=contactSpeeds[contactSpeeds.Count/2];
                foreach(var driver in game.GetComponentsInChildren<RobotAnimation>(true))
                { driver.referenceSpeed=nativeSpeed; driver.strideLength=nativeSpeed*clip.length; EditorUtility.SetDirty(driver); }
                Debug.Log("ROBOT GAIT CALIBRATED: "+nativeSpeed+" m/s, stride "+(nativeSpeed*clip.length)+"m, clip "+clip.length+"s, contact samples "+contactSpeeds.Count);
            }
            finally { UnityEngine.Object.DestroyImmediate(sample); }
        }
        static void EnsureOverrides(Transform model,Animator animator,RuntimeAnimatorController controller,Material material)
        {
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }
        static Transform Attach(GameObject actor,GameObject model,RuntimeAnimatorController controller,Material material,HuntGame game)
        {
            var wrapper=new GameObject(actor==game.Player.gameObject?"Blue Player Robot":"Red Sentinel Robot").transform;
            wrapper.SetParent(actor.transform,false);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,wrapper);
            instance.transform.localPosition=Vector3.zero; instance.transform.localRotation=Quaternion.identity; instance.transform.localScale=Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            foreach(var collider in instance.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach(var camera in instance.GetComponentsInChildren<Camera>(true)) UnityEngine.Object.DestroyImmediate(camera.gameObject);
            var renderers=instance.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0) throw new Exception("Robot FBX has no renderers.");
            var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale=1.78f/Mathf.Max(.01f,bounds.size.y); wrapper.localScale=Vector3.one*scale;
            bounds=renderers[0].bounds; foreach(var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            Vector3 localCenter=actor.transform.InverseTransformPoint(bounds.center);
            Vector3 bottom=actor.transform.InverseTransformPoint(new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
            wrapper.localPosition=new Vector3(-localCenter.x,-bottom.y,-localCenter.z);
            var animator=instance.GetComponentInChildren<Animator>(true);
            if(animator==null) animator=instance.AddComponent<Animator>();
            animator.runtimeAnimatorController=controller; animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            var driver=actor.AddComponent<RobotAnimation>(); driver.animator=animator; driver.game=game;
            driver.referenceSpeed=1.5f;
            Debug.Log("ROBOT ATTACHED: "+actor.name+" original bounds "+bounds.size+"; scale "+scale+"; material "+material.name);
            return wrapper;
        }
        static Material RobotMaterial(bool enemy)
        {
            string path=Generated+(enemy?"Robot_Red.mat":"Robot_Blue.mat");
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material!=null) return material;
            var shader=Shader.Find("Treasure Hunt/Robot Palette");
            if(shader==null) throw new Exception("Robot palette shader has not imported.");
            material=new Material(shader) {name=enemy?"Red Robot Armor":"Original Blue Robot Armor"};
            material.SetTexture("_BaseMap",Texture("tripo_node_e0d7fd6a_gameasset_Mat_1_Diffuse.png"));
            var normal=Texture("tripo_node_e0d7fd6a_gameasset_Mat_1_Normal.png");
            var normalImporter=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal)) as TextureImporter;
            if(normalImporter!=null && normalImporter.textureType!=TextureImporterType.NormalMap) { normalImporter.textureType=TextureImporterType.NormalMap; normalImporter.SaveAndReimport(); }
            material.SetTexture("_BumpMap",normal); material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",Texture("tripo_node_e0d7fd6a_gameasset_Mat_1_Metallic.png")); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetTexture("_EmissionMap",Texture("tripo_node_e0d7fd6a_gameasset_EmissionStre.png")); material.SetColor("_EmissionColor",new Color(.07f,.07f,.07f)); material.EnableKeyword("_EMISSION");
            material.SetColor("_BaseColor",new Color(1.6f,1.6f,1.6f)); material.SetFloat("_EnemyRecolor",enemy?1:0);
            material.SetFloat("_Metallic",.45f); material.SetFloat("_Smoothness",.4f);
            AssetDatabase.CreateAsset(material,path); return material;
        }
        static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+name);
        static RuntimeAnimatorController Controller()
        {
            const string path=Generated+"RobotLocomotion.controller";
            var saved=AssetDatabase.LoadAssetAtPath<AnimatorController>(path); if(saved!=null) return saved;
            var source=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
            if(source==null) throw new Exception("The robot contains no walking animation.");
            var walk=UnityEngine.Object.Instantiate(source); walk.name="Robot Walk In Place"; walk.legacy=false;
            var bindings=AnimationUtility.GetCurveBindings(walk);
            var translations=bindings.Where(b=>b.type==typeof(Transform)&&(b.propertyName=="m_LocalPosition.x"||b.propertyName=="m_LocalPosition.z")).ToArray();
            int shortest=translations.Length>0?translations.Min(b=>b.path.Count(c=>c=='/')):-1;
            foreach(var binding in translations.Where(b=>b.path.Count(c=>c=='/')==shortest))
            {
                var curve=AnimationUtility.GetEditorCurve(walk,binding); float value=curve.Evaluate(0);
                AnimationUtility.SetEditorCurve(walk,binding,AnimationCurve.Constant(0,walk.length,value));
            }
            var settings=AnimationUtility.GetAnimationClipSettings(walk); settings.loopTime=true; settings.loopBlend=true; AnimationUtility.SetAnimationClipSettings(walk,settings);
            AssetDatabase.CreateAsset(walk,Generated+"RobotWalk.anim");
            var idle=new AnimationClip {name="Robot Idle Pose",frameRate=30};
            foreach(var binding in AnimationUtility.GetCurveBindings(walk))
            {
                float value=AnimationUtility.GetEditorCurve(walk,binding).Evaluate(0);
                AnimationUtility.SetEditorCurve(idle,binding,AnimationCurve.Constant(0,1,value));
            }
            AssetDatabase.CreateAsset(idle,Generated+"RobotIdle.anim");
            var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Moving",AnimatorControllerParameterType.Bool); controller.AddParameter("Playback",AnimatorControllerParameterType.Float);
            var machine=controller.layers[0].stateMachine; var idleState=machine.AddState("Idle"); idleState.motion=idle; machine.defaultState=idleState;
            var walkState=machine.AddState("Walk"); walkState.motion=walk; walkState.speedParameter="Playback"; walkState.speedParameterActive=true;
            var start=idleState.AddTransition(walkState); start.hasExitTime=false; start.duration=.12f; start.AddCondition(AnimatorConditionMode.If,0,"Moving");
            var stop=walkState.AddTransition(idleState); stop.hasExitTime=false; stop.duration=.12f; stop.AddCondition(AnimatorConditionMode.IfNot,0,"Moving");
            Debug.Log("ROBOT ANIMATION: source "+source.name+" length "+source.length+"s; bindings "+bindings.Length+"; in-place translation bindings "+translations.Length);
            return controller;
        }
    }
}
