import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';

abstract final class QuizSourceType {
  static const myList = 'my_list';
  static const personalTopic = 'personal_topic';
}

class QuizConfigState {
  const QuizConfigState({
    this.listId,
    this.sourceType = QuizSourceType.myList,
    this.scopeType = 'all',
    this.mode = 'standard',
    this.questionType = 1,
    this.answerMethod = 'multiple_choice',
    this.timeLimitSec,
    this.lives,
    this.dateFrom,
    this.dateTo,
    this.lists = const [],
    this.personalTopics = const [],
    this.isLoadingSources = true,
    this.isCreating = false,
    this.errorMessage,
  });

  final int? listId;
  final String sourceType;
  final String scopeType;
  final String mode;
  final int questionType;
  final String answerMethod;
  final int? timeLimitSec;
  final int? lives;
  final DateTime? dateFrom;
  final DateTime? dateTo;
  final List<UserList> lists;
  final List<PersonalTopicSummary> personalTopics;
  final bool isLoadingSources;
  final bool isCreating;
  final String? errorMessage;

  QuizConfigState copyWith({
    int? listId,
    String? sourceType,
    String? scopeType,
    String? mode,
    int? questionType,
    String? answerMethod,
    int? timeLimitSec,
    int? lives,
    DateTime? dateFrom,
    DateTime? dateTo,
    List<UserList>? lists,
    List<PersonalTopicSummary>? personalTopics,
    bool? isLoadingSources,
    bool? isCreating,
    String? errorMessage,
    bool clearListId = false,
    bool clearTime = false,
    bool clearLives = false,
    bool clearDateFrom = false,
    bool clearDateTo = false,
    bool clearError = false,
  }) => QuizConfigState(
    listId: clearListId ? null : listId ?? this.listId,
    sourceType: sourceType ?? this.sourceType,
    scopeType: scopeType ?? this.scopeType,
    mode: mode ?? this.mode,
    questionType: questionType ?? this.questionType,
    answerMethod: answerMethod ?? this.answerMethod,
    timeLimitSec: clearTime ? null : timeLimitSec ?? this.timeLimitSec,
    lives: clearLives ? null : lives ?? this.lives,
    dateFrom: clearDateFrom ? null : dateFrom ?? this.dateFrom,
    dateTo: clearDateTo ? null : dateTo ?? this.dateTo,
    lists: lists ?? this.lists,
    personalTopics: personalTopics ?? this.personalTopics,
    isLoadingSources: isLoadingSources ?? this.isLoadingSources,
    isCreating: isCreating ?? this.isCreating,
    errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
  );
}
