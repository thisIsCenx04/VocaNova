import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';

class SecureStorage implements TokenStorage {
  SecureStorage({
    FlutterSecureStorage secureStorage = const FlutterSecureStorage(),
  }) : _secureStorage = secureStorage;

  static final SecureStorage instance = SecureStorage();

  static const accessTokenKey = 'access_token';
  static const refreshTokenKey = 'refresh_token';

  final FlutterSecureStorage _secureStorage;

  @override
  Future<String?> getAccessToken() => _secureStorage.read(key: accessTokenKey);

  @override
  Future<String?> getRefreshToken() =>
      _secureStorage.read(key: refreshTokenKey);

  Future<void> saveAccessToken(String token) =>
      _secureStorage.write(key: accessTokenKey, value: token);

  Future<void> saveRefreshToken(String token) =>
      _secureStorage.write(key: refreshTokenKey, value: token);

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    await Future.wait([
      saveAccessToken(accessToken),
      saveRefreshToken(refreshToken),
    ]);
  }

  @override
  Future<void> clearTokens() async {
    await Future.wait([
      _secureStorage.delete(key: accessTokenKey),
      _secureStorage.delete(key: refreshTokenKey),
    ]);
  }
}
