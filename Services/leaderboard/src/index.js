// Standalone Cloudflare Worker. Finished scores and optional display names only.
const MODES = new Set(["fill", "boardgame", "endless"]);
const READ_ORIGINS = new Set(["https://likaikai.net", "https://www.likaikai.net"]);

function reply(data, status = 200) {
  return new Response(JSON.stringify(data), { status, headers: {
    "content-type": "application/json; charset=utf-8",
    "cache-control": "no-store",
    "x-content-type-options": "nosniff",
  } });
}

function withReadCors(response, request) {
  const origin = request.headers.get("origin");
  if (request.method === "GET" && READ_ORIGINS.has(origin)) {
    response.headers.set("access-control-allow-origin", origin);
    response.headers.set("vary", "Origin");
  }
  return response;
}

function validateScore(input) {
  if (!input || typeof input !== "object" || Array.isArray(input) ||
      !["boardRulesVersion,mode,score,scoringVersion",
        "boardRulesVersion,displayName,mode,score,scoringVersion"]
        .includes(Object.keys(input).sort().join(","))) return null;
  const mode = typeof input.mode === "string" ? input.mode.toLowerCase() : "";
  const version = input.scoringVersion;
  const rules = input.boardRulesVersion;
  const score = input.score;
  const displayName = Object.hasOwn(input, "displayName")
    ? normalizeName(input.displayName) : null;
  if (!MODES.has(mode) || !Number.isSafeInteger(version) ||
      version < 8 || version > 99 || !Number.isSafeInteger(rules) ||
      (mode === "boardgame" ? rules < 1 || rules > 10 : rules !== 0) ||
      !Number.isSafeInteger(score) || score < -10000000 ||
      score > 10000000 ||
      (Object.hasOwn(input, "displayName") && !displayName)) return null;
  return { mode, version, rules, score, displayName };
}

function normalizeName(value) {
  if (typeof value !== "string") return null;
  const name = value.trim();
  return /^[A-Za-z0-9_. \u4e00-\u9fff-]{1,16}$/u.test(name) &&
    !name.includes("  ") ? name : null;
}

async function readJson(request) {
  const limit = 4096;
  const declared = Number(request.headers.get("content-length"));
  if (Number.isFinite(declared) && declared > limit) throw new Error("too_large");
  if (!request.body) throw new Error("invalid_json");
  const reader = request.body.getReader();
  const chunks = [];
  let length = 0;
  try {
    while (true) {
      const part = await reader.read();
      if (part.done) break;
      length += part.value.byteLength;
      if (length > limit) { await reader.cancel(); throw new Error("too_large"); }
      chunks.push(part.value);
    }
  } finally { reader.releaseLock(); }
  const bytes = new Uint8Array(length);
  let offset = 0;
  for (const chunk of chunks) { bytes.set(chunk, offset); offset += chunk.byteLength; }
  try { return JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(bytes)); }
  catch { throw new Error("invalid_json"); }
}

async function ipDigest(request, secret) {
  const ip = request.headers.get("cf-connecting-ip");
  if (!ip || ip.length > 64 || !secret || secret.length < 32) return null;
  const encoder = new TextEncoder();
  const key = await crypto.subtle.importKey("raw", encoder.encode(secret),
    { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
  const bytes = new Uint8Array(await crypto.subtle.sign("HMAC", key,
    encoder.encode(ip)));
  return Array.from(bytes, value => value.toString(16).padStart(2, "0")).join("");
}

async function submitScore(request, env) {
  if (!request.headers.get("content-type")?.toLowerCase().startsWith("application/json"))
    return reply({ error: "unsupported_media_type" }, 415);
  const digest = await ipDigest(request, env.IP_HASH_SECRET);
  if (!digest) return reply({ error: "identity_unavailable" }, 503);
  let input;
  try { input = await readJson(request); }
  catch (error) { return reply({ error: error.message }, error.message === "too_large" ? 413 : 400); }
  const entry = validateScore(input);
  if (!entry) return reply({ error: "invalid_score" }, 422);
  await env.DB.prepare("INSERT INTO community_scores " +
    "(mode, scoring_version, board_rules_version, ip_digest, best_score, submitted_at_ms, display_name) " +
    "VALUES (?, ?, ?, ?, ?, ?, ?) ON CONFLICT " +
    "(mode, scoring_version, board_rules_version, ip_digest) DO UPDATE SET " +
    "best_score = excluded.best_score, submitted_at_ms = excluded.submitted_at_ms " +
    "WHERE excluded.best_score > community_scores.best_score").bind(
      entry.mode, entry.version, entry.rules, digest, entry.score, Date.now(),
      entry.displayName ?? "Player").run();
  if (entry.displayName)
    await env.DB.prepare("UPDATE community_scores SET display_name = ? " +
      "WHERE ip_digest = ? AND display_name <> ?").bind(
        entry.displayName, digest, entry.displayName).run();
  const saved = await env.DB.prepare("SELECT best_score, display_name FROM community_scores " +
    "WHERE mode = ? AND scoring_version = ? AND board_rules_version = ? " +
    "AND ip_digest = ?").bind(entry.mode, entry.version, entry.rules, digest).first();
  const rank = await env.DB.prepare("SELECT COUNT(*) + 1 AS rank FROM community_scores " +
    "WHERE mode = ? AND scoring_version = ? AND board_rules_version = ? " +
    "AND best_score > ?").bind(entry.mode, entry.version, entry.rules,
      saved.best_score).first();
  return reply({ score: saved.best_score, rank: rank.rank,
    name: saved.display_name });
}

async function listScores(url, db) {
  const mode = (url.searchParams.get("mode") || "fill").toLowerCase();
  const version = Number(url.searchParams.get("version") || "8");
  const rules = Number(url.searchParams.get("rules") ||
    (mode === "boardgame" ? "2" : "0"));
  if (!validateScore({ mode, scoringVersion: version,
    boardRulesVersion: rules, score: 0 }))
    return reply({ error: "invalid_query" }, 400);
  const rows = await db.prepare("SELECT best_score, display_name FROM community_scores " +
    "WHERE mode = ? AND scoring_version = ? AND board_rules_version = ? " +
    "ORDER BY best_score DESC, submitted_at_ms ASC LIMIT 20").bind(
      mode, version, rules).all();
  const count = await db.prepare("SELECT COUNT(*) AS count FROM community_scores " +
    "WHERE mode = ? AND scoring_version = ? AND board_rules_version = ?")
    .bind(mode, version, rules).first();
  return reply({ scores: rows.results.map(row => row.best_score),
    entries: rows.results.map(row => ({ name: row.display_name,
      score: row.best_score })), count: count.count });
}

async function resetScores(request, env) {
  const secret = env.RESET_TOKEN;
  const supplied = request.headers.get("authorization")?.replace(/^Bearer /i, "");
  if (!secret || secret.length < 32 || !supplied ||
      supplied.length !== secret.length) return reply({ error: "unauthorized" }, 401);
  let difference = 0;
  for (let i = 0; i < secret.length; i++)
    difference |= secret.charCodeAt(i) ^ supplied.charCodeAt(i);
  if (difference !== 0) return reply({ error: "unauthorized" }, 401);
  let body;
  try { body = await readJson(request); }
  catch { return reply({ error: "invalid_confirmation" }, 400); }
  if (body?.confirm !== "CLEAR_ALL_RANKINGS" || Object.keys(body).length !== 1)
    return reply({ error: "invalid_confirmation" }, 400);
  await env.DB.prepare("DELETE FROM community_scores").run();
  return reply({ cleared: true });
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (url.pathname === "/healthz" && request.method === "GET")
      return withReadCors(reply({ ok: true }), request);
    if (!env?.DB) return withReadCors(reply({ error: "database_unavailable" }, 503), request);
    let response;
    try {
      if (url.pathname === "/api/v2/scores" && request.method === "POST")
        response = await submitScore(request, env);
      else if (url.pathname === "/api/v2/scores" && request.method === "GET")
        response = await listScores(url, env.DB);
      else if (url.pathname === "/api/v2/admin/reset" && request.method === "POST")
        response = await resetScores(request, env);
      else response = reply({ error: "not_found" }, 404);
    } catch { response = reply({ error: "service_unavailable" }, 503); }
    return withReadCors(response, request);
  },
};
