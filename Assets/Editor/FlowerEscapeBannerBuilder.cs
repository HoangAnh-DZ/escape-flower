using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class FlowerEscapeBannerBuilder
{
    static TMP_FontAsset font;
    static RectTransform Rect(string name,Transform p,float x,float y,float w,float h)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(p,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);
        r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static Image Box(string n,Transform p,float x,float y,float w,float h,Color c)
    {var i=Rect(n,p,x,y,w,h).gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
    static void Shape(string n,Transform p,float x,float y,float w,float h,Color c,BannerShape.Shape s)
    {var g=Rect(n,p,x,y,w,h).gameObject.AddComponent<BannerShape>();g.color=c;g.shape=s;g.raycastTarget=false;g.SetVerticesDirty();}
    static void Text(string n,Transform p,string value,float x,float y,float w,float h,float size,Color c,bool bold=false)
    {
        var t=Rect(n,p,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=c;t.raycastTarget=false;
        t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.margin=Vector4.zero;
        t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Overflow;
    }
    public static void Build(Transform parent)
    {
        const string path="Assets/Art/Banner/BannerFont.asset";
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(font==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Banner/BannerFont.ttf");
            if(source==null)throw new System.Exception("Banner font source missing");
            font=TMP_FontAsset.CreateFontAsset(source);AssetDatabase.CreateAsset(font,path);
            if(font.material!=null)AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures)if(atlas!=null)AssetDatabase.AddObjectToAsset(atlas,font);
        }
        var root=Rect("AdvertisementBanner",parent,0,778,402,96);
        Box("Background",root,0,0,402,96,Color.black);
        var art=Rect("CoinIllustration",root,0,25,88,71);
        Box("PedestalShadow",art,15,53,57,18,new Color32(12,19,30,255));
        Box("PedestalFront",art,20,48,48,20,new Color32(28,42,62,255));
        Box("PedestalTop",art,16,45,56,6,new Color32(55,75,98,255));
        Shape("CoinOuter",art,21,10,42,42,new Color32(133,112,64,255),BannerShape.Shape.Ellipse);
        Shape("CoinFace",art,24,13,36,36,new Color32(199,181,130,255),BannerShape.Shape.Ellipse);
        Text("CoinCurrency",art,"$",34,15,24,34,25,new Color32(132,115,70,255),true);
        float[] xs={7,64,4,70},ys={29,9,48,37};
        for(int i=0;i<4;i++)
        {
            Shape("Diamond_"+i,art,xs[i],ys[i],12,10,new Color32(135,183,223,255),BannerShape.Shape.Diamond);
            Shape("DiamondGlint_"+i,art,xs[i]+3,ys[i],5,4,Color.white,BannerShape.Shape.Triangle);
        }
        Text("AdDisclosure",root,"Ad",2,79,12,11,7,Color.white);
        Text("Headline",root,"Airdrop+ đang bùng nổ",91,38,198,13,10.5f,Color.white);
        Text("RewardText",root,"$12M đã được nhận.",91,51,195,15,12.5f,new Color32(232,222,184,255),true);
        Text("ParticipationText",root,"467K người dùng đã tham gia. Còn bạn?",91,66,201,14,9.2f,Color.white,true);
        var logo=Rect("MexcLogo",root,319,34,78,19);
        Shape("MountainLeft",logo,0,3,11,13,Color.white,BannerShape.Shape.Triangle);
        Shape("MountainRight",logo,8,3,11,13,Color.white,BannerShape.Shape.Triangle);
        Text("BrandName",logo,"MEXC",21,0,59,23,18,Color.white,true);
        var cta=Box("CallToActionButton",root,293,67,109,29,new Color32(239,239,241,255));
        cta.raycastTarget=true;var button=cta.gameObject.AddComponent<Button>();button.targetGraphic=cta;
        Text("CallToActionLabel",cta.transform,"Tham gia ngay",8,4,98,23,13.5f,new Color32(25,48,84,255),true);
    }
}
