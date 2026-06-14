import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/features/dictionary/application/connectivity_service.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_detail_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late MockWordDetailRepository repository;
  late MockConnectivityService connectivity;
  late LocalStorage storage;
  late ProviderContainer container;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    storage = LocalStorage.create(
      preferences: await SharedPreferences.getInstance(),
    );
    repository = MockWordDetailRepository();
    connectivity = MockConnectivityService();
    container = ProviderContainer(
      overrides: [
        wordDetailRepositoryProvider.overrideWithValue(repository),
        wordDetailLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
      ],
    );
  });

  tearDown(() => container.dispose());

  test('fresh two-hour cache is checked before API', () async {
    await storage.setWithTtl(
      StorageKeys.wordCacheJson(7),
      jsonEncode(word.toJson()),
    );

    await container.read(wordDetailProvider(7).notifier).load();

    expect(container.read(wordDetailProvider(7)).word?.word, 'hello');
    verifyNever(() => repository.getWord(any()));
  });

  test('offline uses stale cache without API', () async {
    await storage.set(StorageKeys.wordCacheJson(7), jsonEncode(word.toJson()));
    when(() => connectivity.isOnline).thenAnswer((_) async => false);

    await container.read(wordDetailProvider(7).notifier).load();

    expect(container.read(wordDetailProvider(7)).isOffline, isTrue);
    expect(container.read(wordDetailProvider(7)).word?.word, 'hello');
    verifyNever(() => repository.getWord(any()));
  });

  test('saveToWordBook persists saved word key', () async {
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.getWord(7)).thenAnswer((_) async => word);
    final notifier = container.read(wordDetailProvider(7).notifier);
    await notifier.load();

    await notifier.saveToWordBook();

    expect(container.read(wordDetailProvider(7)).isSaved, isTrue);
    expect(await storage.get<String>(StorageKeys.savedWordJson(7)), isNotNull);
  });
}

class MockWordDetailRepository extends Mock implements WordDetailRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

const word = WordDetail(
  wordId: 7,
  word: 'hello',
  isPhrase: false,
  senses: [],
  examples: [],
  relations: [],
  audio: [],
  topics: [],
);
