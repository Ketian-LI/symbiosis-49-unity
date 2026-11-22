# Construction controls

The keyboard and mouse controls below apply to the construction scenes in
[ConstructionPrototypeController.cs](../Assets/Scripts/Presentation/ConstructionPrototypeController.cs),
[ScoreTrace.cs](../Assets/Scripts/Presentation/ConstructionPrototypeController.ScoreTrace.cs) and
[QuickGuide.cs](../Assets/Scripts/Presentation/ConstructionPrototypeController.QuickGuide.cs).

| Input | Action | Scope |
| --- | --- | --- |
| Click a hand card | Select its room category | Fill; selection alone does not place a room |
| Click a lit empty plot, then confirm | Commit the selected room | A legal preview and explicit confirmation are required |
| Hover a built plot | Inspect status and causes | Construction board |
| Click a built room | Pin its coverage, pairings and recorded score relationships; click it again to cancel | Construction scenes; warning and coverage can coexist |
| Click a home | Inspect automatic work/meal destinations and missing links | No manual dispatch editor |
| Click the draw pile | Inspect actual remaining / initial category counts | Fill; outside click, close or Esc dismisses it |
| H or Score sources | Open or close the score-source index | After a recorded settlement |
| Tab | Open the index or select the next entry | Wraps after the last entry |
| Shift+Tab | Select the previous index entry | Opens at the last entry when closed |
| Previous / Next buttons | Move through score entries | Same index as the keyboard controls |
| F | Centre the indexed plot | Endless; Fill already displays its complete board |
| Esc | Close an open index, otherwise clear visible room selection, otherwise open/close pause | Other overlays retain their existing pause/close behaviour |
| O | Open or close the game manual | Linked terms jump to marked definitions |
| P | Open/close rules and building examples | Three pages; Esc also closes |
| I | Open/close the independent settlement journal | Actual placements, deductions and upgrades |
| Hover / click a linked term | Show meaning / jump to its manual definition | Reading surfaces; button/input actions retained |
| Arrow keys | Pan the board viewport | Active Endless play, with the board open |

Index shortcuts are suspended while a name input, manual, pause menu, remaining-card catalogue, placement confirmation or blocking overlay is active.
They also wait for the opening tour and case animation to finish. The player can review the finished layout before opening
the index; a visible result overlay keeps the index inactive.

## Reading the highlights

During Fill settlement, the existing score animation switches the highlights between resident, animal and shared-score
contributors. Pairing/build cues also identify their relevant rooms. The index reopens these observed causes after animation.
Its heading shows the selected entry and total entry count. It describes the last settled day, not cumulative lifetime score.

Room corner brackets identify the relevant plots. Smaller brackets follow visible residents or animals as they move.
Animal contributions use green; residents retain the score cue's bronze/silver/gold palette; shared contributions use blue.
Negative contributions use rust. The index also states the cause and points, so colour is not the only cue.

An animal entry reads its recorded meal source and home. A resident entry reads the recorded routine and associated rooms.
Unfed animals and residents without awarded activity are excluded. Missing historical identities fall back to known rooms;
an animal that has left or died is not recreated for review. Animals sharing a home use a stable ordinal within that visible
home group; the save does not provide permanent individual animal IDs.

The index covers observed animal/resident contributions and recorded placement causes. The three goal cards keep their own
progress and reward review. This interface does not claim a separate object-level explanation for every historical goal reward.
