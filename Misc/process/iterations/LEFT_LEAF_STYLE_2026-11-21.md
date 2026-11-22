# Left-leaf visual alignment, 21 November 2026

The left construction leaf uses the oak-paper artwork already used by the right leaf. The printed interior in the opening
scene uses the same source, so its paper does not switch back to the city illustration during the transition.
[ConstructionCasePaper.cs](../../../Assets/Scripts/Presentation/ConstructionCasePaper.cs) owns the interior resource path.
The exterior lid artwork remains a separate asset.

The journal heading uses the same 23-unit reading role as the right score heading. Its date and previous-story line use
16 units; body text uses 18 in both languages. Story and goal sections share the right-side 288-unit content width and quiet
brass rules. The journal's vertical colour stripe becomes a horizontal rule, with rust retained for a concern. The story
background is transparent, and a narrower paragraph leaves room for the printed oak branches. The goal section keeps the
reading-paper backing used by the right-side recent-cause panel.

Story content, three goal rows and progress remain live UI. Tutorial headings restore their compact layout when the journal
is hidden. This revision changes presentation; it does not alter simulation or score rules.

The selected interface batch passes 21/21 checks, including actual bilingual SDF rendering, goals/restart, hand and score-source
interactions. A separate live navigation check passes 1/1, including the shared paper in both printed interiors and live leaves,
plus the menu/game return flow. Batches overlap earlier exports and should not be added into a historical unique-test count.

Two older opening checks fail with this revision and with the pre-change presentation and original test files. One observes
the case still opening after a fixed 1.4-second wait; the other compares a closed-case tree scale of zero with its restored
scale. These inherited failures remain unresolved here and are retained in the
[pre-change report](../../evidence/automated-tests/left-leaf-baseline-opening.xml) and
[initial revision report](../../evidence/automated-tests/left-leaf-legacy-opening-after.xml). They are not counted as passing checks.

Evidence: [interface report](../../evidence/automated-tests/left-leaf-final.xml),
[opening report](../../evidence/automated-tests/left-leaf-opening.xml),
[Chinese render](../../../Documentation/media/screenshots/game-readable-zh.png),
[English render](../../../Documentation/media/screenshots/game-readable-en.png).
The renders show synthetic Unity fixtures. No new raster art or executable build was produced in this source revision.
