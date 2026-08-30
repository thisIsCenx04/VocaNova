import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/services/quiz_api_service.dart';
import 'package:vocanova_mobile/features/quiz/domain/models/quiz_config.dart';
import 'package:vocanova_mobile/features/quiz/presentation/quiz_session_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  late MockQuizApiService repository;

  setUp(() {
    repository = MockQuizApiService();
  });

  testWidgets('renders progress, choices, timer, and answer feedback', (
    tester,
  ) async {
    when(
      () => repository.submitAnswer(
        sessionId: any(named: 'sessionId'),
        wordId: any(named: 'wordId'),
        answer: any(named: 'answer'),
      ),
    ).thenAnswer((_) async => wrongAnswerWithNext);
    await pumpSession(tester, repository, timedSession);

    expect(find.text('Question 1/2'), findsOneWidget);
    expect(find.byKey(const Key('quiz-progress-bar')), findsOneWidget);
    expect(find.byKey(const Key('quiz-timer')), findsOneWidget);
    expect(find.text('apple'), findsWidgets);
    expect(find.byKey(const Key('quiz-answer-3')), findsOneWidget);

    await tester.tap(find.byKey(const Key('quiz-answer-1')));
    await tester.pump();
    await tester.pump();

    expect(find.text('Next'), findsOneWidget);
    final wrongButton = tester.widget<OutlinedButton>(
      find.descendant(
        of: find.byKey(const Key('quiz-answer-1')),
        matching: find.byType(OutlinedButton),
      ),
    );
    final correctButton = tester.widget<OutlinedButton>(
      find.descendant(
        of: find.byKey(const Key('quiz-answer-0')),
        matching: find.byType(OutlinedButton),
      ),
    );
    expect(wrongButton.style?.backgroundColor, isNotNull);
    expect(correctButton.style?.backgroundColor, isNotNull);

    await tester.tap(find.byKey(const Key('next-question-button')));
    await tester.pump();
    expect(find.text('Question 2/2'), findsOneWidget);
    expect(find.text('pear'), findsWidgets);
  });

  testWidgets('shows elimination lives and confirms abandon', (tester) async {
    when(() => repository.finishSession(9)).thenAnswer((_) async {});
    final router = await pumpSession(tester, repository, eliminationSession);

    expect(find.byKey(const Key('quiz-lives')), findsOneWidget);
    expect(find.text('3'), findsOneWidget);

    await tester.tap(find.byKey(const Key('abandon-quiz-button')));
    await tester.pumpAndSettle();
    expect(find.text('Quit the quiz?'), findsOneWidget);
    await tester.tap(find.widgetWithText(FilledButton, 'Quit'));
    await tester.pumpAndSettle();

    verify(() => repository.finishSession(9)).called(1);
    expect(router.state.uri.path, AppRoutes.quizResult);
    expect(router.state.uri.queryParameters['sessionId'], '9');
  });

  testWidgets('timed session finishes and opens result when time expires', (
    tester,
  ) async {
    when(() => repository.finishSession(9)).thenAnswer((_) async {});
    final router = await pumpSession(
      tester,
      repository,
      const QuizSessionStart(
        sessionId: 9,
        answerMethod: 'multiple_choice',
        mode: 'timed',
        questionCount: 2,
        timeLimitSec: 1,
        firstQuestion: firstQuestion,
      ),
    );

    await tester.pump(const Duration(seconds: 1));
    await tester.pumpAndSettle();

    verify(() => repository.finishSession(9)).called(1);
    expect(router.state.uri.path, AppRoutes.quizResult);
  });

  testWidgets('exact typing autofocuses and submits from keyboard', (
    tester,
  ) async {
    when(
      () => repository.submitAnswer(
        sessionId: any(named: 'sessionId'),
        wordId: any(named: 'wordId'),
        answer: any(named: 'answer'),
      ),
    ).thenAnswer((_) async => exactAnswerResult);
    await pumpSession(tester, repository, exactTypingSession);

    final field = tester.widget<TextField>(
      find.byKey(const Key('typing-answer-input')),
    );
    expect(field.autofocus, isTrue);
    expect(
      find.textContaining("Case and trailing punctuation don't matter."),
      findsOneWidget,
    );

    await tester.enterText(
      find.byKey(const Key('typing-answer-input')),
      'APPLE!!!',
    );
    await tester.testTextInput.receiveAction(TextInputAction.done);
    await tester.pumpAndSettle();

    verify(
      () =>
          repository.submitAnswer(sessionId: 9, wordId: 7, answer: 'APPLE!!!'),
    ).called(1);
    expect(find.text('Correct'), findsOneWidget);
    expect(find.text('Answer: apple'), findsOneWidget);
  });

  testWidgets('AI typing shows loading, score, explanation, and suggestion', (
    tester,
  ) async {
    final response = Completer<QuizAnswerResult>();
    when(
      () => repository.submitAnswer(
        sessionId: any(named: 'sessionId'),
        wordId: any(named: 'wordId'),
        answer: any(named: 'answer'),
      ),
    ).thenAnswer((_) => response.future);
    await pumpSession(tester, repository, aiTypingSession);

    await tester.enterText(
      find.byKey(const Key('typing-answer-input')),
      'a sweet fruit',
    );
    await tester.tap(find.byKey(const Key('submit-typing-answer-button')));
    await tester.pump();

    expect(find.text('AI is grading...'), findsOneWidget);
    expect(find.byType(CircularProgressIndicator), findsWidgets);

    response.complete(aiAnswerResult);
    await tester.pumpAndSettle();

    expect(find.text('AI score: 75%'), findsOneWidget);
    expect(find.text('Ý đúng nhưng chưa đủ.'), findsOneWidget);
    expect(find.text('Suggestion: Bổ sung màu sắc.'), findsOneWidget);
  });
}

Future<GoRouter> pumpSession(
  WidgetTester tester,
  QuizApiService repository,
  QuizSessionStart session,
) async {
  final router = GoRouter(
    initialLocation: '/',
    routes: [
      GoRoute(path: '/', builder: (_, _) => const SizedBox.shrink()),
      GoRoute(
        path: AppRoutes.quizActive,
        builder: (_, state) =>
            QuizSessionScreen(session: state.extra! as QuizSessionStart),
      ),
      GoRoute(
        path: AppRoutes.quizResult,
        builder: (_, _) => const Scaffold(body: Text('Quiz result')),
      ),
    ],
  );
  await tester.pumpWidget(
    ProviderScope(
      overrides: [quizApiServiceProvider.overrideWithValue(repository)],
      child: MaterialApp.router(
        routerConfig: router,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
      ),
    ),
  );
  router.go(
    AppRoutes.quizSession(session.sessionId.toString()),
    extra: session,
  );
  await tester.pumpAndSettle();
  return router;
}

class MockQuizApiService extends Mock implements QuizApiService {}

const firstQuestion = QuizQuestion(
  wordId: 7,
  senseId: 8,
  questionType: 1,
  displayContent: 'apple',
  choices: ['apple', 'orange', 'pear', 'grape'],
);

const nextQuestion = QuizQuestion(
  wordId: 10,
  senseId: 11,
  questionType: 1,
  displayContent: 'pear',
  choices: ['pear', 'orange', 'apple', 'grape'],
);

const timedSession = QuizSessionStart(
  sessionId: 9,
  answerMethod: 'multiple_choice',
  mode: 'timed',
  questionCount: 2,
  timeLimitSec: 60,
  firstQuestion: firstQuestion,
);

const eliminationSession = QuizSessionStart(
  sessionId: 9,
  answerMethod: 'multiple_choice',
  mode: 'elimination',
  questionCount: 2,
  lives: 3,
  firstQuestion: firstQuestion,
);

const exactTypingSession = QuizSessionStart(
  sessionId: 9,
  answerMethod: 'exact_typing',
  mode: 'standard',
  questionCount: 1,
  firstQuestion: firstQuestion,
);

const aiTypingSession = QuizSessionStart(
  sessionId: 9,
  answerMethod: 'ai_typing',
  mode: 'standard',
  questionCount: 1,
  firstQuestion: firstQuestion,
);

const wrongAnswerWithNext = QuizAnswerResult(
  isCorrect: false,
  expectedAnswer: 'apple',
  correctCount: 0,
  wrongCount: 1,
  score: 0,
  nextQuestion: nextQuestion,
);

const exactAnswerResult = QuizAnswerResult(
  isCorrect: true,
  expectedAnswer: 'apple',
  correctCount: 1,
  wrongCount: 0,
  score: 1,
);

const aiAnswerResult = QuizAnswerResult(
  isCorrect: true,
  expectedAnswer: 'apple',
  correctCount: 1,
  wrongCount: 0,
  score: 1,
  aiScore: 0.75,
  aiExplanation: 'Ý đúng nhưng chưa đủ.',
  aiSuggestion: 'Bổ sung màu sắc.',
);
