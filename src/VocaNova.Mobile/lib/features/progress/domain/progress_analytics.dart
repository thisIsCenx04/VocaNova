class ProgressChart {
  const ProgressChart({required this.granularity, required this.points});

  final String granularity;
  final List<ProgressChartPoint> points;

  factory ProgressChart.fromJson(Map<String, dynamic> json) => ProgressChart(
    granularity: json['granularity'] as String,
    points: (json['points'] as List<dynamic>)
        .whereType<Map<String, dynamic>>()
        .map(ProgressChartPoint.fromJson)
        .toList(growable: false),
  );
}

class ProgressChartPoint {
  const ProgressChartPoint({
    required this.periodLabel,
    required this.sessionsCount,
    required this.accuracy,
  });

  final String periodLabel;
  final int sessionsCount;
  final double accuracy;

  factory ProgressChartPoint.fromJson(Map<String, dynamic> json) =>
      ProgressChartPoint(
        periodLabel: json['period_label'] as String,
        sessionsCount: json['sessions_count'] as int,
        accuracy: (json['accuracy'] as num).toDouble(),
      );
}

class MasteryBreakdown {
  const MasteryBreakdown({required this.masteryLevel, required this.wordCount});

  final int masteryLevel;
  final int wordCount;

  factory MasteryBreakdown.fromJson(Map<String, dynamic> json) =>
      MasteryBreakdown(
        masteryLevel: json['mastery_level'] as int,
        wordCount: json['word_count'] as int,
      );
}

class WeakestWord {
  const WeakestWord({
    required this.wordId,
    required this.word,
    required this.testCount,
    required this.correctCount,
    required this.wrongCount,
    required this.accuracyRate,
    required this.masteryLevel,
    this.primaryMeaning,
  });

  final int wordId;
  final String word;
  final String? primaryMeaning;
  final int testCount;
  final int correctCount;
  final int wrongCount;
  final double accuracyRate;
  final int masteryLevel;

  factory WeakestWord.fromJson(Map<String, dynamic> json) => WeakestWord(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    primaryMeaning: json['primary_meaning'] as String?,
    testCount: json['test_count'] as int,
    correctCount: json['correct_count'] as int,
    wrongCount: json['wrong_count'] as int,
    accuracyRate: (json['accuracy_rate'] as num).toDouble(),
    masteryLevel: json['mastery_level'] as int,
  );
}
