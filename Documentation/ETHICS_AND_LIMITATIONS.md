# Ethics and limitations

## Representing urban wildlife

The game reduces food, movement, habitat and risk to a small set of rules. The designer chooses which species appear and
which outcomes earn points. A high score is a result within this game, not a judgement about a real city's ecology.

Combining resident and animal points can hide competing needs. The interface shows separate contributions and explains
causes so players can inspect the assumptions. A short story can make an event approachable, but it can also make a complex
relationship seem simple. Whether players understand these limits needs user research.

## Participant records

Playtests and feedback collection began before formal ethics review was completed. On 22 November, the author confirmed
that the application had not yet been sent to the supervisor. A pause on further playtesting, transfer to UAL OneDrive and
handling of old records held through AI or leaderboard services had not been completed.

The [ethics reflection](ETHICS_REFLECTION.md) explains the oversight, risks and lessons. Preparing forms and explaining the
problem do not establish approval or resolve the remaining data-handling work.

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
