import 'dart:convert';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/lists/application/list_detail_state.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/domain/list_word.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

part 'list_detail_notifier.g.dart';

@Riverpod(keepAlive: true)
class ListDetailNotifier extends _$ListDetailNotifier {
  @override
  ListDetailState build(int listId) => const ListDetailState();

  Future<void> load() async {
    final storage = ref.read(listsLocalStorageProvider);
    final cached = _decode(
      await storage.get<String>(StorageKeys.listWordsCacheJson(listId)),
    );
    final online = await ref.read(connectivityServiceProvider).isOnline;
    if (!online) {
      state = ListDetailState(
        words: cached,
        page: 1,
        totalPages: 1,
        isLoading: false,
        isOffline: true,
      );
      return;
    }
    try {
      final result = await ref
          .read(listsRepositoryProvider)
          .getWords(listId: listId, page: 1);
      state = ListDetailState(
        words: result.items,
        page: result.page,
        totalPages: result.totalPages,
        isLoading: false,
      );
      await _cache(result.items);
    } catch (_) {
      state = ListDetailState(
        words: cached,
        page: 1,
        totalPages: 1,
        isLoading: false,
        isOffline: cached.isNotEmpty,
        errorMessage: cached.isEmpty ? _l10n.listsLoadWordsError : null,
      );
    }
  }

  Future<void> loadMore() async {
    if (!state.hasMore || state.isLoadingMore || state.isOffline) return;
    state = state.copyWith(isLoadingMore: true, clearError: true);
    try {
      final result = await ref
          .read(listsRepositoryProvider)
          .getWords(listId: listId, page: state.page + 1);
      final words = [...state.words, ...result.items];
      state = state.copyWith(
        words: words,
        page: result.page,
        totalPages: result.totalPages,
        isLoadingMore: false,
      );
      await _cache(words);
    } catch (_) {
      state = state.copyWith(
        isLoadingMore: false,
        errorMessage: _l10n.listsLoadMoreWordsError,
      );
    }
  }

  Future<bool> addWord(int wordId) async {
    if (!_canMutate()) return false;
    try {
      final word = await ref
          .read(listsRepositoryProvider)
          .addWord(listId: listId, wordId: wordId);
      final words = [
        word,
        ...state.words.where((item) => item.wordId != wordId),
      ];
      state = state.copyWith(words: words, clearError: true);
      await _cache(words);
      return true;
    } catch (_) {
      state = state.copyWith(errorMessage: _l10n.listsAddWordError);
      return false;
    }
  }

  Future<bool> addRandom({
    required int count,
    required String method,
    int? topicId,
  }) async {
    if (!_canMutate()) return false;
    try {
      final added = await ref
          .read(listsRepositoryProvider)
          .addRandomWords(
            listId: listId,
            count: count,
            method: method,
            topicId: topicId,
          );
      final byId = <int, ListWord>{
        for (final word in added) word.wordId: word,
        for (final word in state.words) word.wordId: word,
      };
      final words = byId.values.toList(growable: false);
      state = state.copyWith(words: words, clearError: true);
      await _cache(words);
      return true;
    } catch (_) {
      state = state.copyWith(errorMessage: _l10n.listsAddRandomError);
      return false;
    }
  }

  Future<bool> removeWord(int wordId) async {
    if (!_canMutate()) return false;
    final index = state.words.indexWhere((word) => word.wordId == wordId);
    if (index < 0) return false;
    final removed = state.words[index];
    final optimistic = [...state.words]..removeAt(index);
    state = state.copyWith(words: optimistic, clearError: true);
    try {
      await ref
          .read(listsRepositoryProvider)
          .removeWord(listId: listId, wordId: wordId);
      await _cache(optimistic);
      return true;
    } catch (_) {
      final rollback = [...state.words]..insert(index, removed);
      state = state.copyWith(
        words: rollback,
        errorMessage: _l10n.listsRemoveWordError,
      );
      return false;
    }
  }

  Future<void> _cache(List<ListWord> words) => ref
      .read(listsLocalStorageProvider)
      .set(
        StorageKeys.listWordsCacheJson(listId),
        jsonEncode(words.map((word) => word.toJson()).toList()),
      );

  bool _canMutate() {
    if (!state.isOffline) return true;
    state = state.copyWith(
      isOffline: true,
      errorMessage: _l10n.listsOfflineMutateError,
    );
    return false;
  }

  AppLocalizations get _l10n =>
      lookupAppLocalizations(AppSettingsNotifier.instance.state.locale);

  List<ListWord> _decode(String? source) {
    try {
      return (jsonDecode(source ?? '[]') as List<dynamic>)
          .whereType<Map<String, dynamic>>()
          .map(ListWord.fromJson)
          .toList(growable: false);
    } on FormatException {
      return const [];
    }
  }
}
