import 'dart:convert';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/lists/application/list_detail_state.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/domain/list_word.dart';

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
        errorMessage: cached.isEmpty
            ? 'Không thể tải từ trong danh sách.'
            : null,
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
        errorMessage: 'Không thể tải thêm từ.',
      );
    }
  }

  Future<bool> addWord(int wordId) async {
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
      state = state.copyWith(errorMessage: 'Không thể thêm từ vào danh sách.');
      return false;
    }
  }

  Future<bool> addRandom({
    required int count,
    required String method,
    int? topicId,
  }) async {
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
      state = state.copyWith(errorMessage: 'Không thể thêm từ ngẫu nhiên.');
      return false;
    }
  }

  Future<bool> removeWord(int wordId) async {
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
        errorMessage: 'Không thể xóa từ khỏi danh sách.',
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
