# Automatic commuting and linked terms, 22 November 2026

Resident destination selection is now automatic in live construction runs. The manual dispatch button and editor are no
longer built, so selecting a home cannot place an action over the three goals. Tutorial copy and the manual describe the
automatic behaviour. The previously ambiguous task title is now **Commuting / 通勤**.

## What automatic planning optimizes

The existing capacity-aware flow allocator first maximizes the number of residents with both a reachable job and meal.
Scarce places rotate between households. For the selected group it improves the existing satisfaction/service choices;
reachable route lengths and deterministic ranks resolve choices. Remaining jobs and seats are now also matched across
all unserved households, rather than letting a flexible household greedily take a constrained household's only destination.
This preserves route legality, capacity, closures, blocked homes and departed residents. Actual animation, forecasts and
settlement use the same daily result. Players still decide the layout; automation cannot invent connectivity or capacity.

The maximum completed-routine count is a flow objective within this game's legal route model. Secondary satisfaction
refinement is bounded; it is not a proof of globally maximal total score or a globally shortest multi-stop tour. The route
retains the existing checked home/street/service paths and return home. The score formula and trophy thresholds are unchanged.

Selecting a residence shows its current planned workplace and meal service, with board coordinates and whether a complete
commute is possible. Missing legs distinguish disconnected services from full/closed service capacity. The old dispatch
API is inert in live runs. Valid historical dispatch fields are read and checked but discarded for future allocation;
historical settled results and scores are retained. The legacy direct settlement helper remains for historical compatibility
checks, but every live run preview and settlement passes no manual reservations. The JSON field remains compatible.

## Terms that behave like links

Muted green underlined terms on paper, and light gold on dark guidance, provide a consistent reference action:

1. Hover a linked word for a short meaning. In the manual the explanation appears beside the page, leaving its text readable.
2. Click it to open the manual at that term's actual heading. The heading is marked and placed near the viewport top.
3. Follow linked terms within the explanation to another entry. Scroll, page buttons and O retain their existing behaviour.

The links are present in the manual, tutorials, story/trigger copy, relevant placement/card/room explanations and the
commuting goal title. Only linked glyphs take pointer input; ordinary copy and whitespace do not intercept underlying
controls or the ScrollRect. Card/action button labels retain their own action. Opening a definition over goal review returns
to that review without changing goals. View-only interactions change no run JSON. Language changes retain term identity
and rebuild the correct manual location.

| ID | English | Chinese | Definition entry |
| --- | --- | --- | --- |
| oak | Central oak | 中央橡树、橡树 | Opening habitat and central placement |
| home | Home, residence | 住宅 | One resident and automatic route inspection |
| workshop | Workshop | 工作坊 | Two jobs |
| restaurant | Restaurant | 餐厅 | Job, meal seats and terrace capacity |
| supermarket | Supermarket, market | 超市 | Jobs, seats and actual dining leftovers |
| street | Street | 街道、道路 | Shared-edge connectivity |
| square | Square, plaza | 广场 | Visits, temporary food and traffic |
| waste | Waste room | 垃圾间、垃圾站 | Collection coverage and bad residential pairing |
| green | Green habitat/patch | 绿地 | Habitat, maturity, food and planting type |
| commute | Commuting | 通勤、完整循环 | Work, meal and return, with automatic capacity allocation |

Each room now has its own manual entry, replacing grouped restaurant/workshop/market and street/square headings. Existing
links/markup are preserved without nesting links or changing plain source copy. These are the initial defined terms, not
a claim that every word or UI label is interactive.

## Evidence and remaining work

[72 selected EditMode checks](../Documentation/evidence/automatic-routes-terms-editmode.xml) and
[31 selected PlayMode checks](../Documentation/evidence/automatic-routes-terms-playmode.xml) passed. They include constrained
capacity matching, legacy dispatch migration/retirement, prior resident rules, stock/save checks, bilingual term hit-testing,
hover explanations, exact anchor visibility for all ten entries, room hints, modal ownership, score batches and existing
restart/goal/navigation controls. Obsolete live manual-dispatch acceptance checks were replaced with retirement, read-only
inspection and historical migration checks. A new allocation fixture initially attempted a service before its prerequisite;
it was corrected to use the board's explicit fixture/replay placement path. The screenshot helper changes camera/canvas
geometry; pointer coordinates are recalculated after capture before testing another click.

Final checks used an isolated Unity copy. Unrelated reference/journal and recurring-penalty work in another copy was excluded;
it has not been silently treated as part of this passing batch. Reports overlap earlier checks and are neither cumulative
unique-test totals nor participant findings. Earlier opening-test limitations remain in the evaluation history.

Actual Unity fixture renders:

- [Hover meaning, Chinese](../Documentation/images/term-hover-zh.png) / [English](../Documentation/images/term-hover-en.png)
- [Restaurant manual destination, Chinese](../Documentation/images/manual-restaurant-zh.png) / [English](../Documentation/images/manual-restaurant-en.png)
- [Automatic residence plan, Chinese](../Documentation/images/automatic-commute-zh.png) / [English](../Documentation/images/automatic-commute-en.png)

No new artwork or executable was generated for this revision. Recurring deductions, building/commuting diagrams and the
separate journal remain outside this delivery. See [pending work](PENDING_REQUESTS_2026-11-22.md).

Capture isolation: navigation checks can leave an unrelated legacy town alive. The native capture helper temporarily isolates unrelated room/actor renderers and canvases while retaining the shared case and desk, then restores them. This prevents stale room artwork behind a nine-room model. The 31 interface checks were rerun after this capture-only correction; gameplay rendering is unchanged.
