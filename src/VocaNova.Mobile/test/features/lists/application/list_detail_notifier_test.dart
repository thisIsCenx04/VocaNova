import 'dart:async';
import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_service.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/features/lists/application/list_detail_notifier.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/list_word.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late MockListsRepository repository;
  late MockConnectivityService connectivity;
  late LocalStorage storage;
  late ProviderContainer container;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    storage = LocalStorage.create(
      preferences: await SharedPreferences.getInstance(),
    );
    repository = MockListsRepository();
    connectivity = MockConnectivityService();
    container = ProviderContainer(
      overrides: [
        listsRepositoryProvider.overrideWithValue(repository),
        listsLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
      ],
    );
  });

  tearDown(() => container.dispose());

  test('loads pages and appends the next page', () async {
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.getWords(listId: 3, page: 1)).thenAnswer(
      (_) async => ListWordsPage(items: [hello], page: 1, totalPages: 2),
    );
    when(() => repository.getWords(listId: 3, page: 2)).thenAnswer(
      (_) async => ListWordsPage(items: [world], page: 2, totalPages: 2),
    );
    final notifier = container.read(listDetailProvider(3).notifier);

    await notifier.load();
    await notifier.loadMore();

    final state = container.read(listDetailProvider(3));
    expect(state.words.map((word) => word.word), ['hello', 'world']);
    expect(state.hasMore, isFalse);
  });

  test('offline load uses cached list words', () async {
    await storage.set(
      StorageKeys.listWordsCacheJson(3),
      jsonEncode([hello.toJson()]),
    );
    when(() => connectivity.isOnline).thenAnswer((_) async => false);

    await container.read(listDetailProvider(3).notifier).load();

    final state = container.read(listDetailProvider(3));
    expect(state.isOffline, isTrue);
    expect(state.words.single.word, 'hello');
    verifyNever(
      () => repository.getWords(
        listId: any(named: 'listId'),
        page: any(named: 'page'),
      ),
    );
  });

  test('remove is optimistic and rolls back on API failure', () async {
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.getWords(listId: 3, page: 1)).thenAnswer(
      (_) async => ListWordsPage(items: [hello], page: 1, totalPages: 1),
    );
    final response = Completer<void>();
    when(
      () => repository.removeWord(listId: 3, wordId: 7),
    ).thenAnswer((_) => response.future);
    final notifier = container.read(listDetailProvider(3).notifier);
    await notifier.load();

    final future = notifier.removeWord(7);
    expect(container.read(listDetailProvider(3)).words, isEmpty);
    response.completeError(StateError('failed'));
    expect(await future, isFalse);

    expect(container.read(listDetailProvider(3)).words.single.wordId, 7);
  });
}

class MockListsRepository extends Mock implements ListsRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

final hello = ListWord(
  wordId: 7,
  word: 'hello',
  primaryMeaning: 'xin chào',
  correctCount: 3,
  wrongCount: 1,
  note: 'greeting',
  addedAt: DateTime.utc(2026, 6, 15),
);

final world = ListWord(
  wordId: 8,
  word: 'world',
  correctCount: 0,
  wrongCount: 0,
  addedAt: DateTime.utc(2026, 6, 15),
);
