import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/features/dictionary/application/word_search_notifier.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_state.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

void main() {
  late MockQuizRepository repository;
  late MockWordSearchRepository searchRepository;
  late MockListsRepository listsRepository;
  late ProviderContainer container;

  setUpAll(() => registerFallbackValue(fallbackRequest));

  setUp(() {
    repository = MockQuizRepository();
    searchRepository = MockWordSearchRepository();
    listsRepository = MockListsRepository();
    when(() => listsRepository.getLists()).thenAnswer((_) async => testLists);
    when(
      () => searchRepository.getPersonalTopics(),
    ).thenAnswer((_) async => testPersonalTopics);
    container = ProviderContainer(
      overrides: [
        quizRepositoryProvider.overrideWithValue(repository),
        wordSearchRepositoryProvider.overrideWithValue(searchRepository),
        listsRepositoryProvider.overrideWithValue(listsRepository),
      ],
    );
  });

  tearDown(() => container.dispose());

  test('loads my lists and personal topics as quiz sources', () async {
    final notifier = container.read(quizConfigProvider.notifier);

    await notifier.loadSources();

    final state = container.read(quizConfigProvider);
    expect(state.lists, testLists);
    expect(state.personalTopics, [testPersonalTopics.first]);
    expect(state.personalTopics.every((topic) => topic.wordCount > 0), isTrue);
    expect(state.isLoadingSources, isFalse);
  });

  test('selects exactly one source and clears it when source type changes', () {
    final notifier = container.read(quizConfigProvider.notifier);

    notifier.selectSource(3);
    expect(container.read(quizConfigProvider).listId, 3);

    notifier.setSourceType(QuizSourceType.personalTopic);
    final state = container.read(quizConfigProvider);
    expect(state.sourceType, QuizSourceType.personalTopic);
    expect(state.listId, isNull);

    notifier.selectSource(12);
    expect(container.read(quizConfigProvider).listId, 12);
  });

  test('detects a routed personal topic list as the source type', () async {
    final notifier = container.read(quizConfigProvider.notifier);
    notifier.setListId(12);

    await notifier.loadSources();

    expect(
      container.read(quizConfigProvider).sourceType,
      QuizSourceType.personalTopic,
    );
  });

  test('validates source, timed, elimination, and date range fields', () {
    final notifier = container.read(quizConfigProvider.notifier);
    expect(notifier.validate(), contains('danh sách hoặc chủ đề cá nhân'));

    notifier.setListId(5);
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

  test(
    'clamps a preset larger than the source down to the word count',
    () async {
      when(
        () => repository.createSession(any()),
      ).thenAnswer((_) async => testSession);
      final notifier = container.read(quizConfigProvider.notifier);
      await notifier.loadSources();
      notifier.setSourceType(QuizSourceType.myList);
      notifier.selectSource(7); // Tiny list: 6 words
      notifier.setQuestionLimit(20);

      expect(notifier.validate(), isNull);
      final result = await notifier.createSession();
      expect(result?.sessionId, 9);
      final request =
          verify(() => repository.createSession(captureAny())).captured.single
              as QuizConfigRequest;
      expect(request.wordLimit, 6);
    },
  );

  test('allows the All option for a small source', () async {
    when(
      () => repository.createSession(any()),
    ).thenAnswer((_) async => testSession);
    final notifier = container.read(quizConfigProvider.notifier);
    await notifier.loadSources();
    notifier.setSourceType(QuizSourceType.myList);
    notifier.selectSource(7); // 6 words
    notifier.setQuestionLimit(null); // "Tất cả"

    expect(notifier.validate(), isNull);
    final result = await notifier.createSession();
    expect(result?.sessionId, 9);
    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.wordLimit, isNull);
  });

  test('clamps a custom count larger than the source to the word count', () async {
    when(
      () => repository.createSession(any()),
    ).thenAnswer((_) async => testSession);
    final notifier = container.read(quizConfigProvider.notifier);
    await notifier.loadSources();
    notifier.setSourceType(QuizSourceType.myList);
    notifier.selectSource(7); // 6 words
    notifier.useCustomQuestionLimit();
    notifier.setCustomQuestionLimit(10);

    expect(notifier.validate(), isNull);
    final result = await notifier.createSession();
    expect(result?.sessionId, 9);
    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.wordLimit, 6);
  });

  test('custom question count within the word count is allowed', () async {
    when(
      () => repository.createSession(any()),
    ).thenAnswer((_) async => testSession);
    final notifier = container.read(quizConfigProvider.notifier);
    await notifier.loadSources();
    notifier.setSourceType(QuizSourceType.myList);
    notifier.selectSource(7); // 6 words
    notifier.useCustomQuestionLimit();
    notifier.setCustomQuestionLimit(5);

    expect(notifier.validate(), isNull);
    final result = await notifier.createSession();
    expect(result?.sessionId, 9);
    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.wordLimit, 5);
  });

  test('requires a number when custom option is selected', () async {
    final notifier = container.read(quizConfigProvider.notifier);
    await notifier.loadSources();
    notifier.setSourceType(QuizSourceType.myList);
    notifier.selectSource(7);
    notifier.useCustomQuestionLimit();

    expect(notifier.validate(), contains('nhập số câu hỏi'));
  });

  test('creates session from the selected personal collection', () async {
    when(
      () => repository.createSession(any()),
    ).thenAnswer((_) async => testSession);
    final notifier = container.read(quizConfigProvider.notifier);
    await notifier.loadSources();
    notifier.setSourceType(QuizSourceType.personalTopic);
    notifier.selectSource(12);
    notifier.setMode('timed');
    notifier.setTimeLimit(120);
    notifier.setQuestionType(2);
    notifier.setAnswerMethod('exact_typing');

    final result = await notifier.createSession();

    expect(result?.sessionId, 9);
    final request =
        verify(() => repository.createSession(captureAny())).captured.single
            as QuizConfigRequest;
    expect(request.mode, 'timed');
    expect(request.timeLimitSec, 120);
    expect(request.questionType, 2);
    expect(request.answerMethod, 'exact_typing');
    expect(request.topicIds, isEmpty);
    expect(request.listId, 12);
  });
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
