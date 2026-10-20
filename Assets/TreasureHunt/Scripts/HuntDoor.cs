using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunt
{
    public sealed class HuntDoor : HuntWorldObject
    {
        public KeyKind requiredKey;
        public Transform panel;
        public float openRadius=1.9f;
        public bool Opened { get; private set; }
        Vector3 closedPosition;
        float opening, nextNotice;
        void Start() { closedPosition=panel.localPosition; }
        void Update()
        {
            if(!Game.Playing) return;
            if(Opened)
            {
                opening=Mathf.MoveTowards(opening,1,Time.deltaTime*1.6f);
                panel.localPosition=closedPosition+Vector3.up*3.2f*Mathf.SmoothStep(0,1,opening); return;
            }
            var delta=Game.Player.transform.position-transform.position; delta.y=0;
            if(delta.magnitude>openRadius) return;
            if(Game.HasKeyOf(requiredKey)) Unlock();
            else if(Game.Elapsed>nextNotice)
            {
                nextNotice=Game.Elapsed+3; Game.Tell("LOCKED  /  Find the "+requiredKey.ToString().ToUpper()+" key");
                Game.Audio.At(Game.Audio.bank.locked,transform.position,.5f);
            }
        }
        public void Unlock()
        {
            if(Opened || !Game.HasKeyOf(requiredKey)) return;
            Opened=true;
            foreach(var collider in panel.GetComponentsInChildren<Collider>()) collider.enabled=false;
            var obstacle=panel.GetComponent<NavMeshObstacle>(); if(obstacle!=null) obstacle.enabled=false;
            Game.Audio.At(Game.Audio.bank.unlock,transform.position,.8f);
            Game.Tell(requiredKey.ToString().ToUpper()+" DOOR OPEN  /  Key kept in inventory");
        }
        public void SetBakeEnabled(bool active) { panel.gameObject.SetActive(active); }
    }
}
