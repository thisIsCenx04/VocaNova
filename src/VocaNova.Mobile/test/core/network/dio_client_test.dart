import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/core/network/dio_client.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';

void main() {
  group('DioClient', () {
    test('configures API base URL and timeouts', () {
      final client = DioClient.create(
        baseUrl: 'https://api.vocanova.test',
        tokenStorage: MemoryTokenStorage(),
      );

      expect(client.dio.options.baseUrl, 'https://api.vocanova.test');
      expect(client.dio.options.connectTimeout, const Duration(seconds: 10));
      expect(client.dio.options.receiveTimeout, const Duration(seconds: 30));
    });

    test('refreshes token and retries original request after 401', () async {
      final tokenStorage = MemoryTokenStorage(
        accessToken: 'expired-access-token',
        refreshToken: 'old-refresh-token',
      );
      final requestHeaders = <String?>[];
      var requestCount = 0;

      final mainAdapter = CallbackAdapter((options) {
        requestCount++;
        requestHeaders.add(options.headers['Authorization'] as String?);
        if (requestCount == 1) {
          return jsonResponse(
            401,
            body: {
              'success': false,
              'data': null,
              'message': 'Unauthorized.',
              'errors': ['Unauthorized.'],
            },
          );
        }
        return jsonResponse(200, body: {'success': true, 'data': 'ok'});
      });
      final refreshAdapter = CallbackAdapter((options) {
        expect(options.path, ApiEndpoints.refreshToken);
        expect(options.data, {'refresh_token': 'old-refresh-token'});
        return jsonResponse(
          200,
          body: {
            'success': true,
            'data': {
              'access_token': 'new-access-token',
              'refresh_token': 'new-refresh-token',
              'expires_in': 900,
              'token_type': 'Bearer',
            },
            'errors': <String>[],
          },
        );
      });
      final client = DioClient.create(
        baseUrl: 'https://api.vocanova.test',
        tokenStorage: tokenStorage,
        adapter: mainAdapter,
        refreshAdapter: refreshAdapter,
      );

      final response = await client.dio.get<Map<String, dynamic>>('/protected');

      expect(response.statusCode, 200);
      expect(requestCount, 2);
      expect(requestHeaders, [
        'Bearer expired-access-token',
        'Bearer new-access-token',
      ]);
      expect(tokenStorage.accessToken, 'new-access-token');
      expect(tokenStorage.refreshToken, 'new-refresh-token');
    });

    test('clears tokens and logs out when refresh fails', () async {
      final tokenStorage = MemoryTokenStorage(
        accessToken: 'expired-access-token',
        refreshToken: 'invalid-refresh-token',
      );
      var didLogout = false;
      final client = DioClient.create(
        baseUrl: 'https://api.vocanova.test',
        tokenStorage: tokenStorage,
        adapter: CallbackAdapter(
          (_) => jsonResponse(
            401,
            body: {
              'errors': ['Unauthorized.'],
            },
          ),
        ),
        refreshAdapter: CallbackAdapter(
          (_) => jsonResponse(
            401,
            body: {
              'errors': ['Invalid refresh token.'],
            },
          ),
        ),
        onLogout: () => didLogout = true,
      );

      await expectLater(
        client.dio.get<Map<String, dynamic>>('/protected'),
        throwsA(isA<DioException>()),
      );

      expect(tokenStorage.accessToken, isNull);
      expect(tokenStorage.refreshToken, isNull);
      expect(didLogout, isTrue);
    });

    test('converts backend errors into Vietnamese AppException', () async {
      final client = DioClient.create(
        baseUrl: 'https://api.vocanova.test',
        tokenStorage: MemoryTokenStorage(),
        adapter: CallbackAdapter(
          (_) => jsonResponse(
            400,
            body: {
              'success': false,
              'data': null,
              'message': 'Invalid phone or password.',
              'errors': ['Invalid phone or password.'],
            },
          ),
        ),
      );

      try {
        await client.dio.post<Map<String, dynamic>>(ApiEndpoints.login);
        fail('Expected DioException.');
      } on DioException catch (error) {
        expect(error.error, isA<AppException>());
        final appException = error.error! as AppException;
        expect(appException.message, 'Số điện thoại hoặc mật khẩu không đúng.');
        expect(appException.statusCode, 400);
        expect(appException.errors, ['Invalid phone or password.']);
      }
    });
  });
}

class MemoryTokenStorage implements TokenStorage {
  MemoryTokenStorage({this.accessToken, this.refreshToken});

  String? accessToken;
  String? refreshToken;

  @override
  Future<void> clearTokens() async {
    accessToken = null;
    refreshToken = null;
  }

  @override
  Future<String?> getAccessToken() async => accessToken;

  @override
  Future<String?> getRefreshToken() async => refreshToken;

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    this.accessToken = accessToken;
    this.refreshToken = refreshToken;
  }
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

ResponseBody jsonResponse(int statusCode, {required Object body}) {
  return ResponseBody.fromString(
    jsonEncode(body),
    statusCode,
    headers: {
      Headers.contentTypeHeader: [Headers.jsonContentType],
    },
  );
}
