-- VocaNova — `notifications` table
-- New table for per-user in-app notifications (e.g. when a vocabulary word they use is soft-deleted).
-- Run this ONCE on the VocaNova MySQL database (e.g. via MySQL Workbench) before using the notifications feature.
-- Additive only: does not modify any existing table.

CREATE TABLE IF NOT EXISTS `notifications` (
  `notification_id` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
  `user_id`         INT(10) UNSIGNED NOT NULL,
  `type`            VARCHAR(50)  NOT NULL COMMENT 'e.g. word_deleted, word_restored',
  `title`           VARCHAR(150) NOT NULL,
  `message`         VARCHAR(500) NOT NULL,
  `ref_type`        VARCHAR(50)  NULL COMMENT 'referenced entity type, e.g. word',
  `ref_id`          INT(10) UNSIGNED NULL COMMENT 'referenced entity id, e.g. word_id',
  `is_read`         TINYINT(1)   NOT NULL DEFAULT 0,
  `created_at`      TIMESTAMP    NOT NULL DEFAULT current_timestamp(),
  `read_at`         TIMESTAMP    NULL,
  PRIMARY KEY (`notification_id`),
  KEY `idx_notifications_user_read` (`user_id`, `is_read`),
  KEY `idx_notifications_user_time` (`user_id`, `created_at`),
  CONSTRAINT `fk_notifications_user` FOREIGN KEY (`user_id`)
      REFERENCES `users` (`user_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
