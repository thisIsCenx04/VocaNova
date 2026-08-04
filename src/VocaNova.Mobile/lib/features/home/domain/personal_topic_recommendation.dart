class RecommendedTopicWord {
  const RecommendedTopicWord({
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

  factory RecommendedTopicWord.fromJson(Map<String, dynamic> json) {
    return RecommendedTopicWord(
      wordId: json['word_id'] as int,
      word: json['word'] as String,
      phonetic: json['phonetic'] as String?,
      cefr: json['cefr'] as String?,
      primaryMeaning: json['primary_meaning'] as String?,
    );
  }
}

class PersonalTopicRecommendation {
  const PersonalTopicRecommendation({
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
  final List<RecommendedTopicWord> words;

  String get displayName => nameVi?.trim().isNotEmpty == true ? nameVi! : name;

  factory PersonalTopicRecommendation.fromJson(Map<String, dynamic> json) {
    return PersonalTopicRecommendation(
      topicId: json['topic_id'] as int,
      name: json['name'] as String,
      nameVi: json['name_vi'] as String?,
      icon: json['icon'] as String?,
      wordCount: json['word_count'] as int,
      score: (json['recommendation_score'] as num).toDouble(),
      words: (json['words'] as List<dynamic>? ?? const [])
          .whereType<Map<String, dynamic>>()
          .map(RecommendedTopicWord.fromJson)
          .toList(growable: false),
    );
  }
}
