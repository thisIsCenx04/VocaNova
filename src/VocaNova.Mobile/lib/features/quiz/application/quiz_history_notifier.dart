import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_history_state.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

part 'quiz_history_notifier.g.dart';

@riverpod
class QuizHistoryNotifier extends _$QuizHistoryNotifier {
  @override
  QuizHistoryState build() => const QuizHistoryState();

  Future<void> load() async {
    try {
      final result = await ref.read(quizApiServiceProvider).getHistory(page: 1);
      state = QuizHistoryState(
        items: result.items,
        page: result.page,
        totalPages: result.totalPages,
        isLoading: false,
      );
    } catch (_) {
      state = QuizHistoryState(
        isLoading: false,
        errorMessage: _l10n.quizHistoryLoadError,
      );
    }
  }

  Future<void> loadMore() async {
    if (!state.hasMore || state.isLoadingMore) return;
    state = state.copyWith(isLoadingMore: true, clearError: true);
    try {
      final result = await ref
          .read(quizApiServiceProvider)
          .getHistory(page: state.page + 1);
      state = state.copyWith(
        items: [...state.items, ...result.items],
        page: result.page,
        totalPages: result.totalPages,
        isLoadingMore: false,
      );
    } catch (_) {
      state = state.copyWith(
        isLoadingMore: false,
        errorMessage: _l10n.quizHistoryLoadMoreError,
      );
    }
  }

  AppLocalizations get _l10n =>
      lookupAppLocalizations(AppSettingsNotifier.instance.state.locale);
}
