import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/features/dictionary/application/audio_playback_service.dart';
import 'package:vocanova_mobile/features/dictionary/application/connectivity_service.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_detail_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/word_detail_screen.dart';

void main() {
  late MockWordDetailRepository repository;
  late MockConnectivityService connectivity;
  late MockLocalStorage storage;
  late MockAudioPlaybackService audio;

  setUpAll(() => registerFallbackValue(Duration.zero));

  setUp(() {
    repository = MockWordDetailRepository();
    connectivity = MockConnectivityService();
    storage = MockLocalStorage();
    audio = MockAudioPlaybackService();
    when(() => storage.get<String>(any())).thenAnswer((_) async => null);
    when(
      () => storage.getWithTtl<String>(any(), ttl: any(named: 'ttl')),
    ).thenAnswer((_) async => null);
    when(
      () => storage.setWithTtl<String>(any(), any()),
    ).thenAnswer((_) async {});
    when(() => storage.set<String>(any(), any())).thenAnswer((_) async {});
    when(() => connectivity.isOnline).thenAnswer((_) async => true);
    when(() => repository.getWord(7)).thenAnswer((_) async => word);
    when(() => repository.getLists()).thenAnswer(
      (_) async => const [
        UserListSummary(listId: 3, listName: 'Favorites', wordCount: 2),
      ],
    );
    when(
      () => repository.addWordToList(listId: 3, wordId: 7),
    ).thenAnswer((_) async {});
    when(() => audio.play(any())).thenAnswer((_) async {});
  });

  testWidgets('renders detail, switches accent, plays audio and saves word', (
    tester,
  ) async {
    await pumpDetail(tester, repository, connectivity, storage, audio);

    expect(find.text('hello'), findsOneWidget);
    expect(find.text('/həˈləʊ/'), findsOneWidget);
    expect(find.text('xin chào'), findsOneWidget);

    await tester.tap(find.text('US'));
    await tester.pump();
    expect(find.text('/həˈloʊ/'), findsOneWidget);
    await tester.tap(find.byKey(const Key('play-us')));
    verify(() => audio.play('https://audio.test/us.mp3')).called(1);

    await tester.tap(find.byKey(const Key('save-word-book')));
    await tester.pump();
    expect(find.text('Đã lưu vào sách từ'), findsOneWidget);

    await tester.scrollUntilVisible(find.byKey(const Key('word-topic-2')), 300);
    expect(find.text('Giao tiếp'), findsOneWidget);
  });

  testWidgets('FAB opens AddToListSheet and adds word', (tester) async {
    await pumpDetail(tester, repository, connectivity, storage, audio);

    await tester.tap(find.byKey(const Key('add-to-list-fab')));
    await tester.pumpAndSettle();
    expect(find.text('Favorites'), findsOneWidget);
    await tester.tap(find.byKey(const Key('add-to-list-3')));
    await tester.pumpAndSettle();

    verify(() => repository.addWordToList(listId: 3, wordId: 7)).called(1);
  });
}

Future<void> pumpDetail(
  WidgetTester tester,
  WordDetailRepository repository,
  ConnectivityService connectivity,
  LocalStorage storage,
  AudioPlaybackService audio,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        wordDetailRepositoryProvider.overrideWithValue(repository),
        wordDetailLocalStorageProvider.overrideWithValue(storage),
        connectivityServiceProvider.overrideWithValue(connectivity),
        audioPlaybackServiceProvider.overrideWithValue(audio),
      ],
      child: const MaterialApp(home: WordDetailScreen(wordId: 7)),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
}

class MockWordDetailRepository extends Mock implements WordDetailRepository {}

class MockConnectivityService extends Mock implements ConnectivityService {}

class MockLocalStorage extends Mock implements LocalStorage {}

class MockAudioPlaybackService extends Mock implements AudioPlaybackService {}

const word = WordDetail(
  wordId: 7,
  word: 'hello',
  isPhrase: false,
  cefr: 'A1',
  phoneticUk: '/həˈləʊ/',
  phoneticUs: '/həˈloʊ/',
  senses: [
    WordSense(
      senseId: 1,
      order: 1,
      wordClass: 'interjection',
      englishDefinition: 'used as a greeting',
      vietnameseMeaning: 'xin chào',
      examples: [
        WordExample(exampleEn: 'Hello there!', exampleVi: 'Xin chào!'),
      ],
      relations: [],
    ),
  ],
  examples: [],
  relations: [
    WordRelation(relationType: 'synonym', relatedWord: 'hi', linkedWordId: 8),
  ],
  audio: [
    WordAudio(accent: 'UK', url: 'https://audio.test/uk.mp3'),
    WordAudio(accent: 'US', url: 'https://audio.test/us.mp3'),
  ],
  topics: [WordTopic(topicId: 2, name: 'Communication', nameVi: 'Giao tiếp')],
);
