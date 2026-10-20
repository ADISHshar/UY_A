using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TreasureHunt
{
    public static class MazeChallenges
    {
        public static void Stage(int routeLength,int count,int stage,out int keyIndex,out int doorIndex)
        {
            doorIndex=(stage+1)*(routeLength-1)/count;
            int previous=stage*(routeLength-1)/count;
            keyIndex=previous+(doorIndex-previous)/2;
        }
        public static void Generate(MazeBuilder maze)
        {
            float s=maze.settings.cellSize; var layout=maze.Layout;
            var root=new GameObject("Hazards and Colored Doors").transform; root.SetParent(maze.transform,false); maze.ChallengeRoot=root;
            var doors=Group("Doors",root); var keys=Group("Keys",root); var spikes=Group("Retracting Spikes",root); var lasers=Group("Moving Lasers",root); var crushers=Group("Collapsing Floors",root);
            var reserved=new HashSet<Vector2Int> { Vector2Int.zero,layout.Route[1],layout.Route[layout.Route.Count-1] };
            var random=new System.Random(maze.settings.seed^0x4f41);
            int count=Mathf.Clamp(maze.settings.doors,2,5);
            Mesh ring=RingMesh(), spike=SpikeMesh();
            var steel=MazeBuilder.Material(new Color(.24f,.29f,.31f));
            var floor=MazeBuilder.Material(new Color(.09f,.145f,.175f));
            for(int i=0;i<count;i++)
            {
                Stage(layout.Route.Count,count,i,out int ki,out int di);
                reserved.Add(layout.Route[ki]); reserved.Add(layout.Route[di]);
                var kind=(KeyKind)i; var color=MazeBuilder.Material(HuntKey.ColorFor(kind),true);
                var keyRoot=Group(kind+" Key",keys); keyRoot.position=layout.World(layout.Route[ki],s);
                var key=keyRoot.gameObject.AddComponent<HuntKey>(); key.kind=kind;
                key.model=Group("Key Model",keyRoot); key.model.localPosition=Vector3.up*.95f;
                MeshVisual("Key Ring",key.model,ring,color,Vector3.up*.17f);
                Box("Key Stem",key.model,new Vector3(0,-.22f,0),new Vector3(.09f,.5f,.09f),color,false);
                Box("Tooth 1",key.model,new Vector3(.09f,-.36f,0),new Vector3(.21f,.08f,.09f),color,false);
                Box("Tooth 2",key.model,new Vector3(.09f,-.2f,0),new Vector3(.21f,.08f,.09f),color,false);
                maze.Keys.Add(key);
                var a=layout.World(layout.Route[di-1],s); var b=layout.World(layout.Route[di],s);
                var doorRoot=Group(kind+" Door",doors); doorRoot.position=(a+b)/2; doorRoot.rotation=Quaternion.LookRotation(b-a);
                var door=doorRoot.gameObject.AddComponent<HuntDoor>(); door.requiredKey=kind; door.openRadius=s*.52f;
                door.panel=Box("Sliding Door Panel",doorRoot,Vector3.up*1.35f,new Vector3(s-.3f,2.7f,.24f),color,true).transform;
                var obstacle=door.panel.gameObject.AddComponent<NavMeshObstacle>(); obstacle.carving=true; obstacle.size=Vector3.one;
                for(int j=0;j<4;j++) Box("Metal Brace",door.panel,new Vector3(0,-.32f+j*.22f,-.55f),new Vector3(.96f,.025f,.12f),steel,false);
                Box("Left Frame",doorRoot,new Vector3(-s/2+.11f,1.5f,0),new Vector3(.18f,3,.4f),steel,false);
                Box("Right Frame",doorRoot,new Vector3(s/2-.11f,1.5f,0),new Vector3(.18f,3,.4f),steel,false);
                maze.Doors.Add(door);
            }
            maze.KeyVisual=maze.Keys[0].model; maze.Door=maze.Doors[maze.Doors.Count-1].panel;
            foreach(var cell in layout.Cells)
            {
                if(reserved.Contains(cell) || layout.World(cell,s).magnitude<s*2) continue;
                Vector3 p=layout.World(cell,s);
                if(maze.Spikes.Count<maze.settings.traps)
                {
                    var trapRoot=Group("Spike Trap "+(maze.Spikes.Count+1),spikes); trapRoot.position=p;
                    var trap=trapRoot.gameObject.AddComponent<SpikeTrap>(); trap.cycle=maze.settings.spikeCycle; trap.warning=maze.settings.spikeWarning; trap.phase=(float)random.NextDouble()*trap.cycle;
                    trap.halfSize=Vector2.one*(s/2-.06f); trap.idleMaterial=steel; trap.warningMaterial=maze.Danger;
                    Box("Slotted Steel Plate",trapRoot,Vector3.up*.025f,new Vector3(s-.12f,.07f,s-.12f),steel,true);
                    trap.warningLight=Box("Warning Strip",trapRoot,new Vector3(0,.068f,-s*.27f),new Vector3(s*.5f,.025f,.045f),steel,false).GetComponent<Renderer>();
                    trap.spikes=Group("Spike Rack",trapRoot); trap.spikes.localPosition=Vector3.down*trap.height;
                    for(int x=0;x<8;x++) for(int z=0;z<8;z++)
                    {
                        var point=new Vector3((x-3.5f)*(s-.35f)/7,0,(z-3.5f)*(s-.35f)/7);
                        MeshVisual("Steel Spike",trap.spikes,spike,steel,point);
                        Box("Spike Slot",trapRoot,point+Vector3.up*.065f,new Vector3(.27f,.015f,.27f),maze.Stone,false);
                    }
                    maze.Spikes.Add(trap); continue;
                }
                if(maze.Lasers.Count<maze.settings.lasers)
                {
                    var directions=new List<int>(); for(int d=0;d<4;d++) if(layout.Open[cell.x,cell.y,d] && !reserved.Contains(cell+MazeLayout.Directions[d]) && !maze.Spikes.Exists(t=>Vector3.Distance(t.transform.position,layout.World(cell+MazeLayout.Directions[d],s))<s*.5f) && !maze.Lasers.Exists(t=>Vector3.Distance(t.transform.position,p+new Vector3(MazeLayout.Directions[d].x,0,MazeLayout.Directions[d].y)*s/2)<.1f)) directions.Add(d);
                    if(directions.Count==0) continue;
                    int dir=directions[random.Next(directions.Count)]; var axis=MazeLayout.Directions[dir];
                    var laserRoot=Group("Laser Bar Wall "+(maze.Lasers.Count+1),lasers); laserRoot.position=p+new Vector3(axis.x,0,axis.y)*s/2; laserRoot.rotation=Quaternion.LookRotation(new Vector3(axis.x,0,axis.y));
                    var laser=laserRoot.gameObject.AddComponent<LaserHazard>();
                    laser.pattern=(LaserHazard.Pattern)(maze.Lasers.Count%3);
                    laserRoot.name=laser.pattern+" Laser "+(maze.Lasers.Count+1);
                    laser.rotationSpeed=maze.Lasers.Count%2==0?70:-70;
                    laser.crossRadius=Mathf.Min(1.05f,s/2-.85f);
                    laser.axis=(LaserHazard.TravelAxis)(maze.Lasers.Count%2); laser.width=s-.24f; laser.height=2.75f; laser.gapWidth=1.5f;
                    laser.gapCenter=((float)random.NextDouble()-.5f)*(laser.width-laser.gapWidth-.45f);
                    laser.speed=2.1f+(float)random.NextDouble()*.7f; laser.phase=(float)random.NextDouble()*4;
                    float span=laser.axis==LaserHazard.TravelAxis.Horizontal?laser.width:laser.height;
                    int slots=Mathf.CeilToInt(span/.34f); laser.spacing=span/slots; laser.bars=new LineRenderer[slots*2];
                    var material=MazeBuilder.Material(new Color(1,.12f,.25f),true);
                    for(int j=0;j<laser.bars.Length;j++)
                    {
                        var beamRoot=Group("Laser Bar "+(j+1),laserRoot); var beam=beamRoot.gameObject.AddComponent<LineRenderer>();
                        beam.useWorldSpace=false; beam.sharedMaterial=material; beam.positionCount=2; beam.startWidth=beam.endWidth=.055f; beam.numCapVertices=4; laser.bars[j]=beam;
                    }
                    Box("Left Emitter Rail",laserRoot,new Vector3(-s/2+.1f,1.4f,0),new Vector3(.14f,2.8f,.16f),steel,false);
                    Box("Right Emitter Rail",laserRoot,new Vector3(s/2-.1f,1.4f,0),new Vector3(.14f,2.8f,.16f),steel,false);
                    Box("Top Emitter Rail",laserRoot,Vector3.up*2.85f,new Vector3(s-.1f,.12f,.16f),steel,false);
                    if(laser.pattern==LaserHazard.Pattern.Wall) Box("Gap Marker Left",laserRoot,new Vector3(laser.gapCenter-laser.gapWidth/2,.025f,0),new Vector3(.035f,.035f,.22f),maze.Teal,false);
                    if(laser.pattern==LaserHazard.Pattern.Wall) Box("Gap Marker Right",laserRoot,new Vector3(laser.gapCenter+laser.gapWidth/2,.025f,0),new Vector3(.035f,.035f,.22f),maze.Teal,false);
                    laser.UpdateGrid(0); maze.Lasers.Add(laser); reserved.Add(cell+MazeLayout.Directions[dir]); continue;
                }
                if(maze.Pits.Count<maze.settings.collapsingTiles)
                {
                    var trapRoot=Group("Collapsing Tile "+(maze.Pits.Count+1),crushers); trapRoot.position=p;
                    var trap=trapRoot.gameObject.AddComponent<CollapsingTile>(); trap.delay=maze.settings.collapseDelay; trap.respawnDelay=maze.settings.tileRespawnDelay;
                    float half=s/2; trap.holeSize=Vector2.one*s;
                    trap.cover=Box("Collapsing Cover",trapRoot,Vector3.down*.05f,new Vector3(s,.1f,s),floor,true).transform;
                    var dark=new Material(Shader.Find("Universal Render Pipeline/Unlit")); dark.color=new Color(.001f,.002f,.003f);
                    Box("Dark Shaft Left",trapRoot,new Vector3(-half-.025f,-4,0),new Vector3(.05f,8,half*2),dark,false);
                    Box("Dark Shaft Right",trapRoot,new Vector3(half+.025f,-4,0),new Vector3(.05f,8,half*2),dark,false);
                    Box("Dark Shaft Front",trapRoot,new Vector3(0,-4,-half-.025f),new Vector3(half*2,8,.05f),dark,false);
                    Box("Dark Shaft Back",trapRoot,new Vector3(0,-4,half+.025f),new Vector3(half*2,8,.05f),dark,false);
                    Box("Dark Shaft Bottom",trapRoot,Vector3.down*8,new Vector3(half*2,.1f,half*2),dark,false);
                    var obstacle=trapRoot.gameObject.AddComponent<NavMeshObstacle>(); obstacle.shape=NavMeshObstacleShape.Box; obstacle.size=new Vector3(half*2,2,half*2); obstacle.center=Vector3.up; obstacle.carving=true; obstacle.enabled=false; trap.holeObstacle=obstacle;
                    maze.Pits.Add(trap);
                }

            }
            GenerateWallSpikes(maze);
        }
        public static void GenerateWallSpikes(MazeBuilder maze)
        {
            var root=Group("Fast Wall Spikes",maze.ChallengeRoot); float s=maze.settings.cellSize;
            var steel=MazeBuilder.Material(new Color(.24f,.29f,.31f)); var mesh=SpikeMesh(); int count=0;
            foreach(var cell in maze.Layout.Cells)
            {
                if(count>=maze.settings.wallSpikes) break;
                Vector3 p=maze.Layout.World(cell,s);
                if(p.magnitude<s*3 || maze.Pits.Exists(t=>Vector3.Distance(t.transform.position,p)<s*.6f) || maze.Spikes.Exists(t=>Vector3.Distance(t.transform.position,p)<s*.6f)) continue;
                for(int d=0;d<4;d++)
                {
                    if(maze.Layout.Open[cell.x,cell.y,d]) continue;
                    var axis=MazeLayout.Directions[d]; var outward=new Vector3(axis.x,0,axis.y);
                    var wall=Group("Wall Spike Rack "+(++count),root); wall.position=p+outward*(s/2-.15f)+Vector3.up*1.15f;
                    wall.rotation=Quaternion.LookRotation(Vector3.up,-outward);
                    var trap=wall.gameObject.AddComponent<SpikeTrap>(); trap.wallMounted=true; trap.cycle=maze.settings.wallSpikeCycle;
                    trap.warning=.35f; trap.raisedSeconds=.65f; trap.riseSeconds=.09f; trap.phase=count*.31f; trap.height=.9f;
                    trap.halfSize=new Vector2(s/2-.12f,1.1f); trap.idleMaterial=steel; trap.warningMaterial=maze.Danger;
                    Box("Wall Spike Plate",wall,Vector3.zero,new Vector3(s-.2f,.08f,2.2f),steel,false);
                    trap.warningLight=Box("Wall Warning Strip",wall,new Vector3(0,.06f,-1.05f),new Vector3(s-.3f,.025f,.04f),steel,false).GetComponent<Renderer>();
                    trap.spikes=Group("Moving Wall Spikes",wall); trap.spikes.localPosition=Vector3.zero;
                    for(int x=0;x<8;x++) for(int z=0;z<5;z++) MeshVisual("Wall Spike",trap.spikes,mesh,steel,new Vector3((x-3.5f)*(s-.35f)/7,0,(z-2)*.48f));
                    break;
                }
            }
        }
        static Transform Group(string name,Transform parent) { var t=new GameObject(name).transform; t.SetParent(parent,false); return t; }
        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Material material,bool collider)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material;
            if(!collider) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        static void MeshVisual(string name,Transform parent,Mesh mesh,Material material,Vector3 position)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        static Mesh SpikeMesh()
        {
            var mesh=new Mesh {name="Steel spike pyramid"};
            mesh.vertices=new[]{new Vector3(-.12f,0,-.12f),new Vector3(.12f,0,-.12f),new Vector3(.12f,0,.12f),new Vector3(-.12f,0,.12f),Vector3.up*.9f};
            mesh.triangles=new[]{0,4,1,1,4,2,2,4,3,3,4,0,0,1,2,0,2,3}; mesh.RecalculateNormals(); return mesh;
        }
        static Mesh RingMesh()
        {
            const int n=20,k=6; var vertices=new Vector3[n*k]; var triangles=new int[n*k*6];
            for(int i=0;i<n;i++) for(int j=0;j<k;j++)
            {
                float a=2*Mathf.PI*i/n,b=2*Mathf.PI*j/k,r=.23f+.055f*Mathf.Cos(b);
                int index=i*k+j; vertices[index]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*.055f);
                int next=((i+1)%n)*k+j, jj=i*k+(j+1)%k, both=((i+1)%n)*k+(j+1)%k;
                int t=index*6; triangles[t]=index; triangles[t+1]=next; triangles[t+2]=jj; triangles[t+3]=jj; triangles[t+4]=next; triangles[t+5]=both;
            }
            var mesh=new Mesh {name="Key ring mesh",vertices=vertices,triangles=triangles}; mesh.RecalculateNormals(); return mesh;
        }
    }
}
