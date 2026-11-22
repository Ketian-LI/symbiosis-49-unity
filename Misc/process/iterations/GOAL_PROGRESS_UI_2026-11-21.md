# Goal progress interface, 21 November 2026

The user clarified that the mismatch concerned the three-goal progress component, rather than the printed leaf background.
This revision changes that component. The page artwork, journal, board, room pictures, right-side score and hand tray are
retained from the preceding source revision.

The goals now read as a compact ledger. Each row has a small mounted illustration, a bold goal name, a quieter description
of what is counted and a separate right-aligned progress fraction. Fine brass rules divide the rows. The section heading
uses the right-side 23-unit heading role; goal names use 19, descriptions 16 and fractions 21 (with an 18-unit lower fit limit).
The heading's small ink badge shows the number of achieved goals out of three.

Completed rows receive a green tick and a state label. A goal that awards points at the end of the run says `At finish`
until settlement, rather than implying that those points have already been awarded. Other completed goals say `Active`.
Unfinished rows identify their metric, such as linked greens, full resident routines or animal species; the progress values
still come from the existing goal model.

[ConstructionGoalReadingWash.cs](../../../Project/04-Final/Assets/Scripts/Presentation/ConstructionGoalReadingWash.cs) draws a feathered paper field
behind the reading area. It samples the unprinted centre of the existing hand-card paper, avoiding a second frame and keeping
the printed foliage from competing with numbers and lower-row text. It does not intercept pointer input. No new bitmap was
generated. The previous hard rectangular whole-section backing and duplicate progress bars are no longer shown.

The selected [21-check PlayMode batch](../../evidence/automated-tests/goal-progress-ui.xml) passes. It covers actual bilingual SDF
rendering, goal visibility after restart/skip, goal interactions, hand presentation, result controls and score-source navigation.
It overlaps older exported batches and is not a new cumulative unique-test count. Earlier known opening-test failures remain
recorded in the [evaluation history](../../../Documentation/EVALUATION.md); the opening system is outside this component revision.

Actual Unity fixture renders: [Chinese](../../../Documentation/media/screenshots/game-readable-zh.png),
[English with an achieved goal](../../../Documentation/media/screenshots/penalty-readable-en.png).
The fixtures do not establish participant preference. Goal targets, rewards, scoring and simulation rules are unchanged.
This is a source update; no Windows executable or macOS app was rebuilt.
