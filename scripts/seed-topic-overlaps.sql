-- Add curated many-to-many topic relationships for recognisable vocabulary.
-- Existing primary relationships are preserved. New overlap links are secondary,
-- except for currently untagged words where one explicit primary is provided.
-- Safe to run repeatedly: the (word_id, topic_id) primary key prevents duplicates.

SET NAMES utf8mb4;
START TRANSACTION;

SET @links_before = (SELECT COUNT(*) FROM word_topics);
SET @multi_words_before = (
    SELECT COUNT(*)
    FROM (
        SELECT word_id
        FROM word_topics
        GROUP BY word_id
        HAVING COUNT(*) > 1
    ) existing_multi
);

DROP TEMPORARY TABLE IF EXISTS seed_word_topic_rules;
CREATE TEMPORARY TABLE seed_word_topic_rules (
    word_text VARCHAR(100) NOT NULL,
    topic_id INT UNSIGNED NOT NULL,
    is_primary TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (word_text, topic_id)
) ENGINE = MEMORY DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci;

-- Topic IDs:
-- 1 Business, 2 Technology, 3 Travel, 4 Food & Drink, 5 Health,
-- 6 Education, 7 Nature, 8 Sports, 9 Arts & Culture, 10 Science,
-- 11 Finance, 12 Law & Politics, 13 General.
INSERT INTO seed_word_topic_rules (word_text, topic_id, is_primary) VALUES
    ('account', 1, 0), ('account', 2, 0), ('account', 11, 0),
    ('airport', 1, 0), ('airport', 2, 0), ('airport', 3, 0),
    ('algorithm', 2, 1), ('algorithm', 6, 0), ('algorithm', 10, 0),
    ('artificial intelligence', 1, 0), ('artificial intelligence', 2, 1),
        ('artificial intelligence', 6, 0), ('artificial intelligence', 10, 0),
    ('bank', 1, 0), ('bank', 11, 0), ('bank', 12, 0),
    ('beach', 3, 0), ('beach', 7, 0), ('beach', 8, 0),
    ('body', 5, 0), ('body', 7, 0), ('body', 8, 0),
    ('budget', 1, 0), ('budget', 11, 0), ('budget', 12, 0),
    ('capital', 1, 0), ('capital', 11, 0), ('capital', 12, 0),
    ('climate', 7, 0), ('climate', 10, 0), ('climate', 12, 0),
    ('company', 1, 0), ('company', 11, 0), ('company', 12, 0),
    ('computer', 1, 0), ('computer', 2, 0), ('computer', 6, 0),
    ('contract', 1, 0), ('contract', 11, 0), ('contract', 12, 0),
    ('court', 12, 0), ('court', 13, 0),
    ('data', 1, 0), ('data', 2, 0), ('data', 10, 0),
    ('diet', 4, 0), ('diet', 5, 0), ('diet', 8, 0),
    ('doctor', 5, 0), ('doctor', 6, 0), ('doctor', 10, 0),
    ('economy', 1, 0), ('economy', 11, 0), ('economy', 12, 0),
    ('election', 12, 0), ('election', 13, 0),
    ('energy', 5, 0), ('energy', 7, 0), ('energy', 10, 0),
    ('environment', 7, 0), ('environment', 10, 0), ('environment', 12, 0),
    ('exercise', 5, 0), ('exercise', 6, 0), ('exercise', 8, 0),
    ('festival', 3, 0), ('festival', 4, 0), ('festival', 9, 0), ('festival', 13, 0),
    ('fitness', 5, 0), ('fitness', 6, 0), ('fitness', 8, 0),
    ('football', 1, 0), ('football', 5, 0), ('football', 8, 0),
    ('fruit', 4, 0), ('fruit', 5, 0), ('fruit', 7, 0),
    ('government', 11, 0), ('government', 12, 0), ('government', 13, 0),
    ('health', 5, 0), ('health', 6, 0), ('health', 10, 0),
    ('history', 6, 0), ('history', 9, 0), ('history', 12, 0),
    ('hotel', 1, 0), ('hotel', 3, 0), ('hotel', 4, 0),
    ('innovation', 1, 0), ('innovation', 2, 1), ('innovation', 6, 0), ('innovation', 10, 0),
    ('internet', 1, 0), ('internet', 2, 0), ('internet', 6, 0),
    ('investment', 1, 0), ('investment', 11, 0), ('investment', 12, 0),
    ('journey', 3, 0), ('journey', 9, 0), ('journey', 13, 0),
    ('language', 3, 0), ('language', 6, 0), ('language', 9, 0),
    ('league', 1, 0), ('league', 8, 0), ('league', 12, 0),
    ('literature', 6, 0), ('literature', 9, 0), ('literature', 13, 0),
    ('loan', 1, 0), ('loan', 11, 0), ('loan', 12, 0),
    ('market', 1, 0), ('market', 11, 0), ('market', 13, 0),
    ('medicine', 5, 0), ('medicine', 6, 0), ('medicine', 10, 0),
    ('mountain', 3, 0), ('mountain', 7, 0), ('mountain', 8, 0),
    ('museum', 3, 0), ('museum', 6, 0), ('museum', 9, 0), ('museum', 10, 0),
    ('music', 2, 0), ('music', 6, 0), ('music', 9, 0),
    ('network', 1, 0), ('network', 2, 0), ('network', 13, 0),
    ('nutrition', 4, 0), ('nutrition', 5, 1), ('nutrition', 10, 0),
    ('organic', 4, 0), ('organic', 5, 0), ('organic', 7, 1),
    ('painting', 6, 0), ('painting', 9, 1), ('painting', 13, 0),
    ('passport', 3, 0), ('passport', 12, 0), ('passport', 13, 0),
    ('payment', 1, 0), ('payment', 2, 0), ('payment', 11, 0),
    ('policy', 1, 0), ('policy', 11, 0), ('policy', 12, 0), ('policy', 13, 0),
    ('profit', 1, 0), ('profit', 11, 0), ('profit', 12, 0),
    ('protein', 4, 0), ('protein', 5, 1), ('protein', 10, 0),
    ('regulation', 1, 0), ('regulation', 11, 0), ('regulation', 12, 1),
    ('research', 2, 0), ('research', 5, 0), ('research', 6, 0), ('research', 10, 0),
    ('restaurant', 1, 0), ('restaurant', 3, 0), ('restaurant', 4, 1),
    ('robot', 2, 1), ('robot', 6, 0), ('robot', 10, 0),
    ('salary', 1, 0), ('salary', 11, 0), ('salary', 12, 0),
    ('school', 6, 0), ('school', 12, 0), ('school', 13, 0),
    ('science', 2, 0), ('science', 6, 0), ('science', 10, 0),
    ('software', 1, 0), ('software', 2, 0), ('software', 6, 0),
    ('sponsor', 1, 1), ('sponsor', 8, 0), ('sponsor', 9, 0),
    ('study', 6, 0), ('study', 10, 0), ('study', 13, 0),
    ('tax', 1, 0), ('tax', 11, 0), ('tax', 12, 0),
    ('team', 1, 0), ('team', 6, 0), ('team', 8, 0),
    ('technology', 1, 0), ('technology', 2, 0), ('technology', 6, 0), ('technology', 10, 0),
    ('tourist', 1, 0), ('tourist', 3, 0), ('tourist', 9, 0),
    ('trade', 1, 1), ('trade', 11, 0), ('trade', 12, 0),
    ('training', 1, 0), ('training', 5, 0), ('training', 6, 0), ('training', 8, 0),
    ('university', 1, 0), ('university', 6, 0), ('university', 10, 0),
    ('vegetable', 4, 0), ('vegetable', 5, 0), ('vegetable', 7, 0),
    ('water', 5, 0), ('water', 7, 0), ('water', 10, 0);

INSERT IGNORE INTO word_topics (word_id, topic_id, is_primary)
SELECT
    w.word_id,
    rule.topic_id,
    rule.is_primary
FROM seed_word_topic_rules rule
INNER JOIN words w
    ON LOWER(w.word) = LOWER(rule.word_text)
    AND w.status = 'active'
INNER JOIN topics topic
    ON topic.topic_id = rule.topic_id
    AND topic.status = 'active';

SET @inserted_links = ROW_COUNT();

COMMIT;

SELECT
    @links_before AS links_before,
    (SELECT COUNT(*) FROM word_topics) AS links_after,
    @inserted_links AS inserted_links,
    @multi_words_before AS multi_topic_words_before,
    (
        SELECT COUNT(*)
        FROM (
            SELECT word_id
            FROM word_topics
            GROUP BY word_id
            HAVING COUNT(*) > 1
        ) current_multi
    ) AS multi_topic_words_after;

SELECT
    topic_count,
    COUNT(*) AS word_count
FROM (
    SELECT word_id, COUNT(*) AS topic_count
    FROM word_topics
    GROUP BY word_id
) distribution
GROUP BY topic_count
ORDER BY topic_count;

SELECT
    w.word_id,
    w.word,
    COUNT(*) AS topic_count,
    GROUP_CONCAT(
        CONCAT(topic.topic_name, IF(wt.is_primary = 1, ' (primary)', ''))
        ORDER BY topic.topic_id
        SEPARATOR ', '
    ) AS topics
FROM words w
INNER JOIN word_topics wt ON wt.word_id = w.word_id
INNER JOIN topics topic ON topic.topic_id = wt.topic_id
WHERE EXISTS (
    SELECT 1
    FROM seed_word_topic_rules rule
    WHERE LOWER(rule.word_text) = LOWER(w.word)
)
GROUP BY w.word_id, w.word
HAVING COUNT(*) >= 3
ORDER BY topic_count DESC, w.word;
