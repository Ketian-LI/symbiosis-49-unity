# Project overview and timeline

This is the starting page for the whole project.
First use the [folder map](#what-is-in-each-folder) to find a type of material.
Then follow the [timeline](#follow-the-project-in-time) to see how the work developed.

[Folder map](#what-is-in-each-folder) · [File types](#what-the-file-types-mean) · [Timeline](#follow-the-project-in-time) · [Remaining work](#10-current-position-and-next-steps)

## What is in each folder

| Part | Folder or main file | File types present | What belongs here |
| --- | --- | --- | --- |
| Project entrance | [Root README](../README.md) | .md | What the game is, how to open it and where to read next |
| Project account | [Main documents](README.md) | .md | Report, portfolio, journal, evaluation, ethics, authorship and sources |
| Current technical instructions | [technical](technical/README.md) | .md | Setup, rules, controls, code responsibilities, modes and saves |
| Development decisions | [process](process/README.md) | .md | Why the project changed and which revision to inspect |
| Detailed revisions | [process/iterations](process/iterations/README.md) | .md | Dated notes about trophies, reading, score review and goal UI |
| Version history | [process/history](process/history/README.md) | .csv, .json, .md | Exported commits, recorded repository heads and document moves |
| Software verification | [evidence/automated-tests](evidence/automated-tests/README.md) | .xml, .json, .md | Saved check results, diagnostic settings and report indexes |
| User study | [evidence/user-testing](evidence/user-testing/README.md) | .md at present | Two-round method and evidence status; approved public records are still pending |
| Images and demonstrations | [media](media/README.md) | .png, .md at present | Labelled browser/Unity captures; the final-build demonstration is still pending |
| Assessment material | [assessment](assessment/README.md) | .md | Requirements, where to find evidence and the submission checklist |
| Historical designs | [archive](archive/README.md) | .md, .docx, .png | Superseded GDDs, visual notes and earlier overview pages |
| Dissertation | [Thesis](../Thesis/README.md) | .md at present; final PDF pending | Thesis status and the future approved PDF |
| Runnable game | [Assets](../Assets/) | .cs, .unity, .fbx, .png, .wav, .mat, .asset, .meta and shader/font files | Game code, scenes, models, interface art, audio and Unity asset data |
| Unity dependencies | [Packages](../Packages/) | .json | Package list and lockfile |
| Unity project setup | [ProjectSettings](../ProjectSettings/) | .asset, .txt, .json | Editor version, build scenes, input and project settings |
| Editable source artwork | [SourceAssets](../SourceAssets/) | .blend, .glb, .png, .jpg, .md | Modelling files, references and third-party model records |
| Optional online service | [Services/leaderboard](../Services/leaderboard/README.md) | .js, .mjs, .sql, .toml, .json, .md | Leaderboard server, database setup, configuration example and local checks |
| Supporting tools | [Tools](../Tools/) | .py, .cmd, .svg, .pdf, .csv, .json, .txt, .md | Modelling helpers, camera experiments, printable material and archive checks |
| Source inventory | [SOURCE_MANIFEST.json](../SOURCE_MANIFEST.json) | .json | File sizes and hashes used to check the source copy |

The report explains the project. The journal explains decisions. Evaluation explains what the evidence supports.
Release notes identify a source or download version. They are not substitutes for one another.

## What the file types mean

| Type | Plain meaning | How to use it |
| --- | --- | --- |
| .md | A text document with headings, tables and links | Read it directly on GitHub |
| .pdf / .docx | A fixed-layout document / an editable Word document | Check its title and status; archived documents are not the final thesis |
| .csv | A table | Open it as text or in a spreadsheet |
| .json | Structured information | Used here for configuration, inventories, parameters and recorded results |
| .xml | Structured reports | In the evidence folders, these are exported check results |
| .cs | C# source code | Read or edit it in the Unity project |
| .unity | A Unity scene | Open it in Unity |
| .asset / .mat / .meta | Unity data, materials and asset references | Unity uses them with the associated game assets |
| .blend / .fbx / .glb | Editable or exported 3D models | Use Blender or a compatible 3D tool; Unity consumes the game exports |
| .png / .jpg / .svg | Images or vector artwork | Use the caption or folder record to identify their purpose |
| .wav | Audio | Used by the game |
| .py / .cmd | Helper scripts | Read the relevant instructions before running them |
| .js / .mjs / .sql / .toml | Server code, database scripts and configuration | Used by the optional leaderboard service |

The release ZIPs are downloads outside the source tree.
The Windows ZIP contains a playable build; the Mac-labelled download is source-only.
The [status page](PROJECT_STATUS.md) identifies their dates.

## Follow the project in time

Dates below refer to commits, saved records or publication.
They are not automatically the dates on which participants played.
Some November work has only a month or snapshot date; that uncertainty is kept.

| When | Stage | Main change | Jump |
| --- | --- | --- | --- |
| July-August 2026 | Browser prototype | Feeding became a spatial choice | [01](#01-browser-prototype) |
| September 2026 | Physical input and city planning | Tokens, previews and larger route systems | [02](#02-physical-input-and-city-planning) |
| 22 September-6 October | Room board | A bounded grid with rearrangement and Research configurations | [03](#03-room-board-and-research-configurations) |
| 9-20 October | Progressive construction | Build the board one placement at a time | [04](#04-progressive-construction) |
| November; session dates still to check | Cards, upgrades and playtesting | More choices, revised rules and two rounds of feedback | [05](#05-cards-upgrades-and-playtesting) |
| 19 November snapshot | Scores, goals and review | Layout-sensitive scoring, restart repair and stories | [06](#06-the-19-november-snapshot) |
| 20 November | English archive | Source, history and selected evidence brought together | [07](#07-the-20-november-archive) |
| 21 November | Reading and score sources | Clearer text, contributor highlights and goal progress | [08](#08-the-21-november-interface-work) |
| 22 November | Thesis, ethics and document review | Update the account and disclose unfinished work | [09](#09-the-22-november-review) |
| Pending | Final package | Align the thesis, evidence, source and submission | [10](#10-current-position-and-next-steps) |

## 01 Browser prototype

The browser game began with feeding pigeons. Players later chose where to throw food and watched animals respond.
The question was how a small intervention could change conditions without directly controlling each animal.

Read the [web development account](https://github.com/Ketian-LI/urban-wildlife-simulation/blob/main/docs/WEB_DEMO_DEVELOPMENT.md)
and [commit table](process/history/early-web-history.csv).
The first recorded browser build is [22 July](https://github.com/Ketian-LI/urban-wildlife-simulation/commit/8eed0a56abad62e360fa46ff50e127a606d5f6ab).
This is the web stage, not the Unity game.

[Next stage](#02-physical-input-and-city-planning) · [Timeline](#follow-the-project-in-time)

## 02 Physical input and city planning

The Unity experiments explored physical tokens, camera input, placement previews and city routes.
They expanded the choices, but also introduced more rules and input problems.

The [migration record](https://github.com/Ketian-LI/urban-wildlife-tangible-unity/blob/main/docs/GDD_V3_MIGRATION.md)
and [repository lineage](process/REPOSITORY_LINEAGE.md) explain that direction.
The [camera guide](../Tools/CameraRecognition/README.md) describes the retained tool.
These records do not establish reliable physical play or a completed camera-based study.
The date and build of the earliest Unity transfer remain partly documented.

[Next stage](#03-room-board-and-research-configurations) · [Timeline](#follow-the-project-in-time)

## 03 Room board and Research configurations

The recorded room-project import is [22 September](https://github.com/Ketian-LI/symbiosis-49-unity/commit/aeca20ab64a41fbfdc36031663db6c1d94acd5c1).
A bounded board made room connections and routes easier to compare.
The work included rearrangement, onboarding and Research configurations.

Read the [Unity history](process/history/symbiosis-unity-history.csv) and
[historical design documents](archive/legacy-docs/).
Those modes are an earlier stage; their controls and results should not be read as the current construction rules.

[Next stage](#04-progressive-construction) · [Timeline](#follow-the-project-in-time)

## 04 Progressive construction

The [9 October construction prototype](https://github.com/Ketian-LI/symbiosis-49-unity/commit/7a3691212c01596eb32e4325724be75aecd932cf)
changed the task from rearranging an existing board to constructing one.
The [10 October menu entry](https://github.com/Ketian-LI/symbiosis-49-unity/commit/734b5b7e6e15236ab6684783ed05540b8e3e112a)
connected it to the opening and return flow.
Later October work added category limits and construction-box presentation.

The player now had to learn what each new room did before choosing a layout.
Read the [development journal](DEVELOPMENT_JOURNAL.md#september-october-a-bounded-room-board),
the [20 October source](https://github.com/Ketian-LI/symbiosis-49-unity/commit/d5a8cc7ec03f95993370fe5626156aa61f959690)
and the [current scene/mode guide](technical/SCENES_AND_MODES.md).

[Next stage](#05-cards-upgrades-and-playtesting) · [Timeline](#follow-the-project-in-time)

## 05 Cards upgrades and playtesting

November work developed the illustrated hand, deck area, room upgrades and staged opening.
Feedback raised questions about small text, visual consistency, unclear consequences and repetition.

The author reports 5 people in the first round and 13 in the second.
The original 5 returned, making 13 different people across both rounds.
Exact session dates and executable identifiers still need checking.
The [method record](evidence/user-testing/METHOD.md) and [evaluation](EVALUATION.md) distinguish the rounds and their limits.

The [current rules](technical/GAME_RULES.md) describe the published source.
They should not be applied backwards to every participant session.

[Next stage](#06-the-19-november-snapshot) · [Timeline](#follow-the-project-in-time)

## 06 The 19 November snapshot

This snapshot brought together stronger layout effects, visible deductions, result review and the repair for disappearing goals.
The journal added short stories based on placements and recorded activity.

Use the [journal's revision table](DEVELOPMENT_JOURNAL.md#november-cards-and-consequences),
the [software-report index](evidence/automated-tests/README.md)
and the [StreetJournal release](https://github.com/Ketian-LI/symbiosis-49-unity/releases/tag/archive-2026-11-20).
The release was published on 20 November, but its Windows executable is the 19 November build.

[Next stage](#07-the-20-november-archive) · [Timeline](#follow-the-project-in-time)

## 07 The 20 November archive

The English archive linked the prototypes, project report, development process, selected checks and source inventory.
The source ZIP was prepared below the brief's 1 GB upload limit.
It left the final thesis and approved anonymous research evidence pending.

See the [pinned archive](https://github.com/Ketian-LI/symbiosis-49-unity/tree/archive/2026-11-20),
[release notes](process/RELEASE_NOTES.md#archive-snapshot---20-november-2026)
and [assessment requirements](assessment/REQUIREMENTS.md).
This was a published archive, not a Moodle submission.

[Next stage](#08-the-21-november-interface-work) · [Timeline](#follow-the-project-in-time)

## 08 The 21 November interface work

The next source revisions improved text hierarchy and kept score causes available after the animation.
The score-source view highlighted relevant rooms and visible actors.
Further work changed the inside-leaf presentation and three-goal component.

Read the [reading and balance review](process/iterations/SKILL_GUIDED_UI_2026-11-21.md),
[score-source record](process/iterations/SCORE_INDEX_2026-11-21.md),
[left-page record](process/iterations/LEFT_LEAF_STYLE_2026-11-21.md)
and [goal UI record](process/iterations/GOAL_PROGRESS_UI_2026-11-21.md).
The [screenshots](media/screenshots/README.md) are labelled Unity test renders.
Later feedback can reject a visual design even when its software checks pass.

[Next stage](#09-the-22-november-review) · [Timeline](#follow-the-project-in-time)

## 09 The 22 November review

The documents were rewritten in shorter English and grouped by purpose.
A newer local thesis revision added the later study account.
The ethics reflection recorded the missed review, the risks and the actions still unfinished.

Read the [document map](README.md), [thesis status](../Thesis/README.md),
[ethics reflection](ETHICS_REFLECTION.md) and [project status](PROJECT_STATUS.md).
Private consent documents, chats and audio are not published here.
The reflection does not establish approval.

[Next stage](#10-current-position-and-next-steps) · [Timeline](#follow-the-project-in-time)

## 10 Current position and next steps

The repository contains the source and documented development process.
The downloads, local draft and published source are different snapshots.
The final thesis PDF, permitted research evidence, final-build demonstration and updated submission package remain open.

Use [project status](PROJECT_STATUS.md) for the current position and
the [submission checklist](assessment/SUBMISSION_CHECKLIST.md) for the remaining actions.
The [technical guide](technical/README.md) explains how to run and rebuild the source.

[Back to folder map](#what-is-in-each-folder) · [Back to timeline](#follow-the-project-in-time)
