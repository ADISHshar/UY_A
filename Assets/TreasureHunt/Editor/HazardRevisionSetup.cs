using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TreasureHunt.Editor
{
    public static class HazardRevisionSetup
    {
        public static void Build()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/TreasureHunt.unity");
            var game=Object.FindAnyObjectByType<HuntGame>();
            game.Settings.guards=5; game.Settings.lasers=6; game.Settings.wallSpikes=5;
            EditorUtility.SetDirty(game.Settings);
            GameplayFeatureSetup.UpgradeCurrent();
            Validate(); MazeDesigner.BuildWindows();
        }
        public static void Validate()
        {
            var game=Object.FindAnyObjectByType<HuntGame>(); var maze=game.Maze; float s=game.Settings.cellSize;
            if(game.GetComponentsInChildren<HuntGuard>(true).Length!=5) throw new System.Exception("Expected five sentinels.");
            foreach(var pattern in new[]{LaserHazard.Pattern.Wall,LaserHazard.Pattern.SweepingSingle,LaserHazard.Pattern.RotatingCross})
                if(!maze.Lasers.Exists(l=>l.pattern==pattern)) throw new System.Exception("Missing laser pattern "+pattern);
            foreach(var laser in maze.Lasers)
            {
                var p=laser.transform.position; var f=laser.transform.forward;
                bool found=false;
                foreach(var cell in maze.Layout.Cells) for(int d=0;d<4;d++)
                {
                    var direction=MazeLayout.Directions[d]; var axis=new Vector3(direction.x,0,direction.y);
                    if(maze.Layout.Open[cell.x,cell.y,d] && Vector3.Distance(maze.Layout.World(cell,s)+axis*s/2,p)<.01f && Mathf.Abs(Vector3.Dot(f,axis))>.99f) found=true;
                }
                if(!found) throw new System.Exception("Laser not across an open passage: "+laser.name);
            }
            foreach(var pit in maze.Pits) if(pit.holeSize.x<s-.1f) throw new System.Exception("Pit leaves a bypass ledge.");
            int walls=0;
            foreach(var trap in maze.GetComponentsInChildren<SpikeTrap>(true))
            { if(trap.wallMounted) { walls++; if(trap.cycle>=game.Settings.spikeCycle || trap.riseSeconds>=.18f) throw new System.Exception("Wall spike should be faster."); }
              else if(trap.halfSize.x<s/2-.1f) throw new System.Exception("Ground spikes leave a bypass edge."); }
            if(walls!=game.Settings.wallSpikes) throw new System.Exception("Wrong wall spike count.");
            Debug.Log("HAZARD REVISION VALIDATED: three laser types across open edges, five enemies, full-cell pits and ground spikes, five faster wall racks.");
        }
    }
}
