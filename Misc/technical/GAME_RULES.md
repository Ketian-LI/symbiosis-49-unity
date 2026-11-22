# Current game rules

This page describes the final construction source snapshot from 22 November 2026, held in [Project/04-Final](../../Project/04-Final/README.md).
Earlier full-board rearrangement and Research modes are separate stages. Their rules are in the [archive](../archive/README.md).

## Place and observe

Fill has 49 plots. The opening introduces eight room categories through fixed cards.
After that, the player selects from a hand, previews a legal plot and confirms the placement.
The run then records resident and animal activity. Endless uses an expanding board and separate progression rules.

A green patch, reachable food and an animal actually eating are different conditions.
A planned resident route is also different from a completed home-work-meal-home routine.
The stories and score-source view describe recorded outcomes.

## Card supply in Fill

New hand-based Fill games use a shared supply of 100 cards:

| Category | Initial supply |
| --- | ---: |
| Green | 30 |
| Residence | 18 |
| Street | 14 |
| Plaza | 10 |
| Workshop | 9 |
| Restaurant | 7 |
| Supermarket | 6 |
| Waste facility | 6 |

The undealt count excludes built rooms and cards currently held.
Green variants share the Green supply. They are not separate fixed decks.
The hand offers distinct categories; remaining supply affects draw weights.
These counts do not describe the separate physical BoardGame inventory or an unlimited Endless supply.

The [run model](../../Project/04-Final/Assets/Scripts/Core/ConstructionRunModel.cs) owns the supply and compatibility rules.
Click the draw pile to inspect real remaining counts by category, together with pile, held and built totals. Tutorial queue cards are accounted for separately from ordinary held stock.

## Upgrades and scores

Neighbour conditions can upgrade green patches, homes, restaurants and streets.
A visual upgrade does not by itself prove that food was produced or a resident completed a routine.

The total combines shared activity, construction effects and goal rewards.
New Fill/Endless runs use gameplay scoring V13. New Fill trophies use rule set 2:

| Trophy | Final total |
| --- | ---: |
| Plastic | Below 1500 |
| Iron | 1500-2499 |
| Bronze | 2500-3499 |
| Silver | 3500-4499 |
| Gold | 4500 or more |

Old saves keep their recorded rules. The [award model](../../Project/04-Final/Assets/Scripts/Core/ConstructionAwardModel.cs),
[placement rules](../../Project/04-Final/Assets/Scripts/Core/ConstructionPlacementScore.cs) and
[score ledger](../../Project/04-Final/Assets/Scripts/Core/ConstructionScoreLedger.cs) hold the detailed calculations.
The [balance review](../process/iterations/SKILL_GUIDED_UI_2026-11-21.md) explains the diagnostic runs and their limits.

## Automatic commuting

Work and meal destinations are selected automatically within legal routes and job/seat capacities.
The allocator prioritises complete routines and rotates scarce opportunities. Each leg of the fixed work, meal,
optional square and home itinerary uses a shortest legal shared-edge route. This does not prove a global maximum score
or an optimal tour across every possible stop order. Selecting a home inspects its plan rather than opening a dispatch editor.

## Waste coverage and recurring deductions

A waste room serves its eight neighbouring plots and sources reached through a continuous shared-edge street chain.
Collection coverage does not remove the separate disturbance from a home sharing an edge with that waste room.
Diagonal contact can receive collection without the shared-edge housing penalty.

In current Fill rules, an introducing placement pays its existing bad-pair charge once. Starting with the next confirmed
placement settlement, each existing waste/home edge deducts 80 and each street/green edge deducts 40, including slow streets.
Two homes beside one waste room are two edges. Previewing, reading panels or loading a save cannot add another charge.

The recurring ledger stores the day, both rooms and the amount. Old saves receive no retrospective charges;
future eligible settlements begin recording them. Endless retains its existing building-time pairing rule.
Trophy bands above are unchanged, and the older calibration ranges are not revalidated for recurring deductions.
See [evaluation](../../Documentation/EVALUATION.md#score-calibration).
