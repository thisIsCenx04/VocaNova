import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_charts_state.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

part 'progress_charts_notifier.g.dart';

@riverpod
class ProgressChartsNotifier extends _$ProgressChartsNotifier {
  @override
  ProgressChartsState build() => const ProgressChartsState();

  Future<void> load() async {
    try {
      final apiService = ref.read(progressApiServiceProvider);
      final chartFuture = apiService.getChart(state.granularity);
      final masteryFuture = apiService.getMasteryBreakdown();
      final weakestFuture = apiService.getWeakestWords();
      state = ProgressChartsState(
        granularity: state.granularity,
        chart: await chartFuture,
        mastery: await masteryFuture,
        weakestWords: await weakestFuture,
        isLoading: false,
      );
    } catch (_) {
      final l10n = lookupAppLocalizations(
        AppSettingsNotifier.instance.state.locale,
      );
      state = state.copyWith(
        isLoading: false,
        errorMessage: l10n.progressLoadChartsError,
      );
    }
  }

  Future<void> changeGranularity(String value) async {
    if (value == state.granularity || state.isLoadingChart) return;
    state = state.copyWith(
      granularity: value,
      isLoadingChart: true,
      clearError: true,
    );
    try {
      final chart = await ref.read(progressApiServiceProvider).getChart(value);
      state = state.copyWith(chart: chart, isLoadingChart: false);
    } catch (_) {
      final l10n = lookupAppLocalizations(
        AppSettingsNotifier.instance.state.locale,
      );
      state = state.copyWith(
        isLoadingChart: false,
        errorMessage: l10n.progressChangeGranularityError,
      );
    }
  }
}
