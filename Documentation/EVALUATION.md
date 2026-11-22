# Evaluation

The project has user feedback, automated checks and design critique. They answer different questions.
This page records what each source supports and what still needs checking.

## User testing

The English working thesis reports that five people completed a 49-day Fill session on their own computers.
Three returned questionnaires. The author also collected written and voice feedback.

The thesis describes players reaching the end without understanding some rules. It also records a waste-collection case:
a player chose a valid location, but the old version delayed the visible result until the next day. That delay led to a rule
change. These are findings reported in the draft, not a verified dataset published with this repository.

The older thesis draft described the second round as planned. A local 22 November revision records further feedback.
The exact builds, approved anonymous records and final analysis still need checking. No participant names, voices or signed
forms are included here.

The [ethics reflection](ETHICS_REFLECTION.md) records that the application has not yet been sent to the supervisor. It also
identifies unfinished safeguards. The existence of feedback or consent documents does not settle its permitted research use.

The [method and reconciliation plan](evidence/user-testing/METHOD.md) sets out the proposed questions and analysis.
The [study folder](evidence/user-testing/README.md) lists the evidence still needed. Ethics approval and permission to use
earlier material must be confirmed with the supervisor. A drafted form is not approval.

## Technical checks

Exported Unity checks cover selected scoring, save, goal, result and interface behaviour.
The [automated-check index](evidence/automated-tests/README.md) gives the reports and their results.
Some batches repeat the same checks. Their totals must not be added together as a count of unique tests.

The latest goal-interface batch passes 21/21 checks. It includes bilingual text rendering, restart/skip goal visibility,
hand presentation, result controls and score-source navigation. This is a saved report, not a fresh run of every project test.

Two older opening checks fail both before and after the left-page revision. One checks the case after a fixed 1.4-second wait.
The other compares the tree's closed-case scale with its restored scale. Their reports remain in the index as failures.

## Score calibration

The November review used 72 automated Fill runs: 36 calibration runs and 36 held-out checks.
The policies chose unfavourable edges, random legal plots or connected services. They help expose narrow score ranges
and poor incentives. They are not player results or proof of an optimal strategy.
The [review record](process/iterations/SKILL_GUIDED_UI_2026-11-21.md) gives the setup and limits.

## What is not established

The current records do not show improved ecological understanding, a representative player preference or an advantage
for physical input. Completing 49 days shows task completion; it does not show that the player understood the rules or
wanted to play again. The final analysis should use specific examples, include unclear cases and identify the version tested.
