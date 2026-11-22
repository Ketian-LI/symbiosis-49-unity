# Final Unity version

**FINAL - updated 22 November 2026.** Gameplay source: `ac9685dd0d652f70666369f1c1753457710e3e05`.

The current revision includes automatic resident commuting, linked manual terms, recurring Fill deductions,
P-key building examples and an independent I-key settlement journal. It retains illustrated cards, upgrades,
three goals, the real remaining-card catalogue, room relationships and separate scoring highlights.

## Open the project

1. Use Unity **6000.3.4f1**.
2. In Unity Hub, add this `Project/04-Final/` directory.
3. Open `Assets/Scenes/Main.unity` and press Play.
4. Choose Fill or Endless. Mouse and keyboard play does not require a camera.

`Assets`, `Packages` and `ProjectSettings` stay together here. `SourceAssets` retains editable art;
`Services` contains the optional leaderboard backend; `Tools` contains modelling and camera utilities.

## Source and downloads

- [Download this final source package](https://github.com/Ketian-LI/symbiosis-49-unity/releases/download/project-update-2026-11-22/SYMBIOSIS49-04-Final-2026-11-22-Routes-Source.zip)
- [Pinned gameplay source](https://github.com/Ketian-LI/symbiosis-49-unity/tree/ac9685dd0d652f70666369f1c1753457710e3e05)
- [Build guide](../../Misc/technical/SETUP_AND_BUILD.md)
- [Version index](../README.md)

The [earlier same-day source package](https://github.com/Ketian-LI/symbiosis-49-unity/releases/tag/project-versions-2026-11-22)
is retained as a historical snapshot. It predates this commuting/reference revision.
The Windows executable in [downloads](../Downloads/README.md) is still the 19 November build.
The macOS archive contains source, not a built Mac application.

## Scope and verification

Ten core terms support hover definitions and jumps to marked manual entries. P opens three reference pages;
I opens the recorded journal; O opens the manual. Enter-to-confirm remains outside this snapshot.
Recurring bad-edge deductions apply to future confirmed Fill settlements. They do not retroactively charge old saves;
Endless retains its existing building-time pairing rules.

The [implementation record](../../Misc/process/iterations/COMMUTING_REFERENCE_AND_PENALTIES_2026-11-22.md),
[rules](../../Misc/technical/GAME_RULES.md) and [controls](../../Misc/technical/CONTROLS.md) explain the behaviour.
[Evaluation](../../Documentation/EVALUATION.md#software-checks) identifies the saved checks and limitations.
This archive update verifies the pinned source bytes and imports existing reports; it does not rebuild executables.
