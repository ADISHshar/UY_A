using UnityEngine;
using UnityEngine.InputSystem;

namespace TreasureHunt
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HuntPlayer : MonoBehaviour
    {
        public Camera view;
        public HuntGame game;
        public AudioSource footsteps;
        public Transform robotModel;
        public ThirdPersonCamera followCamera;
        public float walkSpeed=3.7f,sprintSpeed=5.5f,turnSpeed=650;
        public bool Sprinting { get; private set; }
        public bool Falling { get; private set; }
        public float shake;
        CharacterController controller;
        float verticalSpeed;
        public float BodyRadius => controller!=null?controller.radius:.3f;
        public float BodyHeight => controller!=null?controller.height:1.8f;
        void Reset() { var c=GetComponent<CharacterController>(); c.height=1.8f; c.center=Vector3.up*.9f; c.radius=.3f; }
        void Awake() { controller=GetComponent<CharacterController>(); }
        void Update()
        {
            if(game==null || !game.Playing) return;
            var kb=Keyboard.current;
            if(kb!=null && kb.escapeKey.wasPressedThisFrame) { game.Pause(); return; }
            float x=kb==null?0:(kb.dKey.isPressed||kb.rightArrowKey.isPressed?1:0)-(kb.aKey.isPressed||kb.leftArrowKey.isPressed?1:0);
            float z=kb==null?0:(kb.wKey.isPressed||kb.upArrowKey.isPressed?1:0)-(kb.sKey.isPressed||kb.downArrowKey.isPressed?1:0);
            Vector3 forward=view.transform.forward; forward.y=0; forward.Normalize();
            Vector3 right=view.transform.right; right.y=0; right.Normalize();
            Vector3 movement=Vector3.ClampMagnitude(right*x+forward*z,1);
            if(Falling) movement=Vector3.zero;
            Sprinting=!Falling && kb!=null && kb.leftShiftKey.isPressed && movement.sqrMagnitude>.01f;
            if(movement.sqrMagnitude>.01f) transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(movement),turnSpeed*Time.deltaTime);
            verticalSpeed=controller.isGrounded && !Falling?-2:verticalSpeed-22*Time.deltaTime;
            controller.Move((movement*(Sprinting?sprintSpeed:walkSpeed)+Vector3.up*verticalSpeed)*Time.deltaTime);
            shake=Mathf.MoveTowards(shake,0,Time.deltaTime*2);
        }
        void OnDisable() { if(footsteps!=null) footsteps.Stop(); }
        public void BeginFall() { Falling=true; Sprinting=false; if(footsteps!=null) footsteps.Stop(); }
        public void Teleport(Vector3 p)
        {
            if(controller==null) controller=GetComponent<CharacterController>();
            controller.enabled=false; transform.position=p; controller.enabled=true;
            Falling=false; verticalSpeed=-2;
            if(footsteps!=null) footsteps.Stop();
            if(followCamera!=null) followCamera.Snap();
        }
    }
}
