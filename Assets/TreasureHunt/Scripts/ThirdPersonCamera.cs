using UnityEngine;
using UnityEngine.InputSystem;

namespace TreasureHunt
{
    [DefaultExecutionOrder(100)]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        public HuntPlayer target;
        public float distance=2.7f,targetHeight=1.25f,pitch=22,sensitivity=.095f;
        public float minimumPitch=8,maximumPitch=65,collisionRadius=.18f,smoothTime=.045f;
        public float yaw;
        Vector3 velocity;
        readonly RaycastHit[] hits=new RaycastHit[24];
        void Start() { yaw=target.transform.eulerAngles.y; Snap(); }
        void Update()
        {
            if(target==null || !target.game.Playing || Mouse.current==null) return;
            Vector2 delta=Mouse.current.delta.ReadValue()*sensitivity;
            yaw+=delta.x; pitch=Mathf.Clamp(pitch-delta.y,minimumPitch,maximumPitch);
        }
        Vector3 Desired(out Vector3 pivot)
        {
            pivot=target.transform.position+Vector3.up*targetHeight;
            Vector3 direction=Quaternion.Euler(pitch,yaw,0)*Vector3.back;
            float allowed=distance;
            int count=Physics.SphereCastNonAlloc(pivot,collisionRadius,direction,hits,distance,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
                if(!hits[i].transform.IsChildOf(target.transform)) allowed=Mathf.Min(allowed,Mathf.Max(.32f,hits[i].distance-.07f));
            return pivot+direction*allowed;
        }
        public void Snap()
        {
            if(target==null) return;
            transform.position=Desired(out var pivot); transform.LookAt(pivot); velocity=Vector3.zero;
        }
        void LateUpdate()
        {
            if(target==null) return;
            Vector3 desired=Desired(out var pivot);
            // Retract immediately against walls; ease only when opening up again.
            bool obstructed=Vector3.Distance(pivot,desired)<Vector3.Distance(pivot,transform.position);
            transform.position=obstructed?desired:Vector3.SmoothDamp(transform.position,desired,ref velocity,smoothTime);
            transform.LookAt(pivot);
            if(target.game.Playing && target.shake>0) transform.rotation*=Quaternion.Euler(Mathf.Sin(Time.time*47)*target.shake*.6f,0,Mathf.Sin(Time.time*39)*target.shake);
        }
    }
}
