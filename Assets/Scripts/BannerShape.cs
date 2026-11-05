using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class BannerShape : MaskableGraphic
{
    public enum Shape { Ellipse, Triangle, Diamond }
    public Shape shape;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;
        if(shape==Shape.Ellipse)
        {
            vh.AddVert(r.center,color,Vector2.zero);
            for(int i=0;i<=48;i++)
            {
                float a=i*Mathf.PI*2/48;
                vh.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width/2,Mathf.Sin(a)*r.height/2),color,Vector2.zero);
                if(i>0)vh.AddTriangle(0,i+1,i);
            }
        }
        else
        {
            vh.AddVert(new Vector2(r.xMin,shape==Shape.Diamond?r.center.y:r.yMin),color,Vector2.zero);
            vh.AddVert(new Vector2(r.center.x,r.yMax),color,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,shape==Shape.Diamond?r.center.y:r.yMin),color,Vector2.zero);
            vh.AddTriangle(0,1,2);
            if(shape==Shape.Diamond){vh.AddVert(new Vector2(r.center.x,r.yMin),color,Vector2.zero);vh.AddTriangle(0,2,3);}
        }
    }
}
