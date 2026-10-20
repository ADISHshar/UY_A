using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace TreasureHunt
{
    public static class HuntVerification
    {
        static bool valid;
        static void Check(bool pass,string name) { valid &= pass; if(pass) Debug.Log("HUNT CHECK PASSED: "+name); else Debug.LogError("HUNT CHECK FAILED: "+name); }
        static void CheckSoleGeometry(RobotAnimation driver)
        {
            float lowest=float.PositiveInfinity; var mesh=new Mesh();
            foreach(var renderer in driver.animator.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.BakeMesh(mesh);
                foreach(var v in mesh.vertices) lowest=Mathf.Min(lowest,renderer.transform.TransformPoint(v).y);
            }
            Object.Destroy(mesh);
            var origin=driver.transform.position+Vector3.up*.3f;
            float ground=driver.transform.position.y,nearest=float.PositiveInfinity;
            foreach(var hit in Physics.RaycastAll(origin,Vector3.down,.8f))
                if(hit.collider.GetComponentInParent<HuntPlayer>()==null && hit.collider.GetComponentInParent<HuntGuard>()==null && hit.normal.y>.6f && hit.distance<nearest) { nearest=hit.distance; ground=hit.point.y; }
            Check(Mathf.Abs(lowest-ground)<.045f,"visible robot soles meet the floor: "+driver.name+" (clearance "+(lowest-ground)+")");
        }
        static void Set(HuntGame game,string field,object value) => typeof(HuntGame).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,value);
        static void Ready(HuntGame game)
        {
            game.Player.Teleport(game.Maze.Spawn); Set(game,"lives",game.Settings.lives); Set(game,"hurtUntil",0f); game.Resume();
        }
        static void Capture(string name) => ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(Application.dataPath,"../"+name+".png"));
        public static IEnumerator Run(HuntGame game)
        {
            valid=true; string bestKey="TreasureHunt.Best."+game.Settings.seed;
            bool hadBest=PlayerPrefs.HasKey(bestKey); float oldBest=PlayerPrefs.GetFloat(bestKey);
            yield return null;
            var guards=Object.FindObjectsByType<HuntGuard>();
            var spikes=game.Maze.Spikes; var lasers=game.Maze.Lasers; var pits=game.Maze.Pits;
            foreach(var guard in guards) { Check(guard.GetComponent<NavMeshAgent>().isOnNavMesh,"guard NavMesh placement"); guard.enabled=false; }
            foreach(var trap in game.Maze.GetComponentsInChildren<SpikeTrap>()) trap.enabled=false; foreach(var laser in lasers) laser.enabled=false; foreach(var pit in pits) pit.enabled=false;
            Check(game.Player.robotModel!=null && game.Player.robotModel.GetComponentsInChildren<Renderer>().Length>0,"blue robot player model is serialized");
            Check(game.Player.followCamera!=null && game.Player.followCamera.target==game.Player,"third-person orbit camera target");
            Check(Vector3.Distance(game.Player.view.transform.position,game.Player.transform.position+Vector3.up*1.25f)>.7f,"third-person camera sits behind the robot");
            foreach(var guard in guards)
            {
                var driver=guard.GetComponent<RobotAnimation>();
                Check(driver!=null && driver.animator.runtimeAnimatorController!=null,"animated robot sentinel replaces capsule");
                var renderers=driver.animator.GetComponentsInChildren<Renderer>();
                Check(renderers.Length>0 && renderers[0].sharedMaterial.GetFloat("_EnemyRecolor")>.5f,"red armor material assigned to enemy");
            }
            Check(guards.Length==game.Settings.guards,"configured enemy count");
            foreach(var laser in lasers)
            {
                laser.UpdateGrid(0); var initial=laser.bars[0].GetPosition(0); laser.UpdateGrid(.37f);
                Check(Vector3.Distance(initial,laser.bars[0].GetPosition(0))>.01f,"laser pattern moves: "+laser.pattern);
                if(laser.pattern==LaserHazard.Pattern.RotatingCross) Check(laser.bars[0].enabled && laser.bars[1].enabled,"rotating cross has two perpendicular beams");
            }
            foreach(var driver in game.GetComponentsInChildren<RobotAnimation>()) Check(driver.referenceSpeed>.1f && driver.strideLength>.1f,"foot-contact gait calibration serialized");
            Check(game.Audio.playerStepVolume<=.15f && game.Audio.enemyStepVolume<=.2f && game.Audio.spikeVolume<=.15f,"quieter footsteps and spike effects");
            Check(game.Maze.Doors.Count==Mathf.Clamp(game.Settings.doors,2,5),"configured colored doors");
            Check(game.Audio.bank.playerSteps.Length==3 && game.Audio.bank.guardSteps.Length==3 && game.Audio.bank.ambience!=null,"authored footstep and ambience clips");
            yield return new WaitForSeconds(.3f); Capture("menu"); yield return new WaitForSeconds(.2f);
            foreach(var button in game.UI.MainMenu.GetComponentsInChildren<Button>(true))
                if(button.onClick.GetPersistentEventCount()>0 && button.onClick.GetPersistentMethodName(0)==nameof(HuntGame.Resume)) { button.onClick.Invoke(); break; }
            yield return null; yield return null; Check(game.Playing && game.UI.HUD.activeSelf,"Canvas Begin action");
            foreach(var driver in game.GetComponentsInChildren<RobotAnimation>()) CheckSoleGeometry(driver);
            float time=game.Elapsed; game.Pause(); yield return new WaitForSeconds(.1f); Check(game.Elapsed==time && AudioListener.pause,"pause freezes simulation and spatial audio"); game.Resume();
            foreach(var door in game.Maze.Doors) { door.Unlock(); Check(!door.Opened,"door rejects missing "+door.requiredKey+" key"); }
            foreach(var key in game.Maze.Keys)
            {
                game.Player.Teleport(key.transform.position+Vector3.up*.04f); yield return null; yield return null;
                Check(game.HasKeyOf(key.kind),"physical "+key.kind+" key pickup");
                yield return null; Check(game.UI.InventoryLabels[(int)key.kind].text.Contains("KEY"),"inventory displays "+key.kind);
                var door=game.Maze.Doors[(int)key.kind]; game.Player.Teleport(door.transform.position-door.transform.forward+Vector3.up*.04f);
                yield return null; yield return null; Check(door.Opened && !door.panel.GetComponent<Collider>().enabled,"matching door unlocks and clears passage");
            }
            game.Player.Teleport(game.Maze.Spawn); Capture("gameplay"); yield return new WaitForSeconds(.2f);
            // Measure the live animation parameter while the actor actually travels at each locomotion speed.
            var gait=game.Player.GetComponent<RobotAnimation>(); game.Player.enabled=false;
            foreach(float pace in new[]{game.Player.walkSpeed,game.Player.sprintSpeed})
            {
                game.Player.Teleport(game.Maze.Spawn); yield return null;
                for(int frame=0;frame<8;frame++) { yield return null; game.Player.transform.position+=Vector3.right*pace*Time.deltaTime; yield return new WaitForEndOfFrame(); }
                CheckSoleGeometry(gait);
                Check(Mathf.Abs(gait.animator.GetFloat("Playback")-gait.PlaybackRate(pace))<.15f,"walking animation tracks actual travel speed "+pace);
            }
            game.Player.enabled=true; Ready(game);
            foreach(var wall in game.Maze.GetComponentsInChildren<SpikeTrap>())
            {
                if(!wall.wallMounted) continue;
                wall.phase=wall.cycle-wall.raisedSeconds+.3f-game.Elapsed; wall.enabled=true;
                game.Player.Teleport(wall.transform.position+wall.transform.up*.45f-Vector3.up*.9f); int lives=game.Lives;
                yield return null; yield return null;
                Check(game.Lives==lives-1 && wall.Dangerous && wall.riseSeconds<.18f,"fast wall spikes damage on extension");
                wall.phase=-game.Elapsed; yield return null; yield return null;
                Check(!wall.Dangerous && wall.spikes.localPosition==Vector3.zero && wall.spikes.localScale.y<.01f,"retracted wall spikes remain inside their mounting plate");
                var bounds=new Bounds(); bool first=true;
                foreach(var renderer in wall.spikes.GetComponentsInChildren<Renderer>()) { if(first) { bounds=renderer.bounds; first=false; } else bounds.Encapsulate(renderer.bounds); }
                Check(Vector3.Dot(bounds.center-wall.transform.position,wall.transform.up)>=-.01f,"wall spikes never emerge backwards through the wall");
                wall.enabled=false; Ready(game); break;
            }
            if(spikes.Count>0)
            {
                Ready(game); var trap=spikes[0]; trap.phase=trap.cycle-trap.raisedSeconds+.5f-game.Elapsed; trap.enabled=true;
                game.Player.Teleport(trap.transform.position+Vector3.up*.04f); int before=game.Lives;
                yield return null; yield return null; Check(game.Lives==before-1 && trap.Dangerous,"raised spikes damage player");
                int after=game.Lives; yield return null; Check(game.Lives==after,"hazard damage immunity");
                trap.enabled=false; game.Player.Teleport(game.Maze.Spawn);
            }
            if(lasers.Count>0)
            {
                Ready(game); var laser=lasers[0]; float speed=laser.speed; laser.speed=0; laser.UpdateGrid(game.Elapsed);
                LineRenderer contact=null;
                foreach(var bar in laser.bars)
                    if(bar.enabled && Mathf.Abs(bar.GetPosition(0).x)<laser.width/2-.45f) { contact=bar; break; }
                Check(contact!=null,"laser wall has visible bars outside the opening");
                laser.enabled=true;
                var point=contact.transform.TransformPoint((contact.GetPosition(0)+contact.GetPosition(1))/2);
                game.Player.Teleport(new Vector3(point.x,laser.transform.position.y+.04f,point.z));
                int before=game.Lives; yield return null; yield return null; Check(game.Lives==before-1,"laser bar contact damages robot");
                Set(game,"hurtUntil",0f); game.Player.Teleport(laser.GapPosition+Vector3.up*.04f);
                int after=game.Lives; laser.speed=speed;
                yield return new WaitForSeconds(.3f);
                float clearance=laser.DistanceToGrid(game.Player.transform.position);
                Check(game.Lives==after && clearance>game.Player.BodyRadius+.035f,"walking opening remains safe while bars move");
                float span=laser.axis==LaserHazard.TravelAxis.Horizontal?laser.width:laser.height;
                float early=laser.LoopDistance(game.Elapsed),later=laser.LoopDistance(game.Elapsed+.05f);
                Check(Mathf.Abs(Mathf.Repeat(later-early,span)-speed*.05f)<.01f,"laser bars loop one way without oscillation");
                Check(laser.GetComponentsInChildren<AudioSource>().Length==0,"laser wall has no sound source");
                game.Player.Teleport(laser.GapPosition-laser.transform.forward*1.2f+Vector3.up*.04f);
                game.Player.followCamera.yaw=laser.transform.eulerAngles.y; game.Player.followCamera.pitch=20; game.Player.followCamera.Snap();
                yield return new WaitForSeconds(.15f); Capture("laser-wall"); yield return new WaitForSeconds(.15f);
                laser.enabled=false; Ready(game);
            }
            foreach(var laser in lasers)
            {
                if(laser.pattern==LaserHazard.Pattern.Wall) continue;
                Ready(game); float originalSpeed=laser.speed,originalRotation=laser.rotationSpeed;
                laser.speed=0; laser.rotationSpeed=0; laser.UpdateGrid(game.Elapsed); laser.enabled=true;
                var line=laser.bars[0]; var local=(line.GetPosition(0)+line.GetPosition(1))*.5f;
                var contact=laser.transform.TransformPoint(local); contact.y=laser.transform.position.y+.04f;
                // Horizontal beams above head height are intentionally safe; lower the fixed sample for this contact check.
                if(laser.pattern==LaserHazard.Pattern.SweepingSingle && laser.axis==LaserHazard.TravelAxis.Vertical) { laser.phase=-Mathf.PI/2+.5f; laser.UpdateGrid(game.Elapsed); }
                game.Player.Teleport(contact); int lives=game.Lives;
                yield return null; yield return null;
                Check(game.Lives==lives-1,"contact with "+laser.pattern+" damages the player");
                game.Player.Teleport(laser.transform.position-laser.transform.forward*1.1f+Vector3.up*.04f);
                game.Player.followCamera.yaw=laser.transform.eulerAngles.y; game.Player.followCamera.Snap();
                Set(game,"flash",0f);
                yield return new WaitForSeconds(.15f); Capture("laser-"+laser.pattern); yield return new WaitForSeconds(.15f);
                laser.enabled=false; laser.speed=originalSpeed; laser.rotationSpeed=originalRotation;
            }
            if(pits.Count>0)
            {
                Ready(game); var pit=pits[0]; pit.enabled=true;
                game.Player.Teleport(pit.transform.position+Vector3.up*.04f); yield return null; yield return null;
                Check(pit.State==CollapsingTile.Phase.Armed,"stepping on tile arms its collapse");
                game.Player.Teleport(game.Maze.Spawn); yield return new WaitForSeconds(pit.delay+.2f);
                Check(game.Playing && pit.State==CollapsingTile.Phase.Open && !pit.cover.gameObject.activeSelf,"leaving collapsing cover in time is safe");
                Check(!Physics.Raycast(pit.transform.position+Vector3.up*.4f,Vector3.down,.9f,~0,QueryTriggerInteraction.Ignore),"collapsed tile opens a real physical hole");
                Check(pit.holeSize.x>=game.Settings.cellSize-.1f,"collapsing floor covers the entire cell");
                foreach(var corner in new[]{new Vector3(1.5f,0,1.5f),new Vector3(-1.5f,0,-1.5f)})
                    Check(!Physics.Raycast(pit.transform.position+corner+Vector3.up*.4f,Vector3.down,.9f),"pit corners have no bypass ledges");
                yield return new WaitForSeconds(pit.respawnDelay+.15f);
                Check(pit.State==CollapsingTile.Phase.Idle && pit.cover.gameObject.activeSelf && !pit.holeObstacle.enabled,"collapsed tile returns and restores navigation");
                Check(Physics.Raycast(pit.transform.position+Vector3.up*.4f,Vector3.down,.9f),"returned tile supports the player again");
                game.Player.Teleport(pit.transform.position+Vector3.up*.04f); yield return null; yield return null;
                Check(pit.State==CollapsingTile.Phase.Armed,"returned tile can be triggered again");
                yield return new WaitForSeconds(pit.delay+.08f);
                float deadline=Time.time+2;
                while(game.Playing && game.Player.transform.position.y>pit.transform.position.y-1 && Time.time<deadline) yield return null;
                Capture("pit-fall");
                yield return new WaitForSeconds(.7f);
                Check(game.State==4 && game.Player.Falling && game.Player.transform.position.y<pit.transform.position.y-3,"robot falls into dark shaft before game over");
                pit.enabled=false; Ready(game);
            }
            if(guards.Length>0)
            {
                Ready(game); var guard=guards[0]; var entry=game.Maze.Layout.Route[1]; Vector3 forward=new Vector3(entry.x,0,entry.y).normalized;
                guard.GetComponent<NavMeshAgent>().Warp(game.Maze.Spawn+forward*1.4f); guard.GetComponent<NavMeshAgent>().ResetPath(); guard.transform.rotation=Quaternion.LookRotation(forward); guard.enabled=true;
                yield return null; Check(game.Playing,"walking behind guard does not create a sprint alert");
                game.ReportSprintNoise(game.Player.transform.position);
                Check(guard.Investigating && Vector3.Distance(guard.HeardPosition,game.Player.transform.position)<.1f,"nearby sprint noise alerts guard to actual position");
                yield return new WaitForSeconds(.9f); Check(game.State==4,"guard turns toward noise and spots exposed player");
                Capture("gameover"); yield return new WaitForSeconds(.15f); guard.enabled=false; Ready(game);
            }
            game.Player.Teleport(game.Maze.TreasurePosition+Vector3.up*.04f); yield return null; yield return null;
            Check(game.State==3,"treasure wins after all colored doors open"); Capture("victory");
            if(hadBest) PlayerPrefs.SetFloat(bestKey,oldBest); else PlayerPrefs.DeleteKey(bestKey); PlayerPrefs.Save();
            Debug.Log(valid?"HUNT SMOKE TEST PASSED: blue/red robots, third-person camera, calibrated gait, five enemies, fast wall spikes, three laser patterns, full-cell collapsing pits, keys, hearing and victory.":"HUNT SMOKE TEST FAILED");
            yield return new WaitForSeconds(.5f); Application.Quit(valid?0:1);
        }
    }
}
