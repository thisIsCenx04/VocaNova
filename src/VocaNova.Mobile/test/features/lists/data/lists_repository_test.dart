import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/lists/data/lists_repository.dart';

void main() {
  test('lists CRUD requests match backend contracts', () async {
    final dio = Dio();
    final requests = <RequestOptions>[];
    dio.httpClientAdapter = CallbackAdapter((options) {
      requests.add(options);
      if (options.method == 'DELETE') return jsonResponse(true);
      if (options.path == ApiEndpoints.personalTopics) {
        return jsonResponse([personalTopicJson]);
      }
      return jsonResponse(
        options.method == 'GET'
            ? [listJson]
            : {...listJson, 'list_name': options.data['list_name']},
      );
    });
    final repository = ListsRepository(dio: dio);

    expect((await repository.getLists()).single.listName, 'Favorites');
    expect(
      (await repository.getPersonalTopics()).single.displayName,
      'Du lịch',
    );
    expect((await repository.create('Travel')).listName, 'Travel');
    expect(
      (await repository.rename(listId: 3, name: 'Daily')).listName,
      'Daily',
    );
    await repository.delete(3);

    expect(requests.map((request) => request.method), [
      'GET',
      'GET',
      'POST',
      'PUT',
      'DELETE',
    ]);
    expect(requests.map((request) => request.path), [
      ApiEndpoints.lists,
      ApiEndpoints.personalTopics,
      ApiEndpoints.lists,
      ApiEndpoints.list(3),
      ApiEndpoints.list(3),
    ]);
    expect(requests[2].data, {'list_name': 'Travel'});
    expect(requests[3].data, {'list_name': 'Daily'});
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

const listJson = {
  'list_id': 3,
  'list_name': 'Favorites',
  'word_count': 2,
  'created_at': '2026-06-15T10:00:00Z',
};

const personalTopicJson = {
  'topic_id': 8,
  'list_id': 18,
  'name': 'Travel',
  'name_vi': 'Du lịch',
  'icon': '✈️',
  'word_count': 4,
  'contains_word': false,
};
