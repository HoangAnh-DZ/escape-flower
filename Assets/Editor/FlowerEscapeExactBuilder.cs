using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Rebuilds editable UI from individual Figma exports and measured render bounds.</summary>
public static class FlowerEscapeExactBuilder
{
    [Serializable] public class Layout { public Layer[] layers; }
    [Serializable] public class Layer { public string id, name, path, fontSize; public float x,y,w,h; }
    static RectTransform Rect(string name, Transform parent, float height=778)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(402,height);return r;
    }
    static Image Sprite(Layer layer,Transform parent,float originY)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(layer.path);
        if(sprite==null) throw new Exception("Missing exported sprite: "+layer.path);
        var r=Rect(layer.id+" "+layer.name,parent,layer.h);
        r.sizeDelta=new Vector2(layer.w,layer.h);r.anchoredPosition=new Vector2(layer.x,-layer.y+originY);
        CorrectGearPosition(r,layer.id,originY);
        var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;
        // PNG export bounds are already tight. Resizing to the Figma text box
        // would enlarge glyphs; the native 3x export dimensions are authoritative.
        image.preserveAspect=false;return image;
    }
    static bool CombatHud(string id)
    {
        int n=int.Parse(id.Split('-')[1]);
        return n==34||n==60||n==83||n==93||n==95||(n>=135&&n<=145)||n==159||n==179||n==180||n==182||n==187||n==195;
    }
    static void CorrectGearPosition(RectTransform rect,string id,float originY)
    {
        // Align each overlay with its gear's cell; preserve native sprite sizes.
        switch(id)
        {
            case "2-101": rect.anchoredPosition=new Vector2(26,originY-667);break;
            case "2-71": rect.anchoredPosition=new Vector2(170,originY-666);break;
            case "2-72": rect.anchoredPosition=new Vector2(170+(48-rect.sizeDelta.x)/2,originY-666-(48-rect.sizeDelta.y)/2);break;
            case "2-62": rect.anchoredPosition=new Vector2(215,originY-666);break;
            case "2-69": rect.anchoredPosition=new Vector2(215+(48-rect.sizeDelta.x)/2,originY-666-(48-rect.sizeDelta.y)/2);break;
        }
    }
    [MenuItem("Flower Escape/Fix coin and gear alignment")]
    public static void FixAlignment()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        var root=UnityEngine.Object.FindFirstObjectByType<FlowerEscapeResponsive>();
        if(root==null)throw new Exception("Open FlowerEscape scene");
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Fix coin and gear alignment");
        foreach(RectTransform rect in root.combatBody)
            CorrectGearPosition(rect,rect.name.Split(' ')[0],463);
        var coin=root.safeArea.Find("AdvertisementBanner/CoinIllustration/CoinOuter") as RectTransform;
        var face=root.safeArea.Find("AdvertisementBanner/CoinIllustration/CoinFace") as RectTransform;
        if(coin==null||face==null)throw new Exception("Editable banner coin missing");
        coin.sizeDelta=new Vector2(42,42);coin.anchoredPosition=new Vector2(21,-10);
        face.sizeDelta=new Vector2(36,36);face.anchoredPosition=new Vector2(24,-13);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/UI/FlowerEscapeUI.prefab");
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);AssetDatabase.SaveAssets();
    }
    [MenuItem("Flower Escape/Build measured Figma UI")]
    public static void Build()
    {
        if(Application.isPlaying) throw new Exception("Build in Edit Mode");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
        var cam=Camera.main;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color32(12,104,141,255);
        var root=new GameObject("FlowerEscapeUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;canvas.pixelPerfect=true;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(402,874);scaler.matchWidthOrHeight=0;
        var bg=Sprite(new Layer{id="2-30",name="environment background",path="Assets/Art/Figma/Combat/environment background.png",x=0,y=0,w=402,h=874},root.transform,0).rectTransform;
        bg.anchorMin=bg.anchorMax=bg.pivot=new Vector2(.5f,1);bg.anchoredPosition=Vector2.zero;
        var bleed=Rect("BottomSafeAreaBackground",root.transform,0);bleed.anchorMin=Vector2.zero;bleed.anchorMax=new Vector2(1,0);bleed.pivot=new Vector2(.5f,0);bleed.anchoredPosition=Vector2.zero;bleed.sizeDelta=Vector2.zero;bleed.gameObject.AddComponent<Image>().raycastTarget=false;
        var safe=Rect("SafeArea",root.transform);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
        var combat=Rect("CombatUIRoot",safe);var battle=Rect("Battle",combat,320);var panel=Rect("CombatGearPanel",combat,315);var hud=Rect("Hud",combat,145);
        var gear=Rect("GearUIRoot",safe);var body=Rect("GearBody",gear,579);var gearHud=Rect("Hud",gear,145);
        var combatData=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Resources/FlowerEscape/Combat_Layout.json"));
        foreach(var l in combatData.layers)
        {
            if(CombatHud(l.id)) Sprite(l,hud,0);
            else if(l.y>=460) Sprite(l,panel,463);
            else Sprite(l,battle,140);
        }
        Button start=null;
        var gearData=JsonUtility.FromJson<Layout>(File.ReadAllText("Assets/Resources/FlowerEscape/Gear_Layout.json"));
        foreach(var l in gearData.layers)
        {
            var image=Sprite(l,l.y<145?gearHud:body,l.y<145?0:199);
            if(l.name=="button_big_cyan") { image.raycastTarget=true;start=image.gameObject.AddComponent<Button>();start.targetGraphic=image;start.transition=Selectable.Transition.None; }
        }
        FlowerEscapeBannerBuilder.Build(safe);
        var controller=root.AddComponent<FlowerEscapeUiController>();var so=new SerializedObject(controller);
        so.FindProperty("gearRoot").objectReferenceValue=gear.gameObject;so.FindProperty("combatRoot").objectReferenceValue=combat.gameObject;so.FindProperty("startButton").objectReferenceValue=start;so.ApplyModifiedPropertiesWithoutUndo();
        var responsive=root.AddComponent<FlowerEscapeResponsive>();responsive.safeArea=safe;responsive.gearBody=body;responsive.combatBody=panel;responsive.battle=battle;responsive.hud=hud;responsive.gearHud=gearHud;responsive.bottomBleed=bleed;responsive.screenBackground=bg;
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        controller.ShowGear();Canvas.ForceUpdateCanvases();responsive.ApplyLayout();
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/UI/FlowerEscapeUI.prefab");EditorSceneManager.SaveScene(scene,"Assets/Scenes/FlowerEscape.unity");AssetDatabase.SaveAssets();
    }
}
