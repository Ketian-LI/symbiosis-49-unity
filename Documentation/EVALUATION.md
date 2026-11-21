# Evaluation: what is known and what is not

## Four kinds of evidence

| Evidence type | Available record | Claim boundary |
| --- | --- | --- |
| Technical verification | Exported Unity XML, test source, startup result and a Unity-rendered capture | Selected logic, UI and build checks; not a complete test of every mode or a participant study |
| Design feedback | Issues raised in the development conversation, followed by source changes and focused checks | Motivates revisions; not automatically a recruited study or controlled comparison |
| Author-reported remote study | English working thesis named `SYMBIOSIS_49_Hybrid_Thesis_English_Working_Draft_2026-11-11_PlaytestVerified.docx` | Reports 5 completed 49-day Fill runs and 3 questionnaires; raw records/build identifier not verified here |
| Proposed evaluation | [Protocol](USER_TESTING_PROTOCOL.md) | A plan to collect or reconcile evidence, not invented findings |

The working draft also reports text/voice follow-up and permission for anonymous quotation. That declaration is not a substitute
for checking consent, retention, ethics approval and publication rights. Private chats, recordings and identity-bearing records
are deliberately absent. The author asked to leave final PDF and approved anonymous research materials pending.

## Selected reproducible technical batches

See [machine-readable index](evidence/report-index.json). These batches overlap: **do not add them into a count of unique tests**.

| Batch | Passed / total | Scope |
| --- | --- | --- |
| [Scoring and storage](evidence/scoring-and-storage.xml) | 58/58 | Versioned scores, penalties, local records, placement and finite-board save replay |
| [Result interactions](evidence/result-interactions.xml) | 5/5 | Keyboard naming, deduction explanation, result collapse and restart |
| [Goal restart](evidence/goal-restart.xml) | 8/8 | Restart/skip/load goal visibility plus related result checks |
| [Story model](evidence/story-model.xml) | 5/5 | Actual-meal gating, maturity, restore consistency and both negative-pair placement orders |
| [Story interface](evidence/story-interface.xml) | 9/9 | Bilingual story layout plus goal/result regressions |
| [Final render/localisation](evidence/story-final-render.xml) | 1/1 | Final left-leaf render and English/Chinese headings |
| [Optional leaderboard service](evidence/leaderboard-local.xml) | 6/6 | Fresh local Node.js 24 checks of best-score retention, rename, version separation, validation, reset authorisation and CORS; in-memory database only |

The [before-fix goal report](evidence/goal-restart-before-fix.xml) deliberately preserves a failure. Test reports have machine
path prefixes redacted, while result counts and failure descriptions are retained. Source paths and fixtures allow inspection.
The Windows startup check reached the expected initial layout without an exception in its checked log. The test then stopped
the process; it is not evidence of a natural application exit or complete visual play session. Save-file hashes did not change.

## Limits on findings

No learning improvement, ecological understanding, representative preference or superiority of physical input is claimed here.
No aggregate percentage is inferred from the three reported questionnaires. Study results from an unidentified earlier build
cannot validate this current scoring, tutorial, story panel or camera mode. Final thesis conclusions must remain proportionate
to a traceable dataset and a stated method.

## 21 November source revision

The [skill-guided review](../Docs/SKILL_GUIDED_UI_2026-11-21.md) records 90/90 selected EditMode checks and 19/19
PlayMode checks, including real SDF truncation and persistent penalty text. It also reports 72 synthetic Fill runs,
split into 36 calibration samples and 36 held-out checks using unchanged diagnostic policies. These are technical
evidence, not player findings. They overlap prior regression exports and are not a new cumulative unique-test count.
