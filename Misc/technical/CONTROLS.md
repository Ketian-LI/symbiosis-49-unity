# Controls

These controls apply to the construction game.

| Input | Action |
| --- | --- |
| Click a hand card | Select a room category |
| Click a lit empty plot, then confirm | Place the selected room |
| Hover a built plot | Read its status and causes |
| Click a built room | Pin its coverage and relationship highlights; click it again to clear |
| Click a home | Inspect automatic work/meal destinations and missing links |
| Click the draw pile | Open the live remaining-card catalogue |
| H or the Index button | Open or close score-source review after a recorded settlement |
| Tab / Shift+Tab | Move to the next / previous score entry |
| Previous / Next buttons | Move through the same entries |
| F | Centre the indexed plot in Endless |
| O | Open or close the manual |
| P | Open or close the three-page building reference |
| I | Open or close the independent settlement journal |
| Hover / click a linked term | Read a short definition / jump to its marked manual entry |
| Arrow keys | Pan the board during active Endless play |
| Esc | Close the active catalogue or index; otherwise clear visible selection or use the existing pause/close action |

Selecting a card does not place a room. A legal preview still needs confirmation.
Ten core terms are linked in the manual and relevant reading surfaces. Ordinary prose and button labels retain their own actions. Enter-to-confirm remains outside this snapshot.

## Score-source review

The game labels this view **Index**. It explains the last settled day, not the total score across the run.
The separate three-goal review keeps its own progress and rewards.

Room brackets mark relevant plots. Smaller brackets follow visible residents or animals.
Animals use green, residents use bronze/silver/gold cues, shared scores use blue and deductions use rust.
Text also states the cause and points.

Entries use recorded meals, routines, placement effects and recurring bad-edge deductions. Animals that did not eat and residents that earned no
activity score are not added as contributors. If an old record lacks an actor identity, the view uses known rooms.
It does not recreate an animal that has left or died.

During settlement, highlights follow the resident, animal and shared-score phases. Each batch clears before the next.
Review reopens those causes after the animation. F can locate an off-screen plot in Endless.

Index shortcuts pause while a name field, manual, deck catalogue, pause menu, placement confirmation or blocking overlay is active.
They wait for the opening tour and case animation to finish. Close the result panel to review the finished board.

Implementation: [controller](../../Project/04-Final/Assets/Scripts/Presentation/ConstructionPrototypeController.cs),
[score-source controls](../../Project/04-Final/Assets/Scripts/Presentation/ConstructionPrototypeController.ScoreTrace.cs) and
[manual](../../Project/04-Final/Assets/Scripts/Presentation/ConstructionPrototypeController.QuickGuide.cs).
