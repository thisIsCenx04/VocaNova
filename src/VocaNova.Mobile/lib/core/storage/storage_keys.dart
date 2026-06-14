abstract final class StorageKeys {
  static const userProfileJson = 'user_profile_json';
  static const listsCacheJson = 'lists_cache_json';
  static const progressSummaryJson = 'progress_summary_json';
  static const searchHistoryJson = 'search_history_json';
  static const wordSearchCacheJson = 'word_search_cache_json';
  static const topicsCacheJson = 'topics_cache_json';
  static const appLocale = 'app_locale';
  static const appTheme = 'app_theme';

  static String wordCacheJson(int id) => 'word_cache_${id}_json';

  static String savedWordJson(int id) => 'saved_word_${id}_json';
}
