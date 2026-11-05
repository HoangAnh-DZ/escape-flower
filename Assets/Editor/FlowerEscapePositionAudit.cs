using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FlowerEscapePositionAudit
{
    [MenuItem("Flower Escape/Verify all Figma layer positions")]
    public static void Verify()
    {
        var layout = UnityEngine.Object.FindFirstObjectByType<FlowerEscapeResponsive>();
        Canvas.ForceUpdateCanvases();
        layout.ApplyLayout();
        Canvas.ForceUpdateCanvases();
        float scale = Mathf.Min(1f, layout.safeArea.rect.height / 778f);
        var transforms = layout.GetComponentsInChildren<RectTransform>(true);
        int checkedLayers = 0;
        float maxError = 0;
        var report = new System.Text.StringBuilder("Screen\tLayer\tDeltaX\tDeltaY\tDeltaWidth\tDeltaHeight\n");
        foreach (var screen in new[] { "Combat", "Gear" })
        {
            var data = JsonUtility.FromJson<FlowerEscapeExactBuilder.Layout>(File.ReadAllText("Assets/Resources/FlowerEscape/" + screen + "_Layout.json"));
            foreach (var layer in data.layers)
            {
                var rect = transforms.Single(t => t.name == layer.id + " " + layer.name);
                var image = rect.GetComponent<Image>();
                if (image == null || image.sprite == null) throw new Exception("Missing Figma sprite: " + layer.id);
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                var topLeft = layout.safeArea.InverseTransformPoint(corners[1]);
                var bottomRight = layout.safeArea.InverseTransformPoint(corners[3]);
                float x = (topLeft.x - layout.safeArea.rect.xMin - (402f - 402f * scale) / 2f) / scale;
                float y = (layout.safeArea.rect.yMax - topLeft.y) / scale;
                float w = (bottomRight.x - topLeft.x) / scale;
                float h = (topLeft.y - bottomRight.y) / scale;
                float error = Mathf.Max(Mathf.Abs(x-layer.x), Mathf.Abs(y-layer.y), Mathf.Abs(w-layer.w), Mathf.Abs(h-layer.h));
                maxError = Mathf.Max(maxError, error);
                report.AppendLine(screen + "\t" + layer.id + "\t" + (x-layer.x) + "\t" + (y-layer.y) + "\t" + (w-layer.w) + "\t" + (h-layer.h));
                checkedLayers++;
            }
        }
        Directory.CreateDirectory("Evidence/ExactLayout");
        File.WriteAllText("Evidence/ExactLayout/PositionAudit.tsv", report.ToString());
        if (maxError > 0.05f) throw new Exception("Figma position audit failed: maximum error " + maxError);
        Debug.Log("Figma position audit PASS: " + checkedLayers + " layers, maximum position/size error " + maxError + " reference pixels.");
    }
}
