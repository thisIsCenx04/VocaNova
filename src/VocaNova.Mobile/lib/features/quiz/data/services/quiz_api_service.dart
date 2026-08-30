import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/quiz/data/dtos/quiz_config_dto.dart';
import 'package:vocanova_mobile/features/quiz/data/dtos/quiz_result_dto.dart';
import 'package:vocanova_mobile/features/quiz/domain/models/quiz_config.dart';
import 'package:vocanova_mobile/features/quiz/domain/models/quiz_result.dart';

class QuizApiService {
  const QuizApiService({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<QuizSessionStart> createSession(QuizConfigRequest request) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.quizSessions,
      data: QuizConfigRequestDto.fromDomain(request).toJson(),
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid quiz session response.');
    }
    return QuizSessionStartDto.fromJson(data).toDomain();
  }

  Future<QuizAnswerResult> submitAnswer({
    required int sessionId,
    required int wordId,
    required String answer,
    int? listId,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.quizAnswer(sessionId),
      data: {'word_id': wordId, 'user_answer': answer, 'list_id': listId},
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid quiz answer response.');
    }
    return QuizAnswerResultDto.fromJson(data).toDomain();
  }

  Future<void> finishSession(int sessionId) async {
    await _dio.post<Map<String, dynamic>>(ApiEndpoints.quizFinish(sessionId));
  }

  Future<QuizResult> getResult(int sessionId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.quizResult(sessionId),
    );
    return QuizResultDto.fromJson(_dataMap(response, 'quiz result')).toDomain();
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
          .map(WrongWordDto.fromJson)
          .map((dto) => dto.toDomain())
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

typedef QuizRepository = QuizApiService;
