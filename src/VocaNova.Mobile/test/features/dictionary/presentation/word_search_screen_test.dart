import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/features/dictionary/application/connectivity_service.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/word_search_screen.dart';

void main() {
  late MockWordSearchRepository repository;
  late MockConnectivityService connectivity;
  late MockLocalStorage storage;

  setUp(() {
    repository = MockWordSearchRepository();
    connectivity = MockConnectivityService();
    storage = MockLocalStorage();
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => storage.get<String>(any())).thenAnswer((_) async => null);
    when(() => storage.set<String>(any(), any())).thenAnswer((_) async {});
    when(() => repository.getTopics()).thenAnswer(
      (_) async => const [
        TopicSummary(
          topicId: 2,
          name: 'Travel',
          nameVi: 'Du lịch',
          wordCount: 4,
        ),
      ],
    );
  });

  testWidgets('search bar stays visible and renders topics from API', (
    tester,
  ) async {
    await pumpSearch(tester, repository, connectivity, storage);

    expect(find.byKey(const Key('word-search-field')), findsOneWidget);
    expect(find.text('Du lịch'), findsOneWidget);
    expect(find.text('Chưa có lịch sử tìm kiếm.'), findsOneWidget);
  });

  testWidgets('debounced search shows skeleton then word summary card', (
    tester,
  ) async {
    final response = Completer<List<WordSummary>>();
    when(
      () => repository.search(query: 'hel', cefr: null, topicId: null),
    ).thenAnswer((_) => response.future);
    await pumpSearch(tester, repository, connectivity, storage);

    await tester.enterText(find.byKey(const Key('word-search-field')), 'hel');
    await tester.pump(const Duration(milliseconds: 301));

    expect(find.byKey(const Key('word-search-field')), findsOneWidget);
    expect(find.byKey(const Key('search-loading-skeleton')), findsOneWidget);

    response.complete(const [
      WordSummary(
        wordId: 1,
        word: 'hello',
        phonetic: '/həˈləʊ/',
        cefr: 'A1',
        primaryMeaning: 'xin chào',
      ),
    ]);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('hello'), findsOneWidget);
    expect(find.text('/həˈləʊ/'), findsOneWidget);
    expect(find.text('A1'), findsWidgets);
    expect(find.text('xin chào'), findsOneWidget);
  });
}

Future<void> pumpSearch(
  WidgetTester tester,
  WordSearchRepository repository,
  ConnectivityService connectivity,
  LocalStorage storage,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        wordSearchRepositoryProvider.overrideWithValue(repository),
        connectivityServiceProvider.overrideWithValue(connectivity),
        searchLocalStorageProvider.overrideWithValue(storage),
      ],
      child: const MaterialApp(home: WordSearchScreen()),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

class MockWordSearchRepository extends Mock implements WordSearchRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

class MockLocalStorage extends Mock implements LocalStorage {}
