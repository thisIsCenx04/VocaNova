import 'package:vocanova_mobile/features/dictionary/domain/models/word_detail.dart';

class WordDetailDto {
  const WordDetailDto({
    required this.wordId,
    required this.word,
    required this.isPhrase,
    required this.senses,
    required this.examples,
    required this.relations,
    required this.audio,
    required this.topics,
    this.cefr,
    this.phoneticUk,
    this.phoneticUs,
    this.imageUrl,
  });

  final int wordId;
  final String word;
  final bool isPhrase;
  final String? cefr;
  final String? phoneticUk;
  final String? phoneticUs;
  final String? imageUrl;
  final List<WordSenseDto> senses;
  final List<WordExampleDto> examples;
  final List<WordRelationDto> relations;
  final List<WordAudioDto> audio;
  final List<WordTopicDto> topics;

  factory WordDetailDto.fromJson(Map<String, dynamic> json) => WordDetailDto(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    isPhrase: json['is_phrase'] as bool? ?? false,
    cefr: json['cefr'] as String?,
    phoneticUk: json['phonetic_uk'] as String?,
    phoneticUs: json['phonetic_us'] as String?,
    imageUrl: json['image_url'] as String?,
    senses: _maps(json['senses']).map(WordSenseDto.fromJson).toList(),
    examples: _maps(json['examples']).map(WordExampleDto.fromJson).toList(),
    relations: _maps(json['relations']).map(WordRelationDto.fromJson).toList(),
    audio: _maps(json['audio']).map(WordAudioDto.fromJson).toList(),
    topics: _maps(json['topics']).map(WordTopicDto.fromJson).toList(),
  );

  factory WordDetailDto.fromDomain(WordDetail word) => WordDetailDto(
    wordId: word.wordId,
    word: word.word,
    isPhrase: word.isPhrase,
    cefr: word.cefr,
    phoneticUk: word.phoneticUk,
    phoneticUs: word.phoneticUs,
    imageUrl: word.imageUrl,
    senses: word.senses.map(WordSenseDto.fromDomain).toList(),
    examples: word.examples.map(WordExampleDto.fromDomain).toList(),
    relations: word.relations.map(WordRelationDto.fromDomain).toList(),
    audio: word.audio.map(WordAudioDto.fromDomain).toList(),
    topics: word.topics.map(WordTopicDto.fromDomain).toList(),
  );

  WordDetail toDomain() => WordDetail(
    wordId: wordId,
    word: word,
    isPhrase: isPhrase,
    cefr: cefr,
    phoneticUk: phoneticUk,
    phoneticUs: phoneticUs,
    imageUrl: imageUrl,
    senses: senses.map((dto) => dto.toDomain()).toList(),
    examples: examples.map((dto) => dto.toDomain()).toList(),
    relations: relations.map((dto) => dto.toDomain()).toList(),
    audio: audio.map((dto) => dto.toDomain()).toList(),
    topics: topics.map((dto) => dto.toDomain()).toList(),
  );

  Map<String, dynamic> toJson() => {
    'word_id': wordId,
    'word': word,
    'is_phrase': isPhrase,
    'cefr': cefr,
    'phonetic_uk': phoneticUk,
    'phonetic_us': phoneticUs,
    'image_url': imageUrl,
    'senses': senses.map((item) => item.toJson()).toList(),
    'examples': examples.map((item) => item.toJson()).toList(),
    'relations': relations.map((item) => item.toJson()).toList(),
    'audio': audio.map((item) => item.toJson()).toList(),
    'topics': topics.map((item) => item.toJson()).toList(),
  };
}

class WordSenseDto {
  const WordSenseDto({
    required this.senseId,
    required this.order,
    required this.wordClass,
    required this.englishDefinition,
    required this.examples,
    required this.relations,
    this.vietnameseMeaning,
  });

  final int senseId;
  final int order;
  final String wordClass;
  final String englishDefinition;
  final String? vietnameseMeaning;
  final List<WordExampleDto> examples;
  final List<WordRelationDto> relations;

  factory WordSenseDto.fromJson(Map<String, dynamic> json) => WordSenseDto(
    senseId: json['sense_id'] as int,
    order: json['order'] as int,
    wordClass: json['word_class'] as String,
    englishDefinition: json['english_definition'] as String,
    vietnameseMeaning: json['vietnamese_meaning'] as String?,
    examples: _maps(json['examples']).map(WordExampleDto.fromJson).toList(),
    relations: _maps(json['relations']).map(WordRelationDto.fromJson).toList(),
  );

  factory WordSenseDto.fromDomain(WordSense sense) => WordSenseDto(
    senseId: sense.senseId,
    order: sense.order,
    wordClass: sense.wordClass,
    englishDefinition: sense.englishDefinition,
    vietnameseMeaning: sense.vietnameseMeaning,
    examples: sense.examples.map(WordExampleDto.fromDomain).toList(),
    relations: sense.relations.map(WordRelationDto.fromDomain).toList(),
  );

  WordSense toDomain() => WordSense(
    senseId: senseId,
    order: order,
    wordClass: wordClass,
    englishDefinition: englishDefinition,
    vietnameseMeaning: vietnameseMeaning,
    examples: examples.map((dto) => dto.toDomain()).toList(),
    relations: relations.map((dto) => dto.toDomain()).toList(),
  );

  Map<String, dynamic> toJson() => {
    'sense_id': senseId,
    'order': order,
    'word_class': wordClass,
    'english_definition': englishDefinition,
    'vietnamese_meaning': vietnameseMeaning,
    'examples': examples.map((item) => item.toJson()).toList(),
    'relations': relations.map((item) => item.toJson()).toList(),
  };
}

class WordExampleDto {
  const WordExampleDto({required this.exampleEn, this.exampleVi});

  final String exampleEn;
  final String? exampleVi;

  factory WordExampleDto.fromJson(Map<String, dynamic> json) => WordExampleDto(
    exampleEn: json['example_en'] as String,
    exampleVi: json['example_vi'] as String?,
  );

  factory WordExampleDto.fromDomain(WordExample example) => WordExampleDto(
    exampleEn: example.exampleEn,
    exampleVi: example.exampleVi,
  );

  WordExample toDomain() =>
      WordExample(exampleEn: exampleEn, exampleVi: exampleVi);

  Map<String, dynamic> toJson() => {
    'example_en': exampleEn,
    'example_vi': exampleVi,
  };
}

class WordRelationDto {
  const WordRelationDto({
    required this.relationType,
    required this.relatedWord,
    this.linkedWordId,
  });

  final String relationType;
  final String relatedWord;
  final int? linkedWordId;

  factory WordRelationDto.fromJson(Map<String, dynamic> json) =>
      WordRelationDto(
        relationType: json['relation_type'] as String,
        relatedWord: json['related_word'] as String,
        linkedWordId: json['linked_word_id'] as int?,
      );

  factory WordRelationDto.fromDomain(WordRelation relation) => WordRelationDto(
    relationType: relation.relationType,
    relatedWord: relation.relatedWord,
    linkedWordId: relation.linkedWordId,
  );

  WordRelation toDomain() => WordRelation(
    relationType: relationType,
    relatedWord: relatedWord,
    linkedWordId: linkedWordId,
  );

  Map<String, dynamic> toJson() => {
    'relation_type': relationType,
    'related_word': relatedWord,
    'linked_word_id': linkedWordId,
  };
}

class WordAudioDto {
  const WordAudioDto({required this.accent, required this.url});

  final String accent;
  final String url;

  factory WordAudioDto.fromJson(Map<String, dynamic> json) => WordAudioDto(
    accent: json['accent'] as String,
    url: json['url'] as String,
  );

  factory WordAudioDto.fromDomain(WordAudio audio) =>
      WordAudioDto(accent: audio.accent, url: audio.url);

  WordAudio toDomain() => WordAudio(accent: accent, url: url);

  Map<String, dynamic> toJson() => {'accent': accent, 'url': url};
}

class WordTopicDto {
  const WordTopicDto({
    required this.topicId,
    required this.name,
    this.nameVi,
    this.icon,
  });

  final int topicId;
  final String name;
  final String? nameVi;
  final String? icon;

  factory WordTopicDto.fromJson(Map<String, dynamic> json) => WordTopicDto(
    topicId: json['topic_id'] as int,
    name: json['name'] as String,
    nameVi: json['name_vi'] as String?,
    icon: json['icon'] as String?,
  );

  factory WordTopicDto.fromDomain(WordTopic topic) => WordTopicDto(
    topicId: topic.topicId,
    name: topic.name,
    nameVi: topic.nameVi,
    icon: topic.icon,
  );

  WordTopic toDomain() =>
      WordTopic(topicId: topicId, name: name, nameVi: nameVi, icon: icon);

  Map<String, dynamic> toJson() => {
    'topic_id': topicId,
    'name': name,
    'name_vi': nameVi,
    'icon': icon,
  };
}

class UserListSummaryDto {
  const UserListSummaryDto({
    required this.listId,
    required this.listName,
    required this.wordCount,
  });

  final int listId;
  final String listName;
  final int wordCount;

  factory UserListSummaryDto.fromJson(Map<String, dynamic> json) =>
      UserListSummaryDto(
        listId: json['list_id'] as int,
        listName: json['list_name'] as String,
        wordCount: json['word_count'] as int,
      );

  UserListSummary toDomain() =>
      UserListSummary(listId: listId, listName: listName, wordCount: wordCount);
}

List<Map<String, dynamic>> _maps(Object? value) =>
    (value as List<dynamic>? ?? const [])
        .whereType<Map<String, dynamic>>()
        .toList();
