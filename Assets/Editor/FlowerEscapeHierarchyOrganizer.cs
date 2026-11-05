using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Organizes the measured Figma UI without changing rendered positions.</summary>
public static class FlowerEscapeHierarchyOrganizer
{
    const float GearMatchRadius = 38f;
    const string PendingKey = "FlowerEscape.HierarchyOrganization.Pending";

    [InitializeOnLoadMethod]
    static void RunPendingOrganization()
    {
        // The editor can be minimized while scripts recompile. Run once after
        // the reload, then clear the flag so later domain reloads are inert.
        if (!SessionState.GetBool(PendingKey, false) && !EditorPrefs.GetBool(PendingKey + ".AutoOnce", false)) return;
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(PendingKey, false) || !EditorPrefs.GetBool(PendingKey + ".AutoOnce", false))
            {
                SessionState.SetBool(PendingKey, false);
                if (!EditorApplication.isPlaying && EditorSceneManager.GetActiveScene().path == "Assets/Scenes/FlowerEscape.unity")
                {
                    EditorPrefs.SetBool(PendingKey + ".AutoOnce", true);
                    Organize();
                }
            }
        };
    }

    [MenuItem("Flower Escape/Queue hierarchy organization")]
    public static void QueueOrganization()
    {
        SessionState.SetBool(PendingKey, true);
        AssetDatabase.Refresh();
        Debug.Log("Flower Escape hierarchy organization queued for the next editor reload.");
    }

    [MenuItem("Flower Escape/Organize hierarchy by gear")]
    public static void Organize()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before organizing the hierarchy.");
        var root = UnityEngine.Object.FindFirstObjectByType<FlowerEscapeResponsive>();
        if (root == null) throw new InvalidOperationException("Open FlowerEscape.unity first.");

        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Organize Flower Escape hierarchy by gear");
        int groups = 0, members = 0;
        foreach (var parent in new[] { root.battle, root.combatBody, root.gearBody })
        {
            if (parent == null) continue;
            var direct = parent.Cast<Transform>().Where(t => t.childCount == 0).ToList();
            var cores = direct.Where(IsGearCore).ToList();
            foreach (var core in cores)
            {
                var group = MakeGroup(parent, GroupName(core.name));
                var related = direct.Where(t => t != core && IsRelated(t, core, cores)).ToList();
                ReparentPreservingLayout(core, group);
                foreach (var item in related) { ReparentPreservingLayout(item, group); members++; }
                groups++;
            }
            OrganizeSlots(parent, ref groups, ref members);
        }

        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);
        var prefab = "Assets/Prefabs/UI/FlowerEscapeUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("Flower Escape hierarchy organized: " + groups + " gear groups, " + members + " related child objects.");
    }

    [MenuItem("Flower Escape/Refine GearBody hierarchy names")]
    public static void RefineGearBodyHierarchy()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before refining the hierarchy.");
        var root = UnityEngine.Object.FindFirstObjectByType<FlowerEscapeResponsive>();
        if (root == null || root.gearBody == null) throw new InvalidOperationException("Open FlowerEscape.unity first.");
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Refine GearBody hierarchy names");

        var direct = root.gearBody.Cast<Transform>().ToList();
        var gearGroups = direct.Where(t => t.name.StartsWith("GearObject_Gear_", StringComparison.Ordinal)).ToList();
        var numberCounts = new Dictionary<string, int>();
        foreach (var group in gearGroups)
        {
            var match = Regex.Match(group.name, "GearObject_Gear_(\\d+)");
            if (!match.Success) continue;
            var number = match.Groups[1].Value;
            int occurrence = numberCounts.ContainsKey(number) ? ++numberCounts[number] : (numberCounts[number] = 1);
            RefineGearGroup((RectTransform)group, number, occurrence);
        }

        // Remaining direct items are section-level objects. Keep them together
        // so GearBody no longer has a long unstructured list.
        var remaining = root.gearBody.Cast<Transform>().Where(t => !t.name.StartsWith("Gear_", StringComparison.Ordinal)).ToList();
        var sections = new Dictionary<string, RectTransform>();
        foreach (var item in remaining)
        {
            if (item.name.StartsWith("GearBody_", StringComparison.Ordinal)) continue;
            var rect = (RectTransform)item;
            var top = -rect.anchoredPosition.y;
            var sectionName = top < 140f ? "GearBody_Header" : top < 577f ? "GearBody_GearGrid" : "GearBody_ShopFooter";
            RectTransform section;
            if (!sections.TryGetValue(sectionName, out section)) section = sections[sectionName] = MakeGroup(root.gearBody, sectionName);
            ReparentPreservingLayout(item, section);
        }

        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/UI/FlowerEscapeUI.prefab");
        AssetDatabase.SaveAssets();
        Debug.Log("GearBody hierarchy refined: " + gearGroups.Count + " separate gear groups with semantic child names.");
    }

    [MenuItem("Flower Escape/Finalize GearBody semantic names")]
    public static void FinalizeGearBodySemanticNames()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before refining the hierarchy.");
        var root = UnityEngine.Object.FindFirstObjectByType<FlowerEscapeResponsive>();
        if (root == null || root.gearBody == null) throw new InvalidOperationException("Open FlowerEscape.unity first.");
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Finalize GearBody semantic names");
        foreach (var item in root.gearBody.GetComponentsInChildren<Transform>(true))
        {
            if (item.childCount != 0) continue;
            var match = Regex.Match(item.name, @"Gear (\d+)");
            if (match.Success && !item.name.StartsWith("Gear_", StringComparison.Ordinal)) item.name = "Gear_" + match.Groups[1].Value + "_Icon";
        }
        NormalizeSiblingNames(root.gearBody);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        EditorSceneManager.SaveScene(root.gameObject.scene);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, "Assets/Prefabs/UI/FlowerEscapeUI.prefab");
        AssetDatabase.SaveAssets();
        Debug.Log("GearBody semantic names finalized: duplicate labels receive stable numeric suffixes.");
    }

    static void NormalizeSiblingNames(Transform parent)
    {
        foreach (var child in parent) NormalizeSiblingNames((Transform)child);
        var children = parent.Cast<Transform>().ToList();
        foreach (var group in children.GroupBy(t => t.name).Where(g => g.Count() > 1))
        {
            int index = 1;
            foreach (var item in group) item.name = group.Key + "_" + index++.ToString("00");
        }
    }

    static void RefineGearGroup(RectTransform group, string number, int occurrence)
    {
        group.name = "Gear_" + number + "_Instance_" + occurrence.ToString("00");
        var leaves = group.Cast<Transform>().ToList();
        var visuals = MakeGroup(group, "Gear_" + number + "_Visuals");
        var labels = MakeGroup(group, "Gear_" + number + "_Labels");
        var details = MakeGroup(group, "Gear_" + number + "_Details");
        int detailIndex = 1;
        foreach (var item in leaves)
        {
            var lower = item.name.ToLowerInvariant();
            var category = lower.Contains("/s") || IsNumberName(item.name) || lower.Contains("free") || lower.Contains("start") || lower.Contains("refresh") || lower.Contains("mph")
                ? labels : lower.Contains("slot") || lower.Contains("button") || lower.Contains("bullet") || lower.Contains("dot_") || lower.Contains("video")
                ? details : visuals;
            item.name = SemanticChildName(item.name, number, detailIndex++);
            ReparentPreservingLayout(item, category);
        }
        if (visuals.childCount == 0) UnityEngine.Object.DestroyImmediate(visuals.gameObject);
        if (labels.childCount == 0) UnityEngine.Object.DestroyImmediate(labels.gameObject);
        if (details.childCount == 0) UnityEngine.Object.DestroyImmediate(details.gameObject);
    }

    static string SemanticChildName(string original, string number, int index)
    {
        var lower = original.ToLowerInvariant();
        if (lower.Contains("gear connected") || lower.Contains("flower") || lower.Contains("leaf")) return "Gear_" + number + "_Leaf";
        if (lower.Contains("gear rotor")) return "Gear_" + number + "_Rotor";
        if (lower.Contains("/s") || lower.Contains("mph")) return "Gear_" + number + "_RateLabel";
        if (IsNumberName(original) || lower.Contains("free")) return "Gear_" + number + "_Count";
        if (lower.Contains("slot")) return "Gear_" + number + "_Slot";
        if (lower.Contains("button")) return "Gear_" + number + "_ButtonDetail";
        if (lower.Contains("bullet") || lower.Contains("dot_") || lower.Contains("video")) return "Gear_" + number + "_Badge";
        if (lower.Contains("gear ") && !lower.Contains("connected")) return "Gear_" + number + "_Icon";
        return "Gear_" + number + "_Detail_" + index.ToString("00");
    }

    static bool IsNumberName(string value)
    {
        return Regex.IsMatch(value.Trim(), @"(?:^|\s)(?:0|1|2|3|4|5|8|10|13)$");
    }

    static bool IsGearCore(Transform t)
    {
        var n = t.name;
        if (n.Contains("Gear Rotor") || n.Contains("Gear connected") || n.Contains("Slot gear")) return false;
        if (n.Contains("Gear -") || n.Contains("Gear X1_5")) return false;
        return n.IndexOf("Gear ", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string GroupName(string objectName)
    {
        var clean = objectName;
        var space = clean.IndexOf(' ');
        if (space >= 0 && clean.IndexOf('-', 0, space) >= 0) clean = clean.Substring(space + 1);
        clean = clean.Replace(" ", "_").Replace("-", "_");
        return "GearObject_" + clean;
    }

    static bool IsRelated(Transform item, Transform core, List<Transform> cores)
    {
        if (item.parent != core.parent) return false;
        if (cores.Contains(item)) return false;
        var a = (RectTransform)item; var b = (RectTransform)core;
        var delta = a.anchoredPosition - b.anchoredPosition;
        var distance = delta.magnitude;
        if (distance <= GearMatchRadius) return true;
        // Text and small overlay elements often sit at the lower edge of the icon.
        var label = item.name;
        bool labelLike = IsNumericOrLabel(label);
        return labelLike && Mathf.Abs(delta.x) <= 24f && Mathf.Abs(delta.y) <= 34f;
    }

    static bool IsNumericOrLabel(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains(" start") || n.Contains(" refresh") || n.Contains("free") ||
               n.Contains("/s") || n.EndsWith(" 0") || n.EndsWith(" 1") || n.EndsWith(" 2") ||
               n.EndsWith(" 3") || n.EndsWith(" 4") || n.EndsWith(" 5") || n.EndsWith(" 8");
    }

    static void OrganizeSlots(RectTransform parent, ref int groups, ref int members)
    {
        var direct = parent.Cast<Transform>().Where(t => t.childCount == 0).ToList();
        var slots = direct.Where(t => t.name.Contains("Slot gear") || t.name.Contains("button_buy gear")).ToList();
        if (slots.Count < 2) return;
        var ordered = slots.OrderBy(t => ((RectTransform)t).anchoredPosition.x).ToList();
        int index = 1;
        foreach (var slot in ordered)
        {
            var group = MakeGroup(parent, "LoadoutSlot_" + index++);
            ReparentPreservingLayout(slot, group); members++; groups++;
        }
    }

    static RectTransform MakeGroup(Transform parent, string baseName)
    {
        var name = baseName; int i = 2;
        while (parent.Find(name) != null) name = baseName + "_" + i++;
        var go = new GameObject(name, typeof(RectTransform));
        var group = go.GetComponent<RectTransform>(); group.SetParent(parent, false);
        group.anchorMin = new Vector2(0, 1); group.anchorMax = new Vector2(0, 1); group.pivot = new Vector2(0, 1);
        group.anchoredPosition = Vector2.zero; group.sizeDelta = ((RectTransform)parent).rect.size;
        return group;
    }

    static void ReparentPreservingLayout(Transform item, RectTransform group)
    {
        var rect = (RectTransform)item;
        var anchored = rect.anchoredPosition; var size = rect.sizeDelta; var scale = rect.localScale; var rotation = rect.localRotation;
        rect.SetParent(group, false); rect.anchoredPosition = anchored; rect.sizeDelta = size; rect.localScale = scale; rect.localRotation = rotation;
    }
}
