import 'package:vocanova_mobile/features/quiz/domain/models/quiz_config.dart';

class QuizSessionStartDto {
  const QuizSessionStartDto({
    required this.sessionId,
    required this.answerMethod,
    required this.mode,
    required this.questionCount,
    required this.firstQuestion,
    this.listId,
    this.timeLimitSec,
    this.lives,
  });

  final int sessionId;
  final String answerMethod;
  final String mode;
  final int questionCount;
  final int? listId;
  final int? timeLimitSec;
  final int? lives;
  final QuizQuestionDto firstQuestion;

  factory QuizSessionStartDto.fromJson(Map<String, dynamic> json) {
    final session = json['session'] as Map<String, dynamic>;
    final question = json['first_question'] as Map<String, dynamic>;
    return QuizSessionStartDto(
      sessionId: session['session_id'] as int,
      answerMethod: session['answer_method'] as String,
      mode: session['mode'] as String,
      questionCount: session['question_count'] as int,
      listId: session['list_id'] as int?,
      timeLimitSec: session['time_limit_sec'] as int?,
      lives: session['lives'] as int?,
      firstQuestion: QuizQuestionDto.fromJson(question),
    );
  }

  QuizSessionStart toDomain() => QuizSessionStart(
    sessionId: sessionId,
    answerMethod: answerMethod,
    mode: mode,
    questionCount: questionCount,
    listId: listId,
    timeLimitSec: timeLimitSec,
    lives: lives,
    firstQuestion: firstQuestion.toDomain(),
  );
}

class QuizQuestionDto {
  const QuizQuestionDto({
    required this.wordId,
    required this.senseId,
    required this.questionType,
    required this.displayContent,
    required this.choices,
  });

  final int wordId;
  final int senseId;
  final int questionType;
  final String displayContent;
  final List<String> choices;

  factory QuizQuestionDto.fromJson(Map<String, dynamic> json) =>
      QuizQuestionDto(
        wordId: json['word_id'] as int,
        senseId: json['sense_id'] as int,
        questionType: json['question_type'] as int,
        displayContent: json['display_content'] as String,
        choices: (json['choices'] as List<dynamic>).cast<String>(),
      );

  QuizQuestion toDomain() => QuizQuestion(
    wordId: wordId,
    senseId: senseId,
    questionType: questionType,
    displayContent: displayContent,
    choices: choices,
  );
}

class QuizAnswerResultDto {
  const QuizAnswerResultDto({
    required this.isCorrect,
    required this.expectedAnswer,
    required this.correctCount,
    required this.wrongCount,
    required this.score,
    this.aiScore,
    this.aiExplanation,
    this.aiSuggestion,
    this.nextQuestion,
  });

  final bool isCorrect;
  final String expectedAnswer;
  final int correctCount;
  final int wrongCount;
  final double score;
  final double? aiScore;
  final String? aiExplanation;
  final String? aiSuggestion;
  final QuizQuestionDto? nextQuestion;

  factory QuizAnswerResultDto.fromJson(Map<String, dynamic> json) =>
      QuizAnswerResultDto(
        isCorrect: json['is_correct'] as bool,
        expectedAnswer: json['expected_answer'] as String,
        correctCount: json['correct_count'] as int,
        wrongCount: json['wrong_count'] as int,
        score: (json['score'] as num).toDouble(),
        aiScore: (json['ai_score'] as num?)?.toDouble(),
        aiExplanation: json['ai_explanation'] as String?,
        aiSuggestion: json['ai_suggestion'] as String?,
        nextQuestion: json['next_question'] == null
            ? null
            : QuizQuestionDto.fromJson(
                json['next_question'] as Map<String, dynamic>,
              ),
      );

  QuizAnswerResult toDomain() => QuizAnswerResult(
    isCorrect: isCorrect,
    expectedAnswer: expectedAnswer,
    correctCount: correctCount,
    wrongCount: wrongCount,
    score: score,
    aiScore: aiScore,
    aiExplanation: aiExplanation,
    aiSuggestion: aiSuggestion,
    nextQuestion: nextQuestion?.toDomain(),
  );
}

class QuizConfigRequestDto {
  const QuizConfigRequestDto({
    required this.mode,
    required this.questionType,
    required this.scopeType,
    required this.topicIds,
    required this.answerMethod,
    this.wordOrder = 'random',
    this.wordLimit = 20,
    this.listId,
    this.scopeDateFrom,
    this.scopeDateTo,
    this.timeLimitSec,
    this.lives,
  });

  final String mode;
  final int questionType;
  final String scopeType;
  final String wordOrder;
  final int? wordLimit;
  final int? listId;
  final DateTime? scopeDateFrom;
  final DateTime? scopeDateTo;
  final List<int> topicIds;
  final int? timeLimitSec;
  final int? lives;
  final String answerMethod;

  factory QuizConfigRequestDto.fromDomain(QuizConfigRequest request) =>
      QuizConfigRequestDto(
        mode: request.mode,
        questionType: request.questionType,
        scopeType: request.scopeType,
        topicIds: request.topicIds,
        answerMethod: request.answerMethod,
        wordOrder: request.wordOrder,
        wordLimit: request.wordLimit,
        listId: request.listId,
        scopeDateFrom: request.scopeDateFrom,
        scopeDateTo: request.scopeDateTo,
        timeLimitSec: request.timeLimitSec,
        lives: request.lives,
      );

  Map<String, dynamic> toJson() => {
    'mode': mode,
    'question_type': questionType,
    'scope_type': scopeType,
    'list_id': listId,
    'scope_date_from': _date(scopeDateFrom),
    'scope_date_to': _date(scopeDateTo),
    'topic_ids': topicIds.isEmpty ? null : topicIds,
    'word_order': wordOrder,
    'word_limit': wordLimit,
    'time_limit_sec': timeLimitSec,
    'lives': lives,
    'answer_method': answerMethod,
  };

  static String? _date(DateTime? value) {
    if (value == null) return null;
    return '${value.year.toString().padLeft(4, '0')}-'
        '${value.month.toString().padLeft(2, '0')}-'
        '${value.day.toString().padLeft(2, '0')}';
  }
}
