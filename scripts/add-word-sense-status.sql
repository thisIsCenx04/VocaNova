ALTER TABLE `word_senses`
    ADD COLUMN `status` varchar(20) NOT NULL DEFAULT 'active' COMMENT 'active/deleted'
        AFTER `vietnamese_meaning`,
    ADD INDEX `idx_senses_status` (`status`);
