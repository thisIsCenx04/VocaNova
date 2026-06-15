import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/core/connectivity/cache_warming_service.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';
import 'package:vocanova_mobile/features/progress/data/progress_repository.dart';
import 'package:vocanova_mobile/features/progress/domain/progress_summary.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('warms lists and progress summary caches', () async {
    SharedPreferences.setMockInitialValues({});
    final preferences = await SharedPreferences.getInstance();
    final listsRepository = MockListsRepository();
    final progressRepository = MockProgressRepository();
    when(() => listsRepository.getLists()).thenAnswer((_) async => [list]);
    when(
      () => progressRepository.getSummary(),
    ).thenAnswer((_) async => summary);

    await CacheWarmingService(
      listsRepository: listsRepository,
      progressRepository: progressRepository,
      storage: LocalStorage.create(preferences: preferences),
    ).warm();

    expect(
      jsonDecode(preferences.getString(StorageKeys.listsCacheJson)!),
      isA<List<dynamic>>(),
    );
    expect(
      jsonDecode(preferences.getString(StorageKeys.progressSummaryJson)!),
      isA<Map<String, dynamic>>(),
    );
  });
}

class MockListsRepository extends Mock implements ListsRepository {}

class MockProgressRepository extends Mock implements ProgressRepository {}

final list = UserList(
  listId: 3,
  listName: 'Favorites',
  wordCount: 2,
  createdAt: DateTime.utc(2026, 6, 15),
);

const summary = ProgressSummary(
  currentStreakDays: 2,
  longestStreakDays: 5,
  accuracy7Days: 80,
  correct7Days: 8,
  totalAnswers7Days: 10,
  totalWordsInProgress: 4,
  masteredWords: 3,
  sessionsThisMonth: 6,
);
