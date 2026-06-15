import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_session_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

void main() {
  late MockQuizRepository repository;
  late ProviderContainer container;

  setUp(() {
    repository = MockQuizRepository();
    container = ProviderContainer(
      overrides: [quizRepositoryProvider.overrideWithValue(repository)],
    );
  });

  tearDown(() => container.dispose());

  test(
    'submits answer, updates lives, and advances to next question',
    () async {
      when(
        () => repository.submitAnswer(
          sessionId: any(named: 'sessionId'),
          wordId: any(named: 'wordId'),
          answer: any(named: 'answer'),
        ),
      ).thenAnswer((_) async => wrongAnswerWithNext);
      final provider = quizSessionProvider(eliminationSession);
      final subscription = container.listen(provider, (_, _) {});
      addTearDown(subscription.close);
      final notifier = container.read(provider.notifier);

      await notifier.submitAnswer('orange');

      var state = container.read(provider);
      expect(state.answerResult?.isCorrect, isFalse);
      expect(state.remainingLives, 1);
      expect(state.selectedAnswer, 'orange');

      notifier.nextQuestion();
      state = container.read(provider);
      expect(state.questionNumber, 2);
      expect(state.question.wordId, 10);
      expect(state.answerResult, isNull);
    },
  );

  test('finishes elimination session when the last life is lost', () async {
    when(
      () => repository.submitAnswer(
        sessionId: any(named: 'sessionId'),
        wordId: any(named: 'wordId'),
        answer: any(named: 'answer'),
      ),
    ).thenAnswer((_) async => wrongAnswerWithNext);
    when(() => repository.finishSession(9)).thenAnswer((_) async {});
    final session = QuizSessionStart(
      sessionId: eliminationSession.sessionId,
      answerMethod: eliminationSession.answerMethod,
      mode: eliminationSession.mode,
      questionCount: eliminationSession.questionCount,
      lives: 1,
      firstQuestion: eliminationSession.firstQuestion,
    );
    final provider = quizSessionProvider(session);
    final subscription = container.listen(provider, (_, _) {});
    addTearDown(subscription.close);

    await container.read(provider.notifier).submitAnswer('orange');

    expect(container.read(provider).isFinished, isTrue);
    expect(container.read(provider).remainingLives, 0);
    verify(() => repository.finishSession(9)).called(1);
  });
}

class MockQuizRepository extends Mock implements QuizRepository {}

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

const eliminationSession = QuizSessionStart(
  sessionId: 9,
  answerMethod: 'multiple_choice',
  mode: 'elimination',
  questionCount: 2,
  lives: 2,
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
