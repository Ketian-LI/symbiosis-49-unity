# Project report

SYMBIOSIS: 49 is a Unity card-placement game about building a neighbourhood for people and urban wildlife.
The project began with my experience of feeding animals at a church. I wanted to explore what changes when animals
receive food from people. A pigeon-feeding website became a Unity park, then an open city and finally a bounded room grid.

## Research question

How can placement and feedback help players learn a game's spatial rules and use them in another decision?
I distinguish completing an action, noticing its result, explaining the cause and applying that explanation elsewhere.
These distinctions guide the questions used in playtesting.

The [development journal](DEVELOPMENT_JOURNAL.md#timeline) traces the prototypes.
The [design decisions](DESIGN_DECISIONS.md) explain how their problems shaped the current game.

## Teaching through construction

The full-board version introduced too many rooms at once. Following supervisor feedback, I organised the opening around
residents' needs: the central oak comes first, then people arrive and need homes, work and meals.
Eight fixed opening cards introduce those relationships before free card choice.

In Fill, players select a card, preview a legal plot and confirm construction on a 49-plot board.
Endless uses an expanding board and separate progression rules.
Goals offer directions beyond filling every space; the draw pile shows the actual remaining card supply.

## Consequences and explanations

Neighbouring placements can change both appearance and capacity: homes gain gardens and restaurants gain terraces.
An upgrade, food becoming available and an animal actually eating remain different events.
Resident, animal and shared-score batches identify their own contributors. The score-source view retains causes after animation.

Resident work and meal destinations are now allocated automatically within route and capacity constraints.
Selecting a home shows the planned destinations and missing links. Players still decide whether the layout offers enough
connected services. Ten linked terms provide short hover meanings and jump to the relevant manual entry.
P opens building examples; I opens an independent journal of recorded placements, upgrades, commutes and deductions.

Waste coverage and housing disturbance use different spatial conditions. Current collection can follow connected streets,
while a home sharing an edge with a waste room still incurs a penalty. Existing bad edges recur at later Fill settlements.
This changes strategy as well as presentation. The [current rules](../Misc/technical/GAME_RULES.md) distinguish those systems.

## Implementation

Separate models handle run state, food, resident activity, wildlife, placement effects, recurring deductions and saving.
Presentation code turns the recorded results into rooms, highlights, stories and explanations.
The [architecture](../Misc/technical/ARCHITECTURE.md) and [final source page](../Project/04-Final/README.md) identify the code and snapshot.

The desktop game uses mouse and keyboard input. The physical-input prototype used a marked board and 80 printed
tokens. One recognised token could produce its corresponding Unity plot.
Local rankings work offline. An optional network service has separate identity and score-validation limits described in
the [ethics reflection](ETHICS_REFLECTION.md#representation-and-the-optional-leaderboard).

## Evaluation and reflection

The dissertation records two questionnaire rounds and a later observation. Some players changed their actions while
still misidentifying the spatial cause. In the later case, inspection preceded a correct prediction in a changed layout.
These findings support the design focus on explaining the relationship behind a score, as well as showing the score itself.
[Evaluation](EVALUATION.md) presents the counts, method and interpretation.

The resulting artefact lets players inspect a placement through room changes, recorded activity, score causes and layout review.

The [ethics reflection](ETHICS_REFLECTION.md) records the review process, data handling and lessons from the project.
