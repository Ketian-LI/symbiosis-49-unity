# Trophy bands and reading update, 20 November 2026

New Fill games use trophy rule set 2. Gameplay scoring remains V13.

| Trophy | Final total |
| --- | --- |
| Plastic | Below 1500 |
| Iron | 1500–2499 |
| Bronze | 2500–3499 |
| Silver | 3500–4499 |
| Gold | 4500 or more |

`awardRulesVersion` is saved with the run and local ranking payload. Missing metadata keeps the historical bands;
resuming an older game does not silently reclassify its trophy. The game manual, result ranges and next-tier calculation
read the same award model.

## Calibration and limits

36 complete automated Fill runs compare three placement policies over 12 seeds each, with the same eight-room opening.
The policies minimise pairing quality, choose random legal plots, or favour connected services. They are diagnostic
samples, not participant evidence, optimal-play solvers or a claim about the player population.

Unfavourable-edge totals span -251 to 1899 (median 542.5); random legal totals span 1683 to 2838 (median 2485.5);
connected-service totals span 3538 to 4703 (median 4075). The new bands put most of the connected-service samples in silver,
with gold requiring a stronger result. Future player feedback may justify another revision.

## Reading and wording

The [Humanizer skill](https://github.com/blader/humanizer/blob/main/SKILL.md), read at blob
`bc51a89764e96c0178c707a2a7158d8dca4e6920`, informed the prose edit. Its recommendations were applied to supported
in-game text: shorter stories, direct causes, fewer repeated instructions, and clearer action labels. No AI-detection
or authorship claim follows from this edit. The skill reference was read for this task; it was not bundled in the game.

The tabletop art remains in place. Story body text is 18 reference pixels in Chinese and 17 in English, causes are 16,
and manual body text is 19. Card footers use larger lettering within the existing card scale. Metadata has its own size.
These are canvas units, not a claim that all on-screen text is at least 18 physical pixels at every resolution.

Text boxes allow for the real SDF font line height, including dates, card names, deck counts and the swap button.
The three goal rows have more space; the numerical progress stays visible without repeating a progress bar under each row.
The selected-card area hides its empty card frame while waiting for a choice. Wider text gutters and shorter wording keep
the right-hand text away from the printed foliage.

## Save regression found during verification

A patch-mode pigeon reference can point to a later green food source while the arrival day records the original arrival.
The old restore check required that later source to exist on the original arrival day and could reject a valid full-board save.
The revision checks historical habitat capacity at the arrival day and the saved green reference at the current day.
Invalid early arrivals and non-green references still fail validation. A synthetic failing save is retained as a regression
fixture. Runtime movement and scoring are unchanged; all 36 calibration totals match the pre-fix sample.

## Verification boundary

Exported EditMode/PlayMode reports and actual Unity captures are retained in the task work folder. Batches may overlap and
must not be added into a unique-test count. The captures show a fixed synthetic layout, not a participant play session.
This change updates the Unity source project; previous release ZIPs remain snapshots of their own builds.
