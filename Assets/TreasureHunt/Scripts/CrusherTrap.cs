using UnityEngine;

namespace TreasureHunt
{
    [AddComponentMenu("")]
    public sealed class CrusherTrap : HuntWorldObject
    {
        public Transform crushingWall;
        public Vector3 closedPosition, travel;
        public Vector2 halfSize=new Vector2(1.4f,1.4f);
        public float delay=1.2f, strikeSeconds=.42f, returnSeconds=1.3f, cooldown=2;
        public enum Phase { Idle, Armed, Striking, Returning, Cooling }
        public Phase State { get; private set; }
        float changedAt;
        public void Arm()
        {
            if(State!=Phase.Idle || !Game.Playing) return;
            State=Phase.Armed; changedAt=Game.Elapsed;
            Game.Audio.At(Game.Audio.bank.pressureClick,transform.position,.8f);
            Game.Audio.At(Game.Audio.bank.crusherWindup,transform.position,.7f);
            Game.Tell("CLICK!  /  Move off this tile before the wall strikes.");
        }
        void Update()
        {
            if(!Game.Playing) return;
            float t=Game.Elapsed-changedAt;
            switch(State)
            {
                case Phase.Idle:
                    if(NearFloor(Game.Player.transform.position,halfSize.x,halfSize.y)) Arm();
                    break;
                case Phase.Armed:
                    if(t>=delay) { State=Phase.Striking; changedAt=Game.Elapsed; }
                    break;
                case Phase.Striking:
                    var previous=crushingWall.position;
                    crushingWall.localPosition=closedPosition+travel*Mathf.Clamp01(t/strikeSeconds);
                    var bounds=crushingWall.GetComponent<Renderer>().bounds;
                    var previousBounds=bounds; previousBounds.center+=previous-crushingWall.position; bounds.Encapsulate(previousBounds); bounds.Expand(new Vector3(.55f,0,.55f));
                    if(bounds.Contains(Game.Player.transform.position+Vector3.up*.9f)) Game.Finish(false,"A pressure tile triggered the crushing wall.");
                    if(t>=strikeSeconds)
                    {
                        State=Phase.Returning; changedAt=Game.Elapsed;
                        Game.Audio.At(Game.Audio.bank.crusherImpact,crushingWall.position,1); Game.Player.shake=.6f;
                    }
                    break;
                case Phase.Returning:
                    crushingWall.localPosition=closedPosition+travel*(1-Mathf.Clamp01(t/returnSeconds));
                    if(t>=returnSeconds) { State=Phase.Cooling; changedAt=Game.Elapsed; }
                    break;
                case Phase.Cooling:
                    if(t>=cooldown) State=Phase.Idle;
                    break;
            }
        }
    }
}
