import 'package:vocanova_mobile/features/home/domain/models/personal_topic_recommendation.dart';

class RecommendedTopicWordDto {
  const RecommendedTopicWordDto({
    required this.wordId,
    required this.word,
    this.phonetic,
    this.cefr,
    this.primaryMeaning,
  });

  final int wordId;
  final String word;
  final String? phonetic;
  final String? cefr;
  final String? primaryMeaning;

  factory RecommendedTopicWordDto.fromJson(Map<String, dynamic> json) {
    return RecommendedTopicWordDto(
      wordId: json['word_id'] as int,
      word: json['word'] as String,
      phonetic: json['phonetic'] as String?,
      cefr: json['cefr'] as String?,
      primaryMeaning: json['primary_meaning'] as String?,
    );
  }

  RecommendedTopicWord toDomain() => RecommendedTopicWord(
    wordId: wordId,
    word: word,
    phonetic: phonetic,
    cefr: cefr,
    primaryMeaning: primaryMeaning,
  );
}

class PersonalTopicRecommendationDto {
  const PersonalTopicRecommendationDto({
    required this.topicId,
    required this.name,
    required this.wordCount,
    required this.score,
    required this.words,
    this.nameVi,
    this.icon,
  });

  final int topicId;
  final String name;
  final String? nameVi;
  final String? icon;
  final int wordCount;
  final double score;
  final List<RecommendedTopicWordDto> words;

  factory PersonalTopicRecommendationDto.fromJson(Map<String, dynamic> json) {
    return PersonalTopicRecommendationDto(
      topicId: json['topic_id'] as int,
      name: json['name'] as String,
      nameVi: json['name_vi'] as String?,
      icon: json['icon'] as String?,
      wordCount: json['word_count'] as int,
      score: (json['recommendation_score'] as num).toDouble(),
      words: (json['words'] as List<dynamic>? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map(RecommendedTopicWordDto.fromJson)
          .toList(growable: false),
    );
  }

  PersonalTopicRecommendation toDomain() => PersonalTopicRecommendation(
    topicId: topicId,
    name: name,
    nameVi: nameVi,
    icon: icon,
    wordCount: wordCount,
    score: score,
    words: words.map((dto) => dto.toDomain()).toList(growable: false),
  );
}
