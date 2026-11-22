# Evaluation

The evaluation draws on playtesting, software checks and score calibration. Each addresses a different question:
what players understood, whether the implementation behaved as expected, and how the scoring rules responded to layouts.

## Playtesting

There were two rounds of remote playtesting: 5 people in the first and 13 in the second.
The original 5 returned, giving 13 different participants. The first-round account includes 3 questionnaires
alongside written and voice feedback. The [method record](evidence/user-testing/METHOD.md) describes recruitment,
questions and rule differences.

The playtesting account identifies unclear rules, delayed feedback and repetition. One waste-collection example
concerned a legal placement whose visible result appeared on the following day. This raised a design problem:
a valid action was difficult to connect to its consequence.

These observations informed more immediate feedback, visible score causes and a review of the finished layout.
They do not establish the effect of each later feature. Rules and recording methods changed between rounds,
and exact executable identifiers are not available for every session.

The study began before formal ethics review. Raw participant records and an approved set of anonymous extracts
are not published here. The [ethics reflection](ETHICS_REFLECTION.md) explains the data-use limits.

## Software checks

Saved Unity reports cover selected scoring rules, saves, goals, result controls and interface behaviour.
The [report index](../Misc/evidence/automated-tests/README.md) identifies each batch.
Some tests recur across batches, so the batch totals are not a count of unique tests.

The latest goal-interface batch records 21/21 passing checks. It covers bilingual text, restart and tutorial-skip
goal visibility, hand presentation, result controls and score-source navigation.

Two older opening checks fail both before and after the left-page revision. One uses a fixed 1.4-second wait;
the other compares the closed-case tree scale with its restored scale. Those failures remain in the reports.
The saved results describe their named snapshots. The documentation revisions did not rerun Unity tests.

## Score calibration

The November review used 72 automated Fill runs: 36 calibration runs and 36 held-out checks.
The policies chose unfavourable edges, random legal plots or connected services. This compared score ranges
and incentives under different placement strategies.

These are diagnostic runs rather than player results or proof of optimal play.
The [review record](../Misc/process/iterations/SKILL_GUIDED_UI_2026-11-21.md) gives the setup, results and limitations.

## Findings and limits

The project identifies a recurring interaction problem: a player may finish the game without understanding why
a placement helped or harmed the neighbourhood. The design response was to keep consequences available for inspection,
rather than show only a total score.

The current public evidence does not establish improved ecological understanding, representative player preference
or an advantage for physical input. Task completion, rule understanding and willingness to replay are separate outcomes.
Feedback from an earlier build cannot demonstrate the effect of later stories, highlights or card layouts.
