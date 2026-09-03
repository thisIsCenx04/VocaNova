import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_state.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

part 'wrong_words_notifier.g.dart';

@riverpod
class WrongWordsNotifier extends _$WrongWordsNotifier {
  @override
  WrongWordsState build() => const WrongWordsState();

  Future<void> load() async {
    try {
      final result = await ref
          .read(quizApiServiceProvider)
          .getWrongWords(page: 1);
      state = WrongWordsState(
        words: result.items,
        page: result.page,
        totalPages: result.totalPages,
        isLoading: false,
      );
    } catch (_) {
      state = WrongWordsState(
        isLoading: false,
        errorMessage: _l10n.quizWrongWordsLoadError,
      );
    }
  }

  AppLocalizations get _l10n =>
      lookupAppLocalizations(AppSettingsNotifier.instance.state.locale);

  Future<void> loadMore() async {
    if (!state.hasMore || state.isLoadingMore) return;
    state = state.copyWith(isLoadingMore: true, clearError: true);
    try {
      final result = await ref
          .read(quizApiServiceProvider)
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
        errorMessage: _l10n.quizWrongWordsLoadMoreError,
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
      await ref.read(quizApiServiceProvider).removeWrongWord(wordId);
      return true;
    } catch (_) {
      state = state.copyWith(
        words: [...state.words]..insert(index, removed),
        errorMessage: _l10n.quizWrongWordsRemoveError,
      );
      return false;
    }
  }
}
