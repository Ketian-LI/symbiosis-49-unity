# Project overview and timeline

This is the starting page for the whole project.
First use the [folder map](#what-is-in-each-folder) to find a type of material.
Then follow the [timeline](#follow-the-project-in-time) to see how the work developed.
The [design-decision account](DESIGN_DECISIONS.md) connects the main changes to their reasons and evidence.

[Folder map](#what-is-in-each-folder) · [File types](#what-the-file-types-mean) · [Timeline](#follow-the-project-in-time) · [Delivery record](#10-published-record)

## What is in each folder

The assessment reading is in `Documentation` and `Thesis`. Supporting records are grouped in `Misc`.
The timeline below connects the main account to the relevant detail.

| Part | Folder or main file | File types | What belongs here |
| --- | --- | --- | --- |
| Project entrance | [Root README](../README.md) | .md | Game, download and reading routes |
| Main account | [Documentation](README.md) | .md | Portfolio, report, journal, evaluation, ethics and authorship |
| Study account | [evidence/user-testing](evidence/user-testing/README.md) | .md at present | Two-round method and evidence status; approved detailed records remain pending |
| Images and demonstration | [media](media/README.md) | .png, .md | Labelled screenshots; final-build demonstration remains pending |
| Requirements and inventory | [assessment](assessment/README.md) | .md | Required material, its location and delivery record |
| Dissertation | [Thesis](../Thesis/README.md) | .md at present; final PDF pending | Thesis status and the future approved PDF |
| Supporting material | [Misc](../Misc/README.md) | .md, .xml, .json, .csv, .docx, .png | Detailed guides, dated revisions, saved reports, history and old designs |

The report explains the project. The journal explains decisions. Evaluation explains what the evidence supports.
The ethics reflection and authorship records remain in the main reading.

### Source folders

These folders keep the paths used by Unity and the supporting scripts.

| Folder | Main file types | Purpose |
| --- | --- | --- |
| [Assets](../Assets/) | .cs, .unity, .fbx, .png, .wav, .meta, shaders and fonts | Game source, scenes and runtime assets |
| [Packages](../Packages/) and [ProjectSettings](../ProjectSettings/) | .json, .asset, .txt | Dependencies, editor version and project settings |
| [SourceAssets](../SourceAssets/) | .blend, .glb, .png, .jpg, .md | Editable artwork and model source records |
| [Services](../Services/) | .js, .mjs, .sql, .toml, .json, .md | Optional leaderboard backend |
| [Tools](../Tools/) | .py, .cmd, .svg, .pdf, .csv, .json, .txt, .md | Modelling helpers, camera experiments, printable material and archive checks |
| [SOURCE_MANIFEST.json](../SOURCE_MANIFEST.json) | .json | File sizes and hashes for the source copy |

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
| November; precise session dates undocumented | Cards, upgrades and playtesting | More choices, revised rules and two rounds of feedback | [05](#05-cards-upgrades-and-playtesting) |
| 19 November snapshot | Scores, goals and review | Layout-sensitive scoring, restart repair and stories | [06](#06-the-19-november-snapshot) |
| 20 November | English archive | Source, history and selected evidence brought together | [07](#07-the-20-november-archive) |
| 21 November | Reading and score sources | Clearer text, contributor highlights and goal progress | [08](#08-the-21-november-interface-work) |
| 22 November | Thesis, ethics and document review | Update the account and disclose unfinished work | [09](#09-the-22-november-review) |
| 22 November | Published record | Source, documentation and delivery versions | [10](#10-published-record) |

## 01 Browser prototype

The browser game began with feeding pigeons. Players later chose where to throw food and watched animals respond.
The question was how a small intervention could change conditions without directly controlling each animal.

Read the [web development account](https://github.com/Ketian-LI/urban-wildlife-simulation/blob/main/docs/WEB_DEMO_DEVELOPMENT.md)
and [commit table](../Misc/process/history/early-web-history.csv).
The first recorded browser build is [22 July](https://github.com/Ketian-LI/urban-wildlife-simulation/commit/8eed0a56abad62e360fa46ff50e127a606d5f6ab).
This is the web stage, not the Unity game.

[Next stage](#02-physical-input-and-city-planning) · [Timeline](#follow-the-project-in-time)

## 02 Physical input and city planning

The Unity experiments explored physical tokens, camera input, placement previews and city routes.
They expanded the choices, but also introduced more rules and input problems.

The [migration record](https://github.com/Ketian-LI/urban-wildlife-tangible-unity/blob/main/docs/GDD_V3_MIGRATION.md)
and [repository lineage](../Misc/process/REPOSITORY_LINEAGE.md) explain that direction.
The [camera guide](../Tools/CameraRecognition/README.md) describes the retained tool.
These records do not establish reliable physical play or a completed camera-based study.
The date and build of the earliest Unity transfer remain partly documented.

[Next stage](#03-room-board-and-research-configurations) · [Timeline](#follow-the-project-in-time)

## 03 Room board and Research configurations

The recorded room-project import is [22 September](https://github.com/Ketian-LI/symbiosis-49-unity/commit/aeca20ab64a41fbfdc36031663db6c1d94acd5c1).
A bounded board made room connections and routes easier to compare.
The work included rearrangement, onboarding and Research configurations.

Read the [Unity history](../Misc/process/history/symbiosis-unity-history.csv) and
[historical design documents](../Misc/archive/legacy-docs).
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
and the [current scene/mode guide](../Misc/technical/SCENES_AND_MODES.md).

[Next stage](#05-cards-upgrades-and-playtesting) · [Timeline](#follow-the-project-in-time)

## 05 Cards upgrades and playtesting

November work developed the illustrated hand, deck area, room upgrades and staged opening.
Feedback raised questions about small text, visual consistency, unclear consequences and repetition.

Playtesting involved 5 people in the first round and 13 in the second.
The original 5 returned, making 13 different people across both rounds.
Exact session dates and executable identifiers are not documented for every participant in the public account.
The [method record](evidence/user-testing/METHOD.md) and [evaluation](EVALUATION.md) distinguish the rounds and their limits.

The [current rules](../Misc/technical/GAME_RULES.md) describe the published source.
They should not be applied backwards to every participant session.

[Next stage](#06-the-19-november-snapshot) · [Timeline](#follow-the-project-in-time)

## 06 The 19 November snapshot

This snapshot brought together stronger layout effects, visible deductions, result review and the repair for disappearing goals.
The journal added short stories based on placements and recorded activity.

Use the [journal's revision table](DEVELOPMENT_JOURNAL.md#november-cards-and-consequences),
the [software-report index](../Misc/evidence/automated-tests/README.md)
and the [StreetJournal release](https://github.com/Ketian-LI/symbiosis-49-unity/releases/tag/archive-2026-11-20).
The release was published on 20 November, but its Windows executable is the 19 November build.

[Next stage](#07-the-20-november-archive) · [Timeline](#follow-the-project-in-time)

## 07 The 20 November archive

The English archive linked the prototypes, project report, development process, selected checks and source inventory.
The source ZIP was prepared below the brief's 1 GB upload limit.
It left the final thesis and approved anonymous research evidence pending.

See the [pinned archive](https://github.com/Ketian-LI/symbiosis-49-unity/tree/archive/2026-11-20),
[release notes](../Misc/process/RELEASE_NOTES.md#archive-snapshot---20-november-2026)
and [assessment requirements](assessment/REQUIREMENTS.md).
This was a published archive, not a Moodle submission.

[Next stage](#08-the-21-november-interface-work) · [Timeline](#follow-the-project-in-time)

## 08 The 21 November interface work

The next source revisions improved text hierarchy and kept score causes available after the animation.
The score-source view highlighted relevant rooms and visible actors.
Further work changed the inside-leaf presentation and three-goal component.

Read the [reading and balance review](../Misc/process/iterations/SKILL_GUIDED_UI_2026-11-21.md),
[score-source record](../Misc/process/iterations/SCORE_INDEX_2026-11-21.md),
[left-page record](../Misc/process/iterations/LEFT_LEAF_STYLE_2026-11-21.md)
and [goal UI record](../Misc/process/iterations/GOAL_PROGRESS_UI_2026-11-21.md).
The [screenshots](media/screenshots/README.md) are labelled Unity test renders.
Later feedback can reject a visual design even when its software checks pass.

[Next stage](#09-the-22-november-review) · [Timeline](#follow-the-project-in-time)

## 09 The 22 November review

The documents were rewritten in shorter English and grouped by purpose.
The main assessment reading was separated from supporting material in [Misc](../Misc/README.md).
A newer local thesis revision added the later study account.
The ethics reflection recorded the missed review, the risks and the actions still unfinished.

Read the [document map](README.md), [thesis status](../Thesis/README.md),
[ethics reflection](ETHICS_REFLECTION.md) and [project status](PROJECT_STATUS.md).
Private consent documents, chats and audio are not published here.
The reflection does not establish approval.

[Next stage](#10-published-record) · [Timeline](#follow-the-project-in-time)

## 10 Published record

The repository contains the source, project account, labelled screenshots and documented development process.
The downloads and published source are different snapshots. The thesis PDF, detailed anonymous study evidence
and final-build demonstration are not included.

The [delivery record](PROJECT_STATUS.md) identifies versions, and the
[submission inventory](assessment/SUBMISSION_CHECKLIST.md) lists the included material.
The [technical guide](../Misc/technical/README.md) explains how to run and rebuild the source.

[Back to folder map](#what-is-in-each-folder) · [Back to timeline](#follow-the-project-in-time)
