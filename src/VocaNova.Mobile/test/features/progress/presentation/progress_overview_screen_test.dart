import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_service.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/progress/data/progress_repository.dart';
import 'package:vocanova_mobile/features/progress/domain/progress_summary.dart';
import 'package:vocanova_mobile/features/progress/presentation/progress_overview_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('renders all progress overview metrics from API', (tester) async {
    final repository = MockProgressRepository();
    final connectivity = MockConnectivityService();
    SharedPreferences.setMockInitialValues({});
    final storage = LocalStorage.create(
      preferences: await SharedPreferences.getInstance(),
    );
    when(() => repository.getSummary()).thenAnswer((_) async => summary);
    when(() => connectivity.isOnline).thenAnswer((_) async => true);

    await pumpOverview(tester, repository, connectivity, storage);

    expect(find.byKey(const Key('streak-card')), findsOneWidget);
    expect(find.text('4 days in a row'), findsOneWidget);
    expect(find.text('Record: 9 days'), findsOneWidget);
    expect(find.byKey(const Key('accuracy-gauge')), findsOneWidget);
    expect(find.text('75%'), findsOneWidget);
    expect(find.text('15/20 correct'), findsOneWidget);
    expect(find.text('42'), findsOneWidget);
    expect(find.text('12'), findsOneWidget);
    expect(find.text('8'), findsOneWidget);
    expect(find.byKey(const Key('open-progress-charts')), findsOneWidget);
  });

  testWidgets('offline renders fresh cached summary and banner', (
    tester,
  ) async {
    final repository = MockProgressRepository();
    final connectivity = MockConnectivityService();
    SharedPreferences.setMockInitialValues({});
    final storage = LocalStorage.create(
      preferences: await SharedPreferences.getInstance(),
    );
    await storage.setWithTtl(
      StorageKeys.progressSummaryJson,
      jsonEncode(summary.toJson()),
    );
    when(() => connectivity.isOnline).thenAnswer((_) async => false);

    await pumpOverview(tester, repository, connectivity, storage);

    expect(find.byKey(const Key('progress-offline-banner')), findsOneWidget);
    expect(find.text('4 days in a row'), findsOneWidget);
  });
}

Future<void> pumpOverview(
  WidgetTester tester,
  ProgressRepository repository,
  ConnectivityService connectivity,
  LocalStorage storage,
) async {
  tester.view.physicalSize = const Size(800, 1600);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        progressRepositoryProvider.overrideWithValue(repository),
        progressLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
      ],
      child: MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: const ProgressOverviewScreen(),
      ),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

class MockProgressRepository extends Mock implements ProgressRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

const summary = ProgressSummary(
  currentStreakDays: 4,
  longestStreakDays: 9,
  accuracy7Days: 75,
  correct7Days: 15,
  totalAnswers7Days: 20,
  totalWordsInProgress: 42,
  masteredWords: 12,
  sessionsThisMonth: 8,
);
