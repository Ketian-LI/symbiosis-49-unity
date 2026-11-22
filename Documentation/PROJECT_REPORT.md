# Project report

SYMBIOSIS: 49 is a Unity card-placement game. Players build a neighbourhood one room at a time.
Homes need access to work and meals. Animals need food they can reach. Streets, green spaces and waste facilities
can help one part of the neighbourhood while causing problems elsewhere.

This report connects the design problem, implementation and evaluation. The [development journal](DEVELOPMENT_JOURNAL.md)
traces the earlier prototypes, and the [delivery record](PROJECT_STATUS.md) identifies the published material.

## The design problem

The early browser prototype let players feed pigeons. Later versions gave players more choices about space, buildings
and routes. Those choices also made the rules harder to learn. The project asks how placement and feedback can help
players understand the game, and what makes a different layout worth trying.

Three questions guide the current work: does the player know what to do next, can they explain an outcome, and do they
have a reason to keep experimenting? Finishing a session alone does not answer all three questions.

## Playing a turn

In Fill, the player selects a card, previews a legal plot and confirms construction. The opening introduces room types
in stages. Later, the player chooses from a hand. The board has 49 plots. Endless uses an expanding board and its own run rules.

Selecting a card is separate from placing it. A route preview shows a possibility. Points for meals and resident routines
depend on recorded activity. The [rules](../Misc/technical/GAME_RULES.md) and [controls](../Misc/technical/CONTROLS.md) explain these differences.

## Making consequences visible

Neighbouring rooms can change a building's appearance and function. A home beside a connected green patch can gain a garden.
A restaurant beside a plaza can gain capacity. Plant growth and the food actually available remain separate conditions.

The neighbourhood journal describes a change in everyday language and states its trigger. Score reasons explain rewards
and deductions. The score-source view highlights the rooms and visible residents or animals involved in the last settlement.
Players can also close the result panel to review a finished layout.

These features aim to make outcomes easier to read. The current evidence does not establish that they improve learning.

## How it is built

The run model stores the board, turn state and rule versions. Separate systems handle food, resident routines, wildlife,
placement scores and saving. Presentation code turns those results into rooms, animation, stories and interface text.
The [architecture guide](../Misc/technical/ARCHITECTURE.md) links the main classes.

The game supports local rankings. An optional HTTP service receives submitted scores and names. Offline play does not
depend on that service. Its identity method and validation limits are described in [ethics and limitations](ETHICS_AND_LIMITATIONS.md).

## What the evidence shows

Remote playtesting involved two rounds with 13 different participants. The [method record](evidence/user-testing/METHOD.md)
describes the groups and rule differences. Feedback identified unclear rules, delayed feedback and repetition.
The study began before formal ethics review, and exact executable identifiers are not documented for every session.
The [ethics reflection](ETHICS_REFLECTION.md) states the limits on further use of the earlier records.

Automated checks cover selected rules, saving, restart, goal visibility and interface interactions. Unity renders show fixed
layouts in both languages. These checks help find software faults; they do not replace user testing.
The [evaluation](EVALUATION.md) keeps the two types of evidence separate.

The resulting artefact makes placement consequences available through rooms, stories, score reasons and layout review.
It remains a simplified account of urban life, with authored scores and species behaviour rather than ecological predictions.
