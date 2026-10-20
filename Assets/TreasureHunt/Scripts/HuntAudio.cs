using UnityEngine;

namespace TreasureHunt
{
    public sealed class HuntAudio : MonoBehaviour
    {
        public HuntAudioBank bank;
        public AudioSource interfaceSource, ambienceSource;
        public AudioSource[] spatialSources;
        [Range(0,1)] public float effectsVolume=.8f;
        [Range(0,1)] public float playerStepVolume=.12f,enemyStepVolume=.18f,sprintStepVolume=.2f;
        [Range(0,1)] public float spikeVolume=.12f;
        int next;
        AudioClip robotFootstep,robotServo;
        void Start()
        {
            robotFootstep=PrepareOneShot(Resources.Load<AudioClip>("RobotMovement/RobotFootstep"),.24f);
            robotServo=PrepareOneShot(Resources.Load<AudioClip>("RobotMovement/RobotServo"),.22f);
            interfaceSource.ignoreListenerPause=true; ambienceSource.ignoreListenerPause=true;
            ambienceSource.clip=bank.ambience; ambienceSource.loop=true; ambienceSource.Play();
        }
        public void UI(AudioClip clip,float volume=1)
        {
            if(clip!=null) interfaceSource.PlayOneShot(clip,volume*effectsVolume);
        }
        public void At(AudioClip clip,Vector3 point,float volume=1,float pitch=1)
        {
            if(clip==null || spatialSources==null || spatialSources.Length==0) return;
            var source=spatialSources[next++%spatialSources.Length]; source.transform.position=point;
            source.pitch=pitch; source.volume=volume*effectsVolume; source.clip=clip; source.Play();
        }
        public void RobotFootContact(AudioSource source,bool enemy,bool sprint)
        {
            if(source==null) return;
            source.loop=false;
            source.volume=1;
            source.pitch=sprint?1.03f:1;
            float volume=Mathf.Clamp01(effectsVolume*(enemy?enemyStepVolume:sprint?sprintStepVolume*1.25f:playerStepVolume*.35f));
            if(sprint && robotFootstep!=null) source.PlayOneShot(robotFootstep,volume);
            if(robotServo!=null) source.PlayOneShot(robotServo,volume*(sprint?.45f:1f));
        }
        static AudioClip PrepareOneShot(AudioClip original,float duration)
        {
            if(original==null) return null;
            original.LoadAudioData();
            var data=new float[original.samples*original.channels];
            if(!original.GetData(data,0)) return original;
            float peak=0; foreach(float value in data) peak=Mathf.Max(peak,Mathf.Abs(value));
            int start=0;
            while(start<data.Length && Mathf.Abs(data[start])<Mathf.Max(.001f,peak*.02f)) start++;
            int frame=Mathf.Max(0,start/original.channels-(int)(original.frequency*.003f));
            int count=Mathf.Min(original.samples-frame,Mathf.RoundToInt(original.frequency*duration));
            if(count<1) return original;
            var trimmed=new float[count*original.channels];
            System.Array.Copy(data,frame*original.channels,trimmed,0,trimmed.Length);
            for(int i=0;i<count;i++)
            {
                float fade=Mathf.Min(1,Mathf.Min(i/(original.frequency*.001f),(count-1-i)/(original.frequency*.012f)));
                for(int channel=0;channel<original.channels;channel++) trimmed[i*original.channels+channel]*=fade;
            }
            var clip=AudioClip.Create(original.name+" Contact",count,original.channels,original.frequency,false);
            clip.SetData(trimmed,0); return clip;
        }
        void OnDestroy()
        {
            if(robotFootstep!=null && robotFootstep.name.EndsWith(" Contact")) Destroy(robotFootstep);
            if(robotServo!=null && robotServo.name.EndsWith(" Contact")) Destroy(robotServo);
            AudioListener.pause=false;
        }
    }
}
