class ProgressChart {
  const ProgressChart({required this.granularity, required this.points});

  final String granularity;
  final List<ProgressChartPoint> points;
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
}

class MasteryBreakdown {
  const MasteryBreakdown({required this.masteryLevel, required this.wordCount});

  final int masteryLevel;
  final int wordCount;
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
}
