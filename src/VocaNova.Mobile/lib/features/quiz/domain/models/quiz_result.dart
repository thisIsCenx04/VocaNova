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
