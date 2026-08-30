import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_service.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_summary.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/data/services/lists_api_service.dart';
import 'package:vocanova_mobile/features/lists/domain/models/user_list.dart';
import 'package:vocanova_mobile/features/lists/presentation/lists_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  late MockListsApiService repository;
  late MockConnectivityService connectivity;
  late MockLocalStorage storage;

  setUp(() {
    repository = MockListsApiService();
    connectivity = MockConnectivityService();
    storage = MockLocalStorage();
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => storage.get<String>(any())).thenAnswer((_) async => null);
    when(() => storage.set<String>(any(), any())).thenAnswer((_) async {});
    when(() => repository.getLists()).thenAnswer((_) async => [favorites]);
    when(
      () => repository.getPersonalTopics(),
    ).thenAnswer((_) async => [travelTopic]);
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
    expect(find.text('2 words'), findsOneWidget);
    expect(find.text('Created on 15/06/2026'), findsOneWidget);
    expect(find.byKey(const Key('personal-topics-section')), findsOneWidget);
    expect(find.byKey(const Key('personal-topic-card-8')), findsOneWidget);
    expect(find.text('Travel'), findsOneWidget);
    expect(find.text('4 words'), findsOneWidget);

    await tester.tap(find.byKey(const Key('create-list-fab')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('submit-list-name')));
    await tester.pump();
    expect(find.text('Please enter a list name.'), findsOneWidget);

    await tester.enterText(find.byKey(const Key('list-name-field')), 'Travel');
    await tester.tap(find.byKey(const Key('submit-list-name')));
    await tester.pumpAndSettle();
    // The personal topic card also localizes to "Travel" now, so there are
    // two: the new list plus the existing personal topic.
    expect(find.text('Travel'), findsNWidgets(2));
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
  ListsApiService repository,
  ConnectivityService connectivity,
  LocalStorage storage,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        listsApiServiceProvider.overrideWithValue(repository),
        listsLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
      ],
      child: MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: const ListsScreen(),
      ),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

class MockListsApiService extends Mock implements ListsApiService {}

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

const travelTopic = PersonalTopicSummary(
  topicId: 8,
  listId: 18,
  name: 'Travel',
  nameVi: 'Du lịch',
  wordCount: 4,
  containsWord: false,
);
