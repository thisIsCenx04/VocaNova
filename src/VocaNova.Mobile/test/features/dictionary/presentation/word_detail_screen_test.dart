import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/storage/local_storage.dart';
import 'package:vocanova_mobile/features/dictionary/application/audio_playback_service.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_service.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_detail_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_detail_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_detail.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/word_detail_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

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
    when(() => repository.getPersonalTopics(wordId: 7)).thenAnswer(
      (_) async => const [
        PersonalTopicSummary(
          topicId: 2,
          name: 'Communication',
          wordCount: 0,
          containsWord: false,
        ),
      ],
    );
    when(
      () => repository.addWordToList(
        listId: 3,
        wordId: 7,
        note: any(named: 'note'),
      ),
    ).thenAnswer((_) async {});
    when(() => repository.createList('Study')).thenAnswer(
      (_) async =>
          const UserListSummary(listId: 9, listName: 'Study', wordCount: 0),
    );
    when(
      () => repository.addWordToPersonalTopic(
        topicId: 2,
        wordId: 7,
        note: any(named: 'note'),
      ),
    ).thenAnswer(
      (_) async => const PersonalTopicSummary(
        topicId: 2,
        listId: 12,
        name: 'Communication',
        wordCount: 1,
        containsWord: true,
      ),
    );
    when(
      () => audio.playPronunciation(
        word: any(named: 'word'),
        accent: any(named: 'accent'),
        audioUrl: any(named: 'audioUrl'),
      ),
    ).thenAnswer((_) async {});
  });

  testWidgets('renders the Figma detail sections, audio and bookmark action', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await pumpDetail(tester, repository, connectivity, storage, audio);

    expect(find.text('hello'), findsOneWidget);
    expect(find.text('/həˈləʊ/'), findsOneWidget);
    expect(find.text('/həˈloʊ/'), findsOneWidget);
    expect(find.text('DEFINITION'), findsOneWidget);
    expect(find.text('VIETNAMESE'), findsOneWidget);
    expect(find.text('EXAMPLE'), findsOneWidget);
    expect(find.text('xin chào'), findsOneWidget);

    await tester.tap(find.byKey(const Key('play-us')));
    verify(
      () => audio.playPronunciation(
        word: 'hello',
        accent: 'US',
        audioUrl: 'https://audio.test/us.mp3',
      ),
    ).called(1);

    await tester.tap(find.byKey(const Key('save-word-book')));
    await tester.pump();
    expect(find.byIcon(Icons.bookmark), findsOneWidget);

    await tester.scrollUntilVisible(find.byKey(const Key('word-topic-2')), 300);
    expect(find.text('Communication'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('fixed action opens Figma sheet and saves list with a note', (
    tester,
  ) async {
    await pumpDetail(tester, repository, connectivity, storage, audio);

    await tester.tap(find.byKey(const Key('add-to-list-fab')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('add-to-list-sheet')), findsOneWidget);
    expect(find.text('Add to list'), findsWidgets);
    expect(find.text('Favorites'), findsOneWidget);
    await tester.tap(find.byKey(const Key('add-to-list-3')));
    await tester.enterText(
      find.byKey(const Key('add-to-list-note')),
      'Review tomorrow',
    );
    await tester.tap(find.byKey(const Key('save-add-to-list')));
    await tester.pumpAndSettle();

    verify(
      () => repository.addWordToList(
        listId: 3,
        wordId: 7,
        note: 'Review tomorrow',
      ),
    ).called(1);
  });

  testWidgets('Add to list sheet creates and selects a new list', (
    tester,
  ) async {
    await pumpDetail(tester, repository, connectivity, storage, audio);

    await tester.tap(find.byKey(const Key('add-to-list-fab')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('new-list-from-word')));
    await tester.pumpAndSettle();
    await tester.enterText(find.byKey(const Key('new-list-name')), 'Study');
    await tester.tap(find.byKey(const Key('create-list-from-word')));
    await tester.pumpAndSettle();

    expect(find.text('Study'), findsOneWidget);
    verify(() => repository.createList('Study')).called(1);
  });

  testWidgets('Add to list sheet saves into an eligible personal topic', (
    tester,
  ) async {
    await pumpDetail(tester, repository, connectivity, storage, audio);

    await tester.tap(find.byKey(const Key('add-to-list-fab')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('My topics'));
    await tester.pumpAndSettle();
    expect(find.text('Communication'), findsOneWidget);
    expect(find.text('0 personal words'), findsOneWidget);
    await tester.tap(find.byKey(const Key('add-to-topic-2')));
    await tester.tap(find.byKey(const Key('save-add-to-list')));
    await tester.pumpAndSettle();

    verify(
      () => repository.addWordToPersonalTopic(topicId: 2, wordId: 7, note: ''),
    ).called(1);
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
      child: MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: const WordDetailScreen(wordId: 7),
      ),
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
