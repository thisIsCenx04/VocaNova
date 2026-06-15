import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

void main() {
  late MockQuizRepository repository;
  late MockWordSearchRepository searchRepository;
  late ProviderContainer container;

  setUpAll(() => registerFallbackValue(fallbackRequest));

  setUp(() {
    repository = MockQuizRepository();
    searchRepository = MockWordSearchRepository();
    container = ProviderContainer(
      overrides: [
        quizRepositoryProvider.overrideWithValue(repository),
        wordSearchRepositoryProvider.overrideWithValue(searchRepository),
      ],
    );
  });

  tearDown(() => container.dispose());

  test('loads topics and supports multi-select', () async {
    when(() => searchRepository.getTopics()).thenAnswer(
      (_) async => const [
        TopicSummary(topicId: 2, name: 'Travel', wordCount: 10),
        TopicSummary(topicId: 3, name: 'Work', wordCount: 8),
      ],
    );
    final notifier = container.read(quizConfigProvider.notifier);

    await notifier.loadTopics();
    notifier.toggleTopic(2);
    notifier.toggleTopic(3);

    expect(container.read(quizConfigProvider).selectedTopicIds, {2, 3});
  });

  test('validates timed, elimination, and date range cross-fields', () {
    final notifier = container.read(quizConfigProvider.notifier);
    notifier.setMode('timed');
    expect(notifier.validate(), contains('thời gian'));
    notifier.setTimeLimit(60);
    notifier.setMode('elimination');
    expect(notifier.validate(), contains('số mạng'));
    notifier.setLives(3);
    notifier.setScope('date_range');
    expect(notifier.validate(), contains('ngày bắt đầu'));
    notifier.setDateFrom(DateTime(2026, 6, 15));
    notifier.setDateTo(DateTime(2026, 6, 1));
    expect(notifier.validate(), contains('trước hoặc bằng'));
  });

  test('creates session with selected config', () async {
    when(() => repository.createSession(any())).thenAnswer(
      (_) async => const QuizSessionStart(sessionId: 9, firstQuestionWordId: 7),
    );
    final notifier = container.read(quizConfigProvider.notifier);
    notifier.setMode('timed');
    notifier.setTimeLimit(120);
    notifier.setQuestionType(2);
    notifier.setAnswerMethod('exact_typing');
    notifier.toggleTopic(2);

    final result = await notifier.createSession();

    expect(result?.sessionId, 9);
    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.mode, 'timed');
    expect(request.timeLimitSec, 120);
    expect(request.questionType, 2);
    expect(request.answerMethod, 'exact_typing');
    expect(request.topicIds, [2]);
  });
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
