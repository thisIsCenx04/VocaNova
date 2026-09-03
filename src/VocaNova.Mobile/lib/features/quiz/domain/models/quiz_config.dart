class QuizSessionStart {
  const QuizSessionStart({
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
  final QuizQuestion firstQuestion;

  int get firstQuestionWordId => firstQuestion.wordId;
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
}

class QuizConfigRequest {
  const QuizConfigRequest({
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

  /// Số câu hỏi tối đa; null nghĩa là lấy toàn bộ từ trong phạm vi.
  final int? wordLimit;
  final int? listId;
  final DateTime? scopeDateFrom;
  final DateTime? scopeDateTo;
  final List<int> topicIds;
  final int? timeLimitSec;
  final int? lives;
  final String answerMethod;
}
