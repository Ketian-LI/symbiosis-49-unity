# Ethics and limitations

## Representing urban wildlife

The game reduces food, movement, habitat and risk to a small set of rules. The designer chooses which species appear and
which outcomes earn points. A high score is a result within this game, not a judgement about a real city's ecology.

Combining resident and animal points can hide competing needs. The interface shows separate contributions and explains
causes so players can inspect the assumptions. A short story can make an event approachable, but it can also make a complex
relationship seem simple. Whether players understand these limits needs user research.

## Participant records

The 20 November ethics and consent documents are drafts. They state that ethics approval had not yet been obtained and
that earlier feedback needs review. No approval record has been confirmed for this public archive.

Before earlier feedback is analysed or quoted further, the author needs to confirm its permitted use with the supervisor.
Participant identities, chats, audio, contact details and signed forms stay outside this repository.
Only approved, de-identified extracts belong in the [user-testing evidence](evidence/user-testing/README.md).
Signed forms and a private code key should not be published as evidence of consent.

## The optional leaderboard

The service derives an identity from a public IP using an HMAC digest. People on a shared network can share a record.
This is pseudonymous processing, not guaranteed anonymity. The server receives a chosen name and score from the client;
it does not replay the run to prove the score. Offline play and local rankings work without this service.

## Practical limits

Selected checks cover English/Chinese text, keyboard naming, pointer targets and result review.
A full accessibility study has not been completed. The camera prototype has limited physical evidence.
The macOS download is source-only, and no Mac runtime or signing result is claimed.

The [evaluation](EVALUATION.md) records the limits of user feedback and software checks.
The [asset record](ASSET_PROVENANCE.md) separates generated, imported and editable materials.
