# Flower Escape — 2026-11-03

Status: UI implementation and practical runtime verification completed. Both layouts, asset provenance, responsive sizes and state switching verified. Rendering closely matches Figma; literal pixel identity is NOT claimed (measured differences below).

Latest layout replaces all guessed sizes with per-layer Figma measurements and the native render bounds of individual 3x exports. Original game area is 402 x 778; the external advertisement at y=778 is excluded. Corrected cropped background bounds, rotated gears, clipped boss placement, health bars, glyph dimensions, button dimensions and HUD centering on 3:4. Screen background covers the full display; controls respect SafeArea.

## Open and demonstrate

Open this project with Unity 6000.0.68f1, then open Assets/Scenes/FlowerEscape.unity and press Play.
START switches Gear to Combat. G returns to Gear; C previews Combat (Editor/development only).
Inspector context menu Preview/Gear and Preview/Combat is also available. Refresh and reward controls are presentation only; no gameplay or ad service is implemented.
Prefab: Assets/Prefabs/UI/FlowerEscapeUI.prefab.
Rebuild command: Flower Escape > Build UI from exported assets (also Build measured Figma UI). Both call the measured builder and replace the task scene; save unrelated scene work first.

## Source assets

PowerPoint: both slides' text read, both slide 2 game screenshots extracted and inspected.
Authorized Figma copy: CKVnjYSuy9c6KWFj9SlIoj, Main (2:29), Main - Buy gear (2:496), 402 x 874.
After user authenticated the browser, exported each frame's immediate child layers as separate transparent 3x PNGs: 144 Combat, 105 Gear.
249 PNGs were exported. Assets/Art/Figma now holds 174 byte-distinct PNG files. 75 unreferenced duplicate copies and their GUID metadata were archived reversibly under Evidence/ArchivedDuplicateExports after every serialized scene/prefab/asset reference was checked. Tools/FigmaAssetAliases.json preserves the mapping and the fitter resolves it on regeneration.
246 separate Unity Image components: 245 exported Figma sprite references using 171 distinct sprite files, plus one plain footer-bleed Image. Automated scan found zero missing/non-Figma sprite references. Decorative pad groups are independent sub-assets, not full gameplay frames.
Text labels use individual Figma-exported glyph sprites to preserve their exported appearance; editable TMP text/font assets have not been substituted.
Evidence/ExactLayout/AssetProvenance.tsv maps actual Unity hierarchy paths to asset files and source layer IDs. Tools/Combat_FigmaMeasurements.tsv, Gear_FigmaMeasurements.tsv and figma_text_boxes.json hold the measured source bounds and font sizes; runtime layout JSON stores each exported sprite's render bounds.
Full-frame references under Assets/Reference and old unused Resources copies are NOT referenced by the scene UI.
Earlier duplicate deletion was rejected due to reference risk. The final cleanup first remapped identical sprites, rebuilt and saved scene/prefab, verified no remaining old GUID references, then moved unused copies into the reversible archive. No source export was permanently deleted.
Original export ZIPs remain in Downloads (Untitled.zip and Untitled (1).zip).

## Verification

- Scene built and saved; reloading the FlowerEscape scene succeeded.
- Real UI roots: CombatUIRoot and GearUIRoot, each with its own Hud. Battle, CombatGearPanel and GearBody remain independent editable groups. InputSystemUIInputModule is used.
- Actual Game View resolutions: 900 x 2100 and 900 x 1200.
- Both states rendered at both resolutions; image dimensions independently verified.
- Ten actual START pointer raycast/click and editor-return cycles passed at BOTH 900 x 2100 and 900 x 1200 on the measured layout; roots remained mutually exclusive. Repeated after sprite sharing.
- Final shared-sprite 3:4 screenshots are pixel-identical to the pre-sharing captures for BOTH states.
- Latest console query: zero errors or warnings.
- No gameplay, drag/drop, AI, backend, platform builds, or added Unity packages.

## Evidence and visual review

Current evidence: Evidence/ExactLayout/Combat_9x21.png, Combat_3x4.png, Gear_9x21.png, Gear_3x4.png. SharedSprites captures verify the final sprite-sharing change.
Combat_Comparison.png and Gear_Comparison.png compare PowerPoint, Figma's 402 x 778 game area, real Unity 1206 x 2334 captures downsampled to source size, and both responsive sizes.
Tools/compare_flower_ui.py generates these comparisons, not runtime art.
Earlier Evidence subfolders and Assets/Screenshots contain superseded diagnostic captures and are not final test evidence.
Measured mean absolute RGB error against Figma is 4.346/255 for Combat and 3.870/255 for Gear. ComparisonMetrics.json records the method. This is rendering error, NOT a percentage of requirements completed. Small edge, rounding and alpha-compositing differences remain; the output is not bit-identical to Figma. Adaptive water spacing and group scale change with screen aspect ratio while sprite proportions remain intact.
Drive demo was played and inspected: Gear at 20 seconds and Combat at 22–23 seconds confirm the intended two states. No gameplay logic or additional debug buttons were added to the official UI.
Final project saved and reloaded. No unresolved compile, missing-sprite, button-input or responsive clipping blocker was observed in the required four cases. A claim of literal pixel identity would be inaccurate; use the supplied comparisons to review that stricter standard.
