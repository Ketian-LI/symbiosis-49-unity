# Project report

## Purpose and current contribution

SYMBIOSIS: 49 is a practice-based investigation into how an interactive system can make human influence on shared urban
conditions visible. The current artefact uses a room-card construction game. Players arrange homes, workplaces, places to eat,
green habitat, streets and waste facilities. They influence relationships and resources rather than directly commanding each
animal. Its contribution is a designed, inspectable relationship between spatial arrangement and consequence feedback, supported
by a documented process and a rebuildable Unity implementation. It is not a predictive ecological model.

The project's form changed substantially. A browser prototype explored feeding and delayed dependence; a tangible city prototype
explored token input and networks; later room systems bounded the space to a square board. The current construction version
turns this bounded space into a sequence of choices. These stages share an enquiry but remain separate implementations.
Their histories and tests cannot be substituted for one another.

## Creative and interaction decisions

The tabletop presentation uses illustrated cards and an open book-like board. A card's image helps establish the room's role,
while the world view shows relationships between occupied cells. The selected hand remains below the board; score information
is grouped on one side, and goals remain accessible on the other. Repeated design critique identified that selecting a new card
automatically could hide the player's chance to inspect outcomes. Free placement now waits for another explicit choice.

Neighbour upgrades make arrangements visible. Connected greens can improve a patch, a home can benefit from bordering connected
green space, a restaurant next to a plaza gains capacity, and a street between opposite green plots becomes less risky.
These are deliberately simplified game conditions. An upgrade does not by itself prove that anyone benefited: plant maturity,
available food, route accessibility and actual activity remain distinct parts of settlement.

The neighbourhood journal adds a short narrative to selected recorded changes. A garden home may be described as a new view
outside a window, followed by the precise adjacency condition that triggered it. A meal or completed daily routine appears only
when the model records that activity. Negative stories describe a bad adjacency or road death without manufacturing reassurance.
The journal is intended to connect an abstract rule to an everyday image; its interpretive effectiveness is not established by a
successful render or a unit test.

## Computational realisation

The Unity project separates board/run state, food, resident activity, wildlife outcomes, scoring, persistence and presentation.
Placements are previewed and then committed. The model determines what settles, while controller partials coordinate room
visuals, card motion, goal cards, score reasons and result review. The narrative catalogue is read-only: rebuilding a story list
does not create resources, scores or a new simulation event. The same saved facts can recreate its records after loading.

Versioned score and placement data allow old saves to retain their rules. V13 was introduced after the user reported small
differences between contrasting layouts. Its stronger adjacency rewards and penalties make that policy more consequential.
One fixed six-plot Endless comparison widened the score difference from 3 to 166, but this does not establish global balance,
an optimum, or an ecological measure. The score expresses choices made by the designer.

The optional HTTP leaderboard is separated from local rankings and ordinary play. It stores a best result by public-IP digest,
mode and rule version. This means people sharing a public network may share a record. Client-submitted totals are not independently
replayed by the server, and pseudonymous IP-derived records should not be described as anonymous accounts. The limitation is
part of the current technical account rather than hidden behind a decorative online label.

## Iteration and verification

The archive contains selected exported checks for scoring, storage, result controls, goal restart and narrative presentation.
One particularly useful fault concerned disappearing goals: restarting retained a UI flag even though the new run contained
three task records. A before-fix regression failed, the UI state was rebound to the run ID, and the corrected flow passed.
Testing also checks pointer occlusion, keyboard focus, read-only result review and whether a restart preserves the finished score.

The final narrative capture was rendered by Unity from a fixed nine-placement fixture. English and Chinese text sizes and
separation from the goal panel were checked. The Windows build was produced and startup-checked without changing game save files.
This remains a bounded technical verification. It is not a complete human playthrough of every mode, a long-term performance
study, or proof that the experience communicates its intended interpretation.

## Research evaluation and limits

An existing English working thesis reports that five people independently completed 49 Fill days on their own computers and
three returned questionnaires. The exact distributed build identifier remains unresolved, and the archive does not contain
approved anonymous raw records or a completed coding trail. Those reports must not be presented as an evaluation of the current
V13/narrative snapshot. The [evaluation document](EVALUATION.md) distinguishes author-reported study history, technical tests,
design feedback and proposed further work.

The camera path has a different evidence boundary. Retained documents describe a single physical-frame check and synthetic
observations, while continuous multi-tile physical play remains unverified. Remote keyboard/mouse Fill play is not a camera
comparison. The Mac delivery likewise remains source-only because an application was not built or tested on macOS.

## Responsibility and next work

The project simplifies species, growth, risk and coexistence for interaction. Combining human and animal points can imply that
unlike interests are directly exchangeable. Making reasons visible helps expose this design decision, but it does not remove it.
The critical task is to ask what players infer, which interests remain hidden, and whether care is being reduced to optimisation.
Scores, journal prose and compressed timescales should therefore be read as authored representations.

Next steps are to identify the evaluated build, complete an anonymous action-to-interpretation analysis, resolve final thesis
and ethics documentation, and test the revised feedback with a specified method. AI-assisted development and visual production
must remain disclosed. This archive provides a stable, inspectable record for that work rather than claiming that pending
research has already been completed.
