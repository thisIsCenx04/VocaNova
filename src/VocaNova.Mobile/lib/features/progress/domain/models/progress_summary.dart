class ProgressSummary {
  const ProgressSummary({
    required this.currentStreakDays,
    required this.longestStreakDays,
    required this.accuracy7Days,
    required this.correct7Days,
    required this.totalAnswers7Days,
    required this.totalWordsInProgress,
    required this.masteredWords,
    required this.sessionsThisMonth,
  });

  final int currentStreakDays;
  final int longestStreakDays;
  final double accuracy7Days;
  final int correct7Days;
  final int totalAnswers7Days;
  final int totalWordsInProgress;
  final int masteredWords;
  final int sessionsThisMonth;
}
