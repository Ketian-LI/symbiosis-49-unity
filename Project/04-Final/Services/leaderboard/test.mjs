import test from "node:test";
import assert from "node:assert/strict";
import { DatabaseSync } from "node:sqlite";
import { readFile } from "node:fs/promises";
import worker from "./src/index.js";

const migration = await readFile(new URL("./migrations/0001_community_scores.sql", import.meta.url), "utf8");
const namesMigration = await readFile(new URL("./migrations/0002_display_names.sql", import.meta.url), "utf8");

function fixture() {
  const sqlite = new DatabaseSync(":memory:");
  sqlite.exec(migration);
  sqlite.exec(namesMigration);
  const DB = { prepare(sql) {
    const statement = sqlite.prepare(sql);
    return { run: async () => statement.run(), bind(...params) { return {
      first: async () => statement.get(...params) ?? null,
      all: async () => ({ results: statement.all(...params) }),
      run: async () => statement.run(...params),
    }; } };
  } };
  return { sqlite, DB, IP_HASH_SECRET: "i".repeat(40),
    RESET_TOKEN: "r".repeat(40) };
}

async function send(env, method, path, body, ip = "203.0.113.7", headers = {}) {
  return worker.fetch(new Request("https://scores.example.test" + path, {
    method, headers: { "cf-connecting-ip": ip,
      ...(body === undefined ? {} : { "content-type": "application/json" }),
      ...headers },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  }), env);
}

function score(value, mode = "fill", version = 8, rules = 0, displayName) {
  return { mode, scoringVersion: version, boardRulesVersion: rules, score: value,
    ...(displayName === undefined ? {} : { displayName }) };
}

test("one IP keeps only its best final number; IP is not returned", async () => {
  const env = fixture();
  for (const value of [12, 9, 23])
    assert.equal((await send(env, "POST", "/api/v2/scores", score(value))).status, 200);
  assert.equal((await send(env, "POST", "/api/v2/scores", score(17), "198.51.100.8")).status, 200);
  const response = await send(env, "GET", "/api/v2/scores?mode=fill&version=8&rules=0");
  assert.deepEqual(await response.json(), { scores: [23, 17],
    entries: [{ name: "Player", score: 23 },
      { name: "Player", score: 17 }], count: 2 });
  assert.equal(env.sqlite.prepare("SELECT COUNT(*) AS n FROM community_scores").get().n, 2);
  const row = env.sqlite.prepare("SELECT * FROM community_scores LIMIT 1").get();
  assert.ok(!JSON.stringify(row).includes("203.0.113.7"));
  assert.match(row.ip_digest, /^[0-9a-f]{64}$/);
  env.sqlite.close();
});

test("mode, scoring version and board rules have separate boards", async () => {
  const env = fixture();
  await send(env, "POST", "/api/v2/scores", score(10));
  await send(env, "POST", "/api/v2/scores", score(20, "fill", 9));
  await send(env, "POST", "/api/v2/scores", score(30, "boardgame", 8, 2));
  assert.deepEqual(await (await send(env, "GET", "/api/v2/scores?mode=fill&version=8&rules=0")).json(),
    { scores: [10], entries: [{ name: "Player", score: 10 }], count: 1 });
  assert.deepEqual(await (await send(env, "GET", "/api/v2/scores?mode=boardgame&version=8&rules=2")).json(),
    { scores: [30], entries: [{ name: "Player", score: 30 }], count: 1 });
  env.sqlite.close();
});

test("a lower score can rename the same IP across modes without lowering its best score", async () => {
  const env = fixture();
  await send(env, "POST", "/api/v2/scores", score(42, "fill", 8, 0, "小鹿"));
  await send(env, "POST", "/api/v2/scores", score(18, "boardgame", 8, 2, "小鹿"));
  const renamed = await send(env, "POST", "/api/v2/scores",
    score(9, "fill", 8, 0, "橡树 7"));
  assert.deepEqual(await renamed.json(), { score: 42, rank: 1, name: "橡树 7" });
  assert.deepEqual(env.sqlite.prepare("SELECT best_score, display_name FROM community_scores ORDER BY mode").all().map(row => ({ ...row })),
    [{ best_score: 18, display_name: "橡树 7" },
      { best_score: 42, display_name: "橡树 7" }]);
  const other = await send(env, "POST", "/api/v2/scores",
    score(70, "fill", 8, 0, "另一位"), "198.51.100.8");
  assert.equal(other.status, 200);
  const list = await (await send(env, "GET", "/api/v2/scores?mode=fill&version=8&rules=0")).json();
  assert.deepEqual(list.entries, [{ name: "另一位", score: 70 },
    { name: "橡树 7", score: 42 }]);
  env.sqlite.close();
});

test("old score submissions keep a previously chosen name; invalid names are rejected", async () => {
  const env = fixture();
  await send(env, "POST", "/api/v2/scores", score(10, "fill", 8, 0, "小鹿"));
  const legacy = await send(env, "POST", "/api/v2/scores", score(20));
  assert.deepEqual(await legacy.json(), { score: 20, rank: 1, name: "小鹿" });
  for (const name of ["", "a  b", "<b>", "a".repeat(17)])
    assert.equal((await send(env, "POST", "/api/v2/scores",
      score(30, "fill", 8, 0, name))).status, 422);
  assert.equal(env.sqlite.prepare("SELECT best_score FROM community_scores").get().best_score, 20);
  env.sqlite.close();
});

test("invalid submissions and unauthorized resets do not alter scores", async () => {
  const env = fixture();
  assert.equal((await send(env, "POST", "/api/v2/scores", score(10))).status, 200);
  assert.equal((await send(env, "POST", "/api/v2/scores", { ...score(50), nickname: "x" })).status, 422);
  assert.equal((await send(env, "POST", "/api/v2/admin/reset",
    { confirm: "CLEAR_ALL_RANKINGS" })).status, 401);
  assert.equal((await send(env, "POST", "/api/v2/admin/reset",
    { confirm: "wrong" }, undefined,
    { authorization: "Bearer " + env.RESET_TOKEN })).status, 400);
  assert.equal(env.sqlite.prepare("SELECT COUNT(*) AS n FROM community_scores").get().n, 1);
  assert.equal((await send(env, "POST", "/api/v2/admin/reset",
    { confirm: "CLEAR_ALL_RANKINGS" }, undefined,
    { authorization: "Bearer " + env.RESET_TOKEN })).status, 200);
  assert.equal(env.sqlite.prepare("SELECT COUNT(*) AS n FROM community_scores").get().n, 0);
  env.sqlite.close();
});

test("only approved website origins receive read CORS headers", async () => {
  const env = fixture();
  const allowed = await send(env, "GET", "/api/v2/scores",
    undefined, undefined, { origin: "https://likaikai.net" });
  assert.equal(allowed.headers.get("access-control-allow-origin"), "https://likaikai.net");
  const other = await send(env, "GET", "/api/v2/scores",
    undefined, undefined, { origin: "https://other.example" });
  assert.equal(other.headers.get("access-control-allow-origin"), null);
  env.sqlite.close();
});
