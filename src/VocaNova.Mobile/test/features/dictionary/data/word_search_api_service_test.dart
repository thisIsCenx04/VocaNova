import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/dictionary/data/services/word_search_api_service.dart';

void main() {
  test('search sends query filters and parses word summaries', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.words);
      expect(options.queryParameters, {
        'q': 'hel',
        'page': 1,
        'limit': 50,
        'cefr': 'A1',
        'topicId': 2,
      });
      return jsonResponse({
        'items': [
          {
            'word_id': 1,
            'word': 'hello',
            'phonetic': '/həˈləʊ/',
            'cefr': 'A1',
            'primary_meaning': 'xin chào',
            'image_url': null,
          },
        ],
        'page': 1,
        'limit': 50,
        'totalItems': 1,
        'totalPages': 1,
      });
    });

    final words = await WordSearchApiService(
      dio: dio,
    ).search(query: 'hel', cefr: 'A1', topicId: 2);

    expect(words.single.word, 'hello');
    expect(words.single.primaryMeaning, 'xin chào');
  });

  test('getTopics parses topic API contract', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.topics);
      return jsonResponse([
        {
          'topic_id': 2,
          'name': 'Travel',
          'name_vi': 'Du lịch',
          'icon': 'plane',
          'word_count': 12,
        },
      ]);
    });

    final topics = await WordSearchApiService(dio: dio).getTopics();

    expect(topics.single.name, 'Travel');
    expect(topics.single.nameVi, 'Du lịch');
    expect(topics.single.wordCount, 12);
  });

  test(
    'getTopicWords uses topic endpoint and parses learning progress',
    () async {
      final dio = Dio();
      dio.httpClientAdapter = CallbackAdapter((options) {
        expect(options.path, ApiEndpoints.topicWords(2));
        expect(options.queryParameters, {'page': 1, 'limit': 100});
        return jsonResponse({
          'items': [
            {
              'word_id': 3,
              'word': 'journey',
              'learning_status': 'learning',
              'mastery_score': 65,
            },
          ],
        });
      });

      final words = await WordSearchApiService(dio: dio).getTopicWords(2);

      expect(words.single.topicIds, contains(2));
      expect(words.single.learningStatus, 'learning');
      expect(words.single.masteryScore, 65);
    },
  );

  test('personal topic APIs expose saved counts, words and removal', () async {
    final dio = Dio();
    final requests = <RequestOptions>[];
    dio.httpClientAdapter = CallbackAdapter((options) {
      requests.add(options);
      if (options.path == ApiEndpoints.personalTopics) {
        return jsonResponse([
          {
            'topic_id': 2,
            'list_id': 12,
            'name': 'Travel',
            'name_vi': null,
            'icon': null,
            'word_count': 1,
            'contains_word': false,
          },
        ]);
      }
      if (options.method == 'DELETE') return jsonResponse(true);
      return jsonResponse({
        'items': [
          {'word_id': 3, 'word': 'journey', 'primary_meaning': 'chuyến đi'},
        ],
      });
    });
    final repository = WordSearchApiService(dio: dio);

    final topics = await repository.getPersonalTopics();
    final words = await repository.getPersonalTopicWords(2);
    await repository.removePersonalTopicWord(topicId: 2, wordId: 3);

    expect(topics.single.listId, 12);
    expect(words.single.word, 'journey');
    expect(requests.map((request) => request.path), [
      ApiEndpoints.personalTopics,
      ApiEndpoints.personalTopicWords(2),
      ApiEndpoints.personalTopicWord(2, 3),
    ]);
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
  ) async {
    return callback(options);
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody jsonResponse(Object data) {
  return ResponseBody.fromString(
    jsonEncode({'success': true, 'data': data, 'errors': <String>[]}),
    200,
    headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    },
  );
}
