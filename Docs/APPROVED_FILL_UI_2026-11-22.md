# Approved Fill interface, 22 November 2026

The approved six-function concept set has been applied to the Fill interface. This revision follows the earlier
[highlight delivery](ROOM_HIGHLIGHTS_2026-11-22.md); it does not implement the remaining recurring-penalty or glossary requests.

## Reading layout

The left leaf again uses town paper, with the town kept below the reading area. Street journal and date share a quiet header;
one story title leads into ordinary body text and an emphasized trigger. The former duplicate heading, story event icon,
previous-story line and lower-right changes block are not painted. Three compact goal rows show room-card artwork, counts
and thin progress tracks on the continuous page. The commute goal retains paired home/work artwork. Goal identity and
bookmark behaviour after restart or tutorial skip are retained.

The right leaf separates total score, three equal score columns, trophy distance, the full-width **Score sources · H** row
and the selected card. **Manual · O** remains visible. Guidance selectively emphasizes actions, targets and confirmation;
it does not make the whole paragraph bold. The legacy Text remains the plain data source and its SDF bridge renders emphasis.
Long room explanations omit the illustration when needed. Room selection uses the same continuous paper as the card preview.

The two v02 paper textures are new generated adaptations of the project's existing city and oak prints. They reserve clean
reading space without stretching the illustrated buildings or leaves. Both the 3D opening lids and live leaf UI reference the
same textures. The [asset record and exact prompts](FILL_PAPER_ASSETS_2026-11-22.json) distinguish generated backgrounds
from the native Unity fixture screenshots below. No generated text or game controls are baked into these backgrounds.

The existing card palette is retained: paper `#F5DDB0`, ink `#19323D`, quiet brass `#C9AD7D`. It is paired with the existing
green, blue and rust relationship colours, not a new scoring rule.

## Live remaining-card catalogue

Click the draw pile to open a centred lower-board catalogue. It leaves both information leaves readable. It shows each
category's remaining / initial count and the actual deck / held / built totals. Green planting types share one supply.
Tutorial queue cards are separate from ordinary held stock, so they are not counted twice. Swapping returns the old card to
the pile. Legacy random-deck saves explicitly say random rather than inventing a finite count. Clicking outside, the close
button or Esc dismisses the catalogue; opening the manual also closes it. It blocks board and score-source input while open.
Viewing it changes no saved state. Three-, four- and five-card tray behaviour remains covered by the existing hand checks.

## Selection and settlement

The previous resident -> animal -> shared 1/3, 2/3, 3/3 animation is retained, with each participant batch cleared before the
next. Selection and H review wait for animation/modal ownership. Clicking a built waste room pins blue collection coverage
and separate red frames on shared-edge homes at the same time. A diagonal home can be covered without being a bad pairing.
Recorded blue selection relations now follow resident/animal entries, excluding build-pairing and shared aggregate entries;
green and red pairing frames already communicate those direct relations. Repeated viewing neither adds nor deducts points.
Pairing explanations still identify the **building-time** deduction. Recurring deductions are pending.

## Verification and limits

[38 selected EditMode checks](../Documentation/evidence/approved-fill-ui-editmode.xml) and
[27 selected PlayMode checks](../Documentation/evidence/approved-fill-ui-playmode.xml) passed. They cover real stock
conservation through tutorial/swap/restore, random-save compatibility, non-nested emphasis, bilingual SDF text, native Fill
selection/coverage/warnings, modal input, restart/skip goals, existing score batches and exact menu-to-game navigation.
These overlap prior batches and are not cumulative unique-test or participant totals. Initial checks found clipped counter
and English category text; both were corrected before the passing exports. One empty-board fixture initially omitted the
explicit tutorial skip, so it tested the tutorial view rather than the requested post-skip view; it was corrected to exercise
that actual state. Older opening-test limitations remain in the evaluation history; the exact navigation check passes here.

Actual Unity renders of fixed Fill fixtures:

- [Journal, goals and hand](../Documentation/images/fill-approved-zh.png)
- [Remaining-card catalogue, Chinese](../Documentation/images/fill-deck-zh.png) / [English](../Documentation/images/fill-deck-en.png)
- [Pinned waste coverage and warning, Chinese](../Documentation/images/fill-room-selection-zh.png) / [English](../Documentation/images/fill-room-selection-en.png)
- [Empty board after tutorial skip](../Documentation/images/fill-opening-zh.png)
- [Resident batch](../Documentation/images/score-batch-resident.png), [animal batch](../Documentation/images/score-batch-animal.png), [shared batch](../Documentation/images/score-batch-shared.png)

This is a source and asset revision, not a newly exported Windows or macOS build. No trophy threshold, score formula,
save format, recurring deduction or participant finding is changed. The remaining requests are listed separately in the
[pending work](PENDING_REQUESTS_2026-11-22.md).
