import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/lists/data/services/lists_api_service.dart';

void main() {
  test('list word requests match pagination and mutation contracts', () async {
    final dio = Dio();
    final requests = <RequestOptions>[];
    dio.httpClientAdapter = CallbackAdapter((options) {
      requests.add(options);
      if (options.method == 'GET') {
        return jsonResponse({
          'items': [wordJson],
          'page': 2,
          'limit': 20,
          'totalItems': 21,
          'totalPages': 2,
        });
      }
      if (options.path.endsWith('/random')) {
        return jsonResponse({
          'added_count': 1,
          'words': [wordJson],
        });
      }
      if (options.method == 'POST') return jsonResponse(wordJson);
      return jsonResponse(true);
    });
    final repository = ListsApiService(dio: dio);

    final page = await repository.getWords(listId: 3, page: 2);
    final added = await repository.addWord(listId: 3, wordId: 7);
    final random = await repository.addRandomWords(
      listId: 3,
      count: 5,
      method: 'random_topic',
      topicId: 2,
    );
    await repository.removeWord(listId: 3, wordId: 7);

    expect(page.items.single.word, 'hello');
    expect(page.totalPages, 2);
    expect(added.wordId, 7);
    expect(random.single.wordId, 7);
    expect(requests[0].queryParameters, {'page': 2, 'limit': 20});
    expect(requests[1].data, {
      'word_id': 7,
      'add_method': 'search',
      'note': null,
    });
    expect(requests[2].data, {
      'topic_id': 2,
      'count': 5,
      'method': 'random_topic',
    });
    expect(requests[3].path, ApiEndpoints.listWord(3, 7));
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

const wordJson = {
  'word_id': 7,
  'word': 'hello',
  'primary_meaning': 'xin chào',
  'correct_count': 3,
  'wrong_count': 1,
  'note': 'greeting',
  'added_at': '2026-06-15T10:00:00Z',
};
