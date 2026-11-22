# Automatic commuting, linked terms and continuous consequences, 22 November 2026

This source revision follows the [approved Fill interface](APPROVED_FILL_UI_2026-11-22.md). The author requested automatic
resident routes, linked terms, recurring bad-edge deductions, building examples on P and an independent journal on I.

## Automatic commuting

Live construction no longer builds a manual dispatch button or editor. Clicking a home shows its automatic workplace,
meal service, coordinates and any missing leg. The task previously called Out and home again is now **Commuting / 通勤**.

The capacity-aware flow allocator maximizes complete work-and-meal routines within legal routes. Scarce places rotate.
Within that policy it refines satisfaction/service choices; remaining jobs and seats are also matched globally so a flexible
household cannot greedily consume a constrained household's only destination. Blocked/departed homes, unavailable services
and job/seat capacities remain binding. Secondary satisfaction refinement is bounded, not a proof of globally maximum total
score. After destinations are fixed, each leg of work -> meal -> optional square -> home uses a shortest shared-edge route
through streets and that resident's own home. It no longer inserts a return home between every two destinations. This is
shortest routing for that fixed stop order, not a general travelling-salesperson or total-score optimum.

Historical dispatch records are validated on load, then discarded for future allocation. Historical human results and earned
scores remain intact. Compatibility setter/editor methods are inert. The lower-level historical human-model dispatch input
remains for legacy regression checks but is not used by live runs. Forecasts, actual settlement and animation use the same
daily result. Players still decide whether enough connected services exist.

## Linked terms

Ten terms have stable manual anchors: central oak, home, workshop, restaurant, supermarket, street, square, waste room,
green habitat and commuting. Hover shows a short definition; clicking opens the manual, scrolls to the corresponding real
heading and marks it. Manual explanations cross-link the same terms. Shared workshop/restaurant/market and street/square
entries were split so each room has its own destination. The [term map](TERM_INDEX_2026-11-22.md) lists coverage.

TMP link hit testing only takes linked glyphs. Whitespace and ordinary prose retain underlying card/scroll behaviour; input
fields and button labels retain their own actions. Light copy uses brass links and paper copy uses muted green links. This
does not make every word in every historical UI interactive. English and Chinese anchors refer to the same concept IDs.

## Recurring bad-edge deductions in Fill

An introducing placement still pays its existing negative pairing once. Starting with the next confirmed placement settlement,
each existing bad shared edge pays again. Current layout rules deduct **80 per waste/home shared edge** and **40 per
street/green shared edge**, including slow streets. The introduction is not charged twice. Two homes beside the same waste
room are two edges. Diagonal homes can be covered by cleanup without paying this shared-edge penalty. Cleanup does not
remove residential disturbance.

The separate recurring ledger records settlement day, both tile IDs and the amount. It contributes to building and overall
totals, final results, source inspection and visible deduction cues. Reopening a panel, choosing a card, previewing a route,
loading a save or repeating a rejected settlement cannot add a charge. Historical saves have no retroactive charges: future
eligible settlements begin recording deductions. Existing recorded entries round-trip; malformed amounts, duplicate pairs
and invalid geometry are rejected. This changes future Fill scoring, so earlier balance examples and archived rankings are
historical evidence rather than directly comparable benchmarks. Trophy thresholds have not been recalibrated in this pass.
Endless retains its existing building-time pairing rule.

The left journal calls out a recurring deduction; H identifies the recorded cause and related rooms. Blue waste coverage and
red shared-edge home frames can coexist. These views read the ledger and do not modify it.

## P examples and I journal

P toggles a three-page rules reference:

1. Home -> work -> meal -> home, shared-edge street links and a disconnected diagonal example.
2. Waste cleanup coverage versus residential shared-edge disturbance, repeated deduction and street separation.
3. Connected-green levels, garden homes, terrace restaurants and slow streets; level and maturity are distinguished.

I toggles a separate scrolling settlement journal, newest first. It lists placed rooms, initial bad edges, repeated deductions
with both coordinates, construction-triggered upgrades, observed feeding and completed commutes. It is reconstructed from
recorded immutable placements and settled records, not predicted activity. Journal terms link to the manual. P/I switch
panels, Esc closes them and the input shield prevents board placement underneath. O opens the manual. Name typing retains
keyboard ownership. A destroyed name-field selection is now treated as no focus instead of causing an Update exception.

## Road markers

Zero cumulative road deaths no longer paint a death-warning badge. Fill uses one current traffic-risk issue icon rather than
also showing a duplicate roadside-exposure badge. An actual nonzero death count remains available. This fixes the verified
zero-value/repeated-warning cases; a different unspecified road-symbol defect needs a concrete location to identify it.

## Verification

[77 selected EditMode checks](../Documentation/evidence/commuting-reference-penalties-editmode.xml) and
[32 selected PlayMode checks](../Documentation/evidence/commuting-reference-penalties-playmode.xml) passed. Checks include
constrained assignment, shortest legal itinerary, legacy dispatch migration, repeated deductions without duplicate charges,
negative-ledger restore/rejection, bilingual word hit tests and anchor visibility, P/I reference layout and modal ownership,
zero-death markers, previous score batches, restart/skip goals and exact menu navigation. Four additional targeted UI/capture checks passed after final presentation adjustments. These overlap previous batches;
they are not cumulative unique-test totals or participant findings. Older opening-test limitations remain in the evaluation
history. Early initialization/focus faults and invalid test setup/wait assumptions were corrected before these exports.
The source copy used for tests is checked against the delivered scripts/tests. Native screenshots use a temporary camera
canvas sorting override so world actors remain behind opaque UI, as in the normal screen-overlay presentation.

Native Unity fixtures, shown separately by function:

- [Automatic commute inspection](../Documentation/images/automatic-commute-zh.png)
- [Hover definition](../Documentation/images/term-hover-zh.png) and [marked manual destination](../Documentation/images/manual-restaurant-zh.png)
- [Commuting/streets](../Documentation/images/building-example-1-zh.png), [cleanup/deductions](../Documentation/images/building-example-2-zh.png), [upgrades](../Documentation/images/building-example-3-zh.png)
- [Independent journal](../Documentation/images/independent-journal-zh.png)

This is a source revision. Windows/macOS executables were not rebuilt. No new raster art was generated; diagrams use the
existing room illustrations and native UI. Research findings, final thesis status and participant materials are unchanged.
