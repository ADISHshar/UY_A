using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TreasureHunt.Editor
{
    public static class RobotGroundingSetup
    {
        public static void Apply()
        {
            Configure(UnityEngine.Object.FindAnyObjectByType<HuntGame>());
        }
        public static void Configure(HuntGame game)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/TreasureHunt/Generated/RobotWalk.anim");
            Physics.SyncTransforms();
            foreach(var driver in game.GetComponentsInChildren<RobotAnimation>(true))
            {
                var visual=driver.animator.transform;
                while(visual.parent!=driver.transform) visual=visual.parent;
                driver.visualRoot=visual;
                var sample=UnityEngine.Object.Instantiate(driver.animator.gameObject,visual);
                sample.hideFlags=HideFlags.HideAndDontSave;
                sample.transform.localPosition=driver.animator.transform.localPosition;
                sample.transform.localRotation=driver.animator.transform.localRotation;
                sample.transform.localScale=driver.animator.transform.localScale;
                try
                {
                    clip.SampleAnimation(sample,0);
                    var feet=sample.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="CC_Base_L_Foot"||t.name=="CC_Base_R_Foot").ToArray();
                    if(feet.Length!=2) throw new Exception("Robot foot bones are missing.");
                    var points=new[]{new List<Vector3>(),new List<Vector3>()};
                    var baked=new Mesh();
                    try
                    {
                        foreach(var renderer in sample.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            renderer.BakeMesh(baked);
                            foreach(var vertex in baked.vertices)
                            {
                                var world=renderer.transform.TransformPoint(vertex);
                                int f=(world-feet[0].position).sqrMagnitude<(world-feet[1].position).sqrMagnitude?0:1;
                                if(world.y<=feet[f].position.y+.015f && Vector3.Distance(world,feet[f].position)<.35f)
                                    points[f].Add(feet[f].InverseTransformPoint(world));
                            }
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(baked); }
                    driver.soles=new RobotAnimation.SoleProbe[2];
                    for(int f=0;f<2;f++)
                    {
                        if(points[f].Count==0) throw new Exception("No sole geometry found for "+feet[f].name);
                        string path=AnimationUtility.CalculateTransformPath(feet[f],sample.transform);
                        driver.soles[f]=new RobotAnimation.SoleProbe {bone=driver.animator.transform.Find(path),points=points[f].ToArray()};
                    }
                    clip.SampleAnimation(driver.animator.gameObject,0);
                    driver.GroundFeet(); EditorUtility.SetDirty(driver); EditorUtility.SetDirty(visual);
                    Debug.Log("ROBOT GROUNDED: "+driver.name+"; sole samples "+points[0].Count+"/"+points[1].Count+"; model local height "+visual.localPosition.y);
                }
                finally { UnityEngine.Object.DestroyImmediate(sample); }
            }
        }
    }
}
