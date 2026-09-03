import 'package:vocanova_mobile/features/dictionary/domain/models/word_summary.dart';

class WordSummaryDto {
  const WordSummaryDto({
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

  factory WordSummaryDto.fromJson(Map<String, dynamic> json) => WordSummaryDto(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    phonetic: json['phonetic'] as String?,
    cefr: json['cefr'] as String?,
    primaryMeaning: json['primary_meaning'] as String?,
    imageUrl: json['image_url'] as String?,
    topicIds:
        (json['topic_ids'] as List<dynamic>?)?.whereType<int>().toSet() ??
        const {},
    learningStatus:
        (json['learning_status'] ?? json['status'])?.toString().toLowerCase() ??
        'new',
    masteryScore: (json['mastery_score'] as num?)?.toDouble() ?? 0,
  );

  factory WordSummaryDto.fromDomain(WordSummary word) => WordSummaryDto(
    wordId: word.wordId,
    word: word.word,
    phonetic: word.phonetic,
    cefr: word.cefr,
    primaryMeaning: word.primaryMeaning,
    imageUrl: word.imageUrl,
    topicIds: word.topicIds,
    learningStatus: word.learningStatus,
    masteryScore: word.masteryScore,
  );

  WordSummary toDomain() => WordSummary(
    wordId: wordId,
    word: word,
    phonetic: phonetic,
    cefr: cefr,
    primaryMeaning: primaryMeaning,
    imageUrl: imageUrl,
    topicIds: topicIds,
    learningStatus: learningStatus,
    masteryScore: masteryScore,
  );

  Map<String, dynamic> toJson() => {
    'word_id': wordId,
    'word': word,
    'phonetic': phonetic,
    'cefr': cefr,
    'primary_meaning': primaryMeaning,
    'image_url': imageUrl,
    'topic_ids': topicIds.toList(),
    'learning_status': learningStatus,
    'mastery_score': masteryScore,
  };
}

class TopicSummaryDto {
  const TopicSummaryDto({
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

  factory TopicSummaryDto.fromJson(Map<String, dynamic> json) =>
      TopicSummaryDto(
        topicId: json['topic_id'] as int,
        name: json['name'] as String,
        nameVi: json['name_vi'] as String?,
        icon: json['icon'] as String?,
        wordCount: json['word_count'] as int,
      );

  factory TopicSummaryDto.fromDomain(TopicSummary topic) => TopicSummaryDto(
    topicId: topic.topicId,
    name: topic.name,
    nameVi: topic.nameVi,
    icon: topic.icon,
    wordCount: topic.wordCount,
  );

  TopicSummary toDomain() => TopicSummary(
    topicId: topicId,
    name: name,
    nameVi: nameVi,
    icon: icon,
    wordCount: wordCount,
  );

  Map<String, dynamic> toJson() => {
    'topic_id': topicId,
    'name': name,
    'name_vi': nameVi,
    'icon': icon,
    'word_count': wordCount,
  };
}

class PersonalTopicSummaryDto extends TopicSummaryDto {
  const PersonalTopicSummaryDto({
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

  factory PersonalTopicSummaryDto.fromJson(Map<String, dynamic> json) {
    return PersonalTopicSummaryDto(
      topicId: json['topic_id'] as int,
      listId: json['list_id'] as int?,
      name: json['name'] as String,
      nameVi: json['name_vi'] as String?,
      icon: json['icon'] as String?,
      wordCount: json['word_count'] as int,
      containsWord: json['contains_word'] as bool? ?? false,
    );
  }

  factory PersonalTopicSummaryDto.fromDomain(PersonalTopicSummary topic) =>
      PersonalTopicSummaryDto(
        topicId: topic.topicId,
        listId: topic.listId,
        name: topic.name,
        nameVi: topic.nameVi,
        icon: topic.icon,
        wordCount: topic.wordCount,
        containsWord: topic.containsWord,
      );

  @override
  PersonalTopicSummary toDomain() => PersonalTopicSummary(
    topicId: topicId,
    listId: listId,
    name: name,
    nameVi: nameVi,
    icon: icon,
    wordCount: wordCount,
    containsWord: containsWord,
  );

  @override
  Map<String, dynamic> toJson() => {
    ...super.toJson(),
    'list_id': listId,
    'contains_word': containsWord,
  };
}
