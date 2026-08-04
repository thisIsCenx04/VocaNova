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

  test('analytics requests match backend contracts and parse data', () async {
    final dio = Dio();
    final requests = <RequestOptions>[];
    dio.httpClientAdapter = CallbackAdapter((options) {
      requests.add(options);
      return jsonResponse(switch (options.path) {
        ApiEndpoints.progressChart => chartJson,
        ApiEndpoints.progressMasteryBreakdown => masteryJson,
        _ => weakestJson,
      });
    });
    final repository = ProgressRepository(dio: dio);

    final chart = await repository.getChart('weekly');
    final mastery = await repository.getMasteryBreakdown();
    final weakest = await repository.getWeakestWords();

    expect(chart.granularity, 'weekly');
    expect(chart.points.single.sessionsCount, 3);
    expect(mastery.last.masteryLevel, 5);
    expect(weakest.single.accuracyRate, 25);
    expect(requests[0].queryParameters, {'granularity': 'weekly'});
    expect(requests[2].queryParameters, {'limit': 10});
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

const chartJson = {
  'granularity': 'weekly',
  'points': [
    {'period_label': '2026-06-09', 'sessions_count': 3, 'accuracy': 75},
  ],
};

const masteryJson = [
  {'mastery_level': 0, 'word_count': 2},
  {'mastery_level': 5, 'word_count': 7},
];

const weakestJson = [
  {
    'word_id': 7,
    'word': 'apple',
    'primary_meaning': 'quả táo',
    'test_count': 4,
    'correct_count': 1,
    'wrong_count': 3,
    'accuracy_rate': 25,
    'mastery_level': 1,
  },
];
