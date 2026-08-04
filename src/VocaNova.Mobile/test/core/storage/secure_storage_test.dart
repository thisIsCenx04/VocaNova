import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/storage/secure_storage.dart';

void main() {
  late MockFlutterSecureStorage flutterStorage;
  late SecureStorage storage;

  setUp(() {
    flutterStorage = MockFlutterSecureStorage();
    storage = SecureStorage(secureStorage: flutterStorage);
  });

  test('saves and reads access and refresh tokens', () async {
    when(
      () => flutterStorage.write(
        key: SecureStorage.accessTokenKey,
        value: 'access',
      ),
    ).thenAnswer((_) async {});
    when(
      () => flutterStorage.write(
        key: SecureStorage.refreshTokenKey,
        value: 'refresh',
      ),
    ).thenAnswer((_) async {});
    when(
      () => flutterStorage.read(key: SecureStorage.accessTokenKey),
    ).thenAnswer((_) async => 'access');
    when(
      () => flutterStorage.read(key: SecureStorage.refreshTokenKey),
    ).thenAnswer((_) async => 'refresh');

    await storage.saveAccessToken('access');
    await storage.saveRefreshToken('refresh');

    expect(await storage.getAccessToken(), 'access');
    expect(await storage.getRefreshToken(), 'refresh');
  });

  test('clearTokens deletes both token keys', () async {
    when(
      () => flutterStorage.delete(key: SecureStorage.accessTokenKey),
    ).thenAnswer((_) async {});
    when(
      () => flutterStorage.delete(key: SecureStorage.refreshTokenKey),
    ).thenAnswer((_) async {});

    await storage.clearTokens();

    verify(
      () => flutterStorage.delete(key: SecureStorage.accessTokenKey),
    ).called(1);
    verify(
      () => flutterStorage.delete(key: SecureStorage.refreshTokenKey),
    ).called(1);
  });
}

class MockFlutterSecureStorage extends Mock implements FlutterSecureStorage {}
