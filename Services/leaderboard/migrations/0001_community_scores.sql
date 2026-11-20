CREATE TABLE community_scores (
  mode TEXT NOT NULL CHECK (mode IN ('fill', 'boardgame', 'endless')),
  scoring_version INTEGER NOT NULL CHECK (scoring_version >= 8),
  board_rules_version INTEGER NOT NULL CHECK (board_rules_version >= 0),
  ip_digest TEXT NOT NULL,
  best_score INTEGER NOT NULL,
  submitted_at_ms INTEGER NOT NULL,
  PRIMARY KEY (mode, scoring_version, board_rules_version, ip_digest)
);

CREATE INDEX community_scores_rank_idx ON community_scores
  (mode, scoring_version, board_rules_version, best_score DESC, submitted_at_ms ASC);
