import 'package:vocanova_mobile/features/progress/domain/progress_analytics.dart';

class ProgressChartsState {
  const ProgressChartsState({
    this.granularity = 'daily',
    this.chart,
    this.mastery = const [],
    this.weakestWords = const [],
    this.isLoading = true,
    this.isLoadingChart = false,
    this.errorMessage,
  });

  final String granularity;
  final ProgressChart? chart;
  final List<MasteryBreakdown> mastery;
  final List<WeakestWord> weakestWords;
  final bool isLoading;
  final bool isLoadingChart;
  final String? errorMessage;

  ProgressChartsState copyWith({
    String? granularity,
    ProgressChart? chart,
    List<MasteryBreakdown>? mastery,
    List<WeakestWord>? weakestWords,
    bool? isLoading,
    bool? isLoadingChart,
    String? errorMessage,
    bool clearError = false,
  }) => ProgressChartsState(
    granularity: granularity ?? this.granularity,
    chart: chart ?? this.chart,
    mastery: mastery ?? this.mastery,
    weakestWords: weakestWords ?? this.weakestWords,
    isLoading: isLoading ?? this.isLoading,
    isLoadingChart: isLoadingChart ?? this.isLoadingChart,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
