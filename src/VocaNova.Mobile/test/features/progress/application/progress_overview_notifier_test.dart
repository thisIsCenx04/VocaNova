import 'dart:convert';

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

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late MockProgressRepository repository;
  late MockConnectivityService connectivity;
  late LocalStorage storage;
  late ProviderContainer container;
  var now = DateTime(2026, 6, 15, 10);

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    now = DateTime(2026, 6, 15, 10);
    storage = LocalStorage.create(
      preferences: await SharedPreferences.getInstance(),
      clock: () => now,
    );
    repository = MockProgressRepository();
    connectivity = MockConnectivityService();
    container = ProviderContainer(
      overrides: [
        progressRepositoryProvider.overrideWithValue(repository),
        progressLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
      ],
    );
  });

  tearDown(() => container.dispose());

  test('online load gets API summary and writes 15 minute cache', () async {
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.getSummary()).thenAnswer((_) async => summary);

    await container.read(progressOverviewProvider.notifier).load();

    final state = container.read(progressOverviewProvider);
    expect(state.summary, summary);
    expect(state.isOffline, isFalse);
    final cached = await storage.getWithTtl<String>(
      StorageKeys.progressSummaryJson,
      ttl: ProgressOverviewNotifier.cacheTtl,
    );
    expect(cached, isNotNull);
  });

  test(
    'offline load uses a fresh cached summary and shows offline state',
    () async {
      await storage.setWithTtl(
        StorageKeys.progressSummaryJson,
        jsonEncode(summary.toJson()),
      );
      when(() => connectivity.isOnline).thenAnswer((_) async => false);

      await container.read(progressOverviewProvider.notifier).load();

      final state = container.read(progressOverviewProvider);
      expect(state.summary?.currentStreakDays, 4);
      expect(state.isOffline, isTrue);
      verifyNever(() => repository.getSummary());
    },
  );

  test('offline load rejects cache after 15 minutes', () async {
    await storage.setWithTtl(
      StorageKeys.progressSummaryJson,
      jsonEncode(summary.toJson()),
    );
    now = now.add(const Duration(minutes: 15));
    when(() => connectivity.isOnline).thenAnswer((_) async => false);

    await container.read(progressOverviewProvider.notifier).load();

    final state = container.read(progressOverviewProvider);
    expect(state.summary, isNull);
    expect(state.isOffline, isTrue);
    expect(state.errorMessage, isNotNull);
  });
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
