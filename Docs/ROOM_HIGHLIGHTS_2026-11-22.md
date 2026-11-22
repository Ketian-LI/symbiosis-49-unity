# Room selection and three settlement batches, 22 November 2026

The author requested that the remaining work be completed one item at a time, beginning with highlights. This revision
implements room selection and the three settlement groups. The approved visual mockup, deck catalogue, recurring penalties,
manual cross-references/diagrams and the P journal remain separate pending work.

Click a built room to pin its relationships. Pointer exit no longer cancels the pin. Click it again, select another room,
use the Cancel selection button or press Esc to clear it. Choosing a hand card or an empty plot clears the pin. Manual,
pause, placement, result and score-source overlays suspend these highlights; a hidden pin does not take a modal's Esc key.
A new run clears the previous run's selection. Visible relationships follow panning in Endless.

For a waste room, translucent blue tiles and thin blue boundaries show its collection range: the surrounding eight plots
plus plots sharing edges with a continuous street chain. The selected room has a brass outline. A residence covered by
collection can simultaneously have a red warning frame for a bad shared-edge pairing. A diagonal residence can be covered
without receiving that shared-edge penalty. The range uses the same
[CoveragePlots](../Assets/Scripts/Core/ConstructionWasteModel.cs) helper as actual collection; it is not inferred from the
presence of waste or from an arbitrary circular radius. Legacy cleanup capacities and pairing versions are retained.

The [relationship projection](../Assets/Scripts/Presentation/ConstructionRoomRelations.cs) also identifies favourable
pairings and related rooms/visible actors from the last recorded settlement. Green frames identify favourable pairings;
blue corner frames identify recorded relationships. The shared aggregate is excluded when following one selected room,
so an individual selection does not automatically illuminate the entire town. Departed historical actors are not recreated.

Automatic settlement shows residents first, animals second and shared score third, with a labelled 1/3, 2/3, 3/3 phase.
Resident highlights retain bronze/silver/gold, animal highlights use green, shared highlights use blue and deductions retain
their warning colour. Each batch clears its brackets before the next. Selection highlights are suspended throughout the
animation, and H review waits until it ends. Placement cues retain their separate numeric explanation without becoming a
fourth automatic participant-highlight group. The same ordering is checked in Fill and Endless.

This interface does not deduct points. Pairing explanations explicitly identify the current building-time rule. Recurring
deductions were requested but are **not implemented in this revision**. The projection and repeated selection checks verify
unchanged run JSON. No score formula, trophy threshold, save format or assessment claim changes here.

Verification: [17 EditMode checks](../Documentation/evidence/room-highlights-editmode.xml) and
[23 PlayMode checks](../Documentation/evidence/room-highlights-playmode.xml), including range geometry, simultaneous coverage
and warning frames, pointer exit, modal isolation, cancellation, bilingual SDF text, phase order/cleanup and existing controls.
These are overlapping technical batches, not a new cumulative unique-test total or participant evidence. The older opening
test limitations remain in the evaluation history. Existing score-motion checks were updated to wait for the requested batch
order rather than assuming that animal cues appear first.

Actual Unity fixture renders: [selected waste room](../Documentation/images/room-selection-zh.png),
[English](../Documentation/images/room-selection-en.png), [residents](../Documentation/images/score-batch-resident.png),
[animals](../Documentation/images/score-batch-animal.png), [shared](../Documentation/images/score-batch-shared.png).
The capture helper now accounts for the Endless viewport's world offset; this keeps a captured room underneath its UI frame.
This is a source revision. No executable was rebuilt.
