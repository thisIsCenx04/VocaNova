import 'package:vocanova_mobile/features/progress/domain/models/progress_analytics.dart';

class ProgressChartDto {
  const ProgressChartDto({required this.granularity, required this.points});

  final String granularity;
  final List<ProgressChartPointDto> points;

  factory ProgressChartDto.fromJson(Map<String, dynamic> json) =>
      ProgressChartDto(
        granularity: json['granularity'] as String,
        points: (json['points'] as List<dynamic>)
            .whereType<Map<String, dynamic>>()
            .map(ProgressChartPointDto.fromJson)
            .toList(growable: false),
      );

  ProgressChart toDomain() => ProgressChart(
    granularity: granularity,
    points: points.map((dto) => dto.toDomain()).toList(growable: false),
  );
}

class ProgressChartPointDto {
  const ProgressChartPointDto({
    required this.periodLabel,
    required this.sessionsCount,
    required this.accuracy,
  });

  final String periodLabel;
  final int sessionsCount;
  final double accuracy;

  factory ProgressChartPointDto.fromJson(Map<String, dynamic> json) =>
      ProgressChartPointDto(
        periodLabel: json['period_label'] as String,
        sessionsCount: json['sessions_count'] as int,
        accuracy: (json['accuracy'] as num).toDouble(),
      );

  ProgressChartPoint toDomain() => ProgressChartPoint(
    periodLabel: periodLabel,
    sessionsCount: sessionsCount,
    accuracy: accuracy,
  );
}

class MasteryBreakdownDto {
  const MasteryBreakdownDto({
    required this.masteryLevel,
    required this.wordCount,
  });

  final int masteryLevel;
  final int wordCount;

  factory MasteryBreakdownDto.fromJson(Map<String, dynamic> json) =>
      MasteryBreakdownDto(
        masteryLevel: json['mastery_level'] as int,
        wordCount: json['word_count'] as int,
      );

  MasteryBreakdown toDomain() =>
      MasteryBreakdown(masteryLevel: masteryLevel, wordCount: wordCount);
}

class WeakestWordDto {
  const WeakestWordDto({
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

  factory WeakestWordDto.fromJson(Map<String, dynamic> json) => WeakestWordDto(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    primaryMeaning: json['primary_meaning'] as String?,
    testCount: json['test_count'] as int,
    correctCount: json['correct_count'] as int,
    wrongCount: json['wrong_count'] as int,
    accuracyRate: (json['accuracy_rate'] as num).toDouble(),
    masteryLevel: json['mastery_level'] as int,
  );

  WeakestWord toDomain() => WeakestWord(
    wordId: wordId,
    word: word,
    primaryMeaning: primaryMeaning,
    testCount: testCount,
    correctCount: correctCount,
    wrongCount: wrongCount,
    accuracyRate: accuracyRate,
    masteryLevel: masteryLevel,
  );
}
