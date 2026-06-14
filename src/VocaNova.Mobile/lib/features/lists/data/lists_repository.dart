import 'package:dio/dio.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/lists/domain/user_list.dart';

class ListsRepository {
  const ListsRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<List<UserList>> getLists() async {
    final response = await _dio.get<Map<String, dynamic>>(ApiEndpoints.lists);
    final data = response.data?['data'];
    if (data is! List) throw const FormatException('Invalid lists response.');
    return data
        .whereType<Map<String, dynamic>>()
        .map(UserList.fromJson)
        .toList(growable: false);
  }

  Future<UserList> create(String name) async {
    final response = await _dio.post<Map<String, dynamic>>(
      ApiEndpoints.lists,
      data: {'list_name': name},
    );
    return UserList.fromJson(_dataMap(response));
  }

  Future<UserList> rename({required int listId, required String name}) async {
    final response = await _dio.put<Map<String, dynamic>>(
      ApiEndpoints.list(listId),
      data: {'list_name': name},
    );
    return UserList.fromJson(_dataMap(response));
  }

  Future<void> delete(int listId) async {
    await _dio.delete<Map<String, dynamic>>(ApiEndpoints.list(listId));
  }

  Map<String, dynamic> _dataMap(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'];
    if (data is! Map<String, dynamic>) {
      throw const FormatException('Invalid list response.');
    }
    return data;
  }
}
