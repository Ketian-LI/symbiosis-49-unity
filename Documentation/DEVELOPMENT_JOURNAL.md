# Development journal

This account was reconstructed from commits, saved design documents, test reports and captures.
It was first assembled on 20 November and reorganised on 22 November 2026.
Retrospective entries are not presented as notes written at the time.

The [project overview and linked timeline](PROJECT_OVERVIEW.md#follow-the-project-in-time) gives a shorter route through these stages.
The [design-decision account](DESIGN_DECISIONS.md) explains the reasons behind the changes and links them to source and verification records.

## July-August: feeding pigeons

The browser prototype began with a feeding action. Later changes let players choose where to throw food and watch nearby
animals respond. This raised a design question: could the player change conditions without controlling every animal directly?

The [web history](../Misc/process/history/early-web-history.csv) and
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
The [repository lineage](../Misc/process/REPOSITORY_LINEAGE.md) links the source stages and dated commits.

## November: cards and consequences

Illustrated cards, a lower hand tray and room upgrades developed through repeated visual review.
Feedback identified small text, detached hinges, wasted space and panels covering useful information.
These comments guided revisions. They were not a controlled comparison of visual styles.

The main software revisions were:

| Problem | Change | Evidence |
| --- | --- | --- |
| Good and poor layouts ended with similar scores | V13 increased the effect of favourable and unfavourable pairings | [Scoring report](../Misc/evidence/automated-tests/scoring-and-storage.xml) |
| Name entry and restart controls could be blocked | Input focus and the topmost pointer target were checked | [Result checks](../Misc/evidence/automated-tests/result-interactions.xml) |
| Goals disappeared after restart or tutorial skip | Goal presentation was reset for each run | [Failure](../Misc/evidence/automated-tests/goal-restart-before-fix.xml) and [repair](../Misc/evidence/automated-tests/goal-restart.xml) |
| The left page had little to explain | Stories were derived from placements and recorded activity | [Story checks](../Misc/evidence/automated-tests/story-model.xml) |
| Score animations ended before causes could be reviewed | A score-source view reopened the last settlement's rooms and actors | [Interface record](../Misc/process/iterations/SCORE_INDEX_2026-11-21.md) |
| The three-goal component did not fit the page | Names, metric labels, fractions and achieved states were separated | [Goal UI record](../Misc/process/iterations/GOAL_PROGRESS_UI_2026-11-21.md) |

A story, an upgrade and a score are related but different. An upgrade can change capacity.
A routine or meal still has to happen before an activity score is recorded.

## 20-21 November: archive and review

The English archive brought source, history, selected checks and screenshots together.
A further review checked text in both languages and compared diagnostic placement policies.
The [iteration index](../Misc/process/iterations/README.md) links the detailed records.

## 22 November: documentation and ethics reflection

The report, portfolio, journal and evaluation were organised into a single reading route, with a linked timeline.
Detailed guides, individual revisions, software reports and older designs were grouped in [Misc](../Misc/README.md).
Links, source hashes and saved report counts were checked. This work changed documentation rather than gameplay.

The two-round study account records five first-round participants returning within a second-round group of thirteen.
The [ethics reflection](ETHICS_REFLECTION.md) documents the missed review, external processing of earlier feedback
and the safeguards that remained unresolved on 22 November. Private participant records are excluded from the public archive.

Later proposals include card-based goal icons, a deck-composition display, revised background and recent-changes visuals,
manual cross-links and an Enter key for placement confirmation. These proposals are separate from the implemented source.
The [delivery record](PROJECT_STATUS.md) identifies the scope of the published version.
