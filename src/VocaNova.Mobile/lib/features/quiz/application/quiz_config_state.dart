import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';

class QuizConfigState {
  const QuizConfigState({
    this.listId,
    this.scopeType = 'all',
    this.selectedTopicIds = const {},
    this.mode = 'standard',
    this.questionType = 1,
    this.answerMethod = 'multiple_choice',
    this.timeLimitSec,
    this.lives,
    this.dateFrom,
    this.dateTo,
    this.topics = const [],
    this.isLoadingTopics = true,
    this.isCreating = false,
    this.errorMessage,
  });

  final int? listId;
  final String scopeType;
  final Set<int> selectedTopicIds;
  final String mode;
  final int questionType;
  final String answerMethod;
  final int? timeLimitSec;
  final int? lives;
  final DateTime? dateFrom;
  final DateTime? dateTo;
  final List<TopicSummary> topics;
  final bool isLoadingTopics;
  final bool isCreating;
  final String? errorMessage;

  QuizConfigState copyWith({
    int? listId,
    String? scopeType,
    Set<int>? selectedTopicIds,
    String? mode,
    int? questionType,
    String? answerMethod,
    int? timeLimitSec,
    int? lives,
    DateTime? dateFrom,
    DateTime? dateTo,
    List<TopicSummary>? topics,
    bool? isLoadingTopics,
    bool? isCreating,
    String? errorMessage,
    bool clearTime = false,
    bool clearLives = false,
    bool clearDateFrom = false,
    bool clearDateTo = false,
    bool clearError = false,
  }) => QuizConfigState(
    listId: listId ?? this.listId,
    scopeType: scopeType ?? this.scopeType,
    selectedTopicIds: selectedTopicIds ?? this.selectedTopicIds,
    mode: mode ?? this.mode,
    questionType: questionType ?? this.questionType,
    answerMethod: answerMethod ?? this.answerMethod,
    timeLimitSec: clearTime ? null : timeLimitSec ?? this.timeLimitSec,
    lives: clearLives ? null : lives ?? this.lives,
    dateFrom: clearDateFrom ? null : dateFrom ?? this.dateFrom,
    dateTo: clearDateTo ? null : dateTo ?? this.dateTo,
    topics: topics ?? this.topics,
    isLoadingTopics: isLoadingTopics ?? this.isLoadingTopics,
    isCreating: isCreating ?? this.isCreating,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
