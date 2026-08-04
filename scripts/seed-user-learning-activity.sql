-- Seed realistic learning activity for learner accounts that do not have it yet.
-- The script is idempotent: each data group is only added when that user has none.
-- Run with a utf8mb4 client against the VocaNova database.

SET NAMES utf8mb4;
START TRANSACTION;

DROP TEMPORARY TABLE IF EXISTS seed_numbers;
CREATE TEMPORARY TABLE seed_numbers (
    n INT NOT NULL PRIMARY KEY
) ENGINE = MEMORY;

INSERT INTO seed_numbers (n) VALUES
    (1), (2), (3), (4), (5), (6), (7), (8), (9), (10),
    (11), (12), (13), (14), (15), (16), (17), (18), (19), (20),
    (21), (22), (23), (24);

DROP TEMPORARY TABLE IF EXISTS seed_users;
CREATE TEMPORARY TABLE seed_users AS
SELECT
    u.user_id,
    u.created_at,
    NOT EXISTS (
        SELECT 1
        FROM user_topic_preferences pref
        WHERE pref.user_id = u.user_id
    ) AS needs_topics,
    NOT EXISTS (
        SELECT 1
        FROM test_sessions session
        WHERE session.user_id = u.user_id
    ) AS needs_sessions,
    NOT EXISTS (
        SELECT 1
        FROM user_word_progress progress
        WHERE progress.user_id = u.user_id
    ) AS needs_progress
FROM users u
INNER JOIN roles r ON r.role_id = u.role_id
WHERE r.role_name = 'user'
HAVING needs_topics = 1 OR needs_sessions = 1 OR needs_progress = 1;

ALTER TABLE seed_users ADD PRIMARY KEY (user_id);

-- Give each learner two selected topics and one KNN suggestion.
INSERT IGNORE INTO user_topic_preferences (
    user_id,
    topic_id,
    source,
    status,
    created_at
)
SELECT
    su.user_id,
    MOD(su.user_id * 3 + CASE sn.n WHEN 1 THEN 0 WHEN 2 THEN 4 ELSE 8 END, 13) + 1,
    CASE sn.n WHEN 1 THEN 'onboarding' WHEN 2 THEN 'user_selected' ELSE 'knn_suggested' END,
    'active',
    TIMESTAMPADD(MINUTE, sn.n * 12, su.created_at)
FROM seed_users su
INNER JOIN seed_numbers sn ON sn.n <= 3
INNER JOIN topics topic
    ON topic.topic_id = MOD(su.user_id * 3 + CASE sn.n WHEN 1 THEN 0 WHEN 2 THEN 4 ELSE 8 END, 13) + 1
    AND topic.status = 'active'
WHERE su.needs_topics = 1;

-- Select 24 deterministic active words from the learner's seeded topics.
DROP TEMPORARY TABLE IF EXISTS seed_words;
CREATE TEMPORARY TABLE seed_words AS
SELECT
    ranked.user_id,
    ranked.word_id,
    ranked.word,
    ranked.sense_id,
    ranked.word_no
FROM (
    SELECT
        candidates.user_id,
        candidates.word_id,
        candidates.word,
        candidates.sense_id,
        ROW_NUMBER() OVER (
            PARTITION BY candidates.user_id
            ORDER BY
                MOD(candidates.word_id * 37 + candidates.user_id * 101, 1000003),
                candidates.word_id
        ) AS word_no
    FROM (
        SELECT DISTINCT
            su.user_id,
            w.word_id,
            w.word,
            ws.sense_id
        FROM seed_users su
        INNER JOIN word_topics wt
            ON wt.topic_id IN (
                MOD(su.user_id * 3, 13) + 1,
                MOD(su.user_id * 3 + 4, 13) + 1,
                MOD(su.user_id * 3 + 8, 13) + 1
            )
        INNER JOIN words w ON w.word_id = wt.word_id AND w.status = 'active'
        INNER JOIN word_senses ws
            ON ws.sense_id = (
                SELECT MIN(first_sense.sense_id)
                FROM word_senses first_sense
                WHERE first_sense.word_id = w.word_id
            )
        WHERE su.needs_sessions = 1 OR su.needs_progress = 1
    ) candidates
) ranked
WHERE ranked.word_no <= 24;

ALTER TABLE seed_words ADD PRIMARY KEY (user_id, word_no);
ALTER TABLE seed_words ADD UNIQUE KEY uq_seed_user_word (user_id, word_id);

-- Add eight completed sessions per learner: four 15-question and four 10-question sessions.
INSERT INTO test_sessions (
    user_id,
    test_type,
    mode,
    question_type,
    scope_type,
    scope_date_from,
    scope_date_to,
    word_order,
    word_limit,
    time_limit_sec,
    lives,
    question_count,
    correct_count,
    wrong_count,
    score,
    max_streak,
    started_at,
    ended_at,
    status
)
SELECT
    su.user_id,
    CASE WHEN MOD(sn.n, 3) = 0 THEN 'flashcard' ELSE 'quiz' END,
    CASE WHEN MOD(sn.n, 3) = 0 THEN 'challenge' ELSE 'practice' END,
    MOD(sn.n - 1, 3) + 1,
    'topic',
    NULL,
    NULL,
    CASE MOD(sn.n, 3) WHEN 0 THEN 'srs' WHEN 1 THEN 'random' ELSE 'sequential' END,
    CASE WHEN MOD(sn.n, 2) = 0 THEN 10 ELSE 15 END,
    (CASE WHEN MOD(sn.n, 2) = 0 THEN 10 ELSE 15 END) * 45,
    CASE WHEN MOD(sn.n, 3) = 0 THEN 3 ELSE NULL END,
    CASE WHEN MOD(sn.n, 2) = 0 THEN 10 ELSE 15 END,
    0,
    0,
    0,
    0,
    TIMESTAMP(
        DATE_ADD(DATE(su.created_at), INTERVAL (5 + sn.n * 3) DAY),
        MAKETIME(18 + MOD(su.user_id + sn.n, 4), MOD(su.user_id * 7 + sn.n * 11, 60), 0)
    ),
    TIMESTAMPADD(
        SECOND,
        (CASE WHEN MOD(sn.n, 2) = 0 THEN 10 ELSE 15 END) * 45,
        TIMESTAMP(
            DATE_ADD(DATE(su.created_at), INTERVAL (5 + sn.n * 3) DAY),
            MAKETIME(18 + MOD(su.user_id + sn.n, 4), MOD(su.user_id * 7 + sn.n * 11, 60), 0)
        )
    ),
    'completed'
FROM seed_users su
INNER JOIN seed_numbers sn ON sn.n <= 8
WHERE su.needs_sessions = 1
  AND (SELECT COUNT(*) FROM seed_words sw WHERE sw.user_id = su.user_id) = 24;

-- Resolve the IDs generated for the sessions above.
DROP TEMPORARY TABLE IF EXISTS seed_sessions;
CREATE TEMPORARY TABLE seed_sessions AS
SELECT
    ts.session_id,
    ts.user_id,
    ROW_NUMBER() OVER (
        PARTITION BY ts.user_id
        ORDER BY ts.started_at, ts.session_id
    ) AS session_no,
    ts.question_type,
    ts.question_count
FROM test_sessions ts
INNER JOIN seed_users su ON su.user_id = ts.user_id
WHERE su.needs_sessions = 1 OR su.needs_progress = 1;

ALTER TABLE seed_sessions ADD PRIMARY KEY (session_id);
ALTER TABLE seed_sessions ADD UNIQUE KEY uq_seed_user_session_no (user_id, session_no);

-- Associate every session with one of the learner's selected topics.
INSERT IGNORE INTO test_session_topics (session_id, topic_id)
SELECT
    ss.session_id,
    MOD(
        ss.user_id * 3
        + CASE MOD(ss.session_no - 1, 3) WHEN 0 THEN 0 WHEN 1 THEN 4 ELSE 8 END,
        13
    ) + 1
FROM seed_sessions ss;

-- Add graded answers. Wrong answers are intentionally mixed into every session.
INSERT INTO test_answers (
    session_id,
    word_id,
    sense_id,
    question_number,
    question_type,
    display_content,
    expected_answer,
    accepted_answers,
    user_answer,
    is_correct,
    ai_score,
    ai_explanation,
    ai_suggestion
)
SELECT
    ss.session_id,
    sw.word_id,
    sw.sense_id,
    sn.n,
    ss.question_type,
    CONCAT('Choose the correct answer for: ', sw.word),
    sw.word,
    JSON_ARRAY(sw.word),
    CASE
        WHEN sn.n <= ss.question_count - (1 + MOD(ss.user_id + ss.session_no, 4)) THEN sw.word
        ELSE 'Not sure'
    END,
    sn.n <= ss.question_count - (1 + MOD(ss.user_id + ss.session_no, 4)),
    NULL,
    NULL,
    NULL
FROM seed_sessions ss
INNER JOIN seed_numbers sn ON sn.n <= ss.question_count
INNER JOIN seed_words sw
    ON sw.user_id = ss.user_id
    AND sw.word_no = MOD((ss.session_no - 1) * 13 + sn.n - 1, 24) + 1
WHERE NOT EXISTS (
    SELECT 1
    FROM test_answers existing_answer
    WHERE existing_answer.session_id = ss.session_id
);

-- Recalculate session totals from the answers so dashboard statistics stay consistent.
UPDATE test_sessions ts
INNER JOIN (
    SELECT
        ta.session_id,
        COUNT(*) AS answered_count,
        SUM(ta.is_correct = 1) AS correct_count,
        SUM(ta.is_correct = 0) AS wrong_count
    FROM test_answers ta
    INNER JOIN seed_sessions ss ON ss.session_id = ta.session_id
    GROUP BY ta.session_id
) totals ON totals.session_id = ts.session_id
SET
    ts.correct_count = totals.correct_count,
    ts.wrong_count = totals.wrong_count,
    ts.score = ROUND(totals.correct_count / ts.question_count * 100, 1),
    ts.max_streak = totals.correct_count;

-- Build SRS learning progress from the newly generated answer events.
DROP TEMPORARY TABLE IF EXISTS seed_progress_events;
CREATE TEMPORARY TABLE seed_progress_events AS
SELECT
    ts.user_id,
    ta.word_id,
    ta.is_correct,
    ss.session_no * 100 + ta.question_number AS event_order,
    TIMESTAMPADD(SECOND, ta.question_number * 30, ts.started_at) AS event_at
FROM test_answers ta
INNER JOIN test_sessions ts ON ts.session_id = ta.session_id
INNER JOIN seed_sessions ss ON ss.session_id = ts.session_id
INNER JOIN seed_users su ON su.user_id = ts.user_id AND su.needs_progress = 1;

DROP TEMPORARY TABLE IF EXISTS seed_progress_summary;
CREATE TEMPORARY TABLE seed_progress_summary AS
SELECT
    user_id,
    word_id,
    COUNT(*) AS test_count,
    SUM(is_correct = 1) AS correct_count,
    SUM(is_correct = 0) AS wrong_count,
    MAX(event_order) AS last_event_order,
    MAX(CASE WHEN is_correct = 0 THEN event_order END) AS last_wrong_order,
    MAX(event_at) AS last_tested_at,
    MAX(CASE WHEN is_correct = 0 THEN event_at END) AS last_wrong_at
FROM seed_progress_events
GROUP BY user_id, word_id;

INSERT INTO user_word_progress (
    user_id,
    word_id,
    test_count,
    correct_count,
    wrong_count,
    consecutive_correct,
    is_in_wrong_list,
    mastery_level,
    srs_interval,
    ease_factor,
    last_tested_at,
    last_wrong_at,
    next_review_at,
    updated_at
)
SELECT
    summary.user_id,
    summary.word_id,
    summary.test_count,
    summary.correct_count,
    summary.wrong_count,
    SUM(
        CASE
            WHEN event.is_correct = 1
             AND (summary.last_wrong_order IS NULL OR event.event_order > summary.last_wrong_order)
            THEN 1 ELSE 0
        END
    ) AS consecutive_correct,
    COALESCE(summary.last_wrong_order = summary.last_event_order, 0),
    LEAST(5, FLOOR(summary.correct_count / 5)),
    CASE
        WHEN summary.last_wrong_order = summary.last_event_order THEN 1
        WHEN SUM(CASE WHEN event.is_correct = 1 AND (summary.last_wrong_order IS NULL OR event.event_order > summary.last_wrong_order) THEN 1 ELSE 0 END) <= 1 THEN 1
        WHEN SUM(CASE WHEN event.is_correct = 1 AND (summary.last_wrong_order IS NULL OR event.event_order > summary.last_wrong_order) THEN 1 ELSE 0 END) = 2 THEN 6
        WHEN SUM(CASE WHEN event.is_correct = 1 AND (summary.last_wrong_order IS NULL OR event.event_order > summary.last_wrong_order) THEN 1 ELSE 0 END) = 3 THEN 15
        ELSE 30
    END AS srs_interval,
    GREATEST(1.3, LEAST(3.5, 2.5 + summary.correct_count * 0.1 - summary.wrong_count * 0.32)),
    summary.last_tested_at,
    summary.last_wrong_at,
    TIMESTAMPADD(
        DAY,
        CASE
            WHEN summary.last_wrong_order = summary.last_event_order THEN 1
            WHEN SUM(CASE WHEN event.is_correct = 1 AND (summary.last_wrong_order IS NULL OR event.event_order > summary.last_wrong_order) THEN 1 ELSE 0 END) <= 1 THEN 1
            WHEN SUM(CASE WHEN event.is_correct = 1 AND (summary.last_wrong_order IS NULL OR event.event_order > summary.last_wrong_order) THEN 1 ELSE 0 END) = 2 THEN 6
            WHEN SUM(CASE WHEN event.is_correct = 1 AND (summary.last_wrong_order IS NULL OR event.event_order > summary.last_wrong_order) THEN 1 ELSE 0 END) = 3 THEN 15
            ELSE 30
        END,
        summary.last_tested_at
    ),
    summary.last_tested_at
FROM seed_progress_summary summary
INNER JOIN seed_progress_events event
    ON event.user_id = summary.user_id
    AND event.word_id = summary.word_id
GROUP BY
    summary.user_id,
    summary.word_id,
    summary.test_count,
    summary.correct_count,
    summary.wrong_count,
    summary.last_event_order,
    summary.last_wrong_order,
    summary.last_tested_at,
    summary.last_wrong_at;

COMMIT;

-- Verification summary.
SELECT
    u.user_id,
    r.role_name,
    u.status,
    COUNT(DISTINCT pref.topic_id) AS topic_count,
    COUNT(DISTINCT session.session_id) AS session_count,
    COUNT(DISTINCT answer.answer_id) AS answer_count,
    COUNT(DISTINCT progress.progress_id) AS progress_word_count
FROM users u
INNER JOIN roles r ON r.role_id = u.role_id
LEFT JOIN user_topic_preferences pref ON pref.user_id = u.user_id
LEFT JOIN test_sessions session ON session.user_id = u.user_id
LEFT JOIN test_answers answer ON answer.session_id = session.session_id
LEFT JOIN user_word_progress progress ON progress.user_id = u.user_id
WHERE r.role_name = 'user'
GROUP BY u.user_id, r.role_name, u.status
ORDER BY u.user_id;
