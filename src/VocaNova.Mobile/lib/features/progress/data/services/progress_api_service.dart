import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/progress/data/dtos/progress_analytics_dto.dart';
import 'package:vocanova_mobile/features/progress/data/dtos/progress_summary_dto.dart';
import 'package:vocanova_mobile/features/progress/domain/models/progress_analytics.dart';
import 'package:vocanova_mobile/features/progress/domain/models/progress_summary.dart';

class ProgressApiService {
  const ProgressApiService({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<ProgressSummary> getSummary() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.progressSummary,
    );
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid progress summary response.');
    }
    return ProgressSummaryDto.fromJson(data).toDomain();
  }

  Future<ProgressChart> getChart(String granularity) async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.progressChart,
      queryParameters: {'granularity': granularity},
    );
    return ProgressChartDto.fromJson(
      _dataMap(response, 'progress chart'),
    ).toDomain();
  }

  Future<List<MasteryBreakdown>> getMasteryBreakdown() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.progressMasteryBreakdown,
    );
    return _dataList(response, 'mastery breakdown')
        .map(MasteryBreakdownDto.fromJson)
        .map((dto) => dto.toDomain())
        .toList(growable: false);
  }

  Future<List<WeakestWord>> getWeakestWords() async {
    final response = await _dio.get<Map<String, dynamic>>(
      ApiEndpoints.progressWeakestWords,
      queryParameters: const {'limit': 10},
    );
    return _dataList(response, 'weakest words')
        .map(WeakestWordDto.fromJson)
        .map((dto) => dto.toDomain())
        .toList(growable: false);
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

  Iterable<Map<String, dynamic>> _dataList(
    Response<Map<String, dynamic>> response,
    String name,
  ) {
    final data = response.data?['data'];
    if (data is! List) {
      throw FormatException('Invalid $name response.');
    }
    return data.whereType<Map<String, dynamic>>();
  }
}

typedef ProgressRepository = ProgressApiService;
