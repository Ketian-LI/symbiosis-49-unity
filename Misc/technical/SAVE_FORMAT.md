# Construction saves

Construction runs use separate files for each mode under Unity's `Application.persistentDataPath`.

| Data | File |
| --- | --- |
| Fill run | `construction-run-v1.json` |
| Endless run | `construction-endless-v1.json` |
| BoardGame run | `construction-boardgame-v1.json` |
| Local ranking history | `construction-local-leaderboard-v1.json` |

[ConstructionRunModel.Export](../../Project/04-Final/Assets/Scripts/Core/ConstructionRunModel.cs) writes the run data.
`TryRestore` checks it before a run is loaded. Rule-version fields keep earlier runs from silently adopting a new score scale.
The filename suffix alone does not identify every scoring or card-rule version.

[ConstructionRunStore](../../Project/04-Final/Assets/Scripts/Core/ConstructionRunStore.cs) writes a temporary file, then replaces the saved file.
An ordinary save refuses to overwrite unreadable data. A confirmed new run can archive an invalid or mismatched file before
trying again. Failure is reported to the interface.

[ConstructionLocalLeaderboardStore](../../Project/04-Final/Assets/Scripts/Presentation/ConstructionLocalLeaderboardStore.cs) keeps local results
separate from the current run. The optional HTTP ranking service is another system.

The [legacy session schema](../archive/legacy-docs/SAVE_DATA_SCHEMA.md) describes the earlier room-rearrangement project.
Its `session-v2.json` is not the current construction save.
Never include personal saves or ranking exports in a public evidence archive without reviewing their contents.

## Current commuting and recurring records

Valid historical manual-dispatch fields are read and checked, then discarded for future automatic allocation.
Previously settled human results and scores remain intact. The recurring-penalty ledger persists its day, room pair
and amount; duplicate pairs, invalid geometry and malformed amounts are rejected. Saves without the ledger are not
back-charged. [ConstructionRecurringPenalties](../../Project/04-Final/Assets/Scripts/Core/ConstructionRecurringPenalties.cs)
holds those records.
