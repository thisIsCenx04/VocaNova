import 'dart:async';
import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_state.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

part 'word_search_notifier.g.dart';

@Riverpod(keepAlive: true)
WordSearchRepository wordSearchRepository(Ref ref) {
  return WordSearchRepository(dio: DioClient.instance.dio);
}

@Riverpod(keepAlive: true)
LocalStorage searchLocalStorage(Ref ref) => LocalStorage.instance;

@Riverpod(keepAlive: true)
class WordSearchNotifier extends _$WordSearchNotifier {
  static const debounceDuration = Duration(milliseconds: 300);
  static const maxHistoryItems = 20;
  static const maxCachedWords = 200;

  Timer? _debounce;
  List<WordSummary> _cachedWords = const [];
  int _requestId = 0;

  @override
  WordSearchState build() {
    ref.onDispose(() => _debounce?.cancel());
    return const WordSearchState();
  }

  Future<void> initialize() async {
    final storage = ref.read(searchLocalStorageProvider);
    final historyJson = await storage.get<String>(
      StorageKeys.searchHistoryJson,
    );
    final wordsJson = await storage.get<String>(
      StorageKeys.wordSearchCacheJson,
    );
    final topicsJson = await storage.get<String>(StorageKeys.topicsCacheJson);
    _cachedWords = _decodeWords(wordsJson);
    final online = await ref.read(connectivityServiceProvider).isOnline;
    final cachedTopics = _decodeTopics(topicsJson);
    state = state.copyWith(
      history: _decodeHistory(historyJson),
      topics: cachedTopics,
      isOffline: !online,
    );
    if (online) {
      await _loadTopics();
    }
  }

  void queryChanged(String query) {
    _debounce?.cancel();
    _requestId++;
    final isEmpty = query.trim().isEmpty;
    state = state.copyWith(
      query: query,
      results: isEmpty ? const [] : state.results,
      isLoading: false,
      clearCefr: isEmpty,
      clearTopic: isEmpty,
      clearError: true,
    );
    if (isEmpty) {
      return;
    }
    _debounce = Timer(debounceDuration, search);
  }

  Future<void> selectCefr(String? cefr) async {
    state = state.copyWith(
      selectedCefr: cefr,
      clearCefr: cefr == null,
      clearError: true,
    );
    await search();
  }

  Future<void> selectTopic(int? topicId) async {
    state = state.copyWith(
      selectedTopicId: topicId,
      clearTopic: topicId == null,
      clearError: true,
    );
    await search();
  }

  Future<void> search() async {
    _debounce?.cancel();
    final query = state.query.trim();
    if (query.isEmpty && state.selectedTopicId == null) {
      state = state.copyWith(results: const [], isLoading: false);
      return;
    }

    final requestId = ++_requestId;
    final online = await ref.read(connectivityServiceProvider).isOnline;
    if (!online) {
      state = state.copyWith(
        results: _searchCache(query),
        isLoading: false,
        isOffline: true,
        clearError: true,
      );
      return;
    }

    state = state.copyWith(isLoading: true, isOffline: false, clearError: true);
    try {
      final results = await ref
          .read(wordSearchRepositoryProvider)
          .search(
            query: query,
            cefr: state.selectedCefr,
            topicId: state.selectedTopicId,
          );
      if (requestId != _requestId) {
        return;
      }
      final enriched = results
          .map((word) => word.withTopic(state.selectedTopicId))
          .toList(growable: false);
      await _cacheWords(enriched);
      state = state.copyWith(results: enriched, isLoading: false);
    } catch (error) {
      if (requestId != _requestId) {
        return;
      }
      state = state.copyWith(
        results: _searchCache(query),
        isLoading: false,
        isOffline: false,
        errorMessage: _errorMessage(error),
      );
    }
  }

  Future<void> recordHistory(String word) async {
    final normalized = word.trim();
    final history = [
      normalized,
      ...state.history.where(
        (item) => item.toLowerCase() != normalized.toLowerCase(),
      ),
    ].take(maxHistoryItems).toList(growable: false);
    state = state.copyWith(history: history);
    await ref
        .read(searchLocalStorageProvider)
        .set(StorageKeys.searchHistoryJson, jsonEncode(history));
  }

  Future<void> clearHistory() async {
    state = state.copyWith(history: const []);
    await ref
        .read(searchLocalStorageProvider)
        .remove(StorageKeys.searchHistoryJson);
  }

  Future<void> useHistory(String word) async {
    state = state.copyWith(query: word);
    await search();
  }

  Future<void> _loadTopics() async {
    try {
      final topics = await ref.read(wordSearchRepositoryProvider).getTopics();
      state = state.copyWith(topics: topics);
      await ref
          .read(searchLocalStorageProvider)
          .set(
            StorageKeys.topicsCacheJson,
            jsonEncode(topics.map((topic) => topic.toJson()).toList()),
          );
    } catch (_) {
      // Cached topics remain available when the request fails.
    }
  }

  List<WordSummary> _searchCache(String query) {
    final normalized = query.toLowerCase();
    return _cachedWords
        .where((word) {
          final matchesText =
              word.word.toLowerCase().contains(normalized) ||
              (word.primaryMeaning?.toLowerCase().contains(normalized) ??
                  false);
          final matchesCefr =
              state.selectedCefr == null || word.cefr == state.selectedCefr;
          final matchesTopic =
              state.selectedTopicId == null ||
              word.topicIds.contains(state.selectedTopicId);
          return matchesText && matchesCefr && matchesTopic;
        })
        .toList(growable: false);
  }

  Future<void> _cacheWords(List<WordSummary> words) async {
    final byId = <int, WordSummary>{};
    for (final word in words) {
      final cached = _cachedWords
          .where((item) => item.wordId == word.wordId)
          .firstOrNull;
      byId[word.wordId] = cached?.merge(word) ?? word;
    }
    for (final word in _cachedWords) {
      byId.putIfAbsent(word.wordId, () => word);
    }
    _cachedWords = byId.values.take(maxCachedWords).toList(growable: false);
    await ref
        .read(searchLocalStorageProvider)
        .set(
          StorageKeys.wordSearchCacheJson,
          jsonEncode(_cachedWords.map((word) => word.toJson()).toList()),
        );
  }

  List<String> _decodeHistory(String? source) {
    try {
      return (jsonDecode(source ?? '[]') as List<dynamic>)
          .whereType<String>()
          .take(maxHistoryItems)
          .toList(growable: false);
    } on Object {
      return const [];
    }
  }

  List<WordSummary> _decodeWords(String? source) {
    try {
      return (jsonDecode(source ?? '[]') as List<dynamic>)
          .whereType<Map<String, dynamic>>()
          .map(WordSummary.fromJson)
          .toList(growable: false);
    } on Object {
      return const [];
    }
  }

  List<TopicSummary> _decodeTopics(String? source) {
    try {
      return (jsonDecode(source ?? '[]') as List<dynamic>)
          .whereType<Map<String, dynamic>>()
          .map(TopicSummary.fromJson)
          .toList(growable: false);
    } on Object {
      return const [];
    }
  }

  String _errorMessage(Object error) {
    if (error is AppException) {
      return error.message;
    }
    if (error is DioException && error.error is AppException) {
      return (error.error! as AppException).message;
    }
    return lookupAppLocalizations(
      AppSettingsNotifier.instance.state.locale,
    ).dictSearchRefreshError;
  }
}
