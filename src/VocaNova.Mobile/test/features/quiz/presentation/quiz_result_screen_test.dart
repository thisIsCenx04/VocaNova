import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_result.dart';
import 'package:vocanova_mobile/features/quiz/presentation/quiz_result_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  Future<GoRouter> pumpResult(WidgetTester tester) async {
    final repository = MockQuizRepository();
    when(() => repository.getResult(9)).thenAnswer((_) async => result);
    final router = GoRouter(
      initialLocation: AppRoutes.quizResultFor('9'),
      routes: [
        GoRoute(
          path: AppRoutes.quizResult,
          builder: (_, _) => const QuizResultScreen(sessionId: 9),
        ),
        GoRoute(
          path: AppRoutes.wrongWords,
          builder: (_, _) => const Scaffold(body: Text('Wrong words')),
        ),
        GoRoute(
          path: AppRoutes.quizConfig,
          builder: (_, _) => const Scaffold(body: Text('Quiz config')),
        ),
        GoRoute(
          path: AppRoutes.home,
          builder: (_, _) => const Scaffold(body: Text('Home')),
        ),
      ],
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [quizRepositoryProvider.overrideWithValue(repository)],
        child: MaterialApp.router(
          routerConfig: router,
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
        ),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(seconds: 1));
    return router;
  }

  testWidgets('renders accuracy ring, headline, stats, and breakdown', (
    tester,
  ) async {
    await pumpResult(tester);

    expect(find.byKey(const Key('score-animation')), findsOneWidget);
    expect(find.text('50%'), findsWidgets);
    expect(find.text('Well done!'), findsOneWidget);
    expect(find.textContaining('1/2 correct'), findsOneWidget);
    expect(find.textContaining('1m 15s'), findsOneWidget);
    expect(find.text('Best streak'), findsOneWidget);
    expect(find.text('RESULTS'), findsOneWidget);
    expect(find.textContaining('You answered: orange'), findsOneWidget);
    expect(find.textContaining('Answer: apple'), findsOneWidget);
  });

  testWidgets('review, retry, and done buttons navigate correctly', (
    tester,
  ) async {
    final router = await pumpResult(tester);

    await tester.tap(find.byKey(const Key('review-wrong-words-button')));
    await tester.pumpAndSettle();
    expect(router.state.uri.path, AppRoutes.wrongWords);

    router.go(AppRoutes.quizResultFor('9'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('retry-quiz-button')));
    await tester.pumpAndSettle();
    expect(router.state.uri.path, AppRoutes.quizConfig);

    router.go(AppRoutes.quizResultFor('9'));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('done-button')));
    await tester.pumpAndSettle();
    expect(router.state.uri.path, AppRoutes.home);
  });
}

class MockQuizRepository extends Mock implements QuizRepository {}

const result = QuizResult(
  sessionId: 9,
  status: 'completed',
  correctCount: 1,
  wrongCount: 1,
  questionCount: 2,
  answeredCount: 2,
  accuracy: 50,
  durationSec: 75,
  maxStreak: 1,
  score: 50,
  answers: [
    QuizResultAnswer(
      wordId: 7,
      questionNumber: 1,
      displayContent: 'apple',
      expectedAnswer: 'apple',
      userAnswer: 'orange',
      isCorrect: false,
    ),
  ],
);
