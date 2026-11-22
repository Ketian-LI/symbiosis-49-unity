# Controls

These controls apply to the construction game.

| Input | Action |
| --- | --- |
| Click a hand card | Select a room category |
| Click a lit empty plot, then confirm | Place the selected room |
| Hover a built plot | Read its status and causes |
| Click a home | Inspect work and meal choices, where available |
| H or the Index button | Open or close score-source review after a recorded settlement |
| Tab / Shift+Tab | Move to the next / previous score entry |
| Previous / Next buttons | Move through the same entries |
| F | Centre the indexed plot in Endless |
| O | Open or close the manual |
| Arrow keys | Pan the board during active Endless play |
| Esc | Close an open score index; otherwise use the existing pause/close action |

Selecting a card does not place a room. A legal preview still needs confirmation.
Enter-to-confirm and clickable manual glossary terms are planned; they are not implemented in this source.

## Score-source review

The game labels this view **Index**. It explains the last settled day, not the total score across the run.
The separate three-goal review keeps its own progress and rewards.

Room brackets mark relevant plots. Smaller brackets follow visible residents or animals.
Animals use green, residents use bronze/silver/gold cues, shared scores use blue and deductions use rust.
Text also states the cause and points.

Entries use recorded meals, routines and placement effects. Animals that did not eat and residents that earned no
activity score are not added as contributors. If an old record lacks an actor identity, the view uses known rooms.
It does not recreate an animal that has left or died.

During Fill settlement, highlights follow the animal, resident and shared-score phases.
Review reopens those causes after the animation. F can locate an off-screen plot in Endless.

Index shortcuts pause while a name field, manual, pause menu, placement confirmation or blocking overlay is active.
They wait for the opening tour and case animation to finish. Close the result panel to review the finished board.

Implementation: [controller](../../Assets/Scripts/Presentation/ConstructionPrototypeController.cs),
[score-source controls](../../Assets/Scripts/Presentation/ConstructionPrototypeController.ScoreTrace.cs) and
[manual](../../Assets/Scripts/Presentation/ConstructionPrototypeController.QuickGuide.cs).
