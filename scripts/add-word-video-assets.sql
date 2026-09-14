-- Word video MVP. Apply before deploying API code that reads this table.
-- One row per word, including soft-deleted rows; replacements reuse the row.
CREATE TABLE IF NOT EXISTS word_video_assets (
    video_id INT UNSIGNED NOT NULL AUTO_INCREMENT,
    word_id INT UNSIGNED NOT NULL,
    source VARCHAR(20) NOT NULL DEFAULT 'uploaded',
    public_id VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    storage_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'active' COMMENT 'active/deleted',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (video_id),
    UNIQUE KEY idx_video_word (word_id),
    CONSTRAINT fk_video_word FOREIGN KEY (word_id) REFERENCES words (word_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Rollback: roll back application code first. Keep this additive table to retain
-- uploaded video metadata. Dropping a populated table requires a separate backup
-- and explicit data-deletion decision; it is not part of deployment rollback.
