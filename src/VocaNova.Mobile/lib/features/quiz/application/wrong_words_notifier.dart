import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_state.dart';

part 'wrong_words_notifier.g.dart';

@riverpod
class WrongWordsNotifier extends _$WrongWordsNotifier {
  @override
  WrongWordsState build() => const WrongWordsState();

  Future<void> load() async {
    try {
      final result = await ref
          .read(quizRepositoryProvider)
          .getWrongWords(page: 1);
      state = WrongWordsState(
        words: result.items,
        page: result.page,
        totalPages: result.totalPages,
        isLoading: false,
      );
    } catch (_) {
      state = const WrongWordsState(
        isLoading: false,
        errorMessage: 'Không thể tải danh sách từ sai.',
      );
    }
  }

  Future<void> loadMore() async {
    if (!state.hasMore || state.isLoadingMore) return;
    state = state.copyWith(isLoadingMore: true, clearError: true);
    try {
      final result = await ref
          .read(quizRepositoryProvider)
          .getWrongWords(page: state.page + 1);
      state = state.copyWith(
        words: [...state.words, ...result.items],
        page: result.page,
        totalPages: result.totalPages,
        isLoadingMore: false,
      );
    } catch (_) {
      state = state.copyWith(
        isLoadingMore: false,
        errorMessage: 'Không thể tải thêm từ sai.',
      );
    }
  }

  Future<bool> remove(int wordId) async {
    final index = state.words.indexWhere((word) => word.wordId == wordId);
    if (index < 0) return false;
    final removed = state.words[index];
    state = state.copyWith(
      words: [...state.words]..removeAt(index),
      clearError: true,
    );
    try {
      await ref.read(quizRepositoryProvider).removeWrongWord(wordId);
      return true;
    } catch (_) {
      state = state.copyWith(
        words: [...state.words]..insert(index, removed),
        errorMessage: 'Không thể bỏ từ khỏi danh sách sai.',
      );
      return false;
    }
  }
}
