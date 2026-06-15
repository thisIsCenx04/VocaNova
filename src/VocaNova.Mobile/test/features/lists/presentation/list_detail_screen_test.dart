import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_service.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/list_word.dart';
import 'package:vocanova_mobile/features/lists/presentation/list_detail_screen.dart';

void main() {
  late MockListsRepository repository;
  late MockWordSearchRepository searchRepository;
  late MockConnectivityService connectivity;
  late MockLocalStorage storage;

  setUp(() {
    repository = MockListsRepository();
    searchRepository = MockWordSearchRepository();
    connectivity = MockConnectivityService();
    storage = MockLocalStorage();
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => storage.get<String>(any())).thenAnswer((_) async => null);
    when(() => storage.set<String>(any(), any())).thenAnswer((_) async {});
    when(() => repository.getWords(listId: 3, page: 1)).thenAnswer(
      (_) async => ListWordsPage(items: [hello], page: 1, totalPages: 1),
    );
    when(
      () => repository.addWord(listId: 3, wordId: 8),
    ).thenAnswer((_) async => world);
    when(
      () => repository.addRandomWords(
        listId: 3,
        count: 5,
        method: 'random_topic',
        topicId: 2,
      ),
    ).thenAnswer((_) async => [world]);
    when(
      () => searchRepository.search(query: 'wor', cefr: null, topicId: null),
    ).thenAnswer(
      (_) async => const [
        WordSummary(wordId: 8, word: 'world', primaryMeaning: 'thế giới'),
      ],
    );
    when(() => searchRepository.getTopics()).thenAnswer(
      (_) async => const [
        TopicSummary(topicId: 2, name: 'Travel', wordCount: 10),
      ],
    );
  });

  testWidgets('renders word card details and action buttons', (tester) async {
    await pumpDetail(
      tester,
      repository,
      searchRepository,
      connectivity,
      storage,
    );

    expect(find.text('hello'), findsOneWidget);
    expect(find.text('xin chào'), findsOneWidget);
    expect(find.text('Đúng: 3'), findsOneWidget);
    expect(find.text('Sai: 1'), findsOneWidget);
    expect(find.text('Ghi chú: greeting'), findsOneWidget);
    expect(find.byKey(const Key('add-word-fab')), findsOneWidget);
    expect(find.byKey(const Key('add-random-words')), findsOneWidget);
    expect(find.byKey(const Key('start-list-quiz')), findsOneWidget);
  });

  testWidgets('AddWordSheet searches and adds selected word', (tester) async {
    await pumpDetail(
      tester,
      repository,
      searchRepository,
      connectivity,
      storage,
    );

    await tester.tap(find.byKey(const Key('add-word-fab')));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('add-word-search-field')),
      'wor',
    );
    await tester.pump(const Duration(milliseconds: 301));
    await tester.pump();
    await tester.tap(find.byKey(const Key('add-search-word-8')));
    await tester.pumpAndSettle();

    verify(() => repository.addWord(listId: 3, wordId: 8)).called(1);
    expect(find.text('world'), findsOneWidget);
  });

  testWidgets('random dialog submits topic method and count', (tester) async {
    await pumpDetail(
      tester,
      repository,
      searchRepository,
      connectivity,
      storage,
    );

    await tester.tap(find.byKey(const Key('add-random-words')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('submit-random-words')));
    await tester.pumpAndSettle();

    verify(
      () => repository.addRandomWords(
        listId: 3,
        count: 5,
        method: 'random_topic',
        topicId: 2,
      ),
    ).called(1);
    expect(find.text('world'), findsOneWidget);
  });
}

Future<void> pumpDetail(
  WidgetTester tester,
  ListsRepository repository,
  WordSearchRepository searchRepository,
  ConnectivityService connectivity,
  LocalStorage storage,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        listsRepositoryProvider.overrideWithValue(repository),
        listsLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
        wordSearchRepositoryProvider.overrideWithValue(searchRepository),
      ],
      child: const MaterialApp(home: ListDetailScreen(listId: 3)),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

class MockListsRepository extends Mock implements ListsRepository {}

class MockWordSearchRepository extends Mock implements WordSearchRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

class MockLocalStorage extends Mock implements LocalStorage {}

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
  primaryMeaning: 'thế giới',
  correctCount: 0,
  wrongCount: 0,
  addedAt: DateTime.utc(2026, 6, 15),
);
