import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/quiz/data/quiz_repository.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

void main() {
  test(
    'createSession sends current backend contract and parses response',
    () async {
      final dio = Dio();
      dio.httpClientAdapter = CallbackAdapter((options) {
        expect(options.path, ApiEndpoints.quizSessions);
        expect(options.method, 'POST');
        expect(options.data, {
          'mode': 'timed',
          'question_type': 2,
          'scope_type': 'date_range',
          'scope_date_from': '2026-06-01',
          'scope_date_to': '2026-06-15',
          'topic_ids': [2, 3],
          'word_order': 'random',
          'word_limit': 20,
          'time_limit_sec': 120,
          'lives': null,
          'answer_method': 'exact_typing',
        });
        return jsonResponse({
          'session': {
            'session_id': 9,
            'answer_method': 'exact_typing',
            'mode': 'timed',
            'question_count': 20,
            'time_limit_sec': 120,
            'lives': null,
          },
          'first_question': {
            'word_id': 7,
            'sense_id': 8,
            'question_type': 2,
            'display_content': 'quả táo',
            'choices': ['apple', 'orange', 'pear', 'grape'],
          },
        });
      });

      final result = await QuizRepository(dio: dio).createSession(
        QuizConfigRequest(
          mode: 'timed',
          questionType: 2,
          scopeType: 'date_range',
          scopeDateFrom: DateTime(2026, 6, 1),
          scopeDateTo: DateTime(2026, 6, 15),
          topicIds: const [2, 3],
          timeLimitSec: 120,
          answerMethod: 'exact_typing',
        ),
      );

      expect(result.sessionId, 9);
      expect(result.firstQuestionWordId, 7);
      expect(result.questionCount, 20);
      expect(result.firstQuestion.choices, hasLength(4));
    },
  );

  test(
    'submitAnswer sends answer and parses feedback with next question',
    () async {
      final dio = Dio();
      dio.httpClientAdapter = CallbackAdapter((options) {
        expect(options.path, ApiEndpoints.quizAnswer(9));
        expect(options.method, 'POST');
        expect(options.data, {'word_id': 7, 'user_answer': 'orange'});
        return jsonResponse({
          'is_correct': false,
          'expected_answer': 'apple',
          'correct_count': 0,
          'wrong_count': 1,
          'score': 0,
          'ai_score': 0.75,
          'ai_explanation': 'Gần đúng.',
          'ai_suggestion': 'Dùng từ apple.',
          'next_question': {
            'word_id': 10,
            'sense_id': 11,
            'question_type': 2,
            'display_content': 'quả lê',
            'choices': ['pear', 'orange', 'apple', 'grape'],
          },
        });
      });

      final result = await QuizRepository(
        dio: dio,
      ).submitAnswer(sessionId: 9, wordId: 7, answer: 'orange');

      expect(result.isCorrect, isFalse);
      expect(result.expectedAnswer, 'apple');
      expect(result.nextQuestion?.wordId, 10);
      expect(result.aiScore, 0.75);
      expect(result.aiExplanation, 'Gần đúng.');
      expect(result.aiSuggestion, 'Dùng từ apple.');
    },
  );

  test('finishSession posts to current backend endpoint', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.quizFinish(9));
      expect(options.method, 'POST');
      return jsonResponse({});
    });

    await QuizRepository(dio: dio).finishSession(9);
  });

  test('loads quiz result with answer breakdown', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.quizResult(9));
      return jsonResponse(resultJson);
    });

    final result = await QuizRepository(dio: dio).getResult(9);

    expect(result.accuracy, 50);
    expect(result.durationSec, 75);
    expect(result.answers.single.userAnswer, 'orange');
  });

  test('loads paged wrong words and removes one', () async {
    final dio = Dio();
    final requests = <RequestOptions>[];
    dio.httpClientAdapter = CallbackAdapter((options) {
      requests.add(options);
      return jsonResponse(
        options.method == 'GET'
            ? {
                'items': [wrongWordJson],
                'page': 1,
                'totalPages': 2,
              }
            : true,
      );
    });
    final repository = QuizRepository(dio: dio);

    final page = await repository.getWrongWords(page: 1);
    await repository.removeWrongWord(7);

    expect(page.items.single.word, 'apple');
    expect(page.totalPages, 2);
    expect(requests[0].queryParameters, {'page': 1, 'limit': 20});
    expect(requests[1].path, ApiEndpoints.quizWrongWord(7));
    expect(requests[1].method, 'DELETE');
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
  201,
  headers: {
    Headers.contentTypeHeader: [Headers.jsonContentType],
  },
);

const resultJson = {
  'session_id': 9,
  'status': 'completed',
  'correct_count': 1,
  'wrong_count': 1,
  'question_count': 2,
  'answered_count': 2,
  'accuracy': 50,
  'duration_sec': 75,
  'max_streak': 1,
  'score': 50,
  'answers': [
    {
      'word_id': 7,
      'question_number': 1,
      'display_content': 'apple',
      'expected_answer': 'apple',
      'user_answer': 'orange',
      'is_correct': false,
    },
  ],
};

const wrongWordJson = {
  'word_id': 7,
  'word': 'apple',
  'primary_meaning': 'quả táo',
  'test_count': 4,
  'correct_count': 1,
  'wrong_count': 3,
  'mastery_level': 2,
};
