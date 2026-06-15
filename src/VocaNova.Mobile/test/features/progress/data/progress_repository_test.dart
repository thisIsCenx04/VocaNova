import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/progress/data/progress_repository.dart';

void main() {
  test('getSummary calls current API and parses all overview fields', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.progressSummary);
      expect(options.method, 'GET');
      return jsonResponse(summaryJson);
    });

    final summary = await ProgressRepository(dio: dio).getSummary();

    expect(summary.currentStreakDays, 4);
    expect(summary.longestStreakDays, 9);
    expect(summary.accuracy7Days, 75);
    expect(summary.totalWordsInProgress, 42);
    expect(summary.masteredWords, 12);
    expect(summary.sessionsThisMonth, 8);
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

const summaryJson = {
  'current_streak_days': 4,
  'longest_streak_days': 9,
  'accuracy_7d': 75,
  'correct_7d': 15,
  'total_answers_7d': 20,
  'total_words_in_progress': 42,
  'mastered_words': 12,
  'sessions_this_month': 8,
};
