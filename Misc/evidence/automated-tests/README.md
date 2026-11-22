# Automated check reports

These are saved reports from the revisions named in each batch. No Unity tests were rerun for the document move.
Batches overlap, so do not add their totals together. Failures remain visible.

| Report | Passed / total | Failed |
| --- | --- | ---: |
| [scoring-and-storage](scoring-and-storage.xml) | 58 / 58 | 0 |
| [result-interactions](result-interactions.xml) | 5 / 5 | 0 |
| [goal-restart](goal-restart.xml) | 8 / 8 | 0 |
| [goal-restart-before-fix](goal-restart-before-fix.xml) | 1 / 2 | 1 |
| [story-model](story-model.xml) | 5 / 5 | 0 |
| [story-interface](story-interface.xml) | 9 / 9 | 0 |
| [story-final-render](story-final-render.xml) | 1 / 1 | 0 |
| [skill-polish-editmode](skill-polish-editmode.xml) | 90 / 90 | 0 |
| [skill-polish-playmode](skill-polish-playmode.xml) | 19 / 19 | 0 |
| [score-index-editmode](score-index-editmode.xml) | 16 / 16 | 0 |
| [score-index-playmode](score-index-playmode.xml) | 21 / 21 | 0 |
| [left-leaf-final](left-leaf-final.xml) | 21 / 21 | 0 |
| [left-leaf-opening](left-leaf-opening.xml) | 1 / 1 | 0 |
| [left-leaf-legacy-opening-after](left-leaf-legacy-opening-after.xml) | 1 / 3 | 2 |
| [left-leaf-baseline-opening](left-leaf-baseline-opening.xml) | 1 / 3 | 2 |
| [goal-progress-ui](goal-progress-ui.xml) | 21 / 21 | 0 |
| [leaderboard-local](leaderboard-local.xml) | 6 / 6 | 0 |
| [trophy-reading-editmode](trophy-reading-editmode.xml) | 86 / 86 | 0 |
| [trophy-reading-playmode](trophy-reading-playmode.xml) | 18 / 18 | 0 |

The optional leaderboard checks use an in-memory local database. They do not test every deployed network condition.
Older room-system reports are kept in [legacy](legacy). Their rules differ from the current construction game.
See [evaluation](../../../Documentation/EVALUATION.md) for what these records support and the known opening-test failures.

## Final source, 22 November

The final interface exports record [38 selected EditMode checks](approved-fill-ui-editmode.xml) and
[27 selected PlayMode checks](approved-fill-ui-playmode.xml).
The preceding room-relationship exports record [17 EditMode checks](room-highlights-editmode.xml) and
[23 PlayMode checks](room-highlights-playmode.xml). These batches overlap; they are not cumulative unique-test totals.
The folder reorganisation did not rerun these tests.
