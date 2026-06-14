import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/auth/data/auth_repository.dart';

void main() {
  test('login sends backend contract and parses wrapped tokens', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.login);
      expect(options.data, {'phone': '0901234567', 'password': 'Password1'});
      return jsonResponse({
        'success': true,
        'data': {
          'access_token': 'access',
          'refresh_token': 'refresh',
          'expires_in': 900,
          'token_type': 'Bearer',
        },
        'errors': <String>[],
      });
    });

    final tokens = await AuthRepository(
      dio: dio,
    ).login(phone: '0901234567', password: 'Password1');

    expect(tokens.accessToken, 'access');
    expect(tokens.refreshToken, 'refresh');
    expect(tokens.expiresIn, 900);
  });

  test('getCurrentUser parses backend profile contract', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter(
      (_) => jsonResponse({
        'success': true,
        'data': {
          'user_id': 7,
          'phone': '0901234567',
          'display_name': 'Nhut',
          'avatar_url': null,
          'role': 'user',
          'status': 'active',
          'learning_profile': {
            'age_range_id': 1,
            'region_id': 2,
            'occupation_id': 3,
            'education_level_id': 4,
            'learning_purpose_id': 5,
          },
        },
        'errors': <String>[],
      }),
    );

    final user = await AuthRepository(dio: dio).getCurrentUser();

    expect(user.userId, 7);
    expect(user.displayName, 'Nhut');
    expect(user.learningProfile?.learningPurposeId, 5);
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

ResponseBody jsonResponse(Object body) {
  return ResponseBody.fromString(
    jsonEncode(body),
    200,
    headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    },
  );
}
