import 'dart:async';
import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/dictionary/application/connectivity_service.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';

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

  test('offline load shows cached lists without API request', () async {
    await storage.set(
      StorageKeys.listsCacheJson,
      jsonEncode([favorites.toJson()]),
    );
    when(() => connectivity.isOnline).thenAnswer((_) async => false);

    await container.read(listsProvider.notifier).load();

    final state = container.read(listsProvider);
    expect(state.isOffline, isTrue);
    expect(state.lists.single.listName, 'Favorites');
    verifyNever(repository.getLists);
  });

  test('delete is optimistic and rolls back when API fails', () async {
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.getLists()).thenAnswer((_) async => [favorites]);
    final deleteResponse = Completer<void>();
    when(() => repository.delete(3)).thenAnswer((_) => deleteResponse.future);
    final notifier = container.read(listsProvider.notifier);
    await notifier.load();

    final future = notifier.delete(3);
    expect(container.read(listsProvider).lists, isEmpty);
    deleteResponse.completeError(StateError('failed'));
    expect(await future, isFalse);

    expect(container.read(listsProvider).lists.single.listId, 3);
    expect(container.read(listsProvider).errorMessage, isNotNull);
  });

  test('create and rename update state and cache', () async {
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.getLists()).thenAnswer((_) async => []);
    when(() => repository.create('Travel')).thenAnswer((_) async => travel);
    when(
      () => repository.rename(listId: 4, name: 'Trips'),
    ).thenAnswer((_) async => trips);
    final notifier = container.read(listsProvider.notifier);
    await notifier.load();

    expect(await notifier.create('Travel'), isTrue);
    expect(await notifier.rename(listId: 4, name: 'Trips'), isTrue);

    expect(container.read(listsProvider).lists.single.listName, 'Trips');
    expect(
      await storage.get<String>(StorageKeys.listsCacheJson),
      contains('Trips'),
    );
  });
}

class MockListsRepository extends Mock implements ListsRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

final favorites = UserList(
  listId: 3,
  listName: 'Favorites',
  wordCount: 2,
  createdAt: DateTime.utc(2026, 6, 15),
);

final travel = UserList(
  listId: 4,
  listName: 'Travel',
  wordCount: 0,
  createdAt: DateTime.utc(2026, 6, 15),
);

final trips = UserList(
  listId: 4,
  listName: 'Trips',
  wordCount: 0,
  createdAt: DateTime.utc(2026, 6, 15),
);
