import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_result.dart';

class QuizRepository {
  const QuizRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<QuizSessionStart> createSession(QuizConfigRequest request) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.quizSessions,
      data: request.toJson(),
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid quiz session response.');
    }
    return QuizSessionStart.fromJson(data);
  }

  Future<QuizAnswerResult> submitAnswer({
    required int sessionId,
    required int wordId,
    required String answer,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.quizAnswer(sessionId),
      data: {'word_id': wordId, 'user_answer': answer},
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid quiz answer response.');
    }
    return QuizAnswerResult.fromJson(data);
  }

  Future<void> finishSession(int sessionId) async {
    await _dio.post<Map<String, dynamic>>(ApiEndpoints.quizFinish(sessionId));
  }

  Future<QuizResult> getResult(int sessionId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.quizResult(sessionId),
    );
    return QuizResult.fromJson(_dataMap(response, 'quiz result'));
  }

  Future<WrongWordsPage> getWrongWords({required int page}) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.quizWrongWords,
      queryParameters: {'page': page, 'limit': 20},
    );
    final data = _dataMap(response, 'wrong words');
    final items = data['items'];
    if (items is! List) {
      throw const FormatException('Invalid wrong words response.');
    }
    return WrongWordsPage(
      items: items
          .whereType<Map<String, dynamic>>()
          .map(WrongWord.fromJson)
          .toList(growable: false),
      page: data['page'] as int,
      totalPages: data['totalPages'] as int,
    );
  }

  Future<void> removeWrongWord(int wordId) async {
    await _dio.delete<Map<String, dynamic>>(ApiEndpoints.quizWrongWord(wordId));
  }

  Map<String, dynamic> _dataMap(
    Response<Map<String, dynamic>> response,
    String name,
  ) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw FormatException('Invalid $name response.');
    }
    return data;
  }
}
