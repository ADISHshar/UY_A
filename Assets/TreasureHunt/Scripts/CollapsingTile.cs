using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunt
{
    public sealed class CollapsingTile : HuntWorldObject
    {
        public Transform cover;
        public NavMeshObstacle holeObstacle;
        public Vector2 holeSize=new Vector2(2.3f,2.3f);
        public float delay=1.2f,gameOverDepth=3.2f;
        [Min(.5f),Tooltip("Seconds the hole stays open before its floor returns.")]
        public float respawnDelay=4f;
        public enum Phase { Idle, Armed, Open }
        public Phase State { get; private set; }
        float armedAt,openedAt;
        public void Arm()
        {
            if(State!=Phase.Idle || !Game.Playing) return;
            State=Phase.Armed; armedAt=Game.Elapsed;
            Game.Audio.At(Game.Audio.bank.pressureClick,transform.position,.28f);
            Game.Tell("CLICK!  /  This tile is collapsing. Move off it now!");
        }
        void Update()
        {
            if(!Game.Playing) return;
            bool overHole=NearFloor(Game.Player.transform.position,holeSize.x/2-.06f,holeSize.y/2-.06f);
            if(State==Phase.Idle && overHole) Arm();
            if(State==Phase.Armed && Game.Elapsed-armedAt>=delay)
            {
                State=Phase.Open; openedAt=Game.Elapsed; cover.gameObject.SetActive(false); holeObstacle.enabled=true;
                Game.Audio.At(Game.Audio.bank.crusherImpact,transform.position,.13f,.72f);
            }
            if(State!=Phase.Open) return;
            var local=transform.InverseTransformPoint(Game.Player.transform.position);
            bool inside=Mathf.Abs(local.x)<holeSize.x/2 && Mathf.Abs(local.z)<holeSize.y/2;
            if(inside && local.y<-.22f)
            {
                Game.Player.BeginFall();
                if(local.y<-gameOverDepth) Game.Finish(false,"You fell into the dark shaft beneath a collapsing tile.");
            }
            // Let an ongoing fall finish instead of rebuilding a collider through the robot.
            if(Game.Playing && Game.Elapsed-openedAt>=respawnDelay && !(inside && local.y<-.1f))
            {
                holeObstacle.enabled=false; cover.gameObject.SetActive(true); State=Phase.Idle;
            }
        }
    }
}
