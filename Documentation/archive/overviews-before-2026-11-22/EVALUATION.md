> Archived overview, replaced on 22 November 2026. It describes its recorded snapshot.

# Evaluation: what is known and what is not

## Four kinds of evidence

| Evidence type | Available record | Claim boundary |
| --- | --- | --- |
| Technical verification | Exported Unity XML, test source, startup result and a Unity-rendered capture | Selected logic, UI and build checks; not a complete test of every mode or a participant study |
| Design feedback | Issues raised in the development conversation, followed by source changes and focused checks | Motivates revisions; not automatically a recruited study or controlled comparison |
| Author-reported remote study | English working thesis named `SYMBIOSIS_49_Hybrid_Thesis_English_Working_Draft_2026-11-11_PlaytestVerified.docx` | Reports 5 completed 49-day Fill runs and 3 questionnaires; raw records/build identifier not verified here |
| Proposed evaluation | [Protocol](../../evidence/user-testing/METHOD.md) | A plan to collect or reconcile evidence, not invented findings |

The working draft also reports text/voice follow-up and permission for anonymous quotation. That declaration is not a substitute
for checking consent, retention, ethics approval and publication rights. Private chats, recordings and identity-bearing records
are deliberately absent. The author asked to leave final PDF and approved anonymous research materials pending.

## Selected reproducible technical batches

See [machine-readable index](../../evidence/automated-tests/report-index.json). These batches overlap: **do not add them into a count of unique tests**.

| Batch | Passed / total | Scope |
| --- | --- | --- |
| [Scoring and storage](../../evidence/automated-tests/scoring-and-storage.xml) | 58/58 | Versioned scores, penalties, local records, placement and finite-board save replay |
| [Result interactions](../../evidence/automated-tests/result-interactions.xml) | 5/5 | Keyboard naming, deduction explanation, result collapse and restart |
| [Goal restart](../../evidence/automated-tests/goal-restart.xml) | 8/8 | Restart/skip/load goal visibility plus related result checks |
| [Story model](../../evidence/automated-tests/story-model.xml) | 5/5 | Actual-meal gating, maturity, restore consistency and both negative-pair placement orders |
| [Story interface](../../evidence/automated-tests/story-interface.xml) | 9/9 | Bilingual story layout plus goal/result regressions |
| [Final render/localisation](../../evidence/automated-tests/story-final-render.xml) | 1/1 | Final left-leaf render and English/Chinese headings |
| [Optional leaderboard service](../../evidence/automated-tests/leaderboard-local.xml) | 6/6 | Fresh local Node.js 24 checks of best-score retention, rename, version separation, validation, reset authorisation and CORS; in-memory database only |

The [before-fix goal report](../../evidence/automated-tests/goal-restart-before-fix.xml) deliberately preserves a failure. Test reports have machine
path prefixes redacted, while result counts and failure descriptions are retained. Source paths and fixtures allow inspection.
The Windows startup check reached the expected initial layout without an exception in its checked log. The test then stopped
the process; it is not evidence of a natural application exit or complete visual play session. Save-file hashes did not change.

## Limits on findings

No learning improvement, ecological understanding, representative preference or superiority of physical input is claimed here.
No aggregate percentage is inferred from the three reported questionnaires. Study results from an unidentified earlier build
cannot validate this current scoring, tutorial, story panel or camera mode. Final thesis conclusions must remain proportionate
to a traceable dataset and a stated method.

## 21 November source revision

The [skill-guided review](../../process/iterations/SKILL_GUIDED_UI_2026-11-21.md) records 90/90 selected EditMode checks and 19/19
PlayMode checks, including real SDF truncation and persistent penalty text. It also reports 72 synthetic Fill runs,
split into 36 calibration samples and 36 held-out checks using unchanged diagnostic policies. These are technical
evidence, not player findings. They overlap prior regression exports and are not a new cumulative unique-test count.

## Score-index source revision

| Batch | Passed / total | Scope |
| --- | --- | --- |
| [Contributor model](../../evidence/automated-tests/score-index-editmode.xml) | 16/16 | Actual meals/routines, pairing deductions, restore consistency, absent contribution and nearby score/story regressions |
| [Index and interface](../../evidence/automated-tests/score-index-playmode.xml) | 21/21 | Index navigation, input and modal isolation, visible actor brackets, Fill score phases, Endless relocation and existing hand/goal/result/localisation checks |

The [test source](../../../Assets/Tests/PlayMode/ConstructionScoreIndexPresentationTests.cs) checks visible actor resolution,
room targets, unchanged saved state and topmost pointer targets. The [capture](../../media/screenshots/score-index-zh.png) is a Unity-rendered
fixture. The new batches overlap earlier reports and do not add a new unique-test total. The 72-run score calibration remains
the separate [skill-guided diagnostic record](../../process/iterations/SKILL_GUIDED_UI_2026-11-21.md); this read-only interface revision changes no scoring formula.

## Left-leaf visual revision

The [visual record](../../process/iterations/LEFT_LEAF_STYLE_2026-11-21.md) covers 21/21 interface checks and a separate 1/1 live navigation check.
The checks include real SDF text, preserved goal/score controls and shared printed paper in the opening scene and live UI.
The exported [interface](../../evidence/automated-tests/left-leaf-final.xml) and [opening](../../evidence/automated-tests/left-leaf-opening.xml) reports retain their
own scope; they overlap earlier exports. Two older opening tests failed both before and after this visual change; the
[baseline](../../evidence/automated-tests/left-leaf-baseline-opening.xml) and [initial revision](../../evidence/automated-tests/left-leaf-legacy-opening-after.xml) reports
retain those failures. The captures are synthetic fixtures and do not establish a participant preference.

## Goal progress component revision

The [component record](../../process/iterations/GOAL_PROGRESS_UI_2026-11-21.md) describes the goal UI redesign and actual bilingual captures.
The selected [PlayMode batch](../../evidence/automated-tests/goal-progress-ui.xml) passes 21/21 checks covering rendered text, goals/restart/skip,
hand presentation, result interactions and score-source controls. This batch overlaps earlier exports and does not increase
a cumulative unique-test total. The displayed layouts are synthetic fixtures, not participant preference evidence. No model
or scoring rules changed, and the previously recorded opening-test limitations remain outside this component revision.
