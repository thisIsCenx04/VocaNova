class WordDetail {
  const WordDetail({
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
  final List<WordSense> senses;
  final List<WordExample> examples;
  final List<WordRelation> relations;
  final List<WordAudio> audio;
  final List<WordTopic> topics;

  factory WordDetail.fromJson(Map<String, dynamic> json) => WordDetail(
    wordId: json['word_id'] as int,
    word: json['word'] as String,
    isPhrase: json['is_phrase'] as bool? ?? false,
    cefr: json['cefr'] as String?,
    phoneticUk: json['phonetic_uk'] as String?,
    phoneticUs: json['phonetic_us'] as String?,
    imageUrl: json['image_url'] as String?,
    senses: _maps(json['senses']).map(WordSense.fromJson).toList(),
    examples: _maps(json['examples']).map(WordExample.fromJson).toList(),
    relations: _maps(json['relations']).map(WordRelation.fromJson).toList(),
    audio: _maps(json['audio']).map(WordAudio.fromJson).toList(),
    topics: _maps(json['topics']).map(WordTopic.fromJson).toList(),
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

class WordSense {
  const WordSense({
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
  final List<WordExample> examples;
  final List<WordRelation> relations;

  factory WordSense.fromJson(Map<String, dynamic> json) => WordSense(
    senseId: json['sense_id'] as int,
    order: json['order'] as int,
    wordClass: json['word_class'] as String,
    englishDefinition: json['english_definition'] as String,
    vietnameseMeaning: json['vietnamese_meaning'] as String?,
    examples: _maps(json['examples']).map(WordExample.fromJson).toList(),
    relations: _maps(json['relations']).map(WordRelation.fromJson).toList(),
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

class WordExample {
  const WordExample({required this.exampleEn, this.exampleVi});

  final String exampleEn;
  final String? exampleVi;

  factory WordExample.fromJson(Map<String, dynamic> json) => WordExample(
    exampleEn: json['example_en'] as String,
    exampleVi: json['example_vi'] as String?,
  );

  Map<String, dynamic> toJson() => {
    'example_en': exampleEn,
    'example_vi': exampleVi,
  };
}

class WordRelation {
  const WordRelation({
    required this.relationType,
    required this.relatedWord,
    this.linkedWordId,
  });

  final String relationType;
  final String relatedWord;
  final int? linkedWordId;

  factory WordRelation.fromJson(Map<String, dynamic> json) => WordRelation(
    relationType: json['relation_type'] as String,
    relatedWord: json['related_word'] as String,
    linkedWordId: json['linked_word_id'] as int?,
  );

  Map<String, dynamic> toJson() => {
    'relation_type': relationType,
    'related_word': relatedWord,
    'linked_word_id': linkedWordId,
  };
}

class WordAudio {
  const WordAudio({required this.accent, required this.url});

  final String accent;
  final String url;

  factory WordAudio.fromJson(Map<String, dynamic> json) =>
      WordAudio(accent: json['accent'] as String, url: json['url'] as String);

  Map<String, dynamic> toJson() => {'accent': accent, 'url': url};
}

class WordTopic {
  const WordTopic({
    required this.topicId,
    required this.name,
    this.nameVi,
    this.icon,
  });

  final int topicId;
  final String name;
  final String? nameVi;
  final String? icon;
  String get displayName => nameVi?.trim().isNotEmpty == true ? nameVi! : name;

  factory WordTopic.fromJson(Map<String, dynamic> json) => WordTopic(
    topicId: json['topic_id'] as int,
    name: json['name'] as String,
    nameVi: json['name_vi'] as String?,
    icon: json['icon'] as String?,
  );

  Map<String, dynamic> toJson() => {
    'topic_id': topicId,
    'name': name,
    'name_vi': nameVi,
    'icon': icon,
  };
}

class UserListSummary {
  const UserListSummary({
    required this.listId,
    required this.listName,
    required this.wordCount,
  });

  final int listId;
  final String listName;
  final int wordCount;

  factory UserListSummary.fromJson(Map<String, dynamic> json) =>
      UserListSummary(
        listId: json['list_id'] as int,
        listName: json['list_name'] as String,
        wordCount: json['word_count'] as int,
      );
}

List<Map<String, dynamic>> _maps(Object? value) =>
    (value as List<dynamic>? ?? const [])
        .whereType<Map<String, dynamic>>()
        .toList();
