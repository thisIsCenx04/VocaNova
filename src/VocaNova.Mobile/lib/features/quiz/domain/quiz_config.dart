class QuizSessionStart {
  const QuizSessionStart({
    required this.sessionId,
    required this.answerMethod,
    required this.mode,
    required this.questionCount,
    required this.firstQuestion,
    this.timeLimitSec,
    this.lives,
  });

  final int sessionId;
  final String answerMethod;
  final String mode;
  final int questionCount;
  final int? timeLimitSec;
  final int? lives;
  final QuizQuestion firstQuestion;

  int get firstQuestionWordId => firstQuestion.wordId;

  factory QuizSessionStart.fromJson(Map<String, dynamic> json) {
    final session = json['session'] as Map<String, dynamic>;
    final question = json['first_question'] as Map<String, dynamic>;
    return QuizSessionStart(
      sessionId: session['session_id'] as int,
      answerMethod: session['answer_method'] as String,
      mode: session['mode'] as String,
      questionCount: session['question_count'] as int,
      timeLimitSec: session['time_limit_sec'] as int?,
      lives: session['lives'] as int?,
      firstQuestion: QuizQuestion.fromJson(question),
    );
  }
}

class QuizQuestion {
  const QuizQuestion({
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

  factory QuizQuestion.fromJson(Map<String, dynamic> json) => QuizQuestion(
    wordId: json['word_id'] as int,
    senseId: json['sense_id'] as int,
    questionType: json['question_type'] as int,
    displayContent: json['display_content'] as String,
    choices: (json['choices'] as List<dynamic>).cast<String>(),
  );
}

class QuizAnswerResult {
  const QuizAnswerResult({
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
  final QuizQuestion? nextQuestion;

  factory QuizAnswerResult.fromJson(Map<String, dynamic> json) =>
      QuizAnswerResult(
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
            : QuizQuestion.fromJson(
                json['next_question'] as Map<String, dynamic>,
              ),
      );
}

class QuizConfigRequest {
  const QuizConfigRequest({
    required this.mode,
    required this.questionType,
    required this.scopeType,
    required this.topicIds,
    required this.answerMethod,
    this.scopeDateFrom,
    this.scopeDateTo,
    this.timeLimitSec,
    this.lives,
  });

  final String mode;
  final int questionType;
  final String scopeType;
  final DateTime? scopeDateFrom;
  final DateTime? scopeDateTo;
  final List<int> topicIds;
  final int? timeLimitSec;
  final int? lives;
  final String answerMethod;

  Map<String, dynamic> toJson() => {
    'mode': mode,
    'question_type': questionType,
    'scope_type': scopeType,
    'scope_date_from': _date(scopeDateFrom),
    'scope_date_to': _date(scopeDateTo),
    'topic_ids': topicIds.isEmpty ? null : topicIds,
    'word_order': 'random',
    'word_limit': 20,
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
