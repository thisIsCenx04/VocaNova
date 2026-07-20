-- Fill the latest 30-day activity window with a natural, non-periodic trend.
-- Each day is brought up to a target session count and accuracy. Existing
-- completed sessions are retained, so rerunning the script does not duplicate data.

SET NAMES utf8mb4;
START TRANSACTION;

DROP TEMPORARY TABLE IF EXISTS seed_numbers;
CREATE TEMPORARY TABLE seed_numbers (
    n INT NOT NULL PRIMARY KEY
) ENGINE = MEMORY;

INSERT INTO seed_numbers (n) VALUES
    (1), (2), (3), (4), (5), (6), (7), (8), (9), (10),
    (11), (12), (13), (14), (15), (16), (17), (18), (19), (20);

-- day_offset 29 is the oldest point; day_offset 0 is today (UTC).
DROP TEMPORARY TABLE IF EXISTS seed_activity_targets;
CREATE TEMPORARY TABLE seed_activity_targets (
    day_offset INT NOT NULL PRIMARY KEY,
    desired_sessions INT NOT NULL,
    target_accuracy DECIMAL(5,2) NOT NULL
) ENGINE = MEMORY;

INSERT INTO seed_activity_targets (day_offset, desired_sessions, target_accuracy) VALUES
    (29,  6, 71), (28,  9, 68), (27, 11, 74), (26,  8, 72), (25, 13, 77),
    (24, 10, 75), (23,  7, 79), (22, 12, 73), (21,  9, 76), (20, 14, 81),
    (19, 11, 78), (18,  8, 74), (17, 10, 82), (16, 15, 77), (15, 12, 80),
    (14,  9, 84), (13, 13, 79), (12, 11, 83), (11,  7, 76), (10, 10, 81),
    ( 9, 14, 85), ( 8, 12, 80), ( 7,  8, 78), ( 6, 13, 86), ( 5, 16, 82),
    ( 4, 11, 84), ( 3,  9, 79), ( 2, 14, 85), ( 1, 10, 83), ( 0, 12, 87);

DROP TEMPORARY TABLE IF EXISTS seed_activity_existing;
CREATE TEMPORARY TABLE seed_activity_existing AS
SELECT
    target.day_offset,
    UTC_DATE() - INTERVAL target.day_offset DAY AS activity_date,
    target.desired_sessions,
    target.target_accuracy,
    COUNT(session.session_id) AS existing_sessions,
    COALESCE(SUM(session.correct_count), 0) AS existing_correct,
    COALESCE(SUM(session.correct_count + session.wrong_count), 0) AS existing_attempts
FROM seed_activity_targets target
LEFT JOIN test_sessions session
    ON session.status = 'completed'
    AND session.started_at >= UTC_DATE() - INTERVAL target.day_offset DAY
    AND session.started_at < UTC_DATE() - INTERVAL target.day_offset DAY + INTERVAL 1 DAY
GROUP BY
    target.day_offset,
    target.desired_sessions,
    target.target_accuracy;

DROP TEMPORARY TABLE IF EXISTS seed_activity_plan;
CREATE TEMPORARY TABLE seed_activity_plan AS
SELECT
    counts.*,
    LEAST(
        counts.new_session_count * 10,
        GREATEST(
            counts.new_session_count * 5,
            ROUND(
                counts.target_accuracy / 100
                * (counts.existing_attempts + counts.new_session_count * 10)
            ) - counts.existing_correct
        )
    ) AS new_correct_needed
FROM (
    SELECT
        existing.*,
        GREATEST(existing.desired_sessions - existing.existing_sessions, 0) AS new_session_count
    FROM seed_activity_existing existing
) counts;

-- Active learners are rotated across dates so activity is spread across accounts.
DROP TEMPORARY TABLE IF EXISTS seed_active_users;
CREATE TEMPORARY TABLE seed_active_users AS
SELECT
    u.user_id,
    ROW_NUMBER() OVER (ORDER BY u.user_id) AS user_no
FROM users u
INNER JOIN roles role ON role.role_id = u.role_id
WHERE role.role_name = 'user'
  AND u.status = 'active';

ALTER TABLE seed_active_users ADD PRIMARY KEY (user_id);
ALTER TABLE seed_active_users ADD UNIQUE KEY uq_seed_active_user_no (user_no);
SET @active_user_count = (SELECT COUNT(*) FROM seed_active_users);

DROP TEMPORARY TABLE IF EXISTS seed_session_candidates;
CREATE TEMPORARY TABLE seed_session_candidates AS
SELECT
    plan.day_offset,
    plan.activity_date,
    number.n AS session_no,
    active_user.user_id,
    TIMESTAMP(
        plan.activity_date,
        MAKETIME(
            7 + MOD(number.n * 3 + plan.day_offset, 15),
            MOD(number.n * 17 + plan.day_offset * 7, 60),
            MOD(number.n * 13, 60)
        )
    ) AS started_at,
    LEAST(
        10,
        GREATEST(
            5,
            FLOOR(plan.new_correct_needed / NULLIF(plan.new_session_count, 0))
            + IF(number.n <= MOD(plan.new_correct_needed, NULLIF(plan.new_session_count, 0)), 1, 0)
        )
    ) AS correct_count
FROM seed_activity_plan plan
INNER JOIN seed_numbers number ON number.n <= plan.new_session_count
INNER JOIN seed_active_users active_user
    ON active_user.user_no = MOD(plan.day_offset * 7 + number.n * 11, @active_user_count) + 1;

ALTER TABLE seed_session_candidates ADD PRIMARY KEY (activity_date, session_no);

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
    candidate.user_id,
    CASE MOD(candidate.session_no + candidate.day_offset, 3)
        WHEN 0 THEN 'multiple_choice'
        WHEN 1 THEN 'exact_typing'
        ELSE 'ai_typing'
    END,
    CASE MOD(candidate.session_no + candidate.day_offset, 4)
        WHEN 0 THEN 'standard'
        WHEN 1 THEN 'timed'
        WHEN 2 THEN 'challenge'
        ELSE 'elimination'
    END,
    MOD(candidate.session_no + candidate.day_offset, 3) + 1,
    'all',
    NULL,
    NULL,
    CASE MOD(candidate.session_no * 2 + candidate.day_offset, 4)
        WHEN 0 THEN 'newest'
        WHEN 1 THEN 'oldest'
        WHEN 2 THEN 'random'
        ELSE 'by_difficulty'
    END,
    10,
    CASE WHEN MOD(candidate.session_no + candidate.day_offset, 4) = 1 THEN 450 ELSE NULL END,
    CASE WHEN MOD(candidate.session_no + candidate.day_offset, 4) = 3 THEN 3 ELSE NULL END,
    10,
    candidate.correct_count,
    10 - candidate.correct_count,
    candidate.correct_count * 10,
    candidate.correct_count,
    candidate.started_at,
    TIMESTAMPADD(SECOND, 300 + candidate.session_no * 17, candidate.started_at),
    'completed'
FROM seed_session_candidates candidate
WHERE NOT EXISTS (
    SELECT 1
    FROM test_sessions existing
    WHERE existing.user_id = candidate.user_id
      AND existing.started_at = candidate.started_at
);

SET @inserted_sessions = ROW_COUNT();

DROP TEMPORARY TABLE IF EXISTS seed_new_sessions;
CREATE TEMPORARY TABLE seed_new_sessions AS
SELECT
    session.session_id,
    candidate.day_offset,
    candidate.activity_date,
    candidate.session_no,
    candidate.user_id,
    candidate.correct_count
FROM seed_session_candidates candidate
INNER JOIN test_sessions session
    ON session.user_id = candidate.user_id
    AND session.started_at = candidate.started_at
WHERE NOT EXISTS (
    SELECT 1
    FROM test_answers answer
    WHERE answer.session_id = session.session_id
);

ALTER TABLE seed_new_sessions ADD PRIMARY KEY (session_id);

-- Build a 60-word pool per learner from their selected topics.
DROP TEMPORARY TABLE IF EXISTS seed_user_words;
CREATE TEMPORARY TABLE seed_user_words AS
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
                MOD(candidates.word_id * 7919 + candidates.user_id * 104729, 1000003),
                candidates.word_id
        ) AS word_no
    FROM (
        SELECT DISTINCT
            active_user.user_id,
            word.word_id,
            word.word,
            sense.sense_id
        FROM seed_active_users active_user
        INNER JOIN user_topic_preferences preference
            ON preference.user_id = active_user.user_id
            AND preference.status = 'active'
        INNER JOIN word_topics word_topic ON word_topic.topic_id = preference.topic_id
        INNER JOIN words word ON word.word_id = word_topic.word_id AND word.status = 'active'
        INNER JOIN word_senses sense
            ON sense.sense_id = (
                SELECT MIN(first_sense.sense_id)
                FROM word_senses first_sense
                WHERE first_sense.word_id = word.word_id
            )
    ) candidates
) ranked
WHERE ranked.word_no <= 60;

ALTER TABLE seed_user_words ADD PRIMARY KEY (user_id, word_no);
ALTER TABLE seed_user_words ADD UNIQUE KEY uq_seed_activity_user_word (user_id, word_id);

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
    seed_session.session_id,
    seed_word.word_id,
    seed_word.sense_id,
    number.n,
    session.question_type,
    CONCAT('Choose the correct answer for: ', seed_word.word),
    seed_word.word,
    JSON_ARRAY(seed_word.word),
    CASE WHEN number.n <= seed_session.correct_count THEN seed_word.word ELSE 'Not sure' END,
    number.n <= seed_session.correct_count,
    CASE WHEN session.test_type = 'ai_typing' THEN IF(number.n <= seed_session.correct_count, 1, 0) ELSE NULL END,
    NULL,
    NULL
FROM seed_new_sessions seed_session
INNER JOIN test_sessions session ON session.session_id = seed_session.session_id
INNER JOIN seed_numbers number ON number.n <= 10
INNER JOIN seed_user_words seed_word
    ON seed_word.user_id = seed_session.user_id
    AND seed_word.word_no = MOD(seed_session.day_offset * 7 + seed_session.session_no * 11 + number.n - 1, 60) + 1;

SET @inserted_answers = ROW_COUNT();

COMMIT;

SELECT
    @inserted_sessions AS inserted_sessions,
    @inserted_answers AS inserted_answers;

SELECT
    DATE(session.started_at) AS activity_date,
    COUNT(*) AS sessions,
    SUM(session.correct_count) AS correct_answers,
    SUM(session.wrong_count) AS wrong_answers,
    ROUND(
        SUM(session.correct_count) / NULLIF(SUM(session.correct_count + session.wrong_count), 0) * 100,
        2
    ) AS accuracy
FROM test_sessions session
WHERE session.status = 'completed'
  AND session.started_at >= UTC_DATE() - INTERVAL 29 DAY
  AND session.started_at < UTC_DATE() + INTERVAL 1 DAY
GROUP BY DATE(session.started_at)
ORDER BY activity_date;
