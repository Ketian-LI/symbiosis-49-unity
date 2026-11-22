# Reading, feedback and skill review — 21 November 2026

The latest working Unity source was checkpointed before this revision at commit
`a1fe868ff766e5e3bfb5bc6e914834ec381ff567`. This update changes the Fill interface and adds diagnostic evidence;
it does not change gameplay scoring V13 or the new-run trophy rules introduced on 20 November.

## References actually consulted

These are publicly available skill instruction files, read at pinned revisions. They were used as review references,
not installed globally or allowed to execute repository scripts. No third-party implementation or new raster artwork
was copied into the game. The adaptation to Unity and the code/tests in this update were AI-assisted.

| Area | GitHub skill | Read revision | Repository licence |
| --- | --- | --- | --- |
| layout | [hueyexe/frontend-agent-skills](https://github.com/hueyexe/frontend-agent-skills/blob/2841c079dd8a9c634882227194dc42e25227710d/ui-visual-composition/SKILL.md) | `2841c079dd8a` | MIT |
| copy | [blader/humanizer](https://github.com/blader/humanizer/blob/a32f24407364c74c812b43c15926d638bee1e072/SKILL.md) | `a32f24407364` | MIT |
| balance | [Getty/skills](https://github.com/Getty/skills/blob/e3cd5ab11f7acf196992985b8fc8535dfeb13a57/game-development/game-balance-math/SKILL.md) | `e3cd5ab11f7a` | Artistic-2.0 |
| visuals | [myshenoy/skills-open-source](https://github.com/myshenoy/skills-open-source/blob/0c321175433804913ef8d2909924601924771864/game-visuals/SKILL.md) | `0c3211754338` | MIT |

The same Getty revision also supplied
[simulation-balancing](https://github.com/Getty/skills/blob/e3cd5ab11f7acf196992985b8fc8535dfeb13a57/game-development/simulation-balancing/SKILL.md).
The layout checklist and the visual skill's simulation-art/visual-problems references were consulted as supporting material.
Full paths and blob identifiers are retained in [the source record](../../evidence/automated-tests/skill-sources.json).
Licences in this table describe the referenced repositories, not a relicensing of this Unity project.

## Applied decisions

| Reference area | Decision in this game | Boundary |
| --- | --- | --- |
| Layout | Keep score, three components and next-trophy distance together; preserve a separate selected-card area; put recent causes beneath it | The established tabletop, leaf art, hinges and compact hand tray remain the visual baseline |
| Humanizer | Use short labels and concrete observed effects; shorten long English goal titles; replace awkward plural placeholders | Facts, quantities and player-facing rules take priority over stylistic suggestions; no AI-detection claim |
| Balance math / simulation | Repeat the same three policies with 12 additional seeds and report ranges, medians and score composition | Diagnostic policies are neither an optimal solver nor human player evidence; this is a reason to hold the current bands for now |
| Game visuals | Use blue for shared score, green with an up-arrow for upgrades, and rust with a warning mark and a negative number for deductions | Colour is reinforced by symbols and words; no full-screen colour filter or unsupported ecological claim |

## What the interface now says

| Information | Position | Example |
| --- | --- | --- |
| Total score | Upper part of the right leaf | `214 pts` / `214 分` |
| Score components | Three columns directly beneath total | Shared 102 · Build 112 · Goals 0 |
| Next trophy | Beneath score components | Iron: 1,286 pts to go |
| Selected card | Middle of the right leaf | Artwork, room name, valid plot count and one short purpose sentence |
| Recent observed causes | Bottom of the right leaf, up to three rows | Habitat upgraded to level 2; Street beside green ×1 -40 |
| Journal and all three goals | Left leaf | Story and goal progress remain visible together |
| Upgrade preview | Upper edge of affected board plot | Up-arrow on the existing neighbour-upgrade outline |

At current scoring V13, total = shared score + placement/build score + goal score. Shared score already contains
resident linkage points and subtracts actual traffic penalties; linkage must not be added a second time.
The three-column display reads these same model values, including during score animation.

The recent-cause panel reads the last settled placement and actual settlement data. Priority is an actual collision
deduction, negative adjacency groups, neighbouring upgrades, then food actually eaten. It retains the explanation after
the flying cue disappears. It does not invent a meal, award points or replace the full score ledger. Up to three selected
causes are shown, so the list is deliberately not an exhaustive daily transaction history. The previous version's
adjacency neighbourhood is respected when a historical run is displayed.

### Reading and colour values

| Role | Value | Use |
| --- | --- | --- |
| Shared score ink | `#24506B` | Blue score component |
| Upgrade ink | `#2D604A` | Observed upgrade and arrow |
| Deduction ink | `#984A35` | Warning mark, reason and negative points |
| Main ink | Existing dark ink, approximately `#21303D` | Main labels and body text |
| Materials | Existing warm paper, oak foliage and brass | No unrelated visual theme was introduced |

New component labels, award distance and recent causes use 16 canvas units; component values use 21. Selected-card
purpose text keeps its 18-unit reading role. These are reference canvas units, not guaranteed physical pixels at every
resolution. The inspection uses actual SDF text, including truncation checks, rather than relying on legacy Text metrics
alone. An arrow's fallback font initially hid its following sentence; keeping the symbol in its own label fixed the render.

## Numerical check

The existing policy implementation (revision 1) is unchanged. Every run has the same eight-placement tutorial opening,
then chooses only legal cards/plots. Each policy uses seeds 1–24: seeds 1–12 repeat the earlier calibration and 13–24 are
held-out checks. All 72 runs complete 49 plots. The original 36 samples match their previous values.

| Policy | Seed set | Minimum | Median | Maximum | Gold (≥4500) |
| --- | --- | ---: | ---: | ---: | ---: |
| unfavourable-edges | Calibration (1–12) | -251 | 542.5 | 1899 | 0/12 |
| unfavourable-edges | Held-out (13–24) | -117 | 553.5 | 1077 | 0/12 |
| random-legal | Calibration (1–12) | 1683 | 2485.5 | 2838 | 0/12 |
| random-legal | Held-out (13–24) | 2095 | 2473.5 | 2963 | 0/12 |
| connected-services | Calibration (1–12) | 3538 | 4075 | 4703 | 1/12 |
| connected-services | Held-out (13–24) | 3006 | 4183 | 4610 | 1/12 |

The held-out connected-service median is 4183, compared with 2473.5 for random legal play and 553.5 for unfavourable
edges. The ranges still overlap across some policies and these small samples cannot estimate player trophy rates.
No threshold was fitted to the held-out seeds. New Fill bands remain Plastic <1500, Iron 1500–2499, Bronze 2500–3499,
Silver 3500–4499 and Gold ≥4500. Historical runs keep their saved award rules. Future human play may justify retuning.

## Verification and delivery

The selected EditMode batch passes 90/90 checks; PlayMode passes 19/19. Coverage includes persistent penalty reasons,
real upgrade conditions, an un-settled next placement, no invented opening event, scoring, saves, award versions,
goal skip/restart, result controls, hand presentation, all eight room-detail texts in both languages, and world-view resizing. Batches overlap older exports and must
not be added into a cumulative unique-test count.

Actual 1920×1080 Unity renders were inspected in both languages, including a real waste/home -80 deduction fixture.
The capture helper now reframes the world camera when switching to its off-screen target; previously the UI resized
but the world board did not, creating a misleading gap in screenshots. This is a test-capture correction, not a production
camera redesign. Tested scenes are synthetic fixtures, not participant sessions; different monitor scale settings and
every possible localisation string have not all been visually inspected.

The source project and GitHub branch are updated. No new Windows executable or macOS `.app` was built in this task.
The downloadable 20 November archive release remains immutable and contains its earlier deliverables.

Evidence: [EditMode](../../evidence/automated-tests/skill-polish-editmode.xml),
[PlayMode](../../evidence/automated-tests/skill-polish-playmode.xml),
[72-run data](../../evidence/automated-tests/skill-calibration-72.json),
[Chinese render](../../../Documentation/media/screenshots/game-readable-zh.png),
[English render](../../../Documentation/media/screenshots/game-readable-en.png),
[deduction render](../../../Documentation/media/screenshots/penalty-readable-zh.png).
