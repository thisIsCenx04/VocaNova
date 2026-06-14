import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/features/dictionary/application/connectivity_service.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';
import 'package:vocanova_mobile/features/lists/presentation/lists_screen.dart';

void main() {
  late MockListsRepository repository;
  late MockConnectivityService connectivity;
  late MockLocalStorage storage;

  setUp(() {
    repository = MockListsRepository();
    connectivity = MockConnectivityService();
    storage = MockLocalStorage();
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => storage.get<String>(any())).thenAnswer((_) async => null);
    when(() => storage.set<String>(any(), any())).thenAnswer((_) async {});
    when(() => repository.getLists()).thenAnswer((_) async => [favorites]);
    when(() => repository.create('Travel')).thenAnswer((_) async => travel);
    when(
      () => repository.rename(listId: 3, name: 'Daily'),
    ).thenAnswer((_) async => daily);
    when(() => repository.delete(3)).thenAnswer((_) async {});
  });

  testWidgets('renders grid and create dialog validates then creates list', (
    tester,
  ) async {
    await pumpLists(tester, repository, connectivity, storage);

    expect(find.byKey(const Key('lists-grid')), findsOneWidget);
    expect(find.text('Favorites'), findsOneWidget);
    expect(find.text('2 từ'), findsOneWidget);
    expect(find.text('Tạo ngày 15/06/2026'), findsOneWidget);

    await tester.tap(find.byKey(const Key('create-list-fab')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('submit-list-name')));
    await tester.pump();
    expect(find.text('Vui lòng nhập tên danh sách.'), findsOneWidget);

    await tester.enterText(find.byKey(const Key('list-name-field')), 'Travel');
    await tester.tap(find.byKey(const Key('submit-list-name')));
    await tester.pumpAndSettle();
    expect(find.text('Travel'), findsOneWidget);
    verify(() => repository.create('Travel')).called(1);
  });

  testWidgets('long press supports rename and confirmed delete', (
    tester,
  ) async {
    await pumpLists(tester, repository, connectivity, storage);

    await tester.longPress(find.byKey(const Key('list-card-3')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('rename-list-action')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('list-name-field')), 'Daily');
    await tester.tap(find.byKey(const Key('submit-list-name')));
    await tester.pumpAndSettle();
    expect(find.text('Daily'), findsOneWidget);

    await tester.longPress(find.byKey(const Key('list-card-3')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('delete-list-action')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('confirm-delete-list')));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('list-card-3')), findsNothing);
    verify(() => repository.delete(3)).called(1);
  });
}

Future<void> pumpLists(
  WidgetTester tester,
  ListsRepository repository,
  ConnectivityService connectivity,
  LocalStorage storage,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        listsRepositoryProvider.overrideWithValue(repository),
        listsLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
      ],
      child: const MaterialApp(home: ListsScreen()),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

class MockListsRepository extends Mock implements ListsRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

class MockLocalStorage extends Mock implements LocalStorage {}

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

final daily = UserList(
  listId: 3,
  listName: 'Daily',
  wordCount: 2,
  createdAt: DateTime.utc(2026, 6, 15),
);
