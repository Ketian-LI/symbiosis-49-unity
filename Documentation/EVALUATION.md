# Evaluation

This account follows the latest supplied dissertation, dated 22 November 2026.
It separates participant feedback, a later observation, software checks and scoring diagnostics.

## Playtesting

### Participants and method

Acquaintances were invited privately and played remotely on their own computers. The no-camera Fill game was the main condition.

| Stage | Recorded participation | Method |
| --- | --- | --- |
| First round | 5 participants; 3 returned questionnaires | A 49-day task, written and voice feedback |
| Second round | 13 participant-completed forms, including the original 5 | Four response points during play and two waste-layout exercises |
| Follow-up on 22 November | 1 additional player, FU-01 | Observation and interview, reconstructed from the researcher's recollection |

There were 13 different participants across the first two rounds and 14 including FU-01.
The second-round form separated the player's choice, expected result, noticed result and explanation.
It asked about the fixed opening, first free choice, a later change of plan and the end of the game.
These checkpoints describe the recording design, not proof that every answer was collected at that moment.
Exact executable identifiers are not available for every session.

### Second-round record

| Item | Latest dissertation account |
| --- | ---: |
| Returned participant records | 13 |
| Records with final evaluations | 12 |
| Records with layout answers | 11 |
| All four layout judgements correct | 3/13 overall; 3/11 among layout respondents |
| Self-reported restart: yes / no / unknown | 8 / 3 / 2 |

The presentation contains an earlier nine-form snapshot. The table above uses the updated dissertation record.
Blank or indeterminate layout answers are coded unknown, not correct. Missing answers alone do not establish poor understanding.
An unanswered restart item is unknown, not a refusal; several games reported by one person still count as one record.

The layout analysis uses the rules played in that round: local eight-neighbour waste collection and a penalty for homes sharing
an edge with a waste room. Two records described the coverage rule yet selected a home outside its range in another diagram.
The later extension along connected streets is not applied retrospectively to those answers.

Feedback identified unclear rules, delayed consequences and repetition. A legal waste placement could appear to have no
effect until the next day. These reports informed more immediate feedback, visible causes and layout review.
They do not demonstrate the effect of features added afterwards.

### Additional observation

FU-01 had not joined either earlier round. The retrospective account describes an initial incorrect prediction about a street
beside green space, inspection after noticing a deduction, and a correct prediction after the layout changed.
The researcher reports providing no extra rule explanation or answer correction. This is a paraphrased observation account,
not a coded transcript or a verbatim participant quotation.

This case is analysed separately from the questionnaire totals. It cannot isolate the effects of highlighting, prompts,
repeated play or recurring penalties. It does not evaluate the later street-based waste-collection extension.

## Software checks

The final source is pinned to `ac9685dd0d652f70666369f1c1753457710e3e05`.
The [report index](../Misc/evidence/automated-tests/README.md) retains the original results and named snapshots.

| Saved batch | Result | Scope |
| --- | --- | --- |
| Current EditMode | [77/77](../Misc/evidence/automated-tests/commuting-reference-penalties-editmode.xml) | Route allocation, shortest legal fixed-order itinerary, legacy migration, recurring deductions and persistence |
| Current PlayMode | [32/32](../Misc/evidence/automated-tests/commuting-reference-penalties-playmode.xml) | Bilingual term navigation, manual destinations, P/I views, modal input, road markers and existing controls |
| Final presentation checks | [4/4](../Misc/evidence/automated-tests/commuting-reference-penalties-capture.xml) | Targeted UI and capture checks after presentation adjustments |
| Earlier interface | 38/38 EditMode; 27/27 PlayMode | Previous deck, relationship, goal and navigation checks |
| Older opening checks | Two failures before and after the left-page revision | Fixed 1.4-second wait and closed-case tree-scale comparison |

Tests recur across batches. Totals are neither unique-test counts nor participant findings.
The earlier failures remain in the archive; later passing batches do not replace them.
This repository update imports the saved reports and checks source hashes; it does not rerun Unity tests or rebuild executables.

## Score calibration

The earlier balance review used 72 automated Fill runs: 36 calibration runs and 36 held-out checks.
Policies chose unfavourable edges, random legal plots or connected services.
The [review record](../Misc/process/iterations/SKILL_GUIDED_UI_2026-11-21.md) gives the setup and results.
Those runs are diagnostics, not player outcomes or proof of optimal play.
Recurring deductions change future Fill totals, so the older ranges are not current balance benchmarks.
Trophy thresholds have not been recalibrated for this update.

## Evidence and limits

| Record | What it supports |
| --- | --- |
| Dissertation account above | Reported rule difficulties and one later observation, within the stated method limits |
| [Software reports](../Misc/evidence/automated-tests/README.md) | Selected behaviour in named source snapshots |
| [Screenshot records](media/screenshots/README.md) | The appearance of labelled fixed software layouts |
| [Journal](DEVELOPMENT_JOURNAL.md) and [design decisions](DESIGN_DECISIONS.md) | The sequence and reasons for revisions |

The study began before formal ethics review. Consent forms and the project plan have now been sent to the supervisor;
review is pending. The [ethics reflection](ETHICS_REFLECTION.md) records the outstanding issues.
Private forms, messages, audio and identities remain outside this repository. The public account contains descriptive
summaries, not a participant-level dataset.

Recruitment through personal contacts, prior experience among returning participants, different versions and retrospective
recording limit interpretation. Noticing an outcome, explaining it and applying the rule elsewhere are different achievements.
The evidence does not establish improved ecological understanding, representative preference or an advantage for physical input.
A full accessibility study and a participant study of physical input have not been completed.
