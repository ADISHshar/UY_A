using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace TreasureHunt
{
    public sealed class MazeBuilder : MonoBehaviour
    {
        public MazeSettings settings;
        public MazeLayout Layout { get; private set; }
        public List<Vector3> PatrolPoints = new List<Vector3>();
        public List<Transform> PatrolWaypoints = new List<Transform>();
        public Transform SpawnPoint;
        public Vector3 Spawn => SpawnPoint != null ? SpawnPoint.position : new Vector3(0, .15f, 0);
        public Transform Door;
        public Transform ChallengeRoot;
        public List<HuntDoor> Doors=new List<HuntDoor>();
        public List<HuntKey> Keys=new List<HuntKey>();
        public List<SpikeTrap> Spikes=new List<SpikeTrap>();
        public List<LaserHazard> Lasers=new List<LaserHazard>();
        [HideInInspector] public List<CrusherTrap> Crushers=new List<CrusherTrap>();
        public List<CollapsingTile> Pits=new List<CollapsingTile>();
        public Vector3 KeyPosition => Ground(KeyVisual.position);
        public Vector3 TreasurePosition => Ground(TreasureVisual.position);
        public List<Vector3> TrapPositions = new List<Vector3>();
        public List<Transform> TrapVisuals = new List<Transform>();
        public Transform KeyVisual;
        public Transform TreasureVisual;
        static Vector3 Ground(Vector3 p) => new Vector3(p.x, 0, p.z);
        public Material Stone, Gold, Teal, Danger;

        public void Generate(bool navigation)
        {
            if (settings == null) settings = ScriptableObject.CreateInstance<MazeSettings>();
            Layout = new MazeLayout(settings.width, settings.height, settings.seed);
            PatrolPoints.Clear(); TrapPositions.Clear(); TrapVisuals.Clear();
            Stone = Material(new Color(.13f,.20f,.24f)); Gold = Material(new Color(1,.64f,.16f), true);
            Teal = Material(new Color(.1f,.85f,.74f), true); Danger = Material(new Color(1,.18f,.12f), true);
            float s = settings.cellSize;
            var foundationMaterial=Material(new Color(.055f,.095f,.12f));
            var floorMaterial = Material(new Color(.085f,.14f,.17f));
            foreach (var c in Layout.Cells)
            {
                Vector3 p = Layout.World(c,s); PatrolPoints.Add(p);
                Cube("Foundation Tile",p+Vector3.down*.4f,new Vector3(s,.5f,s),foundationMaterial);
                Cube("Floor inlay", p + Vector3.down * .05f, new Vector3(s-.08f,.1f,s-.08f), floorMaterial);
                for (int d = 0; d < 4; d++)
                {
                    if (Layout.Open[c.x,c.y,d]) continue;
                    if (d == 2 && c.y > 0 || d == 3 && c.x > 0) continue;
                    Vector3 delta = new Vector3(MazeLayout.Directions[d].x,0,MazeLayout.Directions[d].y);
                    var wall = Cube("Maze wall", p + delta*s/2 + Vector3.up*1.5f, d%2==0 ? new Vector3(s+.2f,3,.22f) : new Vector3(.22f,3,s+.2f), Stone);
                    Cube("Wall crown", wall.transform.position+Vector3.up*1.48f, d%2==0 ? new Vector3(s+.24f,.07f,.28f) : new Vector3(.28f,.07f,s+.24f), Teal);
                }
            }
            var treasurePosition=Layout.World(Layout.Route[Layout.Route.Count-1],s);
            SpawnPoint=new GameObject("Explorer spawn").transform; SpawnPoint.SetParent(transform); SpawnPoint.position=new Vector3(0,.15f,0);
            var entry=Layout.Route[1]; SpawnPoint.rotation=Quaternion.LookRotation(new Vector3(entry.x,0,entry.y));
            TreasureVisual=Cube("Lost treasure",treasurePosition+Vector3.up*.45f,new Vector3(1.3f,.85f,.85f),Gold).transform;
            Cube("Treasure seal",TreasurePosition+Vector3.up*.9f,new Vector3(.2f,.08f,.9f),Teal);
            MazeChallenges.Generate(this);
            var surface=gameObject.AddComponent<NavMeshSurface>(); surface.collectObjects=CollectObjects.Children;
            // Bake without the gate or decorative objects; the carved obstacle closes the gate.
            surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
            if(navigation) BakeNavigation();
        }
        public void BakeNavigation()
        {
            var surface=GetComponent<NavMeshSurface>();
            var enabledPanels=new List<GameObject>();
            foreach(var door in Doors) if(door.panel.gameObject.activeSelf) { enabledPanels.Add(door.panel.gameObject); door.SetBakeEnabled(false); }
            Physics.SyncTransforms(); surface.BuildNavMesh();
            foreach(var panel in enabledPanels) panel.SetActive(true);
        }

        public void RestoreSceneData()
        {
            Layout=new MazeLayout(settings.width,settings.height,settings.seed);
            TrapPositions.Clear();
            foreach(var trap in TrapVisuals) TrapPositions.Add(Ground(trap.position));
            if(PatrolWaypoints.Count>0) { PatrolPoints.Clear(); foreach(var point in PatrolWaypoints) PatrolPoints.Add(point.position); }
        }
        public GameObject Cube(string label,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=label; go.transform.SetParent(transform); go.transform.position=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material; return go;
        }
        public static Material Material(Color color,bool glow=false)
        {
            var template=Resources.Load<Material>("HuntMaterial");
            var shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null) shader=Shader.Find("Standard");
            var m=template!=null?new Material(template):new Material(shader); m.color=color;
            if(glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",color*1.4f); }
            else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor",Color.black); }
            return m;
        }
    }
}
