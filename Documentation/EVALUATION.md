# Evaluation

User feedback, software checks and design critique answer different questions.
This page explains what each source can support.

## User testing

The author confirmed two rounds: 5 people in the first and 13 in the second.
The original 5 returned, so there were 13 different people across both rounds.
The first-round account reports three questionnaires alongside written and voice feedback.

The [method record](evidence/user-testing/METHOD.md) describes recruitment, task questions and rule differences.
The older draft called the second round a plan. The local 22 November revision records that it took place.
Exact builds, session completion and the final analysis still need to be matched to the underlying records.

The draft describes players reaching the end without understanding some rules. It also records a waste-collection case:
a player chose a valid plot, but the old version delayed the result until the next day.
These accounts prompted design questions. The approved anonymous material needed to inspect them is not yet in this repository.

The application has not yet been sent to the supervisor.
The [ethics reflection](ETHICS_REFLECTION.md) explains the oversight and outstanding actions.
The public archive does not treat the working draft or consent folder as permission to publish all study material.

## Software checks

Exported Unity checks cover selected scoring, saves, goals, result controls and interface behaviour.
The [report index](../Misc/evidence/automated-tests/README.md) links each saved batch.
Some checks appear in several batches. Adding their totals would overstate the number of unique tests.

The latest goal-interface batch passes 21/21 checks. It covers bilingual text, restart/skip goal visibility,
hand presentation, result controls and score-source navigation.
No Unity tests were rerun for this documentation update.

Two older opening checks fail before and after the left-page revision.
One checks the case after a fixed 1.4-second wait. The other compares a closed-case tree scale with its restored scale.
The reports remain visible as failures. A separate navigation check does not make those failures disappear.

## Score calibration

The November review used 72 automated Fill runs: 36 calibration runs and 36 held-out checks.
The policies chose unfavourable edges, random legal plots or connected services.
They help reveal narrow score ranges and poor incentives.
They are not player results or proof of optimal play.

The [review record](../Misc/process/iterations/SKILL_GUIDED_UI_2026-11-21.md) gives the setup, results and limits.

## Conclusions that remain open

The current public evidence does not establish improved ecological understanding, representative player preference
or an advantage for physical input.
Finishing 49 days shows task completion. Understanding a rule and wanting another game need their own evidence.

The final account should identify the version behind each finding.
Feedback on an older turn system cannot establish the effect of later stories or highlights.
