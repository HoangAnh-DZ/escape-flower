using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Preserve established project menu/API entry points while using measured data.
public static class FlowerEscapeBuilder
{
    [MenuItem("Flower Escape/Build UI from exported assets")]
    public static void Build() { FlowerEscapeExactBuilder.Build(); }

    [MenuItem("Flower Escape/Repair screen background")]
    public static void RepairScreenBackground()
    {
        if(Application.isPlaying) throw new Exception("Save the repair in Edit Mode");
        var layout=UnityEngine.Object.FindFirstObjectByType<FlowerEscapeResponsive>();
        if(layout==null) throw new Exception("Open the FlowerEscape scene first");
        Undo.RegisterFullObjectHierarchyUndo(layout.gameObject,"Update screen background");
        Canvas.ForceUpdateCanvases();layout.ApplyLayout();
        PrefabUtility.SaveAsPrefabAsset(layout.gameObject,"Assets/Prefabs/UI/FlowerEscapeUI.prefab");
        EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
        EditorSceneManager.SaveScene(layout.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
