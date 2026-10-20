using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TreasureHunt.Editor
{
    public static class PitFloorSetup
    {
        public static void RestoreOldPits(MazeBuilder maze)
        {
            foreach(var pit in maze.Pits) if(pit!=null) AddFoundation(maze,pit.transform.position);
        }
        public static void Prepare(MazeBuilder maze)
        {
            // Replace the old single foundation slab so disappearing covers open real holes.
            Transform original=null;
            foreach(var child in maze.GetComponentsInChildren<Transform>(true)) if(child.name=="Foundation") { original=child; break; }
            if(original!=null)
            {
                Undo.DestroyObjectImmediate(original.gameObject);
                foreach(var cell in maze.Layout.Cells) AddFoundation(maze,maze.Layout.World(cell,maze.settings.cellSize));
            }
            foreach(var support in maze.GetComponentsInChildren<BoxCollider>(true))
            {
                if(support.name!="Foundation Tile" && support.name!="Floor inlay") continue;
                bool foundation=support.name=="Foundation Tile"; var t=support.transform;
                var p=t.position; p.y=maze.transform.position.y-(foundation?.4f:.05f); t.position=p;
                float size=maze.settings.cellSize-(foundation?0:.08f); t.localScale=new Vector3(size,foundation?.5f:.1f,size);
            }
            Physics.SyncTransforms();
            var supports=new List<BoxCollider>(maze.GetComponentsInChildren<BoxCollider>(true));
            foreach(var pit in maze.Pits)
            foreach(var support in supports)
            {
                if(support==null || support.transform.IsChildOf(pit.transform)) continue;
                if(support.name!="Foundation Tile" && support.name!="Floor inlay") continue;
                var bounds=support.bounds; Vector3 center=pit.transform.position;
                if(center.x>bounds.min.x && center.x<bounds.max.x && center.z>bounds.min.z && center.z<bounds.max.z && Mathf.Abs(bounds.max.y-center.y)<.2f)
                    Undo.DestroyObjectImmediate(support.gameObject);
            }
            Physics.SyncTransforms();
        }
        static void AddFoundation(MazeBuilder maze,Vector3 position)
        {
            var go=maze.Cube("Foundation Tile",position+Vector3.down*.4f,new Vector3(maze.settings.cellSize,.5f,maze.settings.cellSize),maze.Stone);
            var inlay=maze.Cube("Floor inlay",position+Vector3.down*.05f,new Vector3(maze.settings.cellSize-.08f,.1f,maze.settings.cellSize-.08f),MazeBuilder.Material(new Color(.085f,.14f,.17f)));
            var floors=maze.transform.Find("Floors"); if(floors!=null) inlay.transform.SetParent(floors,true); if(floors!=null) go.transform.SetParent(floors,true);
        }
    }
}
