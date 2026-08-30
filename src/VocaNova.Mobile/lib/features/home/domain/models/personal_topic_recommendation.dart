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
}
