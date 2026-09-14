import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/core/storage/storage_keys.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_service.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/dtos/word_detail_dto.dart';
import 'package:vocanova_mobile/features/dictionary/data/services/word_detail_api_service.dart';
import 'package:vocanova_mobile/features/dictionary/domain/models/word_detail.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late MockWordDetailApiService repository;
  late MockConnectivityService connectivity;
  late LocalStorage storage;
  late ProviderContainer container;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    storage = LocalStorage.create(
      preferences: await SharedPreferences.getInstance(),
    );
    repository = MockWordDetailApiService();
    connectivity = MockConnectivityService();
    container = ProviderContainer(
      overrides: [
        wordDetailApiServiceProvider.overrideWithValue(repository),
        wordDetailLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
      ],
    );
  });

  tearDown(() => container.dispose());

  test('fresh two-hour cache is checked before API', () async {
    await storage.setWithTtl(
      StorageKeys.wordCacheJson(7),
      jsonEncode(WordDetailDto.fromDomain(word).toJson()),
    );

    await container.read(wordDetailProvider(7).notifier).load();

    expect(container.read(wordDetailProvider(7)).word?.word, 'hello');
    verifyNever(() => repository.getWord(any()));
  });

  test('offline uses stale cache without API', () async {
    await storage.set(
      StorageKeys.wordCacheJson(7),
      jsonEncode(WordDetailDto.fromDomain(word).toJson()),
    );
    when(() => connectivity.isOnline).thenAnswer((_) async => false);

    await container.read(wordDetailProvider(7).notifier).load();

    expect(container.read(wordDetailProvider(7)).isOffline, isTrue);
    expect(container.read(wordDetailProvider(7)).word?.word, 'hello');
    verifyNever(() => repository.getWord(any()));
  });

  test(
    'refresh replaces fresh cache without video with current API video',
    () async {
      await storage.setWithTtl(
        StorageKeys.wordCacheJson(7),
        jsonEncode(WordDetailDto.fromDomain(word).toJson()),
      );
      final current = WordDetailDto.fromJson({
        ...WordDetailDto.fromDomain(word).toJson(),
        'video': {
          'video_id': 3,
          'url': 'https://example.test/video.mp4',
          'thumbnail_url': 'https://example.test/video.jpg',
        },
      }).toDomain();
      when(() => connectivity.isOnline).thenAnswer((_) async => true);
      when(() => repository.getWord(7)).thenAnswer((_) async => current);

      await container
          .read(wordDetailProvider(7).notifier)
          .load(forceRefresh: true);

      expect(container.read(wordDetailProvider(7)).word?.video?.videoId, 3);
      final cached =
          jsonDecode((await storage.get<String>(StorageKeys.wordCacheJson(7)))!)
              as Map<String, dynamic>;
      expect((cached['video'] as Map<String, dynamic>)['video_id'], 3);
      verify(() => repository.getWord(7)).called(1);
    },
  );

  test('refresh keeps cached word when offline', () async {
    await storage.setWithTtl(
      StorageKeys.wordCacheJson(7),
      jsonEncode(WordDetailDto.fromDomain(word).toJson()),
    );
    when(() => connectivity.isOnline).thenAnswer((_) async => false);
    await container
        .read(wordDetailProvider(7).notifier)
        .load(forceRefresh: true);
    expect(container.read(wordDetailProvider(7)).word?.word, 'hello');
    expect(container.read(wordDetailProvider(7)).isOffline, isTrue);
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

class MockWordDetailApiService extends Mock implements WordDetailApiService {}

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
