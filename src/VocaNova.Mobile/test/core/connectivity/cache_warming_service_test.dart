import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/core/connectivity/cache_warming_service.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/lists/data/services/lists_api_service.dart';
import 'package:vocanova_mobile/features/lists/domain/models/user_list.dart';
import 'package:vocanova_mobile/features/progress/data/services/progress_api_service.dart';
import 'package:vocanova_mobile/features/progress/domain/models/progress_summary.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  test('warms lists and progress summary caches', () async {
    SharedPreferences.setMockInitialValues({});
    final preferences = await SharedPreferences.getInstance();
    final listsApiService = MockListsApiService();
    final progressApiService = MockProgressApiService();
    when(() => listsApiService.getLists()).thenAnswer((_) async => [list]);
    when(
      () => progressApiService.getSummary(),
    ).thenAnswer((_) async => summary);

    await CacheWarmingService(
      listsApiService: listsApiService,
      progressApiService: progressApiService,
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

class MockListsApiService extends Mock implements ListsApiService {}

class MockProgressApiService extends Mock implements ProgressApiService {}

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
