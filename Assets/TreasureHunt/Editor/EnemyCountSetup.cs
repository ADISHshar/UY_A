using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TreasureHunt.Editor
{
    public static class EnemyCountSetup
    {
        [MenuItem("Treasure Hunt/Set Three Enemies")]
        public static void Apply()
        {
            var game=Object.FindAnyObjectByType<HuntGame>();
            game.Settings.guards=3;
            var guards=game.GetComponentsInChildren<HuntGuard>(true);
            if(guards.Length<3) throw new System.InvalidOperationException("Expected at least three existing enemies.");
            for(int i=3;i<guards.Length;i++) Object.DestroyImmediate(guards[i].gameObject);
            EditorUtility.SetDirty(game.Settings);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("ENEMY COUNT SAVED: "+game.GetComponentsInChildren<HuntGuard>(true).Length);
        }
    }
}
