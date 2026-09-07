import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/services/quiz_api_service.dart';
import 'package:vocanova_mobile/features/quiz/domain/models/quiz_history.dart';
import 'package:vocanova_mobile/features/quiz/presentation/quiz_history_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  testWidgets('renders quiz history and opens result', (tester) async {
    final repository = MockQuizApiService();
    when(() => repository.getHistory(page: 1)).thenAnswer(
      (_) async =>
          QuizHistoryPage(items: [completedSession], page: 1, totalPages: 1),
    );
    final router = GoRouter(
      initialLocation: AppRoutes.quizHistory,
      routes: [
        GoRoute(
          path: AppRoutes.quizHistory,
          builder: (_, _) => const QuizHistoryScreen(),
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
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.byKey(const Key('quiz-history-9')), findsOneWidget);
    expect(find.text('50%'), findsOneWidget);

    await tester.tap(find.byKey(const Key('quiz-history-9')));
    await tester.pumpAndSettle();

    expect(router.state.uri.path, AppRoutes.quizResult);
    expect(router.state.uri.queryParameters['sessionId'], '9');
  });
}

class MockQuizApiService extends Mock implements QuizApiService {}

final completedSession = QuizHistoryItem(
  sessionId: 9,
  answerMethod: 'multiple_choice',
  mode: 'standard',
  questionType: 1,
  questionCount: 2,
  correctCount: 1,
  wrongCount: 1,
  accuracy: 50,
  score: 50,
  maxStreak: 1,
  status: 'completed',
  startedAt: DateTime(2026, 6, 15, 8),
);
