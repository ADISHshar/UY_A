using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace TreasureHunt
{
    [DefaultExecutionOrder(-100)]
    public sealed class HuntGame : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ConfigureVerification()
        {
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-huntSmokeTest")>=0) Application.runInBackground=true;
        }
        public MazeSettings Settings;
        public MazeBuilder Maze;
        public HuntPlayer Player;
        public HuntUI UI;
        public HuntAudio Audio;
        readonly HashSet<KeyKind> inventory=new HashSet<KeyKind>();
        HuntGuard[] guards;
        int state, lives;
        float elapsed,hurtUntil,flash,messageUntil;
        string message="",result="";
        public bool Playing => state==1;
        public int State => state;
        public int Lives => lives;
        public bool HasKey => inventory.Count>0;
        public int KeysCollected => inventory.Count;
        public int DoorsOpened { get { int count=0; foreach(var door in Maze.Doors) if(door.Opened) count++; return count; } }
        public float Elapsed => elapsed;
        public float DamageFlash => flash;
        public string Notice => elapsed<messageUntil?message:"Walk quietly near sentinels. Watch for spikes, lasers and pressure-tile clicks.";
        public string Result => result;
        public float BestTime => PlayerPrefs.GetFloat("TreasureHunt.Best."+Settings.seed,elapsed);

        void Start()
        {
            if(Maze==null || Player==null || UI==null || Audio==null)
            {
                Debug.LogError("Missing scene references. Use Treasure Hunt > Upgrade Gameplay Features.",this); enabled=false; return;
            }
            if(Settings==null) Settings=Maze.settings;
            lives=Settings.lives; Maze.RestoreSceneData(); Maze.BakeNavigation(); Player.game=this;
            guards=FindObjectsByType<HuntGuard>();
            foreach(var guard in guards)
            {
                guard.game=this; var agent=guard.GetComponent<NavMeshAgent>(); agent.enabled=true;
                if(NavMesh.SamplePosition(guard.transform.position,out var hit,2,NavMesh.AllAreas)) agent.Warp(hit.position);
            }
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true; UI.Refresh(this);
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-huntSmokeTest")>=0) StartCoroutine(HuntVerification.Run(this));
        }
        void Update()
        {
            flash=Mathf.MoveTowards(flash,0,Time.unscaledDeltaTime*1.8f);
            if(!Playing) return;
            elapsed+=Time.deltaTime;
            Vector3 delta=Player.transform.position-Maze.TreasurePosition; delta.y=0;
            if(delta.magnitude<1.4f && DoorsOpened==Maze.Doors.Count)
            {
                Burst(Maze.TreasurePosition+Vector3.up,Color.yellow); Finish(true,"The lost treasure is yours.");
            }
        }
        public bool HasKeyOf(KeyKind kind) => inventory.Contains(kind);
        public void CollectKey(KeyKind kind)
        {
            if(!inventory.Add(kind)) return;
            Tell(kind.ToString().ToUpper()+" KEY ACQUIRED  /  Find its matching door");
            Audio.UI(Audio.bank.pickup,.7f); Burst(Player.transform.position+Vector3.up,HuntKey.ColorFor(kind));
        }
        public void ReportSprintNoise(Vector3 position)
        {
            foreach(var guard in guards) if(guard.isActiveAndEnabled) guard.HearSprint(position);
        }
        public void Damage(int amount,string fatalReason)
        {
            if(!Playing || elapsed<hurtUntil) return;
            lives-=amount; hurtUntil=elapsed+Settings.damageImmunity; flash=.65f; Player.shake=1;
            Audio.UI(Audio.bank.damage,.8f); Tell("HAZARD HIT  /  Find a safe gap and watch the timing.");
            if(lives<=0) Finish(false,fatalReason);
        }
        public void Tell(string text) { message=text; messageUntil=elapsed+3.5f; }
        public void Pause() { state=2; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; AudioListener.pause=true; }
        public void Resume() { state=1; Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; AudioListener.pause=false; }
        public void Finish(bool won,string reason)
        {
            if(!Playing) return;
            state=won?3:4; result=reason; Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            Audio.UI(won?Audio.bank.victory:Audio.bank.defeat,.85f); AudioListener.pause=true;
            if(won && elapsed<PlayerPrefs.GetFloat("TreasureHunt.Best."+Settings.seed,float.MaxValue))
            { PlayerPrefs.SetFloat("TreasureHunt.Best."+Settings.seed,elapsed); PlayerPrefs.Save(); }
        }
        void Burst(Vector3 position,Color color)
        {
            var go=new GameObject("Treasure sparks"); go.transform.position=position;
            var ps=go.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.startColor=color; main.startSpeed=3; main.startSize=.12f; main.startLifetime=.8f; main.loop=false; main.duration=1;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial=Maze.Teal; ps.Emit(45); Destroy(go,2);
        }
        void LateUpdate() { if(UI!=null && Audio!=null) UI.Refresh(this); }
        public void Restart() { AudioListener.pause=false; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public void MainMenu() => Restart();
        public void QuitGame()
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #endif
        }
        public static string Format(float t) => ((int)t/60).ToString("00")+":"+((int)t%60).ToString("00")+"."+((int)(t*10)%10);
        void OnDestroy() { AudioListener.pause=false; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
    }
}
