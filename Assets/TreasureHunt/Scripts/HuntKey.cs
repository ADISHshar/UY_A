using UnityEngine;

namespace TreasureHunt
{
    public enum KeyKind { Red, Green, Blue, Yellow, Purple }

    public abstract class HuntWorldObject : MonoBehaviour
    {
        public HuntGame game;
        protected HuntGame Game => game!=null?game:game=GetComponentInParent<HuntGame>();
        protected bool NearFloor(Vector3 point,float halfWidth,float halfDepth)
        {
            var p=transform.InverseTransformPoint(point);
            return Mathf.Abs(p.x)<halfWidth && Mathf.Abs(p.z)<halfDepth && Mathf.Abs(p.y)<.7f;
        }
    }
    public sealed class HuntKey : HuntWorldObject
    {
        public KeyKind kind;
        public Transform model;
        public float pickupRadius=1;
        public bool Collected { get; private set; }
        void Update()
        {
            if(!Game.Playing || Collected) return;
            model.Rotate(0,65*Time.deltaTime,0,Space.World);
            var delta=Game.Player.transform.position-transform.position; delta.y=0;
            if(delta.sqrMagnitude<pickupRadius*pickupRadius) Collect();
        }
        public void Collect()
        {
            if(Collected) return;
            Collected=true; Game.CollectKey(kind); model.gameObject.SetActive(false);
        }
        public static Color ColorFor(KeyKind kind)
        {
            switch(kind)
            {
                case KeyKind.Red:return new Color(1,.17f,.12f);
                case KeyKind.Green:return new Color(.14f,1,.39f);
                case KeyKind.Blue:return new Color(.15f,.58f,1);
                case KeyKind.Yellow:return new Color(1,.8f,.14f);
                default:return new Color(.77f,.3f,1);
            }
        }
    }
}
