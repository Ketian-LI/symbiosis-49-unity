# Evaluation

The evaluation draws on playtesting, software checks and score calibration. They address different questions:
what players understood, whether the implementation behaved as expected, and how scoring responded to layouts.

## Playtesting

### Participants and setting

Acquaintances were invited privately and played remotely on their own computers.
The no-camera Fill game was the main condition.

| Round | Participants | Recorded account |
| --- | ---: | --- |
| First | 5 | A 49-day task, written and voice feedback, and 3 returned questionnaires |
| Second | 13 | Further feedback using revised waste and turn rules |
| Across both | 13 different people | The original 5 returned; 8 more joined the second round |

Counts do not establish completion of every checkpoint or a full 49-day run.
The public record does not identify the exact executable used in every session.
Second-round rules included waste collection across eight neighbouring plots, a penalty for a home sharing an edge
with a waste facility, and turn scoring triggered by card placement.

### Questions and recording

The first round asked participants to complete the game and suggest improvements.
The second-round form asked about four moments: finishing the fixed-card opening, making the first free choice,
changing a plan later and reaching the end. Participants were also asked to judge two layouts.
The form separated their choice, expected result, noticed result and explanation.
These checkpoints describe the recording design; they do not establish that every response was collected at that moment.

### Feedback and interpretation

The account identifies unclear rules, delayed feedback and repetition.
One waste-collection example concerned a legal placement whose visible result appeared on the following day.
A valid action was difficult to connect to its consequence.

These observations informed more immediate feedback, visible score causes and review of the finished layout.
They do not establish the effect of each later feature. Recruitment through personal contacts limits the range of views.
Different rules and recording methods between rounds prevent isolation of a single change.
Feedback from an older build cannot validate later stories, highlights or card layouts.

The study began before formal ethics review. Private forms, messages, audio and participant identities are excluded.
An approved set of anonymous extracts is not included, so the public account cannot support participant-level comparisons
of assistance, missing answers or completion. The [ethics reflection](ETHICS_REFLECTION.md) records the further-use limits.

## Software checks

The [saved-report index](../Misc/evidence/automated-tests/README.md) identifies the named snapshots and results.
Tests recur across batches; their totals are not a count of unique tests.

| Batch | Saved result | Scope |
| --- | --- | --- |
| Final 22 November EditMode | [38/38](../Misc/evidence/automated-tests/approved-fill-ui-editmode.xml) | Selected stock, saved-state, coverage, warning and scoring checks |
| Final 22 November PlayMode | [27/27](../Misc/evidence/automated-tests/approved-fill-ui-playmode.xml) | Selected bilingual interface, modal input, restart/skip goals, settlement and navigation checks |
| Earlier goal-interface batch | 21/21 | Goal visibility, hand presentation, result controls and score-source navigation |
| Older opening checks | Two failures before and after the left-page revision | A fixed 1.4-second wait and a closed-case tree-scale comparison |

The failures remain in the reports. Later passing batches do not replace them or prove every opening issue resolved.
These are software checks, not participant findings. The documentation reorganisation did not rerun Unity tests.

## Score calibration

The November review used 72 automated Fill runs: 36 calibration runs and 36 held-out checks.
Policies chose unfavourable edges, random legal plots or connected services to compare score ranges and incentives.
These diagnostic runs are not player results or proof of optimal play.
The [review record](../Misc/process/iterations/SKILL_GUIDED_UI_2026-11-21.md) gives the setup, results and limits.

## Evidence and limits

| Record | What it supports |
| --- | --- |
| Playtesting account above | Reported rule and interaction problems, within the stated study limits |
| [Software reports](../Misc/evidence/automated-tests/README.md) | Selected model and interface behaviour in named snapshots |
| [Screenshot records](media/screenshots/README.md) | The appearance of labelled software layouts |
| [Journal](DEVELOPMENT_JOURNAL.md) and [design decisions](DESIGN_DECISIONS.md) | Development sequence and the basis for revisions |

A fixed test layout is not a participant session. A design mockup is not a game screenshot.
The recurring interaction problem is that players can finish without understanding why a placement helped or harmed
the neighbourhood. The design response keeps consequences available for inspection after the score animation.

The evidence does not establish improved ecological understanding, representative player preference or an advantage
for physical input. Task completion, rule understanding and willingness to replay are separate outcomes.
A full accessibility study has not been completed, and physical camera input has limited evidence.
