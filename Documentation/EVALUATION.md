# Evaluation

The evaluation examines how players noticed outcomes, explained spatial rules and used those explanations in later choices.
It draws on the dissertation dated 22 November 2026, participant questionnaires, a later observation and software reports.

## Playtesting

### Participants and method

Acquaintances were invited privately and played Fill remotely on their own computers using mouse and keyboard.

| Stage | Recorded participation | Method |
| --- | --- | --- |
| First round | 5 participants; 3 returned questionnaires | A 49-day task, written and voice feedback |
| Second round | 13 participant-completed forms, including the original 5 | Four response points during play and two waste-layout exercises |
| Follow-up on 22 November | 1 additional player, FU-01 | Observation and interview recorded retrospectively by the researcher |

There were 13 different participants across the first two rounds and 14 including FU-01.
The second-round form asked for responses at the fixed opening, first free choice, a later change of plan and the end.
It separated the player's choice, expected result, noticed result and explanation.

### Second-round record

| Item | Dissertation record |
| --- | ---: |
| Returned participant records | 13 |
| Records with final evaluations | 12 |
| Records with layout answers | 11 |
| All four layout judgements correct | 3/13 overall; 3/11 among layout respondents |
| Self-reported restart: yes / no / unknown | 8 / 3 / 2 |

The presentation contains an earlier nine-form snapshot; this account uses the updated dissertation record.
Blank or indeterminate answers are coded unknown. Accuracy counts explicit correct judgements, using both the overall
and layout-respondent denominators. Restart figures count each person's response once.

The layout questions use the round's local eight-neighbour collection rule and the penalty for a home sharing an edge
with a waste room. Two records described that coverage rule yet selected a home outside its range in another diagram.
This shows a gap between stating a rule and applying it to another arrangement.

Feedback also identified delayed consequences and repetition. A legal waste placement could appear to have no effect
until the next day. These reports led to more immediate feedback, visible causes and finished-layout review.

### Additional observation

FU-01 was a new participant. The retrospective account describes an incorrect prediction about a street beside green space,
inspection after a deduction, and a correct prediction after the layout changed. The session used the built-in instructions;
the researcher provided no additional rule explanation or answer correction.

The observation follows the player's prediction, noticed outcome, inspection and later judgement. It contributes a case
of interpreting a street-related deduction alongside the questionnaire accounts.

## Software checks

The final source is pinned to `ac9685dd0d652f70666369f1c1753457710e3e05`.
The [report index](../Misc/evidence/automated-tests/README.md) records each batch and source snapshot.

| Saved batch | Result | Scope |
| --- | --- | --- |
| Current EditMode | [77/77](../Misc/evidence/automated-tests/commuting-reference-penalties-editmode.xml) | Route allocation, fixed-order shortest paths, legacy migration, recurring deductions and persistence |
| Current PlayMode | [32/32](../Misc/evidence/automated-tests/commuting-reference-penalties-playmode.xml) | Bilingual term navigation, manual destinations, P/I views, modal input, road markers and controls |
| Final presentation checks | [4/4](../Misc/evidence/automated-tests/commuting-reference-penalties-capture.xml) | Targeted UI and capture checks |
| Earlier interface | 38/38 EditMode; 27/27 PlayMode | Deck, room relationships, goals and navigation |
| Older opening checks | Two failures before and after the left-page revision | Fixed 1.4-second wait and closed-case tree-scale comparison |

Some checks appear in multiple batches; the table reports each batch's result.

## Score calibration

The earlier balance review used 72 automated Fill runs: 36 calibration runs and 36 held-out checks.
The policies chose unfavourable edges, random legal plots or connected services to compare score ranges and incentives.
The [review record](../Misc/process/iterations/SKILL_GUIDED_UI_2026-11-21.md) gives the setup and results.
Those runs preceded recurring deductions. The current trophy thresholds retain those earlier settings.

## Evidence and limits

| Record | Contribution |
| --- | --- |
| Questionnaires and follow-up account | Rule explanations, reported choices, replay responses and one observed change in judgement |
| [Software reports](../Misc/evidence/automated-tests/README.md) | Behaviour checks for the named source snapshots |
| [Screenshot records](media/screenshots/README.md) | Visual development and interface states |
| [Journal](DEVELOPMENT_JOURNAL.md) and [design decisions](DESIGN_DECISIONS.md) | The development sequence and reasons for revisions |

Recruitment through personal contacts and the five returning players shaped the sample. Rule changes between rounds,
incomplete executable identifiers and the retrospective follow-up account limit comparison. FU-01 experienced highlighting,
prompts and repeated play together, so their individual effects cannot be separated in this case.

The study covers the earlier local waste-collection rule. The later street-based extension, accessibility and physical-input
play remain areas for further evaluation. The [ethics reflection](ETHICS_REFLECTION.md) records the research review and data handling.
