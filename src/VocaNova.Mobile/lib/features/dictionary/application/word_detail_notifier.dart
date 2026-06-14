import 'dart:convert';

import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_state.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_detail_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';

part 'word_detail_notifier.g.dart';

@Riverpod(keepAlive: true)
WordDetailRepository wordDetailRepository(Ref ref) =>
    WordDetailRepository(dio: DioClient.instance.dio);

@Riverpod(keepAlive: true)
LocalStorage wordDetailLocalStorage(Ref ref) => LocalStorage.instance;

@Riverpod(keepAlive: true)
class WordDetailNotifier extends _$WordDetailNotifier {
  static const cacheTtl = Duration(hours: 2);

  @override
  WordDetailState build(int wordId) => const WordDetailState();

  Future<void> load() async {
    final storage = ref.read(wordDetailLocalStorageProvider);
    final key = StorageKeys.wordCacheJson(wordId);
    final savedKey = StorageKeys.savedWordJson(wordId);
    final saved = await storage.get<String>(savedKey);
    final freshCache = await storage.getWithTtl<String>(key, ttl: cacheTtl);
    if (freshCache != null) {
      state = state.copyWith(
        word: _decode(freshCache),
        isLoading: false,
        isSaved: saved != null,
        clearError: true,
      );
      return;
    }

    final staleCache = await storage.get<String>(key);
    final online = await ref.read(connectivityServiceProvider).isOnline;
    if (!online) {
      state = WordDetailState(
        word: _tryDecode(staleCache),
        isLoading: false,
        isOffline: true,
        isSaved: saved != null,
        errorMessage: staleCache == null ? 'Không có dữ liệu từ đã lưu.' : null,
      );
      return;
    }

    try {
      final word = await ref.read(wordDetailRepositoryProvider).getWord(wordId);
      await storage.setWithTtl(key, jsonEncode(word.toJson()));
      state = WordDetailState(
        word: word,
        isLoading: false,
        isSaved: saved != null,
      );
    } catch (_) {
      state = WordDetailState(
        word: _tryDecode(staleCache),
        isLoading: false,
        isOffline: staleCache != null,
        isSaved: saved != null,
        errorMessage: staleCache == null ? 'Không thể tải chi tiết từ.' : null,
      );
    }
  }

  Future<void> saveToWordBook() async {
    final word = state.word;
    if (word == null) return;
    final storage = ref.read(wordDetailLocalStorageProvider);
    await storage.set(
      StorageKeys.savedWordJson(wordId),
      jsonEncode(word.toJson()),
    );
    await storage.setWithTtl(
      StorageKeys.wordCacheJson(wordId),
      jsonEncode(word.toJson()),
    );
    state = state.copyWith(isSaved: true);
  }

  WordDetail _decode(String source) =>
      WordDetail.fromJson(jsonDecode(source) as Map<String, dynamic>);

  WordDetail? _tryDecode(String? source) {
    try {
      return source == null ? null : _decode(source);
    } on FormatException {
      return null;
    }
  }
}
