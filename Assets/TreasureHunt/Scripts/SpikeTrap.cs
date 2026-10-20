using UnityEngine;

namespace TreasureHunt
{
    public sealed class SpikeTrap : HuntWorldObject
    {
        public Transform spikes;
        public Renderer warningLight;
        public Material warningMaterial, idleMaterial;
        public float cycle=4.2f, warning=.65f, raisedSeconds=1.3f, height=.9f, phase;
        public Vector2 halfSize=new Vector2(1.12f,1.12f);
        public bool wallMounted;
        public float riseSeconds=.18f;
        public bool Dangerous { get; private set; }
        bool wasRaised;
        void Update()
        {
            if(!Game.Playing) return;
            float t=Mathf.Repeat(Game.Elapsed+phase,cycle);
            float riseAt=cycle-raisedSeconds;
            bool telegraph=t>=riseAt-warning && t<riseAt;
            Dangerous=t>=riseAt;
            float lift=Dangerous?Mathf.Clamp01((t-riseAt)/riseSeconds):0;
            if(wallMounted)
            {
                // A translating rack would emerge backwards through the thin maze wall.
                // Collapse its depth into the mounting plate instead.
                spikes.localPosition=Vector3.zero;
                spikes.localScale=new Vector3(1,Mathf.Max(.001f,lift),1);
            }
            else spikes.localPosition=Vector3.up*(-height+height*lift);
            warningLight.sharedMaterial=telegraph||Dangerous?warningMaterial:idleMaterial;
            if(Dangerous&&!wasRaised) Game.Audio.At(Game.Audio.bank.spikes,transform.position,Game.Audio.spikeVolume);
            wasRaised=Dangerous;
            var local=transform.InverseTransformPoint(Game.Player.transform.position+Vector3.up*(Game.Player.BodyHeight*.5f));
            bool hit=wallMounted ? Mathf.Abs(local.x)<halfSize.x+Game.Player.BodyRadius && Mathf.Abs(local.z)<halfSize.y+Game.Player.BodyHeight*.5f && local.y>-Game.Player.BodyRadius && local.y<height*lift+Game.Player.BodyRadius : NearFloor(Game.Player.transform.position,halfSize.x,halfSize.y);
            if(Dangerous && lift>.25f && hit)
                Game.Damage(1,"The spikes claimed your last heart.");
        }
    }
}
