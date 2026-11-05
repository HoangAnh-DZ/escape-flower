using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class FlowerEscapeResponsive : MonoBehaviour
{
    public RectTransform safeArea, gearBody, combatBody, battle, hud, gearHud, screenBackground, bottomBleed;
    void LateUpdate() { ApplyLayout(); }
    public void ApplyLayout()
    {
        if (safeArea == null) return;
        var canvas=(RectTransform)safeArea.parent;
        float scale=Mathf.Min(canvas.rect.width/402f,canvas.rect.height/874f);
        safeArea.anchorMin=safeArea.anchorMax=safeArea.pivot=new Vector2(.5f,.5f);
        safeArea.sizeDelta=new Vector2(402,874);safeArea.anchoredPosition=Vector2.zero;
        safeArea.localScale=Vector3.one*scale;
        if(screenBackground!=null)
        {
            screenBackground.anchorMin=screenBackground.anchorMax=screenBackground.pivot=new Vector2(.5f,.5f);
            screenBackground.anchoredPosition=Vector2.zero;screenBackground.sizeDelta=new Vector2(402,874)*scale;
        }
        Place(hud,0);Place(gearHud,0);Place(battle,140);Place(combatBody,463);Place(gearBody,199);
        if(bottomBleed!=null)bottomBleed.GetComponent<Image>().enabled=false;
    }
    static void Place(RectTransform r,float y)
    {if(r!=null){r.localScale=Vector3.one;r.anchoredPosition=new Vector2(0,-y);}}
}
