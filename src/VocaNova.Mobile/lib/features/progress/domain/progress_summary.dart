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

  factory ProgressSummary.fromJson(Map<String, dynamic> json) =>
      ProgressSummary(
        currentStreakDays: json['current_streak_days'] as int,
        longestStreakDays: json['longest_streak_days'] as int,
        accuracy7Days: (json['accuracy_7d'] as num).toDouble(),
        correct7Days: json['correct_7d'] as int,
        totalAnswers7Days: json['total_answers_7d'] as int,
        totalWordsInProgress: json['total_words_in_progress'] as int,
        masteredWords: json['mastered_words'] as int,
        sessionsThisMonth: json['sessions_this_month'] as int,
      );

  Map<String, dynamic> toJson() => {
    'current_streak_days': currentStreakDays,
    'longest_streak_days': longestStreakDays,
    'accuracy_7d': accuracy7Days,
    'correct_7d': correct7Days,
    'total_answers_7d': totalAnswers7Days,
    'total_words_in_progress': totalWordsInProgress,
    'mastered_words': masteredWords,
    'sessions_this_month': sessionsThisMonth,
  };
}
