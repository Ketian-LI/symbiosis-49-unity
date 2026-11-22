# Development journal: decisions, evidence and revisions

**Method:** reconstructed on 20 November 2026 from real Git commits, saved design documents, local test exports and captures.
This is not a backfilled contemporaneous diary. Historical design proposals, working implementations and current features
are separated. Detailed author dates and commit subjects are in [the CSV records](history/).

## Process navigation

| Stage | Design decision to inspect | Evidence owner |
| --- | --- | --- |
| Web feeding prototype | Indirect intervention and delayed consequences | [July-August account](#july-august-2026-feeding-as-a-spatial-intervention) |
| Tangible Unity experiment | Physical tokens, city networks and input reliability | [September account](#september-2026-tangible-input-and-city-networks) |
| Bounded room system | Repeatable layout, routes and observed activity | [Room-system account](#22-september-6-october-2026-a-bounded-room-system) |
| Construction game | Successive placements, card presentation and neighbour upgrades | [Construction account](#october-2026-construction-and-consequence-feedback) |
| Consequence feedback | Score separation, review, stories and participant navigation | Dated November entries below |

The table routes to the historical accounts. Current behaviour belongs to the [project report](PROJECT_REPORT.md);
test counts and research claims belong to [evaluation](EVALUATION.md).

## July-August 2026: feeding as a spatial intervention

The browser prototype began with a Feed control, persistent state and periodic system changes. Successive commits explored
typographic pigeons, throwing food to a chosen position, nearest-first responses and visible acceptance/refusal.
The design moved from selecting a guaranteed recipient towards changing a condition to which animals respond.
Later species and city events made repeated intervention consequential over time.

Evidence: [web history](https://github.com/Ketian-LI/urban-wildlife-simulation/commits/main/),
[local commit export](history/early-web-history.csv), [web development account](https://github.com/Ketian-LI/urban-wildlife-simulation/blob/main/docs/WEB_DEMO_DEVELOPMENT.md).
The lesson carried forward was indirect agency; the browser implementation itself was not the Unity deliverable.

## September 2026: tangible input and city networks

The tangible Unity direction explored physical building tokens, a camera/file bridge and explicit preview/confirmation.
Its migration document distinguishes city state, walking routes, vehicle routes, environmental pressure and trace output.
This broadened the project from feeding towards constructing conditions. It also raised a practical problem: a technically
rich planning model can exceed what a newcomer can read or what a physical table can reliably register.

Evidence: [GDD migration](https://github.com/Ketian-LI/urban-wildlife-tangible-unity/blob/main/docs/GDD_V3_MIGRATION.md).
The document's implementation labels are retained as that repository's claims; this archive did not rerun a complete tangible build.

## 22 September-6 October 2026: a bounded room system

The SYMBIOSIS repository's initial import is commit `aeca20a`. Subsequent commits address onboarding, snapping and swapping,
mode selection, Research setup, result screens and animation sampling. The square room system gave spatial relationships
a repeatable visual grammar. Navigation, food inventory and actual movement still needed separate checks: showing a resource
or planned route does not prove that an animal ate or a resident completed a journey.

Evidence: [Unity commit export](history/symbiosis-unity-history.csv), retained [historical design documents](../Docs/),
and [historical README](../Docs/README-historical-before-archive.md). The earlier full-board rearrangement and Research modes
remain historical implementation evidence, rather than the rules of the current construction game.

## October 2026: construction and consequence feedback

The progressive construction branch introduced sequential placement and room-category limits. Commits through `d5a8cc7`
record construction-box visuals, category counts, animal-meal/resident-commute animation and forecast-risk clarification.
The important change was from managing an already populated board towards making the board over successive placements.
Tutorial pacing, reachable services and legible consequences became central design problems.

Evidence: commits `ac5c6a0`, `28544c3`, `40c7cbe` and the October construction head linked in the [lineage map](REPOSITORY_LINEAGE.md).
These are real commits; later November snapshots are not represented as if they belonged to this sequence.

## November snapshots: printed cards, upgrades and a clearer turn

Local working-tree iterations developed the illustrated card hand, deck/reroll area, book-like side leaves and room upgrades.
The user's design feedback repeatedly identified mismatched materials, detached hinges, wasted sidebar space and information
that obscured the board or goals. The resulting interface concentrates hand cards below the board and keeps score information together.
This is documented design critique, not a controlled participant comparison of visual styles.

Room upgrades were connected to inspectable adjacency conditions. Connected greens, homes next to a connected green patch,
restaurants beside plazas and streets between opposite greens have distinct conditions. Maturity remains separate from
connection: a newly placed plant does not become a source of food merely because its visual upgrade appears.

## 19 November snapshot: layout-sensitive scoring and reasons

The user reported that good and poor layouts ended with similar totals, including penalties. V13 increases the difference
between favourable and unfavourable adjacency and uses an explicit collision penalty. Older saves keep their saved rules.
In one fixed Endless fixture with the same six plots, the old-rule gap was 3 and the V13 gap was 166. That is a bounded
regression example, not evidence that all layouts are well balanced or that the score measures ecological value.

Evidence: [scoring tests](evidence/scoring-and-storage.xml) and
[layout-quality fixture](../Assets/Tests/Editor/ConstructionLayoutQualityScoreTests.cs).
Individual negative pairings remain visible even if simultaneous rewards offset their net total.

## 19 November snapshot: result review, naming and restart

User reports identified unresponsive name entry, ranking overlays that prevented layout review, and restart controls hidden
under another modal. The revision adds a route back to the finished board without allowing further building, keeps keyboard
focus usable, and checks the actual topmost pointer target rather than invoking a hidden button's callback alone.
The online service was checked rather than populated with fabricated rivals. Its public-IP identity limitation is documented.

Evidence: [result interaction batch](evidence/result-interactions.xml). These technical checks do not establish a multi-user study
or independently validate a server-side name write for every deployment condition.

## 19 November snapshot: the disappearing three goals

Restart reused a presentation flag from the previous run. The new run's goal data existed, but the UI believed its selection
had already been shown, leaving the right-hand bookmark hidden. The regression reproduced the failure before the fix.
Presentation state and in-flight animations are now bound to the run ID. Both tutorial-skip routes, loading, confirmation,
bookmark review and restart were checked. The close button is enabled only when its opening animation has finished.

Evidence: [before-fix failure](evidence/goal-restart-before-fix.xml), [after-fix batch](evidence/goal-restart.xml),
and [test implementation](../Assets/Tests/PlayMode/ConstructionGoalRestartTests.cs).
This is an observed software fault with a focused revision, not a retrospective claim that every earlier restart worked.

## 19 November snapshot: the neighbourhood journal

The user asked the empty left leaf to explain effects through stories. The journal projects short text from saved placements
and observed outcomes: a connected patch, a garden home, a real meal, a completed routine, collected waste or a collision.
The text includes a trigger explanation, while all three goal progress rows remain below it. It does not award points or
invent an outcome merely to make the story more positive. Stories reconstruct after loading from the same recorded facts.

Evidence: [model batch](evidence/story-model.xml), [interface batch](evidence/story-interface.xml),
[final render check](evidence/story-final-render.xml) and [actual left-leaf capture](images/street-journal-left.png).
The narrative is an interface intervention; whether it improves players' interpretation remains a user-testing question.

## 20 November: archive and evidence reconciliation

This archive connects the stages, replaces the top-level historical description with English navigation, records source hashes,
preserves relevant code/assets and adds a requirements/status map. The final thesis and approved anonymous research evidence
remain pending by the author's instruction. Publication of this archive is not assessment submission.

## 21 November: skills, reading hierarchy and observed reasons

The current source was checkpointed before another interface pass. Four public GitHub skills informed layout,
plain wording, balance diagnostics and game-state graphics. The adaptation keeps the existing tabletop identity,
separates the three score components, shows next-trophy distance and retains selected observed causes after animation.
Symbols and words accompany colour. Actual renders exposed a score-heading overlap and a fallback-arrow text
truncation; both were corrected before publication. The same balance policies were extended to held-out seeds,
supporting a provisional decision to keep the current bands rather than raise scores without evidence.

See the [dated review](../Docs/SKILL_GUIDED_UI_2026-11-21.md) for pinned references, limitations, captures and test reports.
This is an AI-assisted source revision, not a new participant study or executable release.

## 21 November: score contributors and documentation structure

The user asked to jump between score causes with keyboard shortcuts and highlight the rooms, residents or animals involved.
The revision projects an index from settled placement, meal and routine records. Fill score phases show the corresponding
contributors; manual review reopens the same causes. An Endless jump moves the viewing window to an off-screen contributor.
Input fields and blocking overlays suspend the shortcuts. Missing historical actor identities retain known room causes.

Two documentation skills informed the README, project-report and navigation edits. Current behaviour, historical decisions,
controls and evidence have separate owners. Earlier dated entries and their limitations remain intact. Sources and review
decisions are in the [documentation review](DOCUMENTATION_REVIEW_2026-11-21.md), with runtime scope in the
[score-index record](../Docs/SCORE_INDEX_2026-11-21.md).

## 21 November: matching the inside leaves

The user identified a visual mismatch between the left journal/goal leaf and the right score/card leaf. Both interiors now
reuse the right-side oak paper. A shared resource path also supplies the opening world, addressing transition continuity.
The left journal uses a transparent background, horizontal brass rules and the right-side heading/body scale. Goal content
keeps a reading-paper backing. The [visual record](../Docs/LEFT_LEAF_STYLE_2026-11-21.md) links actual bilingual captures and
separate interface/opening checks. The exterior lid and gameplay rules remain outside this visual change.

## 21 November: redesigning the goal progress component

The user clarified that the remaining mismatch concerned the goal progress UI, rather than the leaf artwork. The three
goals now use a compact ledger: mounted illustrations, distinct goal names and metric labels, aligned fractions and fine brass
rules. A small header badge reports achieved goals; completed rows have a tick and their actual activation/settlement state.
A feathered field reuses hand-card paper texture to protect text over foliage. The [component record](../Docs/GOAL_PROGRESS_UI_2026-11-21.md)
links actual bilingual renders and checks. Page backgrounds, scoring and simulation rules were retained.

## 22 November: persistent room relationships and isolated score batches

The user prioritised highlights before further UI/rule work. Clicking a built room now pins its current pairings and recorded
score relationships. A waste room shows collection coverage and separate red frames for bad residential pairings at the same
time. Pointer exit no longer clears the pin. Residents, animals and shared score animate in three isolated groups, with explicit
phase labels. The [feature record](../Docs/ROOM_HIGHLIGHTS_2026-11-22.md) links checks and actual captures; the
[pending list](../Docs/PENDING_REQUESTS_2026-11-22.md) keeps recurring deductions, terminology, diagrams, the P journal and the
approved wider visual revision outside this delivery. Selection does not award or deduct points.

## 22 November: apply the approved Fill interface

The approved concept was implemented with a restored city motif, restrained story/goal hierarchy, room-card thumbnails, a separate score-source button and selective action emphasis. A live remaining-card catalogue uses actual stock and returns swapped cards to the pile. New generated paper adaptations reserve clear reading space and are shared by opening lids and the live board. Native Fill captures and the 38/38 logic and 27/27 interface batches are linked in the [revision record](../Docs/APPROVED_FILL_UI_2026-11-22.md). Recurring deductions, commuting/glossary diagrams and P journal remain pending.

## 22 November: automatic commuting and visible continuous consequences

Manual resident dispatch was retired in live runs. Capacity-aware allocation and shortest legal fixed-order itinerary now feed inspection and animation. Ten terms link to exact marked manual entries. The author clarified P for rules/examples and I for a separate journal. Fill bad shared edges now deduct at each confirmed settlement, with per-edge persisted causes and no duplicate introduction charge. Zero-value road death warnings and duplicate exposure icons were removed. The [revision record](../Docs/COMMUTING_REFERENCE_AND_PENALTIES_2026-11-22.md) links source details and native renders.
