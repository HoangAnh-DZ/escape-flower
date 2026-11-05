using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteAlways]
public sealed class FlowerEscapeResponsive : MonoBehaviour
{
    public RectTransform safeArea;
    public RectTransform gearBody;
    public RectTransform combatBody;
    public RectTransform battle;
    public RectTransform hud;
    public RectTransform gearHud;
    public RectTransform screenBackground;
    public RectTransform bottomBleed;
    [System.Serializable] class LayoutData { public LayerData[] layers; }
    [System.Serializable] class LayerData { public string id; public float x, y, w, h; }
    readonly Dictionary<string, Vector2> originalPositions = new Dictionary<string, Vector2>();
    LayoutData combatSource;
    private void LateUpdate() { ApplyLayout(); }
    public void ApplyLayout()
    {
        if (safeArea == null) return;
        if(screenBackground!=null)
        {
            var fullRect=((RectTransform)safeArea.parent).rect;
            float scale=Mathf.Max(fullRect.width/402f,fullRect.height/874f);
            screenBackground.sizeDelta=new Vector2(402f*scale,874f*scale);
        }
        var s = Screen.safeArea;
        // Device Simulator may retain an unrelated device's safe area after
        // changing the Editor Game View resolution. Reject out-of-range data.
        if (s.xMin < 0 || s.yMin < 0 || s.xMax > Screen.width || s.yMax > Screen.height)
            s = new Rect(0, 0, Screen.width, Screen.height);
        if (Screen.width > 0 && Screen.height > 0)
        {
            safeArea.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
            // Figma coordinates already include the space above the HUD.
            // Applying the notch inset again shifts every object down.
            safeArea.anchorMax = new Vector2(s.xMax / Screen.width, 1f);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        }
        float h = safeArea.rect.height;
        if (h <= 0) return;
        float designScale = Mathf.Min(1f, h / 794f);
        float topInset = Screen.height > 0 ? ((RectTransform)safeArea.parent).rect.height * (Screen.height-s.yMax)/Screen.height : 0f;
        float topPadding = Mathf.Max(0f, topInset - 32f * designScale);
        PlaceSection(hud, topPadding, designScale);
        PlaceSection(gearHud, topPadding, designScale);

        // Use the full available height as three balanced bands: HUD, action,
        // inventory. Keep the icons undistorted and leave breathing room below.
        float inventoryTop = Mathf.Max(450f * designScale, h - 331f * designScale);
        PlaceSection(combatBody, inventoryTop, designScale);
        float actionTop = topPadding + 145f * designScale;
        float actionHeight = inventoryTop - actionTop - 14f * designScale;
        float actionScale = Mathf.Min(designScale, actionHeight / 320f);
        PlaceSection(battle, actionTop + Mathf.Max(0f, actionHeight-320f*actionScale)*0.35f, actionScale);
        var panelBackground = combatBody != null ? combatBody.Find("2-32 panel_rec_lightgreen") as RectTransform : null;
        if (panelBackground != null)
        {
            // Dock the panel through the home-indicator inset. Put its rounded
            // lower corners below the viewport rather than exposing a gap.
            float canvasHeight = ((RectTransform)safeArea.parent).rect.height;
            panelBackground.sizeDelta = new Vector2(311f, (canvasHeight-inventoryTop)/designScale + 12f);
        }
        BalanceCombatGrid(panelBackground);
        AlignFrozenGear();

        // Allocate tall-phone surplus between the grid and footer rather than
        // leaving a large empty block below the controls.
        float surplus = Mathf.Max(0f, (h-topPadding)/designScale - 794f);
        float shopTop = topPadding + (199f + surplus*0.15f)*designScale;
        PlaceSection(gearBody, shopTop, designScale);
        float gridExtra = surplus*0.55f;
        float footerExtra = surplus*0.30f;
        if (gearBody != null)
        {
            if (originalPositions.Count == 0)
            {
                var source = Resources.Load<TextAsset>("FlowerEscape/Gear_Layout");
                if (source != null)
                    foreach (var layer in JsonUtility.FromJson<LayoutData>(source.text).layers)
                        originalPositions[layer.id] = new Vector2(layer.x, 199f-layer.y);
            }
            foreach (RectTransform item in gearBody)
            {
                Vector2 original;
                string id = item.name.Split(' ')[0];
                if (!originalPositions.TryGetValue(id, out original)) continue;
                float y = -original.y;
                float extra = y < 378f ? Mathf.Clamp01(y/378f)*gridExtra : gridExtra + Mathf.Clamp01((y-378f)/201f)*footerExtra;
                item.anchoredPosition = new Vector2(original.x, original.y-extra);
            }
            var shopBackground = gearBody.Find("2-498 panel_rec_lightgreen") as RectTransform;
            if (shopBackground != null) shopBackground.sizeDelta = new Vector2(376.33334f, 497f+gridExtra);
        }
        if (bottomBleed != null)
        {
            var canvasRect = (RectTransform)safeArea.parent;
            var image = bottomBleed.GetComponent<Image>();
            bool gearVisible = gearBody != null && gearBody.gameObject.activeInHierarchy;
            // Keep the footer background edge-to-edge even when its controls
            // scale down on a short or wide display.
            // Main uses the original environment gradient through the bottom
            // inset. Only the shop needs a solid footer behind its controls.
            float footerHeight = gearVisible ? Mathf.Max(0f, canvasRect.rect.height - shopTop - (378f+gridExtra)*designScale) : 0f;
            bottomBleed.sizeDelta = new Vector2(0, footerHeight);
            image.enabled = gearVisible && bottomBleed.sizeDelta.y > 0;
            image.color = gearVisible ? new Color32(138, 101, 190, 255) : new Color32(12, 104, 141, 255);
        }
    }

    void BalanceCombatGrid(RectTransform panel)
    {
        if (combatBody == null || panel == null) return;
        if (combatSource == null || combatSource.layers == null)
        {
            var source = Resources.Load<TextAsset>("FlowerEscape/Combat_Layout");
            if (source == null) return;
            combatSource = JsonUtility.FromJson<LayoutData>(source.text);
            if (combatSource == null || combatSource.layers == null) return;
        }
        float extra = Mathf.Clamp(panel.sizeDelta.y - 324f, 0f, 48f);
        foreach (var layer in combatSource.layers)
        {
            if (layer.y < 574f || layer.x >= 325f) continue;
            RectTransform item = null;
            foreach (RectTransform candidate in combatBody)
                if (candidate.name.StartsWith(layer.id + " ")) { item = candidate; break; }
            if (item == null) continue;
            float x = layer.x, y = layer.y;
            // Third-row gears were captured between slots in the reference.
            // Snap the complete visuals back onto the lotus-cell centres.
            switch (layer.id)
            {
                case "2-101": x = 26f; y = 671f; break;
                case "2-214": x = 32f; y = 677f; break;
                case "2-71": x = 170f; y = 671f; break;
                case "2-62": x = 218f; y = 671f; break;
                case "2-210": x = 218f; y = 671f; break;
                case "2-69": x = 236f; y = 685.5f; break;
            }
            // The lotus grid is one exported sprite. Translate it and its gear
            // contents together; changing its height stretches every lotus.
            float rowOffset = extra * 0.65f;
            item.anchoredPosition = new Vector2(x, 463f-y-rowOffset);
            item.sizeDelta = new Vector2(layer.w, layer.h);
            item.localScale = Vector3.one;
        }
    }

    void AlignFrozenGear()
    {
        if (combatBody == null) return;
        var gear = combatBody.Find("2-71 Gear 2") as RectTransform;
        var ice = combatBody.Find("2-206 Mask group") as RectTransform;
        var label = combatBody.Find("2-72 2") as RectTransform;
        if (gear == null) return;
        if (ice != null)
            ice.anchoredPosition = gear.anchoredPosition + new Vector2((gear.sizeDelta.x-ice.sizeDelta.x)/2f, -(gear.sizeDelta.y-ice.sizeDelta.y));
        if (label != null)
            label.anchoredPosition = gear.anchoredPosition + new Vector2((gear.sizeDelta.x-label.sizeDelta.x)/2f, -(gear.sizeDelta.y-label.sizeDelta.y)/2f);
    }

    static void PlaceSection(RectTransform section, float referenceY, float scale)
    {
        if (section == null) return;
        section.localScale = Vector3.one * scale;
        section.anchoredPosition = new Vector2((402f - 402f * scale) / 2f, -referenceY * scale);
    }
}
