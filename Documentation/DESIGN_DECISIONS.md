# Design decisions and revisions

SYMBIOSIS: 49 developed from a feeding action into a game about constructing a shared neighbourhood.
My design decisions focused on what players could choose, what a placement changed and how those changes became readable.
This account connects my design decisions with the implementation and saved verification records.
The [development journal](DEVELOPMENT_JOURNAL.md) follows the sequence of revisions.

## From feeding to spatial choices

The browser prototype explored human feeding and animals' dependence on it. A single action offered limited variation,
so I expanded the choices through routes, feeding places and a larger urban setting.
That expansion also made the scene and its rules harder to read.

I used the idea of a building section to organise city functions into rooms. Each room could have a recognisable purpose
within a bounded grid. The first room version began with a populated board and asked players to swap cells.
It concentrated too much explanation at the start and left less room for a sense of construction.
I changed the task to placing rooms one at a time, allowing the tutorial to introduce each space during play.

The [source lineage](../Misc/process/REPOSITORY_LINEAGE.md) identifies the prototypes and the transition between them.

## Habitat first, then human needs

The opening order carries the project's premise: animals already live in the place before people move in.
The central oak therefore comes first. Homes, work and meals follow as the neighbourhood develops.
This connects an instruction to a reason for building, rather than beginning with a list of room functions.

I brought the tutorial into the hand itself. The opening design specified eight fixed cards in 3/3/2 groups before free choice.
Guided steps establish the room relationships, while allowing location choices where the step permits them.
The [hand presentation](../Assets/Scripts/Presentation/ConstructionPrototypeController.FillHand.cs) retains the guided opening
and the transition to player selection.

Three starting goals provide directions beyond filling the board. Their replacement is limited before confirmation,
so players cannot keep drawing until every goal is easy. The requirement that a replacement differ from the current card
became a specific reliability fix. The [goal model](../Assets/Scripts/Core/ConstructionSpecialTaskModel.cs) records those states.

## A coherent board and hand

I repeatedly revised the card proportions, deck, tray and side pages. The deck needed to share the cards' visual form;
the hand also had to accommodate four or five cards without hiding useful plots.
The score and selected-card information were grouped on the right, with a stable area for goals on the left.

The opening animation introduced another problem: a small change in angle or page position could make the playing board
look detached from the object that had just opened. I asked for smaller tilt, aligned side leaves, narrower gaps and revised hinges and shadows.
These were continuing visual revisions rather than a single approved solution.

The [portfolio](PORTFOLIO.md) shows labelled software captures.
The saved [opening reports](../Misc/evidence/automated-tests/README.md) include failures.
An image proposal, a successful compile and a visually resolved interface are separate forms of evidence.

## Make upgrades and resources distinguishable

I wanted neighbouring placements to change the room itself, rather than add points alone.
Connected green patches, garden homes and terrace restaurants therefore needed visible variants as well as functional effects.
The [placement rules](../Assets/Scripts/Core/ConstructionPlacementScore.cs) and
[room visuals](../Assets/Scripts/Presentation/RoomInteriorVisualBuilder.cs) document that relationship.

Later discussion exposed another ambiguity: plant type, habitat level and maturity appeared to describe the same thing.
They were separated in the explanations. A planting choice can follow the plot; maturity describes growth over time;
an upgrade describes a different condition again. Available food is also distinct from an animal actually eating it.
The supermarket explanation now connects its temporary food supply to resident meals and animal use.

Free building also stopped selecting the next card automatically. The fixed opening remains guided,
but later choices wait for a deliberate card click. This leaves the goal area available and keeps the next construction decision with the player.
The relevant logic is in [hand selection](../Assets/Scripts/Presentation/ConstructionPrototypeController.FillHand.cs).
Endless uses the same room systems within its expanding-board progression; its rules remain distinct from Fill.

## Explain a score through its causes

I specified separate settlement phases and colour cues: green for animals, bronze/silver/gold for residents and blue for shared activity.
The aim was to make the source of a change visible before it became part of the total.
I also questioned score ranges when unfavourable layouts could finish too close to considered ones.
The subsequent balance work revised placement effects and trophy bands, with different rule versions retained for older saves.

The [calibration record](../Misc/process/iterations/SKILL_GUIDED_UI_2026-11-21.md) compares automated placement policies.
Those runs examine incentives; they are not player results or proof that an optimal layout has been found.

Score animations alone were too brief for inspection. The score-source view keeps the last settlement available,
highlights contributing rooms and visible actors, and provides entry navigation.
The finished-layout review lets players close the result panel and look at the board itself.
Short neighbourhood stories connect changes to their recorded triggers.
See the [score controls](../Misc/technical/CONTROLS.md), [result review](../Assets/Scripts/Presentation/ConstructionPrototypeController.ResultReview.cs)
and [story model](../Assets/Scripts/Core/ConstructionStoryModel.cs).

## Reliability is part of the interaction

Typing a name, restarting and skipping the tutorial are basic parts of the experience.
Focus and overlapping pointer targets could block those actions. Resetting a run could also lose the goal display.
These issues were recorded separately from visual preferences and checked through named software reports.

The retained [goal failure](../Misc/evidence/automated-tests/goal-restart-before-fix.xml),
[goal repair](../Misc/evidence/automated-tests/goal-restart.xml) and
[result checks](../Misc/evidence/automated-tests/result-interactions.xml) show the scope of that verification.
They do not establish that every interface issue was resolved or replace the [playtesting evaluation](EVALUATION.md).

## Keep prototypes, requests and delivery distinct

Physical layout input explored the familiarity of a tabletop object. Full physical control would have added further recognition
and interaction demands, so camera work was scoped separately from desktop play.
It remains a [technical prototype](../Tools/CameraRecognition/README.md), rather than evidence of a completed physical playtest.

The retained source includes score-source navigation. Clickable manual terms, Enter-to-confirm and per-category deck visibility
are separate requirements outside that source snapshot. A discussion of a feature does not place it in the published build.
The [delivery record](PROJECT_STATUS.md) distinguishes the source from the Windows executable and macOS source-only package.

The [decision register](../Misc/process/records/design-decision-register.csv) lists 26 decisions with supporting files.
The [evidence index](evidence/README.md) links the software reports and evaluation account.
