class QuizResult {
  const QuizResult({
    required this.sessionId,
    required this.status,
    required this.correctCount,
    required this.wrongCount,
    required this.questionCount,
    required this.answeredCount,
    required this.accuracy,
    required this.maxStreak,
    required this.score,
    required this.answers,
    this.durationSec,
  });

  final int sessionId;
  final String status;
  final int correctCount;
  final int wrongCount;
  final int questionCount;
  final int answeredCount;
  final double accuracy;
  final int? durationSec;
  final int maxStreak;
  final double score;
  final List<QuizResultAnswer> answers;

  factory QuizResult.fromJson(Map<String, dynamic> json) => QuizResult(
    sessionId: json['session_id'] as int,
    status: json['status'] as String,
    correctCount: json['correct_count'] as int,
    wrongCount: json['wrong_count'] as int,
    questionCount: json['question_count'] as int,
    answeredCount: json['answered_count'] as int,
    accuracy: (json['accuracy'] as num).toDouble(),
    durationSec: json['duration_sec'] as int?,
    maxStreak: json['max_streak'] as int,
    score: (json['score'] as num).toDouble(),
    answers: (json['answers'] as List<dynamic>)
        .whereType<Map<String, dynamic>>()
        .map(QuizResultAnswer.fromJson)
        .toList(growable: false),
  );
}

class QuizResultAnswer {
  const QuizResultAnswer({
    required this.wordId,
    required this.questionNumber,
    required this.displayContent,
    required this.expectedAnswer,
    required this.isCorrect,
    this.userAnswer,
  });

  final int wordId;
  final int questionNumber;
  final String displayContent;
  final String expectedAnswer;
  final String? userAnswer;
  final bool? isCorrect;

  factory QuizResultAnswer.fromJson(Map<String, dynamic> json) =>
      QuizResultAnswer(
        wordId: json['word_id'] as int,
        questionNumber: json['question_number'] as int,
        displayContent: json['display_content'] as String,
        expectedAnswer: json['expected_answer'] as String,
        userAnswer: json['user_answer'] as String?,
        isCorrect: json['is_correct'] as bool?,
      );
}

class WrongWord {
  const WrongWord({
    required this.wordId,
    required this.word,
    required this.testCount,
    required this.correctCount,
    required this.wrongCount,
    required this.masteryLevel,
    this.primaryMeaning,
  });

  final int wordId;
  final String word;
  final String? primaryMeaning;
  final int testCount;
  final int correctCount;
  final int wrongCount;
  final int masteryLevel;

  factory WrongWord.fromJson(Map<String, dynamic> json) => WrongWord(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    primaryMeaning: json['primary_meaning'] as String?,
    testCount: json['test_count'] as int,
    correctCount: json['correct_count'] as int,
    wrongCount: json['wrong_count'] as int,
    masteryLevel: json['mastery_level'] as int,
  );
}

class WrongWordsPage {
  const WrongWordsPage({
    required this.items,
    required this.page,
    required this.totalPages,
  });

  final List<WrongWord> items;
  final int page;
  final int totalPages;
}
