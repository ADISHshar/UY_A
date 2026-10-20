using UnityEngine;
using UnityEngine.UI;
namespace TreasureHunt
{
    public sealed class HuntGlyph : Image
    {
        public enum Symbol { Heart, Key, Grid }
        public Symbol symbol;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect=GetPixelAdjustedRect();
            if(symbol==Symbol.Grid)
            {
                for(float x=rect.xMin;x<=rect.xMax;x+=54) Quad(mesh,x,rect.yMin,1,rect.height);
                for(float y=rect.yMin;y<=rect.yMax;y+=54) Quad(mesh,rect.xMin,y,rect.width,1);
                return;
            }
            if(symbol==Symbol.Heart)
            {
                mesh.AddVert(new Vector3(rect.center.x,rect.center.y,0),color,Vector2.zero);
                const int segments=64;
                for(int i=0;i<=segments;i++)
                {
                    float t=i*Mathf.PI*2/segments;
                    float x=16*Mathf.Pow(Mathf.Sin(t),3),y=13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t);
                    mesh.AddVert(new Vector3(rect.center.x+x/34*rect.width,rect.center.y+(y+2)/32*rect.height,0),color,Vector2.zero);
                    if(i>0) mesh.AddTriangle(0,i,i+1);
                }
                return;
            }
            float size=Mathf.Min(rect.height,rect.width*.5f),radius=size*.32f;
            Vector2 center=new Vector2(rect.xMin+radius+2,rect.center.y);
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                int index=mesh.currentVertCount;
                foreach(var p in new[]{center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*(radius-2),center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(radius-2)}) mesh.AddVert(p,color,Vector2.zero);
                mesh.AddTriangle(index,index+1,index+2); mesh.AddTriangle(index,index+2,index+3);
            }
            float stem=center.x+radius-1;
            Quad(mesh,stem,center.y-1,rect.xMax-stem-1,2);
            Quad(mesh,rect.xMax-4,center.y-6,2,6);
            Quad(mesh,rect.xMax-10,center.y-5,2,5);
        }
        void Quad(VertexHelper mesh,float x,float y,float width,float height)
        {
            int index=mesh.currentVertCount;
            mesh.AddVert(new Vector3(x,y),color,Vector2.zero);mesh.AddVert(new Vector3(x+width,y),color,Vector2.zero);
            mesh.AddVert(new Vector3(x+width,y+height),color,Vector2.zero);mesh.AddVert(new Vector3(x,y+height),color,Vector2.zero);
            mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index,index+2,index+3);
        }
    }
}
