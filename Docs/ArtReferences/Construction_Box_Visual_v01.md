# Construction box visual implementation (2026-10-18)

The current construction modes use the user-approved C6 opened-box reference as an art direction, not as a flattened gameplay screenshot. The physical 3D oak, squirrel, rooms, board foundation, and clickable plots remain live scene objects.

## Runtime assets

| Resource | Use |
| --- | --- |
| `Assets/Resources/UI/ConstructionCase/lid-city-oak-v01.png` | Shared print sliced across the two hinged 3D lid halves; also used on the fallback opening cover. |
| `Assets/Resources/UI/ConstructionCase/interior-city-v01.png` | Printed interior of the left information leaf. |
| `Assets/Resources/UI/ConstructionCase/interior-oak-v01.png` | Matching printed interior of the right action leaf. |

These three raster assets were generated with the built-in OpenAI image-generation tool using the user's approved C2/C6 visual references on 2026-10-18. They are AI-assisted concept-derived textures, not hand-painted artwork or player-research evidence. The two interior images were prompted as matching tall, flat, text-free parchment panels: a navy/terracotta city along the left bottom and a sage/navy oak canopy around the right edge, respectively. The lid image was prompted as a square flat four-colour city/oak woodblock print split down the middle. Live text, buttons, room content, and board rules are rendered separately by Unity.

### Final production prompts

Built-in image generation was used, not the CLI/API fallback. C6 was the style reference for the interior prints; C2 was the style reference for the lid print.

**Interior city:** “Create one portrait rectangular flat 2D texture, aspect ratio about 0.55 width to height. No frame, hinges, UI, text, numbers or 3D perspective. Warm ivory lithographic paper with subtle grain. Confine a deep-navy and terracotta vintage woodblock small-town print with sage street trees to the bottom edge and lower outer-left corner. Keep the upper 75% blank for live game text. Four-colour 1930s silkscreen style, no watermark.”

**Interior oak:** “Create one portrait rectangular flat 2D texture, aspect ratio about 0.55 width to height. Match the warm ivory grain of the city panel. Add deep-navy and muted-sage oak canopy with ivory branches at the upper-right edge, restrained leaves at the lower edge and two copper acorns at the far lower-right. Keep the central 70% blank for live controls and text. Four-colour 1930s silkscreen style; no frame, UI, text, perspective or watermark.”

**Closed lid:** “Create one square, flat, top-down board-game lid print with a central vertical split into two narrow portrait halves. No 3D mockup, perspective, frame, hinges or text. On the left, navy town buildings, terracotta roofs, ivory streets and sage trees; on the right, sage/navy oak woodland with branching ivory tree roots and copper acorns. Let the streets visually become roots at the seam. Dense intentional 1930s four-colour woodblock ink on warm ivory paper; no watermark.”

The construction board uses near-flush muted-sage 3D slots with narrow warm-ivory joints and the existing navy/wood/brass case frame. The legacy full-board mode retains its previous pale vacant plots; the shared opening board's construction colours and slot sizes are restored when leaving the preview.
