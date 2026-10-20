using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunt
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class HuntGuard : MonoBehaviour
    {
        public HuntGame game;
        public int patrolIndex;
        public MeshFilter sightVisual;
        public AudioSource footsteps;
        public bool Investigating => game!=null && game.Elapsed<alertUntil;
        public Vector3 HeardPosition { get; private set; }
        NavMeshAgent agent;
        Mesh cone;
        float alertUntil, nextAlert;
        const int Segments=32;
        void Start()
        {
            agent=GetComponent<NavMeshAgent>();
            // Clone the saved preview mesh before updating sight against live walls.
            cone=Instantiate(sightVisual.sharedMesh); sightVisual.sharedMesh=cone;
        }
        public void HearSprint(Vector3 position)
        {
            if(!game.Playing || Vector3.Distance(transform.position,position)>game.Settings.sprintHearingRange) return;
            HeardPosition=position; alertUntil=game.Elapsed+game.Settings.investigateSeconds;
            if(game.Elapsed>=nextAlert)
            {
                nextAlert=game.Elapsed+2; game.Audio.At(game.Audio.bank.alert,transform.position,.65f);
                game.Tell("LOUD FOOTSTEPS  /  A sentinel heard you. Walk near enemies!");
            }
        }
        public void CreateSightPreview()
        {
            var visual=new GameObject("Visible field of view",typeof(MeshFilter),typeof(MeshRenderer)); visual.transform.SetParent(transform,false);
            sightVisual=visual.GetComponent<MeshFilter>();
            cone=new Mesh { name="Sentinel sight preview" }; sightVisual.sharedMesh=cone;
            visual.GetComponent<MeshRenderer>().sharedMaterial=MazeBuilder.Material(new Color(.7f,.16f,.08f),true);
            DrawSight();
        }
        void Update()
        {
            if(agent==null||!agent.isOnNavMesh) return;
            agent.isStopped=!game.Playing;
            DrawSight();
            if(!game.Playing) return;
            if(!Investigating) agent.updateRotation=true;
            if(Investigating)
            {
                Vector3 facing=HeardPosition-transform.position; facing.y=0;
                if(facing.sqrMagnitude>.01f) transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(facing),360*Time.deltaTime);
                agent.updateRotation=false;
                // Listen and turn first; then investigate the reachable sound position.
                bool facingNoise=facing.sqrMagnitude<.01f || Vector3.Angle(transform.forward,facing)<12;
                agent.isStopped=!facingNoise;
                if(facingNoise) agent.SetDestination(HeardPosition);
            }
            else if(!agent.hasPath || agent.remainingDistance<.5f)
            {
                agent.updateRotation=true;
                // Neighbouring reachable cells produce consistent patrols through corridors.
                var points=game.Maze.PatrolPoints;
                for(int n=0;n<points.Count;n++)
                {
                    patrolIndex=(patrolIndex+17)%points.Count;
                    if((points[patrolIndex]-transform.position).sqrMagnitude>4 && agent.SetDestination(points[patrolIndex])) break;
                }
            }
            Vector3 from=transform.position+Vector3.up*1.25f;
            Vector3 target=game.Player.transform.position+Vector3.up*1.1f;
            Vector3 delta=target-from;
            if(delta.magnitude<game.Settings.sightRange && Vector3.Angle(transform.forward,new Vector3(delta.x,0,delta.z))<game.Settings.sightAngle/2)
            {
                if(Physics.Raycast(from,delta.normalized,out var hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore) && hit.collider.GetComponent<HuntPlayer>()!=null) game.Finish(false,"A sentinel spotted you.");
            }
        }
        void DrawSight()
        {
            var vertices=new Vector3[Segments+2]; var triangles=new int[Segments*3];
            vertices[0]=Vector3.up*.10f;
            for(int i=0;i<=Segments;i++)
            {
                float angle=-game.Settings.sightAngle/2+game.Settings.sightAngle*i/Segments;
                Vector3 dir=Quaternion.Euler(0,angle,0)*Vector3.forward;
                Vector3 world=transform.TransformDirection(dir);
                float range=game.Settings.sightRange;
                if(Physics.Raycast(transform.position+Vector3.up*.4f,world,out var hit,range,~0,QueryTriggerInteraction.Ignore)) range=hit.distance;
                vertices[i+1]=dir*range+Vector3.up*.10f;
                if(i<Segments) { triangles[i*3]=0; triangles[i*3+1]=i+1; triangles[i*3+2]=i+2; }
            }
            cone.Clear(); cone.vertices=vertices; cone.triangles=triangles; cone.RecalculateNormals();
        }
        void OnDestroy() { if(Application.isPlaying && cone!=null) Destroy(cone); }
    }
}
