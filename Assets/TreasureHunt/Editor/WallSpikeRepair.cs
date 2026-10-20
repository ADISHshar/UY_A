using UnityEditor;
using UnityEngine;
namespace TreasureHunt.Editor
{
    public static class WallSpikeRepair
    {
        public static void Apply()
        {
            var game=Object.FindAnyObjectByType<HuntGame>(); int count=0;
            foreach(var trap in game.Maze.GetComponentsInChildren<SpikeTrap>(true))
            {
                if(!trap.wallMounted) continue;
                Undo.RecordObject(trap.spikes,"Correct wall spike retraction");
                trap.spikes.localPosition=Vector3.zero; trap.spikes.localScale=Vector3.one;
                EditorUtility.SetDirty(trap.spikes); count++;
            }
            Debug.Log("WALL SPIKES REPAIRED: "+count+" racks stay on the mounting side instead of retracting through the wall.");
        }
    }
}
