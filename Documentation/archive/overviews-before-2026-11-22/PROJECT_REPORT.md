> Archived overview, replaced on 22 November 2026. It describes its recorded snapshot.

# Project report

SYMBIOSIS: 49 uses a room-card construction game to investigate how spatial choices affect visible opportunities for residents
and urban wildlife. Players arrange homes, work, meals, green habitat, streets and waste facilities. The model records activity;
the interface connects its consequences back to the rooms and visible participants.

The research question is: how can an interactive simulation make the ways in which human spatial decisions shape urban wildlife
conditions perceptible? The contribution is a designed relationship between arrangement and feedback, supported by inspectable
source and a documented development process. The simplified species and scoring rules express the designer's choices.

## Interaction and interpretation

Fill builds a finite board through successive card placements. Endless extends the layout beyond the visible window.
Players preview a location and confirm construction. Choosing a card changes the preview; it does not place a room or guarantee
that a resident completes a journey or an animal eats. The [controls reference](../../technical/CONTROLS.md) describes the player actions.

The tabletop presentation uses illustrated hand cards and furnished room interiors. Neighbouring placements can alter rooms:
connected greens improve habitat, homes can acquire gardens, restaurants beside plazas gain capacity, and suitable streets become
less risky. Plant maturity, access, available food and actual activity remain separate model conditions. Their implementations
are in [placement rules](../../../Assets/Scripts/Core/ConstructionPlacementScore.cs),
[food settlement](../../../Assets/Scripts/Core/ConstructionFoodModel.cs) and
[resident routines](../../../Assets/Scripts/Core/ConstructionHumanModel.cs).

The neighbourhood journal pairs an everyday description with its trigger. A garden is a view outside a window, followed by the
adjacency condition that produced it. Meal and routine stories appear only after recorded activity. Negative stories retain
bad pairings and road deaths. The [story projection](../../../Assets/Scripts/Presentation/ConstructionNeighborhoodStories.cs) reads
these facts without awarding points or creating another event.

The score index gives players a route from a contribution to its visible cause. Room brackets mark the relevant plots; actor
brackets follow known contributing residents or animals. The player can move between entries and locate an off-screen cause
in Endless. Its [projection](../../../Assets/Scripts/Presentation/ConstructionScoreTrace.cs) reads the last settled day and keeps
previewed outcomes separate. Whether these explanations improve players' interpretations remains a user-research question.

## Implementation and persistence

The Unity source separates run state, resident routes, food, wildlife settlement, scoring, persistence and presentation.
The [technical guide](../../technical/SETUP_AND_BUILD.md) maps the code and the build entry points. Scores and placement rules are versioned,
so historical saves keep their own rules. The model decides the result; controller partials coordinate images, movement,
stories, goal cards, participant highlights and result review.

The optional HTTP leaderboard is separate from local rankings and ordinary play. Its stored best result is keyed by a
public-IP digest, mode and rule version. People sharing a public network may therefore share a record. The service does not
independently replay client-submitted totals; its pseudonymous IP-derived records are not anonymous player accounts.
The source is in [Services/leaderboard](../../../Services/leaderboard), with limitations in the [evaluation record](../../EVALUATION.md).

## Development evidence

The project developed through a browser feeding prototype, a tangible Unity experiment, bounded room rearrangement and the
construction game. The [repository lineage](../../process/REPOSITORY_LINEAGE.md) identifies each implementation; the
[development journal](../../DEVELOPMENT_JOURNAL.md) traces decisions and revisions. Earlier histories and tests retain their own scope.

Recorded faults include lost goal presentation after restart, input focus failures, result overlays blocking review, small score
differences between layouts, and explanations that disappeared with their animation. The journal links these problems to source
changes and selected checks. It also records the score calibration and its diagnostic limits, rather than treating simulation
policy results as player findings.

The [evaluation document](../../EVALUATION.md) owns the exported batch counts and describes what each check establishes.
Its fixtures test observed-meal gating, save reconstruction, negative pairings, input focus, pointer occlusion and bilingual text.
Unity captures show synthetic layouts. They establish rendering evidence for those fixtures, not a complete playthrough of every
mode, a performance study or evidence of ecological understanding.

## Research and delivery boundaries

An English working thesis reports remote Fill completions and questionnaires. The evaluated build identifier and approved raw
records remain unresolved in this archive. The [evaluation record](../../EVALUATION.md) states the reported sample and the missing
evidence. Those reports do not validate the scoring, journal or participant-index revision by association.

The physical-camera work has a separate boundary: retained records describe a single physical-frame check and synthetic
observations, while continuous multi-tile physical play remains unverified. The archived Windows executable was startup-checked
at its own snapshot. The macOS delivery contains source; no Mac application or Mac runtime result is included.

Combining human and animal points can suggest that unlike interests are exchangeable. Visible causes expose that authored
choice but do not resolve the ethical question. Further evaluation should identify what players infer, whose interests remain
hidden and whether optimisation displaces care. The author must reconcile the evaluated build, anonymous analysis, ethics and
final thesis materials. The [AI disclosure](../../AI_AND_AUTHORSHIP.md) records assistance and remaining attribution gaps.
