class WordSummary {
  const WordSummary({
    required this.wordId,
    required this.word,
    this.phonetic,
    this.cefr,
    this.primaryMeaning,
    this.imageUrl,
    this.topicIds = const {},
    this.learningStatus = 'new',
    this.masteryScore = 0,
  });

  final int wordId;
  final String word;
  final String? phonetic;
  final String? cefr;
  final String? primaryMeaning;
  final String? imageUrl;
  final Set<int> topicIds;
  final String learningStatus;
  final double masteryScore;

  WordSummary withTopic(int? topicId) {
    if (topicId == null || topicIds.contains(topicId)) {
      return this;
    }
    return WordSummary(
      wordId: wordId,
      word: word,
      phonetic: phonetic,
      cefr: cefr,
      primaryMeaning: primaryMeaning,
      imageUrl: imageUrl,
      topicIds: {...topicIds, topicId},
      learningStatus: learningStatus,
      masteryScore: masteryScore,
    );
  }

  WordSummary merge(WordSummary other) {
    return WordSummary(
      wordId: wordId,
      word: word,
      phonetic: phonetic ?? other.phonetic,
      cefr: cefr ?? other.cefr,
      primaryMeaning: primaryMeaning ?? other.primaryMeaning,
      imageUrl: imageUrl ?? other.imageUrl,
      topicIds: {...topicIds, ...other.topicIds},
      learningStatus: learningStatus,
      masteryScore: masteryScore,
    );
  }
}

class TopicSummary {
  const TopicSummary({
    required this.topicId,
    required this.name,
    required this.wordCount,
    this.nameVi,
    this.icon,
  });

  final int topicId;
  final String name;
  final String? nameVi;
  final String? icon;
  final int wordCount;
}

class PersonalTopicSummary extends TopicSummary {
  const PersonalTopicSummary({
    required super.topicId,
    required super.name,
    required super.wordCount,
    required this.containsWord,
    super.nameVi,
    super.icon,
    this.listId,
  });

  final int? listId;
  final bool containsWord;
}
