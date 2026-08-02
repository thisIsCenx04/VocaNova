import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/core/network/api_endpoints.dart';
import 'package:vocanova_mobile/features/auth/data/auth_repository.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';

void main() {
  test('register sends backend contract and parses wrapped tokens', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.register);
      expect(options.data, {
        'phone': '0901234567',
        'password': 'Password1',
        'display_name': 'Nhut',
        'otp_code': '123456',
      });
      return tokenResponse();
    });

    final tokens = await AuthRepository(dio: dio).register(
      phone: '0901234567',
      password: 'Password1',
      displayName: 'Nhut',
      otpCode: '123456',
    );

    expect(tokens.accessToken, 'access');
    expect(tokens.refreshToken, 'refresh');
  });

  test('register omits learning-profile fields that were left blank', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      final data = options.data as Map<String, dynamic>;
      expect(data.containsKey('date_of_birth'), isFalse);
      expect(data.containsKey('region_id'), isFalse);
      expect(data['occupation_id'], 3);
      expect(data.containsKey('education_level_id'), isFalse);
      return tokenResponse();
    });

    await AuthRepository(dio: dio).register(
      phone: '0901234567',
      password: 'Password1',
      displayName: 'Nhut',
      otpCode: '123456',
      occupationId: 3,
    );
  });

  test('register sends date of birth as an ISO date', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(
        (options.data as Map<String, dynamic>)['date_of_birth'],
        '2001-03-09',
      );
      return tokenResponse();
    });

    await AuthRepository(dio: dio).register(
      phone: '0901234567',
      password: 'Password1',
      displayName: 'Nhut',
      otpCode: '123456',
      dateOfBirth: DateTime(2001, 3, 9),
    );
  });

  test('parses the public learning-profile options catalog', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.learningProfileOptions);
      return jsonResponse({
        'data': {
          'age_ranges': [
            {'id': 1, 'name': 'Dưới 18 tuổi'},
          ],
          'regions': [
            {'id': 2, 'name': 'Miền Bắc'},
          ],
          'occupations': <Map<String, dynamic>>[],
          'education_levels': <Map<String, dynamic>>[],
          'learning_purposes': [
            {'id': 3, 'name': 'Công việc'},
          ],
        },
      });
    });

    final options = await AuthRepository(dio: dio).getLearningProfileOptions();

    expect(options.ageRanges.single.label, 'Dưới 18 tuổi');
    expect(options.regions.single.id, 2);
    expect(options.occupations, isEmpty);
    expect(options.learningPurposes.single.label, 'Công việc');
  });

  test('selectOnboardingTopics sends the topic id list', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.onboardingTopicSelection);
      expect(options.method, 'PUT');
      expect(options.data, {
        'topic_ids': [11, 12],
      });
      return jsonResponse({'data': 2});
    });

    final stored = await AuthRepository(
      dio: dio,
    ).selectOnboardingTopics([11, 12]);

    expect(stored, 2);
  });

  test('login sends backend contract and parses wrapped tokens', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.login);
      expect(options.data, {'phone': '0901234567', 'password': 'Password1'});
      return tokenResponse();
    });

    final tokens = await AuthRepository(
      dio: dio,
    ).login(phone: '0901234567', password: 'Password1');

    expect(tokens.accessToken, 'access');
    expect(tokens.refreshToken, 'refresh');
    expect(tokens.expiresIn, 900);
  });

  test('Google login sends the Google ID token to the backend', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.googleLogin);
      expect(options.data, {'id_token': 'google-id-token'});
      return tokenResponse();
    });

    final tokens = await AuthRepository(
      dio: dio,
    ).googleLogin('google-id-token');

    expect(tokens.accessToken, 'access');
    expect(tokens.refreshToken, 'refresh');
  });

  test('sendOtp and verifyOtp use backend OTP contracts', () async {
    final dio = Dio();
    var requestNumber = 0;
    dio.httpClientAdapter = CallbackAdapter((options) {
      requestNumber++;
      if (requestNumber == 1) {
        expect(options.path, ApiEndpoints.sendOtp);
        expect(options.data, {'phone': '0901234567', 'purpose': 'verify'});
        return jsonResponse({
          'success': true,
          'data': {'expires_in': 300},
          'errors': <String>[],
        });
      }
      expect(options.path, ApiEndpoints.verifyOtp);
      expect(options.data, {'phone': '0901234567', 'otp_code': '123456'});
      return jsonResponse({
        'success': true,
        'data': {'verified': true},
        'errors': <String>[],
      });
    });
    final repository = AuthRepository(dio: dio);

    final expiresIn = await repository.sendOtp(phone: '0901234567');
    final verified = await repository.verifyOtp(
      phone: '0901234567',
      otpCode: '123456',
    );

    expect(expiresIn, 300);
    expect(verified, isTrue);
  });

  test(
    'forgotPassword, OTP verification, and reset use backend contracts',
    () async {
      final dio = Dio();
      var requestNumber = 0;
      dio.httpClientAdapter = CallbackAdapter((options) {
        requestNumber++;
        if (requestNumber == 1) {
          expect(options.path, ApiEndpoints.forgotPassword);
          expect(options.data, {'phone': '0901234567'});
          return jsonResponse({
            'success': true,
            'data': {'expires_in': 300},
            'errors': <String>[],
          });
        }
        if (requestNumber == 2) {
          expect(options.path, ApiEndpoints.verifyResetOtp);
          expect(options.data, {'phone': '0901234567', 'otp_code': '123456'});
          return jsonResponse({
            'success': true,
            'data': {'verified': true},
            'errors': <String>[],
          });
        }
        expect(options.path, ApiEndpoints.resetPassword);
        expect(options.data, {
          'phone': '0901234567',
          'otp_code': '123456',
          'new_password': 'NewPassword1',
        });
        return jsonResponse({
          'success': true,
          'data': true,
          'errors': <String>[],
        });
      });
      final repository = AuthRepository(dio: dio);

      final expiresIn = await repository.forgotPassword('0901234567');
      final verified = await repository.verifyResetOtp(
        phone: '0901234567',
        otpCode: '123456',
      );
      final reset = await repository.resetPassword(
        phone: '0901234567',
        otpCode: '123456',
        newPassword: 'NewPassword1',
      );

      expect(expiresIn, 300);
      expect(verified, isTrue);
      expect(reset, isTrue);
    },
  );

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

  test('updateLearningProfile sends nullable backend contract', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.updateLearningProfile);
      expect(options.data, {
        'age_range_id': 1,
        'region_id': 2,
        'occupation_id': null,
        'education_level_id': 4,
        'learning_purpose_id': 5,
      });
      return profileResponse();
    });

    final user = await AuthRepository(dio: dio).updateLearningProfile(
      const LearningProfile(
        ageRangeId: 1,
        regionId: 2,
        educationLevelId: 4,
        learningPurposeId: 5,
      ),
    );

    expect(user.learningProfile?.regionId, 2);
    expect(user.learningProfile?.occupationId, isNull);
  });

  test(
    'updateProfile sends backend display name and avatar contract',
    () async {
      final dio = Dio();
      dio.httpClientAdapter = CallbackAdapter((options) {
        expect(options.path, ApiEndpoints.updateProfile);
        expect(options.data, {
          'display_name': 'Nhut Updated',
          'avatar_url': null,
        });
        return profileResponse(displayName: 'Nhut Updated');
      });

      final user = await AuthRepository(
        dio: dio,
      ).updateProfile(displayName: 'Nhut Updated');

      expect(user.displayName, 'Nhut Updated');
    },
  );

  test('changePassword sends authenticated password contract', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.changePassword);
      expect(options.method, 'PUT');
      expect(options.data, {
        'current_password': 'OldPassword1',
        'new_password': 'NewPassword1',
      });
      return jsonResponse({
        'success': true,
        'data': true,
        'errors': <String>[],
      });
    });

    final changed = await AuthRepository(dio: dio).changePassword(
      currentPassword: 'OldPassword1',
      newPassword: 'NewPassword1',
    );

    expect(changed, isTrue);
  });

  test('uploadAvatar sends a multipart image contract', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.uploadAvatar);
      expect(options.method, 'POST');
      final form = options.data as FormData;
      expect(form.files.single.key, 'file');
      expect(form.files.single.value.filename, 'cat.png');
      expect(form.files.single.value.contentType?.mimeType, 'image/png');
      return profileResponse(
        avatarUrl: 'https://res.cloudinary.com/demo/cat.png',
      );
    });

    final user = await AuthRepository(
      dio: dio,
    ).uploadAvatar(bytes: Uint8List.fromList([1, 2, 3]), fileName: 'cat.png');

    expect(user.avatarUrl, 'https://res.cloudinary.com/demo/cat.png');
  });

  test('deleteAccount calls the authenticated account endpoint', () async {
    final dio = Dio();
    dio.httpClientAdapter = CallbackAdapter((options) {
      expect(options.path, ApiEndpoints.deleteAccount);
      expect(options.method, 'DELETE');
      return jsonResponse({
        'success': true,
        'data': true,
        'errors': <String>[],
      });
    });

    await AuthRepository(dio: dio).deleteAccount();
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

ResponseBody tokenResponse() {
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
}

ResponseBody profileResponse({String displayName = 'Nhut', String? avatarUrl}) {
  return jsonResponse({
    'success': true,
    'data': {
      'user_id': 7,
      'phone': '0901234567',
      'display_name': displayName,
      'avatar_url': avatarUrl,
      'role': 'user',
      'status': 'active',
      'learning_profile': {
        'age_range_id': 1,
        'region_id': 2,
        'occupation_id': null,
        'education_level_id': 4,
        'learning_purpose_id': 5,
      },
    },
    'errors': <String>[],
  });
}
