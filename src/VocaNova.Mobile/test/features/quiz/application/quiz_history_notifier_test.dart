import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_history_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/services/quiz_api_service.dart';
import 'package:vocanova_mobile/features/quiz/domain/models/quiz_history.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  SharedPreferences.setMockInitialValues({});

  test('loads quiz history pages', () async {
    final repository = MockQuizApiService();
    when(() => repository.getHistory(page: 1)).thenAnswer(
      (_) async =>
          QuizHistoryPage(items: [completedSession], page: 1, totalPages: 2),
    );
    when(() => repository.getHistory(page: 2)).thenAnswer(
      (_) async =>
          QuizHistoryPage(items: [abandonedSession], page: 2, totalPages: 2),
    );
    final container = ProviderContainer(
      overrides: [quizApiServiceProvider.overrideWithValue(repository)],
    );
    addTearDown(container.dispose);
    final subscription = container.listen(quizHistoryProvider, (_, _) {});
    addTearDown(subscription.close);
    final notifier = container.read(quizHistoryProvider.notifier);

    await notifier.load();
    await notifier.loadMore();

    final state = container.read(quizHistoryProvider);
    expect(state.items, [completedSession, abandonedSession]);
    expect(state.hasMore, isFalse);
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

final abandonedSession = QuizHistoryItem(
  sessionId: 10,
  answerMethod: 'exact_typing',
  mode: 'timed',
  questionType: 2,
  questionCount: 4,
  correctCount: 3,
  wrongCount: 1,
  accuracy: 75,
  score: 75,
  maxStreak: 3,
  status: 'abandoned',
  startedAt: DateTime(2026, 6, 16, 8),
);
