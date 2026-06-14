class WordSummary {
  const WordSummary({
    required this.wordId,
    required this.word,
    this.phonetic,
    this.cefr,
    this.primaryMeaning,
    this.imageUrl,
    this.topicIds = const {},
  });

  final int wordId;
  final String word;
  final String? phonetic;
  final String? cefr;
  final String? primaryMeaning;
  final String? imageUrl;
  final Set<int> topicIds;

  factory WordSummary.fromJson(Map<String, dynamic> json) {
    return WordSummary(
      wordId: json['word_id'] as int,
      word: json['word'] as String,
      phonetic: json['phonetic'] as String?,
      cefr: json['cefr'] as String?,
      primaryMeaning: json['primary_meaning'] as String?,
      imageUrl: json['image_url'] as String?,
      topicIds:
          (json['topic_ids'] as List<dynamic>?)?.whereType<int>().toSet() ??
          const {},
    );
  }

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
    );
  }

  Map<String, dynamic> toJson() => {
    'word_id': wordId,
    'word': word,
    'phonetic': phonetic,
    'cefr': cefr,
    'primary_meaning': primaryMeaning,
    'image_url': imageUrl,
    'topic_ids': topicIds.toList(),
  };
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

  String get displayName => nameVi?.trim().isNotEmpty == true ? nameVi! : name;

  factory TopicSummary.fromJson(Map<String, dynamic> json) {
    return TopicSummary(
      topicId: json['topic_id'] as int,
      name: json['name'] as String,
      nameVi: json['name_vi'] as String?,
      icon: json['icon'] as String?,
      wordCount: json['word_count'] as int,
    );
  }

  Map<String, dynamic> toJson() => {
    'topic_id': topicId,
    'name': name,
    'name_vi': nameVi,
    'icon': icon,
    'word_count': wordCount,
  };
}
