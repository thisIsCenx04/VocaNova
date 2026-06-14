import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/dictionary/data/word_search_repository.dart';

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
      return jsonResponse([
        {
          'word_id': 1,
          'word': 'hello',
          'phonetic': '/həˈləʊ/',
          'cefr': 'A1',
          'primary_meaning': 'xin chào',
          'image_url': null,
        },
      ]);
    });

    final words = await WordSearchRepository(
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

    final topics = await WordSearchRepository(dio: dio).getTopics();

    expect(topics.single.displayName, 'Du lịch');
    expect(topics.single.wordCount, 12);
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

ResponseBody jsonResponse(List<Map<String, dynamic>> data) {
  return ResponseBody.fromString(
    jsonEncode({'success': true, 'data': data, 'errors': <String>[]}),
    200,
    headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    },
  );
}
