import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:vocanova_mobile/features/progress/application/progress_charts_state.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';

part 'progress_charts_notifier.g.dart';

@riverpod
class ProgressChartsNotifier extends _$ProgressChartsNotifier {
  @override
  ProgressChartsState build() => const ProgressChartsState();

  Future<void> load() async {
    try {
      final repository = ref.read(progressRepositoryProvider);
      final chartFuture = repository.getChart(state.granularity);
      final masteryFuture = repository.getMasteryBreakdown();
      final weakestFuture = repository.getWeakestWords();
      state = ProgressChartsState(
        granularity: state.granularity,
        chart: await chartFuture,
        mastery: await masteryFuture,
        weakestWords: await weakestFuture,
        isLoading: false,
      );
    } catch (_) {
      state = state.copyWith(
        isLoading: false,
        errorMessage: 'Không thể tải biểu đồ tiến độ.',
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
      final chart = await ref.read(progressRepositoryProvider).getChart(value);
      state = state.copyWith(chart: chart, isLoadingChart: false);
    } catch (_) {
      state = state.copyWith(
        isLoadingChart: false,
        errorMessage: 'Không thể đổi khoảng thời gian biểu đồ.',
      );
    }
  }
}
