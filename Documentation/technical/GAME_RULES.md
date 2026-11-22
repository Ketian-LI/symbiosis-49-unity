# Current game rules

This page describes the construction source retained from 21 November 2026.
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

The [run model](../../Assets/Scripts/Core/ConstructionRunModel.cs) owns the supply and compatibility rules.
A per-category remaining-card display has been requested but is not implemented in this source.

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

Old saves keep their recorded rules. The [award model](../../Assets/Scripts/Core/ConstructionAwardModel.cs),
[placement rules](../../Assets/Scripts/Core/ConstructionPlacementScore.cs) and
[score ledger](../../Assets/Scripts/Core/ConstructionScoreLedger.cs) hold the detailed calculations.
The [balance review](../process/iterations/SKILL_GUIDED_UI_2026-11-21.md) explains the diagnostic runs and their limits.
