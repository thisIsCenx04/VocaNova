class QuizHistoryItem {
  const QuizHistoryItem({
    required this.sessionId,
    required this.answerMethod,
    required this.mode,
    required this.questionType,
    required this.questionCount,
    required this.correctCount,
    required this.wrongCount,
    required this.accuracy,
    required this.score,
    required this.maxStreak,
    required this.status,
    required this.startedAt,
    this.endedAt,
  });

  final int sessionId;
  final String answerMethod;
  final String mode;
  final int questionType;
  final int questionCount;
  final int correctCount;
  final int wrongCount;
  final double accuracy;
  final double score;
  final int maxStreak;
  final String status;
  final DateTime startedAt;
  final DateTime? endedAt;
}

class QuizHistoryPage {
  const QuizHistoryPage({
    required this.items,
    required this.page,
    required this.totalPages,
  });

  final List<QuizHistoryItem> items;
  final int page;
  final int totalPages;
}
