import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/services/quiz_api_service.dart';
import 'package:vocanova_mobile/features/quiz/domain/models/quiz_result.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  SharedPreferences.setMockInitialValues({});

  test('loads pages and rolls back failed optimistic remove', () async {
    final repository = MockQuizApiService();
    when(() => repository.getWrongWords(page: 1)).thenAnswer(
      (_) async => const WrongWordsPage(items: [apple], page: 1, totalPages: 2),
    );
    when(() => repository.getWrongWords(page: 2)).thenAnswer(
      (_) async => const WrongWordsPage(items: [pear], page: 2, totalPages: 2),
    );
    when(() => repository.removeWrongWord(7)).thenThrow(Exception('failed'));
    final container = ProviderContainer(
      overrides: [quizApiServiceProvider.overrideWithValue(repository)],
    );
    addTearDown(container.dispose);
    final subscription = container.listen(wrongWordsProvider, (_, _) {});
    addTearDown(subscription.close);
    final notifier = container.read(wrongWordsProvider.notifier);

    await notifier.load();
    await notifier.loadMore();
    final removed = await notifier.remove(7);

    expect(container.read(wrongWordsProvider).words, [apple, pear]);
    expect(removed, isFalse);
    expect(container.read(wrongWordsProvider).errorMessage, isNotNull);
  });
}

class MockQuizApiService extends Mock implements QuizApiService {}

const apple = WrongWord(
  wordId: 7,
  word: 'apple',
  testCount: 4,
  correctCount: 1,
  wrongCount: 3,
  masteryLevel: 2,
);

const pear = WrongWord(
  wordId: 8,
  word: 'pear',
  testCount: 3,
  correctCount: 1,
  wrongCount: 2,
  masteryLevel: 1,
);
