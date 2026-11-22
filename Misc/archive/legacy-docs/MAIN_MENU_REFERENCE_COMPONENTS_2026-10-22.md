> Historical record. Use the [current rules](../../technical/GAME_RULES.md) for present behaviour.

# Main menu reference-component assembly

Target: the user's second reference image from 22 October 2026. This is an assembly of independent Unity components, not a screenshot pasted over the interface. Coordinates below use the menu's 1920 × 1080 reference canvas.

| Visual part | Source asset | Unity component / placement |
| --- | --- | --- |
| Desk, ivory sheet, plants and small tabletop objects | `Assets/Resources/UI/GameplayHud/gameplay-tabletop-backdrop-v01.png` | Existing camera-aligned tabletop backdrop, behind the live board; photographic base only |
| Left city cover print | Left half of `Assets/Resources/UI/ConstructionCase/lid-city-oak-v01.png` | `Two-part folding board case/West lid hinge/Printed city lid`, its own mesh and hinge |
| Right oak cover print | Right half of the same original print | `Two-part folding board case/East lid hinge/Printed oak lid`, its own mesh and hinge |
| Slim charcoal case edge, hinges and clasp | Procedural 3D case | The 0.24 m front wall and legacy gameplay rim are both concealed under the shut lid, then revealed as it opens |
| Soft contact shadow below the closed box | Procedural transparent feathered texture | One horizontal plane below the board, fading into the photographed desk |
| Serif title and botanical divider | `Assets/Resources/UI/ConstructionMainMenu/menu-title-lockup-v01.png` | `Main Menu/Desktop Title/SYMBIOSIS 49 Wordmark` (`Image`), rectangle x680 y45 w560 h115 |
| Two matte slate-navy/aged-brass blank mode plates | `Assets/Resources/UI/ConstructionMainMenu/menu-mode-plaque-v02.png` | One non-raycast `Image` in each mode button; x625 and x970, y830, w325 h86. Each also has one soft transparent shadow behind it; the Unity button hit area draws no second frame. |
| Mode icons and translated text | Existing `ConstructionIcons/controls` and live `Text` components | Separate children of the corresponding button; text remains editable/localized |
| Footer round bases and three symbols | Existing MainMenu/ConstructionIcons assets, cropped via sprite rect | Independent `Image` children of Settings, Language, Exit at x740, x904, x1068, y935 |
| Footer divider rules | Unity `Image` objects, no bitmap texture | Four 2 px rules at y971 |

The transparent title and plate files were generated separately from the reference's design language. The exact pre-existing tabletop photograph and city/oak lid print were reused to avoid changing their illustration. Empty transparent margins are excluded by `Sprite.Create` crop rectangles in `ConstructionMainMenuLayout`; this does not modify the original PNGs. Relevant menu textures are imported without NPOT rescaling or lossy compression; the screen-aligned tabletop and UI icons do not use mipmaps. The 3D lid retains mipmaps for angled viewing.

Visual checks should be made in Unity's Game view at **1×**, with **Low Resolution Aspect Ratios unchecked**, or in a native-resolution standalone build. In the actual editor inspection this option was checked and the Game view was displaying at 1.5×; turning it off immediately restored 1× and visibly sharper text, buttons and lid art. This is an editor preview setting, not a game asset.

Image generation prompts (transparent-background mode): (1) a blank, unlabelled horizontal deep-navy and aged-brass chamfered tabletop game plaque matching the supplied board-game reference; (2) a separate dark-navy serif `SYMBIOSIS:49` title with botanical/paw colon and fine horizontal divider. The photographed tabletop and city/oak cover were not regenerated.

23 October visual correction: v02 keeps the original plaque's long shape but uses a lighter, matte slate-navy inset and narrower, less saturated brass edge. The menu camera moves farther away (112 m) with a 36° tilt to reduce the near/far width disparity. The oak room and squirrel are concealed until the lids finish opening, preventing their tall model and food badge from poking through the closed artwork. The subsequent shadow correction distinguishes the sample's soft tabletop shadow from the case itself: a transparent texture feathers below the shut lid. The shadow plane extends past the full fade radius so its edge cannot appear as a dark rectangle. The lid's navy/wood border is shallower front-to-back; the 0.24 m physical front edge and the legacy board's own perimeter are concealed when shut and restored during opening, so neither can form a second opaque lower frame in the menu. The lid illustration and gameplay board remain independent and unchanged. These adjustments are reference-matching approximations, not a claim of pixel-identical lighting to the generated concept image.
