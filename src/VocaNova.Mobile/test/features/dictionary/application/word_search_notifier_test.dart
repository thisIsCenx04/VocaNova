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
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/dtos/word_summary_dto.dart';
import 'package:vocanova_mobile/features/dictionary/data/services/word_search_api_service.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_summary.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late MockWordSearchApiService repository;
  late MockConnectivityService connectivity;
  late LocalStorage storage;
  late ProviderContainer container;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    storage = LocalStorage.create(
      preferences: await SharedPreferences.getInstance(),
    );
    repository = MockWordSearchApiService();
    connectivity = MockConnectivityService();
    container = ProviderContainer(
      overrides: [
        wordSearchApiServiceProvider.overrideWithValue(repository),
        connectivityServiceProvider.overrideWithValue(connectivity),
        searchLocalStorageProvider.overrideWithValue(storage),
      ],
    );
  });

  tearDown(() => container.dispose());

  test('query debounce calls API only after 300 milliseconds', () async {
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(
      () => repository.search(query: 'hello', cefr: null, topicId: null),
    ).thenAnswer((_) async => const []);

    container.read(wordSearchProvider.notifier).queryChanged('hello');
    await Future<void>.delayed(const Duration(milliseconds: 200));
    verifyNever(
      () => repository.search(query: 'hello', cefr: null, topicId: null),
    );
    await Future<void>.delayed(const Duration(milliseconds: 150));

    verify(
      () => repository.search(query: 'hello', cefr: null, topicId: null),
    ).called(1);
  });

  test('history is unique, newest first, and limited to twenty', () async {
    final notifier = container.read(wordSearchProvider.notifier);
    for (var index = 0; index < 22; index++) {
      await notifier.recordHistory('word-$index');
    }
    await notifier.recordHistory('word-10');

    final history = container.read(wordSearchProvider).history;
    expect(history.length, 20);
    expect(history.first, 'word-10');
    expect(history.where((word) => word == 'word-10'), hasLength(1));
  });

  test('offline search filters cached words without API call', () async {
    await storage.set(
      StorageKeys.wordSearchCacheJson,
      jsonEncode([
        WordSummaryDto.fromDomain(
          const WordSummary(
            wordId: 1,
            word: 'hello',
            cefr: 'A1',
            primaryMeaning: 'xin chào',
            topicIds: {2},
          ),
        ).toJson(),
        WordSummaryDto.fromDomain(
          const WordSummary(wordId: 2, word: 'help', cefr: 'A2'),
        ).toJson(),
      ]),
    );
    when(() => connectivity.isOnline).thenAnswer((_) async => false);
    final notifier = container.read(wordSearchProvider.notifier);
    await notifier.initialize();
    notifier.queryChanged('hello');
    await notifier.selectCefr('A1');
    await notifier.selectTopic(2);

    final state = container.read(wordSearchProvider);
    expect(state.isOffline, isTrue);
    expect(state.results.single.word, 'hello');
    verifyNever(
      () => repository.search(
        query: any(named: 'query'),
        cefr: any(named: 'cefr'),
        topicId: any(named: 'topicId'),
      ),
    );
  });

  test(
    'clearing the query resets filters and ignores an in-flight request',
    () async {
      final response = Completer<List<WordSummary>>();
      when(() => connectivity.isOnline).thenAnswer((_) async => true);
      when(
        () => repository.search(query: 'hello', cefr: 'A1', topicId: null),
      ).thenAnswer((_) => response.future);

      final notifier = container.read(wordSearchProvider.notifier);
      notifier.queryChanged('hello');
      final pendingSearch = notifier.selectCefr('A1');
      await Future<void>.delayed(Duration.zero);
      notifier.queryChanged('');
      response.complete(const [WordSummary(wordId: 1, word: 'hello')]);
      await pendingSearch;

      final state = container.read(wordSearchProvider);
      expect(state.query, isEmpty);
      expect(state.selectedCefr, isNull);
      expect(state.selectedTopicId, isNull);
      expect(state.results, isEmpty);
    },
  );
}

class MockWordSearchApiService extends Mock implements WordSearchApiService {}

class MockConnectivityService extends Mock implements ConnectivityService {}
