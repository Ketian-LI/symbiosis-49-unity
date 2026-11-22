# Review of the development conversations

Prepared on 22 November 2026 to support the [design-decision account](../../Documentation/DESIGN_DECISIONS.md)
and [development journal](../../Documentation/DEVELOPMENT_JOURNAL.md).

The review used available project conversations, the recorded source history, current repository files and saved software reports.
Only visible user and assistant messages were considered. Repeated continuation context was deduplicated.
Entries visible only in a conversation directory were not treated as evidence of their message contents.

## From discussion to a public record

Each retained item identifies a design problem, the author's decision, its implementation or historical status,
and supporting files. The [register](records/design-decision-register.csv) records 26 items.
Code and report references were checked against the retained repository. No Unity tests were rerun for this review.

The public account paraphrases design decisions. It does not reproduce conversation transcripts, participant messages,
private forms, account details, local session identifiers or computer paths. The detailed source index remains local.
The existing [ethics reflection](../../Documentation/ETHICS_REFLECTION.md) governs the account of earlier research data;
this review adds no new participant findings.

## Evidence distinctions

| Record | What it establishes | Limit |
| --- | --- | --- |
| Design request | A problem or intended behaviour was stated. | It does not establish implementation. |
| Image proposal | A visual arrangement was explored. | It is not a screenshot of working software. |
| Source file | An implementation is present in the named snapshot. | Its presence alone does not establish visual acceptance or usability. |
| Saved test report | The named checks produced the recorded results. | Reports can overlap and failures remain failures. |
| Playtesting account | A method and observations are described within the stated scope. | Software checks cannot substitute for participant evidence or ethics review. |
| Release record | A particular source or executable was packaged. | Later source changes are not automatically included in that download. |

## Dates and versions

A conversation's creation date, a visible message timestamp and a feature's implementation date can differ.
Continuation records may repeat earlier messages. Retrospective November explanations of earlier choices are therefore
labelled as retrospective; stage dates use the source history where available.

Earlier suggestions about assessment format were checked against the supplied unit brief rather than treated as requirements.
The current [requirements map](../../Documentation/assessment/REQUIREMENTS.md) records that brief.
The [delivery record](../../Documentation/PROJECT_STATUS.md) identifies the retained source and older downloads.
