# Score contributor index, 21 November 2026

The construction interface connects a settled score cause to its rooms and known visible actors. The player can move through
an index after animation, and the Fill score groups highlight their corresponding participants during settlement.
The [controls reference](../../technical/CONTROLS.md) owns the shortcut table and meaning of the colours.

The read-only [trace projection](../../../Project/04-Final/Assets/Scripts/Presentation/ConstructionScoreTrace.cs) uses recorded placement points,
paired neighbours, community awards, meal sources and homes, resident routines and traffic penalties.
The [controller](../../../Project/04-Final/Assets/Scripts/Presentation/ConstructionPrototypeController.ScoreTrace.cs) handles index navigation,
input restrictions and corner brackets. The [world lookup](../../../Project/04-Final/Assets/Scripts/Presentation/ConstructionWorldView.ScoreTrace.cs)
resolves existing visible actors without moving or recreating them.

The index reconstructs after loading and waits for actual settlement. An un-settled next room cannot replace the last day's
participants. Resident and animal entries point to recorded contributors; previews and idle movement cannot award membership.
Missing individual identities are not inferred from a food location. Shared score is a view of the same contributors and does
not award their points twice. Goal-card reward inspection remains in the three goal cards.

The checked fixtures include real input and pointer targets, forward/backward wrap, input-field and modal isolation,
an off-screen Endless contributor, unchanged saves, score-phase transitions and actual bilingual Unity captures.
The [evaluation record](../../../Documentation/EVALUATION.md) owns batch counts and links to their XML reports.
This is source delivery; the earlier executable release has not been rebuilt by this revision.
