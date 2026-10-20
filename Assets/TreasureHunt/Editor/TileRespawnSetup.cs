using UnityEditor;
using UnityEngine;
namespace TreasureHunt.Editor
{
    public static class TileRespawnSetup
    {
        public static void Apply()
        {
            var game=Object.FindAnyObjectByType<HuntGame>();
            foreach(var pit in game.Maze.Pits) { pit.respawnDelay=game.Settings.tileRespawnDelay; EditorUtility.SetDirty(pit); }
            EditorUtility.SetDirty(game.Settings);
            Debug.Log("COLLAPSING TILES UPDATED: floor returns after "+game.Settings.tileRespawnDelay+" seconds and can be triggered again.");
        }
    }
}
