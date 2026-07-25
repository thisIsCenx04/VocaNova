import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/core/connectivity/connectivity_provider.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';
import 'package:vocanova_mobile/features/quiz/presentation/quiz_config_screen.dart';

void main() {
  late MockQuizRepository repository;
  late MockWordSearchRepository searchRepository;
  late MockListsRepository listsRepository;

  setUpAll(() => registerFallbackValue(fallbackRequest));

  setUp(() {
    repository = MockQuizRepository();
    searchRepository = MockWordSearchRepository();
    listsRepository = MockListsRepository();
    when(() => listsRepository.getLists()).thenAnswer((_) async => testLists);
    when(
      () => searchRepository.getPersonalTopics(),
    ).thenAnswer((_) async => testPersonalTopics);
    when(
      () => repository.createSession(any()),
    ).thenAnswer((_) async => testSession);
  });

  testWidgets('renders the personal collection source selector', (
    tester,
  ) async {
    await pumpConfig(tester, repository, searchRepository, listsRepository);

    expect(find.text('Phạm vi từ'), findsOneWidget);
    expect(find.text('Nguồn kiểm tra'), findsOneWidget);
    expect(find.text('Danh sách của tôi'), findsOneWidget);
    expect(find.text('Chủ đề cá nhân'), findsOneWidget);
    expect(find.text('Favorites'), findsOneWidget);
    expect(find.text('Chế độ'), findsOneWidget);
    expect(find.text('Loại câu hỏi'), findsOneWidget);
    expect(find.text('Cách trả lời'), findsOneWidget);

    await tester.tap(find.byKey(const Key('quiz-source-personal-topic')));
    await tester.pump();
    expect(find.text('Du lịch'), findsOneWidget);
    expect(find.text('Giáo dục'), findsNothing);
    expect(find.text('Favorites'), findsNothing);

    await tester.tap(find.byKey(const Key('quiz-mode-timed')));
    await tester.pump();
    expect(find.byKey(const Key('time-limit-input')), findsOneWidget);

    await tester.tap(find.byKey(const Key('quiz-mode-elimination')));
    await tester.pump();
    expect(find.byKey(const Key('lives-input')), findsOneWidget);
  });

  testWidgets('starts a quiz from the selected personal topic', (tester) async {
    final router = await pumpConfig(
      tester,
      repository,
      searchRepository,
      listsRepository,
    );
    await tester.tap(find.byKey(const Key('quiz-source-personal-topic')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('quiz-source-topic-12')));
    await tester.tap(find.byKey(const Key('quiz-mode-timed')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('start-quiz-button')));
    await tester.pump();
    expect(find.textContaining('cần thời gian'), findsOneWidget);

    await tester.enterText(find.byKey(const Key('time-limit-input')), '120');
    await tester.tap(find.byKey(const Key('start-quiz-button')));
    await tester.pumpAndSettle();

    expect(router.state.uri.path, AppRoutes.quizActive);
    expect(router.state.uri.queryParameters['sessionId'], '9');
    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.listId, 12);
    expect(request.topicIds, isEmpty);
  });

  testWidgets('clamps a preset larger than the chosen list to its word count', (
    tester,
  ) async {
    final router = await pumpConfig(
      tester,
      repository,
      searchRepository,
      listsRepository,
    );

    await tester.tap(find.byKey(const Key('quiz-source-list-7'))); // 6 words
    await tester.pump();
    await tester.tap(find.byKey(const Key('question-limit-20')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('start-quiz-button')));
    await tester.pumpAndSettle();

    expect(router.state.uri.path, AppRoutes.quizActive);
    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.wordLimit, 6);
  });

  testWidgets('clamps a custom count larger than the word count', (
    tester,
  ) async {
    final router = await pumpConfig(
      tester,
      repository,
      searchRepository,
      listsRepository,
    );
    await tester.tap(find.byKey(const Key('quiz-source-list-7'))); // 6 words
    await tester.pump();
    await tester.tap(find.byKey(const Key('question-limit-custom')));
    await tester.pump();

    await tester.enterText(
      find.byKey(const Key('custom-question-input')),
      '10',
    );
    await tester.tap(find.byKey(const Key('start-quiz-button')));
    await tester.pumpAndSettle();

    expect(router.state.uri.path, AppRoutes.quizActive);
    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.wordLimit, 6);
  });

  testWidgets('sends scope, order, and question limit to repository', (
    tester,
  ) async {
    await pumpConfig(tester, repository, searchRepository, listsRepository);

    await tester.tap(find.byKey(const Key('scope-wrong-words')));
    await tester.tap(find.byKey(const Key('order-by-difficulty')));
    await tester.tap(find.byKey(const Key('question-limit-all')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('start-quiz-button')));
    await tester.pumpAndSettle();

    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.scopeType, 'wrong_words');
    expect(request.wordOrder, 'by_difficulty');
    expect(request.wordLimit, isNull);
  });

  testWidgets('offline disables start with connectivity tooltip', (
    tester,
  ) async {
    await pumpConfig(
      tester,
      repository,
      searchRepository,
      listsRepository,
      isOnline: false,
    );

    await tester.pump();
    final button = tester.widget<FilledButton>(
      find.byKey(const Key('start-quiz-button')),
    );
    expect(button.onPressed, isNull);
    expect(find.text('Bạn đang ngoại tuyến'), findsOneWidget);
    expect(find.byTooltip('Cần kết nối mạng'), findsOneWidget);
    verifyNever(() => repository.createSession(any()));
  });
}

Future<GoRouter> pumpConfig(
  WidgetTester tester,
  QuizRepository repository,
  WordSearchRepository searchRepository,
  ListsRepository listsRepository, {
  bool isOnline = true,
}) async {
  tester.view.physicalSize = const Size(800, 2400);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  final router = GoRouter(
    initialLocation: '${AppRoutes.quizConfig}?listId=3',
    routes: [
      GoRoute(
        path: AppRoutes.quizConfig,
        builder: (_, state) => QuizConfigScreen(
          initialListId: int.tryParse(
            state.uri.queryParameters['listId'] ?? '',
          ),
        ),
      ),
      GoRoute(
        path: AppRoutes.quizActive,
        builder: (_, _) => const Scaffold(body: Text('Quiz active')),
      ),
    ],
  );
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        quizRepositoryProvider.overrideWithValue(repository),
        wordSearchRepositoryProvider.overrideWithValue(searchRepository),
        listsRepositoryProvider.overrideWithValue(listsRepository),
        connectivityProvider.overrideWith((ref) => Stream.value(isOnline)),
      ],
      child: MaterialApp.router(routerConfig: router),
    ),
  );
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 100));
  return router;
}

class MockQuizRepository extends Mock implements QuizRepository {}

class MockWordSearchRepository extends Mock implements WordSearchRepository {}

class MockListsRepository extends Mock implements ListsRepository {}

final testLists = [
  UserList(
    listId: 3,
    listName: 'Favorites',
    wordCount: 25,
    createdAt: DateTime(2026),
  ),
  UserList(
    listId: 7,
    listName: 'Tiny',
    wordCount: 6,
    createdAt: DateTime(2026),
  ),
];

const testPersonalTopics = [
  PersonalTopicSummary(
    topicId: 2,
    listId: 12,
    name: 'Travel',
    nameVi: 'Du lịch',
    wordCount: 30,
    containsWord: false,
  ),
  PersonalTopicSummary(
    topicId: 3,
    name: 'Education',
    nameVi: 'Giáo dục',
    wordCount: 0,
    containsWord: false,
  ),
];

const fallbackRequest = QuizConfigRequest(
  mode: 'standard',
  questionType: 1,
  scopeType: 'all',
  topicIds: [],
  answerMethod: 'multiple_choice',
);

const testSession = QuizSessionStart(
  sessionId: 9,
  answerMethod: 'multiple_choice',
  mode: 'standard',
  questionCount: 1,
  firstQuestion: QuizQuestion(
    wordId: 7,
    senseId: 8,
    questionType: 1,
    displayContent: 'apple',
    choices: ['quả táo', 'quả cam', 'quả lê', 'quả nho'],
  ),
);
