using UnityEngine;

namespace TreasureHunt
{
    [DefaultExecutionOrder(10)]
    public sealed class RobotAnimation : MonoBehaviour
    {
        public Animator animator;
        public HuntGame game;
        [Tooltip("Meters per second of the source clip, calibrated from planted-foot travel.")]
        public float referenceSpeed=1.5f;
        public float strideLength=2.1f;
        [System.Serializable]
        public struct SoleProbe { public Transform bone; public Vector3[] points; }
        public Transform visualRoot;
        public SoleProbe[] soles;
        [Tooltip("Additional sole clearance above the floor, in meters.")]
        public float groundClearance=.005f;
        readonly RaycastHit[] groundHits=new RaycastHit[16];
        Vector3 previous;
        bool hasRunState,hasContactEvents,moving;
        Vector3 visualRestPosition;
        readonly float[] lastContact={-100,-100};
        RobotFootContact[] footContacts;
        HuntPlayer player;
        AudioSource stepSource;
        public const float RunCycleSeconds=.8f;
        public float PlaybackRate(float speed)
        {
            float rate=Mathf.Max(.05f,speed/Mathf.Max(.1f,referenceSpeed));
            var player=GetComponent<HuntPlayer>();
            return player!=null?Mathf.Min(rate,player.Sprinting && hasRunState?1.1f:player.Sprinting?1.9f:1.25f):rate;
        }
        public void GroundFeet()
        {
            if(visualRoot==null || soles==null || soles.Length==0) return;
            float lowest=float.PositiveInfinity;
            foreach(var sole in soles) if(sole.bone!=null && sole.points!=null)
                foreach(var point in sole.points) lowest=Mathf.Min(lowest,sole.bone.TransformPoint(point).y);
            if(float.IsInfinity(lowest)) return;
            float floor=transform.position.y;
            var player=GetComponent<HuntPlayer>();
            if(player==null || !player.Falling)
            {
                int count=Physics.RaycastNonAlloc(transform.position+Vector3.up*.35f,Vector3.down,groundHits,.85f,~0,QueryTriggerInteraction.Ignore);
                float nearest=float.PositiveInfinity;
                for(int i=0;i<count;i++)
                {
                    var hit=groundHits[i];
                    if(hit.collider.GetComponentInParent<HuntPlayer>()!=null || hit.collider.GetComponentInParent<HuntGuard>()!=null || hit.normal.y<.6f) continue;
                    if(hit.distance<nearest) { nearest=hit.distance; floor=hit.point.y; }
                }
            }
            float correction=floor+groundClearance-lowest;
            bool running=player!=null && player.Sprinting && hasRunState;
            visualRoot.position+=Vector3.up*(running?Mathf.Max(0,correction):correction);
        }
        void Start()
        {
            previous=transform.position;
            player=GetComponent<HuntPlayer>();
            var guard=GetComponent<HuntGuard>();
            stepSource=player!=null?player.footsteps:guard!=null?guard.footsteps:null;
            if(stepSource!=null) { stepSource.Stop(); stepSource.loop=false; stepSource.clip=null; }
            footContacts=new RobotFootContact[soles!=null?soles.Length:0];
            if(visualRoot!=null) visualRestPosition=visualRoot.localPosition;
            var relay=animator.gameObject.GetComponent<RobotAnimationEvents>()??animator.gameObject.AddComponent<RobotAnimationEvents>();
            relay.driver=this;
            foreach(var clip in animator.runtimeAnimatorController.animationClips)
                foreach(var entry in clip.events) if(entry.functionName=="FootContact") hasContactEvents=true;
            foreach(var parameter in animator.parameters)
                if(parameter.name=="Running" && parameter.type==AnimatorControllerParameterType.Bool) hasRunState=true;
        }
        void Update()
        {
            Vector3 delta=transform.position-previous; delta.y=0; previous=transform.position;
            float speed=delta.magnitude/Mathf.Max(Time.deltaTime,.001f);
            moving=game.Playing && speed>.08f && speed<15 && (player==null || !player.Falling);
            if(hasRunState) animator.SetBool("Running",moving && player!=null && player.Sprinting);
            animator.SetBool("Moving",moving); animator.SetFloat("Playback",PlaybackRate(speed));
            animator.speed=game.Playing?1:0;
        }
        void LateUpdate()
        {
            if(player!=null && player.Sprinting && hasRunState && visualRoot!=null) visualRoot.localPosition=visualRestPosition;
            GroundFeet();
            if(!hasContactEvents) UpdateFootContacts(moving);
        }
        public void AnimationFootContact(int foot,bool runningClip)
        {
            if(!moving || !game.Playing || foot<0 || foot>=lastContact.Length) return;
            bool sprint=player!=null && player.Sprinting;
            if(sprint!=runningClip || (player!=null && player.Falling)) return;
            if(game.Elapsed-lastContact[foot]<.12f) return;
            // Ignore contacts over a collapsed tile or while falling into a shaft.
            Vector3 origin=transform.position+Vector3.up*.25f;
            if(soles!=null && foot<soles.Length && soles[foot].bone!=null) origin=soles[foot].bone.position+Vector3.up*.25f;
            bool floor=false;
            int count=Physics.RaycastNonAlloc(origin,Vector3.down,groundHits,.65f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=groundHits[i];
                if(hit.normal.y>.6f && hit.collider.GetComponentInParent<HuntPlayer>()==null && hit.collider.GetComponentInParent<HuntGuard>()==null) floor=true;
            }
            if(!floor) return;
            lastContact[foot]=game.Elapsed;
            if(game.Audio!=null) game.Audio.RobotFootContact(stepSource,player==null,sprint);
            if(sprint) game.ReportSprintNoise(transform.position);
        }
        void UpdateFootContacts(bool active)
        {
            if(footContacts==null || soles==null) return;
            for(int i=0;i<footContacts.Length;i++)
            {
                var sole=soles[i];
                if(sole.bone==null || sole.points==null || sole.points.Length==0)
                { footContacts[i].Update(0,false); continue; }
                Vector3 lowest=sole.bone.TransformPoint(sole.points[0]);
                foreach(var point in sole.points)
                {
                    Vector3 world=sole.bone.TransformPoint(point);
                    if(world.y<lowest.y) lowest=world;
                }
                float floor=float.NegativeInfinity;
                int count=Physics.RaycastNonAlloc(lowest+Vector3.up*.2f,Vector3.down,groundHits,.8f,~0,QueryTriggerInteraction.Ignore);
                for(int hitIndex=0;hitIndex<count;hitIndex++)
                {
                    var hit=groundHits[hitIndex];
                    if(hit.normal.y<.6f || hit.collider.GetComponentInParent<HuntPlayer>()!=null || hit.collider.GetComponentInParent<HuntGuard>()!=null) continue;
                    floor=Mathf.Max(floor,hit.point.y);
                }
                bool landed=footContacts[i].Update(lowest.y-floor,active && !float.IsNegativeInfinity(floor));
                if(!landed || game.Audio==null) continue;
                game.Audio.RobotFootContact(stepSource,player==null,player!=null && player.Sprinting);
                if(player!=null && player.Sprinting) game.ReportSprintNoise(transform.position);
            }
        }
    }
}
