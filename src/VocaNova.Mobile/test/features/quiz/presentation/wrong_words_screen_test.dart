import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_result.dart';
import 'package:vocanova_mobile/features/quiz/presentation/wrong_words_screen.dart';

void main() {
  testWidgets('renders wrong words, swipes remove, and retries via config', (
    tester,
  ) async {
    final repository = MockQuizRepository();
    when(() => repository.getWrongWords(page: 1)).thenAnswer(
      (_) async => const WrongWordsPage(items: [apple], page: 1, totalPages: 1),
    );
    when(() => repository.removeWrongWord(7)).thenAnswer((_) async {});
    final router = GoRouter(
      initialLocation: AppRoutes.wrongWords,
      routes: [
        GoRoute(
          path: AppRoutes.wrongWords,
          builder: (_, _) => const WrongWordsScreen(),
        ),
        GoRoute(
          path: AppRoutes.quizConfig,
          builder: (_, _) => const Scaffold(body: Text('Quiz config')),
        ),
        GoRoute(
          path: AppRoutes.word,
          builder: (_, _) => const Scaffold(body: Text('Word detail')),
        ),
      ],
    );
    await tester.pumpWidget(
      ProviderScope(
        overrides: [quizRepositoryProvider.overrideWithValue(repository)],
        child: MaterialApp.router(routerConfig: router),
      ),
    );
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));

    expect(find.text('apple'), findsOneWidget);
    expect(find.textContaining('Sai: 3'), findsOneWidget);
    await tester.tap(find.byKey(const Key('retry-wrong-words-button')));
    await tester.pumpAndSettle();
    expect(router.state.uri.path, AppRoutes.quizConfig);

    router.go(AppRoutes.wrongWords);
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 100));
    await tester.drag(
      find.byKey(const Key('wrong-word-7')),
      const Offset(-600, 0),
    );
    await tester.pumpAndSettle();
    verify(() => repository.removeWrongWord(7)).called(1);
  });
}

class MockQuizRepository extends Mock implements QuizRepository {}

const apple = WrongWord(
  wordId: 7,
  word: 'apple',
  primaryMeaning: 'quả táo',
  testCount: 4,
  correctCount: 1,
  wrongCount: 3,
  masteryLevel: 2,
);
