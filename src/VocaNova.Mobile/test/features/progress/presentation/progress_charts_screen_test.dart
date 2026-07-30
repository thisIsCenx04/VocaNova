import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/progress/data/progress_repository.dart';
import 'package:vocanova_mobile/features/progress/domain/progress_analytics.dart';
import 'package:vocanova_mobile/features/progress/presentation/progress_charts_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  testWidgets('renders line chart, horizontal mastery, and weakest words', (
    tester,
  ) async {
    final repository = MockProgressRepository();
    when(() => repository.getChart('daily')).thenAnswer((_) async => daily);
    when(() => repository.getChart('weekly')).thenAnswer((_) async => weekly);
    when(
      () => repository.getMasteryBreakdown(),
    ).thenAnswer((_) async => mastery);
    when(() => repository.getWeakestWords()).thenAnswer((_) async => weakest);
    tester.view.physicalSize = const Size(900, 2400);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(
      ProviderScope(
        overrides: [progressRepositoryProvider.overrideWithValue(repository)],
        child: MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: const ProgressChartsScreen(),
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 200));

    expect(find.byType(LineChart), findsOneWidget);
    expect(find.byKey(const Key('mastery-level-0')), findsOneWidget);
    expect(find.byKey(const Key('mastery-level-5')), findsOneWidget);
    expect(find.byKey(const Key('weakest-word-7')), findsOneWidget);
    expect(find.text('25%'), findsOneWidget);

    await tester.tap(find.byKey(const Key('chart-granularity')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Weekly').last);
    await tester.pumpAndSettle();

    verify(() => repository.getChart('weekly')).called(1);
  });
}

class MockProgressRepository extends Mock implements ProgressRepository {}

const daily = ProgressChart(
  granularity: 'daily',
  points: [
    ProgressChartPoint(periodLabel: '06-14', sessionsCount: 1, accuracy: 50),
    ProgressChartPoint(periodLabel: '06-15', sessionsCount: 3, accuracy: 75),
  ],
);

const weekly = ProgressChart(
  granularity: 'weekly',
  points: [
    ProgressChartPoint(
      periodLabel: '2026-06-09',
      sessionsCount: 4,
      accuracy: 80,
    ),
  ],
);

const mastery = [
  MasteryBreakdown(masteryLevel: 0, wordCount: 2),
  MasteryBreakdown(masteryLevel: 1, wordCount: 4),
  MasteryBreakdown(masteryLevel: 2, wordCount: 5),
  MasteryBreakdown(masteryLevel: 3, wordCount: 3),
  MasteryBreakdown(masteryLevel: 4, wordCount: 2),
  MasteryBreakdown(masteryLevel: 5, wordCount: 7),
];

const weakest = [
  WeakestWord(
    wordId: 7,
    word: 'apple',
    primaryMeaning: 'quả táo',
    testCount: 4,
    correctCount: 1,
    wrongCount: 3,
    accuracyRate: 25,
    masteryLevel: 1,
  ),
];
