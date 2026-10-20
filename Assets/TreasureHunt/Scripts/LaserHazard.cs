using UnityEngine;

namespace TreasureHunt
{
    public sealed class LaserHazard : HuntWorldObject
    {
        public enum Pattern { Wall, SweepingSingle, RotatingCross }
        public Pattern pattern;
        public float rotationSpeed=70, crossRadius=1.05f;
        public enum TravelAxis { Horizontal, Vertical }
        public TravelAxis axis;
        public LineRenderer[] bars;
        public float width=3.76f, height=2.75f, gapWidth=1.5f, gapCenter, gapHeight=2.2f;
        public float speed=2.2f, spacing=.34f, phase;
        public bool Powered => true;
        public Vector3 GapPosition => transform.TransformPoint(new Vector3(gapCenter,0,0));
        public float LoopDistance(float time) => Mathf.Repeat(time*speed+phase,axis==TravelAxis.Horizontal?width:height);
        void Update()
        {
            if(!Game.Playing) return;
            UpdateGrid(Game.Elapsed);
            if(DistanceToGrid(Game.Player.transform.position)<Game.Player.BodyRadius+.035f)
                Game.Damage(1,"The laser bars drained your last heart.");
        }
        public void UpdateGrid(float time)
        {
            if(pattern!=Pattern.Wall)
            {
                foreach(var bar in bars) bar.enabled=false;
                bars[0].enabled=true;
                if(pattern==Pattern.SweepingSingle)
                {
                    float wave=(Mathf.Sin(time*speed+phase)+1)*.5f;
                    if(axis==TravelAxis.Horizontal) { float x=Mathf.Lerp(-width/2,width/2,wave); Segment(bars[0],new Vector3(x,.04f,0),new Vector3(x,height,0)); }
                    else { float y=Mathf.Lerp(.12f,height,wave); Segment(bars[0],new Vector3(-width/2,y,0),new Vector3(width/2,y,0)); }
                }
                else
                {
                    bars[1].enabled=true;
                    var rotation=Quaternion.Euler(0,0,phase+time*rotationSpeed);
                    var center=Vector3.up*1.4f;
                    var a=rotation*Vector3.right*crossRadius; var b=rotation*Vector3.up*crossRadius;
                    Segment(bars[0],center-a,center+a); Segment(bars[1],center-b,center+b);
                }
                return;
            }
            float span=axis==TravelAxis.Horizontal?width:height;
            float offset=LoopDistance(time), left=gapCenter-gapWidth/2,right=gapCenter+gapWidth/2;
            for(int i=0;i<bars.Length/2;i++)
            {
                float coordinate=Mathf.Repeat(i*spacing+offset,span);
                var a=bars[i*2]; var b=bars[i*2+1]; b.enabled=false;
                if(axis==TravelAxis.Horizontal)
                {
                    float x=coordinate-width/2; a.enabled=x<left||x>right;
                    Segment(a,new Vector3(x,.04f,0),new Vector3(x,height,0));
                }
                else
                {
                    float y=coordinate+.04f;
                    if(y>=gapHeight) { a.enabled=true; Segment(a,new Vector3(-width/2,y,0),new Vector3(width/2,y,0)); }
                    else
                    {
                        a.enabled=left>-width/2+.01f; b.enabled=right<width/2-.01f;
                        Segment(a,new Vector3(-width/2,y,0),new Vector3(left,y,0));
                        Segment(b,new Vector3(right,y,0),new Vector3(width/2,y,0));
                    }
                }
            }
        }
        void Segment(LineRenderer line,Vector3 a,Vector3 b)
        {
            line.SetPosition(0,a); line.SetPosition(1,b);
        }
        public float DistanceToGrid(Vector3 feet)
        {
            float closest=float.MaxValue;
            Vector3 bottom=feet+Vector3.up*Game.Player.BodyRadius,top=feet+Vector3.up*(Game.Player.BodyHeight-Game.Player.BodyRadius);
            foreach(var bar in bars)
            {
                if(!bar.enabled) continue;
                closest=Mathf.Min(closest,SegmentDistance(bottom,top,bar.transform.TransformPoint(bar.GetPosition(0)),bar.transform.TransformPoint(bar.GetPosition(1))));
            }
            return closest;
        }
        static float SegmentDistance(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {
            Vector3 u=b-a,v=d-c,w=a-c; float aa=Vector3.Dot(u,u),bb=Vector3.Dot(u,v),cc=Vector3.Dot(v,v),dd=Vector3.Dot(u,w),ee=Vector3.Dot(v,w);
            float denom=aa*cc-bb*bb;
            float s=denom>.00001f?Mathf.Clamp01((bb*ee-cc*dd)/denom):0;
            float t=cc>.00001f?Mathf.Clamp01((bb*s+ee)/cc):0;
            s=aa>.00001f?Mathf.Clamp01((bb*t-dd)/aa):0;
            return Vector3.Distance(a+u*s,c+v*t);
        }
    }
}
