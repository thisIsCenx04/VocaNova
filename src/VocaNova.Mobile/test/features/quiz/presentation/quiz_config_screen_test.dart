import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';
import 'package:vocanova_mobile/features/quiz/presentation/quiz_config_screen.dart';

void main() {
  late MockQuizRepository repository;
  late MockWordSearchRepository searchRepository;

  setUpAll(() => registerFallbackValue(fallbackRequest));

  setUp(() {
    repository = MockQuizRepository();
    searchRepository = MockWordSearchRepository();
    when(() => searchRepository.getTopics()).thenAnswer(
      (_) async => const [
        TopicSummary(
          topicId: 2,
          name: 'Travel',
          nameVi: 'Du lịch',
          wordCount: 10,
        ),
      ],
    );
    when(
      () => repository.createSession(any()),
    ).thenAnswer((_) async => testSession);
  });

  testWidgets('renders all sections, topics, and dynamic mode inputs', (
    tester,
  ) async {
    await pumpConfig(tester, repository, searchRepository);

    expect(find.text('Phạm vi từ'), findsOneWidget);
    expect(find.text('Chủ đề'), findsOneWidget);
    expect(find.text('Chế độ'), findsOneWidget);
    expect(find.text('Du lịch'), findsOneWidget);
    expect(find.textContaining('Mở từ danh sách #3'), findsOneWidget);
    expect(find.text('Loại câu hỏi'), findsOneWidget);
    expect(find.text('Cách trả lời'), findsOneWidget);

    await tester.tap(find.byKey(const Key('quiz-mode-timed')));
    await tester.pump();
    expect(find.byKey(const Key('time-limit-input')), findsOneWidget);

    await tester.tap(find.byKey(const Key('quiz-mode-elimination')));
    await tester.pump();
    expect(find.byKey(const Key('lives-input')), findsOneWidget);
  });

  testWidgets('start validates timed input then creates and navigates', (
    tester,
  ) async {
    final router = await pumpConfig(tester, repository, searchRepository);
    await tester.tap(find.byKey(const Key('quiz-mode-timed')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('start-quiz-button')));
    await tester.pump();
    expect(find.textContaining('cần thời gian'), findsOneWidget);

    await tester.enterText(find.byKey(const Key('time-limit-input')), '120');
    await tester.tap(find.byKey(const Key('quiz-topic-2')));
    await tester.tap(find.byKey(const Key('start-quiz-button')));
    await tester.pumpAndSettle();

    expect(router.state.uri.path, AppRoutes.quizActive);
    expect(router.state.uri.queryParameters['sessionId'], '9');
    verify(() => repository.createSession(any())).called(1);
  });
}

Future<GoRouter> pumpConfig(
  WidgetTester tester,
  QuizRepository repository,
  WordSearchRepository searchRepository,
) async {
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
