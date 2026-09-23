# Wildlife room colour samples — 2026-09-23

These colours were sampled from the three approved concept images in `Docs/Images/`. They are **image RGB values under painted lighting**, not measured physical material colours. The Unity material column is a small, coherent palette chosen from those samples; URP lighting, shadows and monitor settings will change the final on-screen pixels. Keep the source image and Unity choice separate when replacing the prototype meshes.

| Area | Source image and sampled RGB | Unity material RGB |
| --- | --- | --- |
| Shared room frame | isolated pigeon concept `#385593` blue; later assembled-board reference is charcoal navy around `#262A33` | `#263653` boundary, `#24324C` wall after lighting; follows the later assembled-board direction |
| Open doorway trim | same — warm threshold spot sample `#D9B487` | `#C8AB82` after lighting compensation; narrower cream inset, no door leaf |
| Pigeon concrete floor | `pigeon-habitat-lowpoly-concept-v02.png` — broad floor median `#B0A9A8` | `#9B989E` after warm-light compensation |
| Pigeon vent metal | same — vent region median `#BDB3B2`, bright facet `#D5CCC8` | `#BDB3B2`, `#D5CCC8` |
| Pigeon loft wood | same — wood region `#B37D51` and sunlit roof `#DAA473` | `#B37D51`, roof `#D49A68` |
| Pigeon water | same — water region dominant `#548FB9` | `#548FB9` |
| Shrub ground | `shrub-habitat-variants-sample-v01.png` — olive ground samples vary around `#8E861F`–`#AA8F36` | `#8F934C` to keep grass distinct from the path |
| Shrub leaves | same — dark `#44521C`, mid `#5B6825`, lit `#C0BC59` | `#3F5127`, `#637239`, `#9BA04F` |
| Shrub dirt path | same — light `#D2A271`, shaded `#B17A3D` | `#D2A271`, `#B17A3D` |
| Shrub stones | same — stone sample `#A9958E` | `#A9958E` |
| Fox earth | `fox-den-final-layout-sample-v01.png` — broad earth median `#D2A86C`, representative pixel `#CDA469` | `#B39562` after warm-light compensation |
| Fox retaining bricks | same — warm brick face `#835440` | `#835440` |
| Fox culvert stone | same — lit rim facet `#9B856F`, shaded stone `#7F6C52` | `#9B856F`, `#7F6C52` |
| Fox vegetation | same — leafy sample `#626A28` | `#626A28` |
| Fox dry leaves | same — ochre leaf sample `#D4995B` | `#D4995B` |
| Fox cardboard | same — cardboard region median `#BD9857` | `#BD9857` |
| Fox tunnel and resting soil | same — entrance `#1C130E`, dark soil `#704D2B` | `#1C130E`, `#704D2B` |

Sampling method: direct RGB pixel checks plus median and six-colour quantization of small object/ground regions in the original PNGs, ignoring the transparent background. The sampled shades were then checked against the Unity top-down preview. The first render of direct `#B0A9A8` concrete appeared near `#CBBDB2` in-game, and direct `#CDA469` earth near `#ECB773`; the compensated material inputs above aim closer to the reference screen pixels. In particular, the reference images depict visible vertical faces while gameplay uses an overhead camera, so the four pigeon cubbies also receive shallow top-facing recesses.

Final preview spot check (unoccluded floor pixel, not a full-image colour metric): pigeon `#B4AAA8` versus reference broad median `#B0A9A8`; fox `#CFA76C` versus reference broad median `#D2A86C`. The shrub floor remains an intentionally flatter olive approximation because the reference is textured and partly covered by plants.

The palette is implemented in `Assets/Scripts/Presentation/UrbanPalette.cs`; room props use it in `RoomInteriorVisualBuilder.cs`, and the shrub paths use it in `UrbanWildlifeBootstrap.cs`. This pass does not change ecological rules or room-door topology. The meshes remain replaceable low-poly blockouts rather than finished assets.

Visual follow-up: the four-door shell retains its cream frame and threshold, but their visible top-down depth and bright surface area are reduced. Each shrub variant now has two major cover islands plus four small tufts in the remaining corners; these tufts are visual only and share the same mechanical cover value as before.

## Assembled-board category colours

`Docs/Images/board-full-art-reference-v01.png` is the later user-approved whole-board illustration. Its RGB values include local lighting, furniture and shadows. The table records small unoccluded floor spots as *screen-colour targets*, not exact diffuse textures; the Unity material values were adjusted with the URP top-down preview. Recheck after any lighting or post-processing change.

| Area | Example source RGB in whole-board image | Unity base material RGB | Decision |
| --- | --- | --- | --- |
| Central park grass | `#89984E` | `#718B49` | Lower the previous neon green toward olive; the pond and paths remain separate. |
| Residence | `#AE96BB` | `#8E83AE` | Retain the muted lilac coding used across the board; individual room concept has natural wood flooring. |
| Office | `#6E878D` | `#587D81` | Desaturate the prior cyan-teal toward grey-teal. |
| Food shop | `#B98358` | `#A4764D` | Shift the previous orange-yellow toward warm wood. |
| Supermarket | `#EBB95C` | `#D6AC55` | Reduce the previous over-bright gold without making it too brown. |
| Garage | grey varies around `#7B7978`–`#9D938E` | `#5C6874` | Keep a distinct cool grey; road markings and cars provide the exact identity. |
| Oak habitat | shaded olive/earth varies widely in the illustration | `#6A7C45` | Replace the overly saturated forest green with a muted olive base. |
| Waste room | standalone waste-room concept grey floor near `#99999D` | bootstrap uses a neutral grey override | Preserve the existing grey brick cross and rubbish-container colours; a single board pixel is not a reliable waste-floor sample. |

The whole-board image is illustrative, not a flat colour swatch. These adjustments are intentionally limited to room bases and shared frames; they do not move doors, props or navigation clearances.
