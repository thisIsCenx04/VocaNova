import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/dictionary/data/services/word_detail_api_service.dart';

void main() {
  test('getWord parses the current word detail API contract', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.word(7));
      return jsonResponse(wordJson);
    });

    final word = await WordDetailApiService(dio: dio).getWord(7);

    expect(word.word, 'hello');
    expect(word.senses.single.vietnameseMeaning, 'xin chào');
    expect(word.audio.single.accent, 'UK');
    expect(word.topics.single.displayName, 'Giao tiếp');
  });

  test('lists and add word requests match backend endpoints', () async {
    final dio = Dio();
    var requests = 0;
    dio.httpClientAdapter = CallbackAdapter((options) {
      requests++;
      if (options.method == 'GET') {
        expect(options.path, ApiEndpoints.lists);
        return jsonResponse([
          {'list_id': 3, 'list_name': 'Favorites', 'word_count': 2},
        ]);
      }
      expect(options.path, ApiEndpoints.listWords(3));
      expect(options.data, {
        'word_id': 7,
        'add_method': 'manual',
        'note': 'Review tomorrow',
      });
      return jsonResponse({'word_id': 7});
    });
    final repository = WordDetailApiService(dio: dio);

    expect((await repository.getLists()).single.listName, 'Favorites');
    await repository.addWordToList(
      listId: 3,
      wordId: 7,
      note: 'Review tomorrow',
    );
    expect(requests, 2);
  });

  test(
    'createList sends the list contract and parses the created list',
    () async {
      final dio = Dio();
      dio.httpClientAdapter = CallbackAdapter((options) {
        expect(options.method, 'POST');
        expect(options.path, ApiEndpoints.lists);
        expect(options.data, {'list_name': 'Study'});
        return jsonResponse({
          'list_id': 9,
          'list_name': 'Study',
          'word_count': 0,
        });
      });

      final list = await WordDetailApiService(dio: dio).createList('Study');

      expect(list.listId, 9);
      expect(list.listName, 'Study');
    },
  );

  test('personal topic destinations and add request match backend', () async {
    final dio = Dio();
    final requests = <RequestOptions>[];
    dio.httpClientAdapter = CallbackAdapter((options) {
      requests.add(options);
      if (options.method == 'GET') {
        expect(options.path, ApiEndpoints.personalTopics);
        expect(options.queryParameters, {'wordId': 7});
        return jsonResponse([
          {
            'topic_id': 2,
            'list_id': null,
            'name': 'Communication',
            'name_vi': null,
            'icon': null,
            'word_count': 0,
            'contains_word': false,
          },
        ]);
      }
      expect(options.path, ApiEndpoints.personalTopicWords(2));
      expect(options.data, {'word_id': 7, 'note': 'Practice'});
      return jsonResponse({
        'topic_id': 2,
        'list_id': 12,
        'name': 'Communication',
        'name_vi': null,
        'icon': null,
        'word_count': 1,
        'contains_word': true,
      });
    });
    final repository = WordDetailApiService(dio: dio);

    final options = await repository.getPersonalTopics(wordId: 7);
    final saved = await repository.addWordToPersonalTopic(
      topicId: 2,
      wordId: 7,
      note: 'Practice',
    );

    expect(options.single.wordCount, 0);
    expect(saved.listId, 12);
    expect(saved.containsWord, isTrue);
    expect(requests, hasLength(2));
  });
}

typedef AdapterCallback = ResponseBody Function(RequestOptions options);

class CallbackAdapter implements HttpClientAdapter {
  CallbackAdapter(this.callback);
  final AdapterCallback callback;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async => callback(options);

  @override
  void close({bool force = false}) {}
}

ResponseBody jsonResponse(Object data) => ResponseBody.fromString(
  jsonEncode({'success': true, 'data': data, 'errors': <String>[]}),
  200,
  headers: {
    Headers.contentTypeHeader: [Headers.jsonContentType],
  },
);

const wordJson = <String, dynamic>{
  'word_id': 7,
  'word': 'hello',
  'word_key': 'hello',
  'cefr': 'A1',
  'phonetic_uk': '/həˈləʊ/',
  'phonetic_us': '/həˈloʊ/',
  'image_url': null,
  'is_phrase': false,
  'senses': [
    {
      'sense_id': 1,
      'order': 1,
      'word_class': 'interjection',
      'english_definition': 'used as a greeting',
      'vietnamese_meaning': 'xin chào',
      'examples': <Map<String, dynamic>>[],
      'relations': <Map<String, dynamic>>[],
    },
  ],
  'examples': <Map<String, dynamic>>[],
  'relations': <Map<String, dynamic>>[],
  'audio': [
    {
      'audio_id': 1,
      'accent': 'UK',
      'source': 'seed',
      'url': 'https://audio.test/uk.mp3',
      'status': 'ready',
    },
  ],
  'derived_forms': <Map<String, dynamic>>[],
  'idioms': <Map<String, dynamic>>[],
  'topics': [
    {
      'topic_id': 2,
      'name': 'Communication',
      'name_vi': 'Giao tiếp',
      'icon': null,
    },
  ],
};
