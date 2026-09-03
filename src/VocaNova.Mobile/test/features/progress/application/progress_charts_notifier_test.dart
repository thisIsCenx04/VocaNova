import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/features/progress/application/progress_charts_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/progress/data/services/progress_api_service.dart';
import 'package:vocanova_mobile/features/progress/domain/models/progress_analytics.dart';

void main() {
  test(
    'loads analytics and only reloads chart when granularity changes',
    () async {
      final repository = MockProgressApiService();
      when(() => repository.getChart('daily')).thenAnswer((_) async => daily);
      when(() => repository.getChart('weekly')).thenAnswer((_) async => weekly);
      when(
        () => repository.getMasteryBreakdown(),
      ).thenAnswer((_) async => mastery);
      when(() => repository.getWeakestWords()).thenAnswer((_) async => weakest);
      final container = ProviderContainer(
        overrides: [progressApiServiceProvider.overrideWithValue(repository)],
      );
      addTearDown(container.dispose);
      final subscription = container.listen(progressChartsProvider, (_, _) {});
      addTearDown(subscription.close);
      final notifier = container.read(progressChartsProvider.notifier);

      await notifier.load();
      await notifier.changeGranularity('weekly');

      final state = container.read(progressChartsProvider);
      expect(state.granularity, 'weekly');
      expect(state.chart, weekly);
      expect(state.mastery, mastery);
      expect(state.weakestWords, weakest);
      verify(() => repository.getMasteryBreakdown()).called(1);
      verify(() => repository.getWeakestWords()).called(1);
    },
  );
}

class MockProgressApiService extends Mock implements ProgressApiService {}

const daily = ProgressChart(
  granularity: 'daily',
  points: [
    ProgressChartPoint(
      periodLabel: '2026-06-15',
      sessionsCount: 2,
      accuracy: 50,
    ),
  ],
);

const weekly = ProgressChart(
  granularity: 'weekly',
  points: [
    ProgressChartPoint(
      periodLabel: '2026-06-09',
      sessionsCount: 3,
      accuracy: 75,
    ),
  ],
);

const mastery = [
  MasteryBreakdown(masteryLevel: 0, wordCount: 2),
  MasteryBreakdown(masteryLevel: 5, wordCount: 7),
];

const weakest = [
  WeakestWord(
    wordId: 7,
    word: 'apple',
    testCount: 4,
    correctCount: 1,
    wrongCount: 3,
    accuracyRate: 25,
    masteryLevel: 1,
  ),
];
