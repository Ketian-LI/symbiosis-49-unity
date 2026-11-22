# Optional leaderboard service

This source accompanies the game's HTTP score client. The playable desktop game also supports local rankings.
The current service keeps one best score per public IP, mode and score-rule version. An HMAC digest reduces direct
IP storage, but this remains pseudonymous processing: shared networks share a record. Scores are client-submitted;
this is not an independently verified anti-cheat system.

Supply your own Cloudflare Worker/D1 configuration and server secret in your deployment environment.
No production database, authentication token, secret or participant score export is included here.
The deployed URL is a convenience endpoint, not a dependency for offline play or a promise of long-term hosting.

## Inspect and test locally

Use Node.js 24 (or a compatible runtime with `node:sqlite`) and run `npm ci`, then `npm test` in this directory.
These tests use in-memory SQLite and example identities; they do not write to the production leaderboard.
Copy `wrangler.example.toml` to `wrangler.toml`, create your own D1 database, replace the placeholder ID,
apply the SQL migrations, and configure `IP_HASH_SECRET` and `RESET_TOKEN` as Worker secrets before deploying.
The example is configuration guidance, not a connection to the author's database.
