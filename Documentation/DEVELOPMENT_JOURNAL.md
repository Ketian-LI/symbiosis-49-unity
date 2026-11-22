# Development journal

This account was reconstructed from commits, saved design documents, test reports and captures.
It was first assembled on 20 November and reorganised on 22 November 2026.
Retrospective entries are not presented as notes written at the time.

## July-August: feeding pigeons

The browser prototype began with a feeding action. Later changes let players choose where to throw food and watch nearby
animals respond. This raised a design question: could the player change conditions without controlling every animal directly?

The [web history](process/history/early-web-history.csv) and
[web development account](https://github.com/Ketian-LI/urban-wildlife-simulation/blob/main/docs/WEB_DEMO_DEVELOPMENT.md)
record that stage. They do not show a Unity build.

## September: physical input and city planning

The tangible Unity experiment introduced building tokens, camera input and a preview/confirm step.
The city model also introduced routes and environmental pressure. There were now more decisions, but more rules to learn
and more input failures to diagnose.

The [migration record](https://github.com/Ketian-LI/urban-wildlife-tangible-unity/blob/main/docs/GDD_V3_MIGRATION.md)
describes that direction. Physical recognition remains a separate, partly verified prototype.

## September-October: a bounded room board

The project moved to a modular room grid. A bounded board made routes and neighbour relationships easier to compare.
Earlier versions began with a populated board. The later construction version let the player build it one placement at a time.

This changed the opening problem. Players needed to learn what a room did before choosing among many room types.
The [repository lineage](process/REPOSITORY_LINEAGE.md) links the source stages and dated commits.

## November: cards and consequences

Illustrated cards, a lower hand tray and room upgrades developed through repeated visual review.
Feedback identified small text, detached hinges, wasted space and panels covering useful information.
These comments guided revisions. They were not a controlled comparison of visual styles.

The main software revisions were:

| Problem | Change | Evidence |
| --- | --- | --- |
| Good and poor layouts ended with similar scores | V13 increased the effect of favourable and unfavourable pairings | [Scoring report](evidence/automated-tests/scoring-and-storage.xml) |
| Name entry and restart controls could be blocked | Input focus and the topmost pointer target were checked | [Result checks](evidence/automated-tests/result-interactions.xml) |
| Goals disappeared after restart or tutorial skip | Goal presentation was reset for each run | [Failure](evidence/automated-tests/goal-restart-before-fix.xml) and [repair](evidence/automated-tests/goal-restart.xml) |
| The left page had little to explain | Stories were derived from placements and recorded activity | [Story checks](evidence/automated-tests/story-model.xml) |
| Score animations ended before causes could be reviewed | A score-source view reopened the last settlement's rooms and actors | [Interface record](process/iterations/SCORE_INDEX_2026-11-21.md) |
| The three-goal component did not fit the page | Names, metric labels, fractions and achieved states were separated | [Goal UI record](process/iterations/GOAL_PROGRESS_UI_2026-11-21.md) |

A story, an upgrade and a score are related but different. An upgrade can change capacity.
A routine or meal still has to happen before an activity score is recorded.

## 20-21 November: archive and review

The English archive brought source, history, selected checks and screenshots together.
It kept the final thesis and approved study evidence pending.
A further review checked text in both languages and compared diagnostic placement policies.
The [iteration index](process/iterations/README.md) links the detailed records.

## 22 November: documents and open design work

The documents were grouped by purpose. Main guides were shortened, and historical plans were moved to an archive.
Links, source hashes and test-report counts were checked. Gameplay source and saved reports were retained.

The city-background restoration, card-matching goal icons, revised recent-changes area and per-category deck display remain
design requests. Manual cross-links and an Enter key to confirm placement also remain planned.
They are not described as implemented features in this revision.

The next submission tasks are to finish the thesis, reconcile study evidence and package the same version that the report describes.
See the [checklist](assessment/SUBMISSION_CHECKLIST.md).

## 22 November ethics reflection

The author supplied staff advice to explain the missed ethics review and reflect on its risks. The author confirmed that
the application had not yet been sent to the supervisor. Pausing further playtesting, moving records to UAL OneDrive and
handling external service records had not been completed. The [reflection](ETHICS_REFLECTION.md) separates the materials
prepared and disclosure made from the work still needed. Private participant forms remain outside the public repository.
