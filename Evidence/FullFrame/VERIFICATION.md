# Full-frame UI verification — 2026-11-05

Current scene: `Assets/Scenes/FlowerEscape.unity`.
Current prefab: `Assets/Prefabs/UI/FlowerEscapeUI.prefab`.

- Restored the complete 402×874 composition. Game content occupies y=0..778; advertisement occupies y=778..874.
- Shared footer is visible in both Main/Combat and Main Buy Gear.
- Banner uses 27 editable RectTransform objects: UI rectangles, vector mesh shapes, seven TMP labels, and a CTA Button. No flattened banner sprite is used.
- Ten Combat/Gear state cycles passed inside Unity. No errors or warnings were returned after final rendering.
- Actual camera renders are `Combat.png` and `Gear.png` at 804×1748.
- The footer is a static visual reconstruction, not an advertising SDK integration. The CTA has no external navigation configured.
- Pixel identity is not achieved: the reconstructed vector illustration and font differ from the raster advertisement in Figma. The prior responsive arrangement and position-only corrections were superseded by the requested full-frame composition.

Earlier `ExactLayout` captures/status reports describe historical revisions and are not evidence of this revision.

## Coin and gear alignment correction

- Coin outer/inner UI bounds are now square (42×42 and 36×36) with concentric centres, removing the tall oval distortion in both states.
- Main's DMG base now aligns with its badge; frozen gear 2 is centred behind its ice overlay and label; video gear 1 is centred behind its video badge and label.
- Sprite dimensions and all unrelated layout positions were preserved.
- Both camera captures were regenerated after the correction; Unity Console returned no errors or warnings.
