# Final Unity version

**FINAL - card-construction game, 22 November 2026.**

This is the final Unity source snapshot identified by this submission archive. It includes the approved Fill interface,
the real remaining-card catalogue, persistent room relationships and separate settlement highlights.
The exact gameplay source is pinned to commit `bb65fc46044f7348d85c8c9ea11c933ef53f4798`.

## Open the project

1. Use Unity **6000.3.4f1**.
2. In Unity Hub, add this `Project/04-Final/` directory.
3. Open `Assets/Scenes/Main.unity` and press Play.
4. Choose Fill or Endless. Mouse and keyboard play does not require a camera.

`Assets`, `Packages` and `ProjectSettings` belong together inside this directory.
`SourceAssets` keeps editable art and model records; `Services` holds the optional leaderboard backend.
`Tools` contains the supporting modelling and camera utilities.

## Source and downloads

- [Download this final Unity source package](https://github.com/Ketian-LI/symbiosis-49-unity/releases/download/project-versions-2026-11-22/SYMBIOSIS49-04-Final-2026-11-22-Source.zip)
- [Original pinned source](https://github.com/Ketian-LI/symbiosis-49-unity/tree/bb65fc46044f7348d85c8c9ea11c933ef53f4798)
- [Build guide](../../Misc/technical/SETUP_AND_BUILD.md)
- [Version index](../README.md)

The earlier Windows executable is listed separately in [downloads](../Downloads/README.md).
It is the 19 November build; it does not contain every change in this final source snapshot.
The macOS archive contains source, not a built Mac application.

## Verification record

The saved final interface reports contain
[38 selected EditMode checks](../../Misc/evidence/automated-tests/approved-fill-ui-editmode.xml) and
[27 selected PlayMode checks](../../Misc/evidence/automated-tests/approved-fill-ui-playmode.xml).
They cover the named source snapshot and overlap earlier batches. The directory reorganisation preserves game-source bytes;
it does not constitute a new gameplay test or executable build.
