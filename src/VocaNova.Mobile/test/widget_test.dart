import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/app/app.dart';
import 'package:vocanova_mobile/app/router/app_router.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';

void main() {
  testWidgets('VocaNova app renders the themed home screen', (tester) async {
    final router = AppRouter.create(
      tokenStorage: TestTokenStorage(accessToken: 'token'),
    ).router;

    await tester.pumpWidget(ProviderScope(child: VocaNovaApp(router: router)));
    await tester.pumpAndSettle();

    expect(find.text('VocaNova'), findsOneWidget);
    expect(find.text('Học từ vựng mỗi ngày'), findsOneWidget);
    expect(find.text('Khám phá VocaNova'), findsOneWidget);
  });
}

class TestTokenStorage implements TokenStorage {
  TestTokenStorage({this.accessToken});

  String? accessToken;

  @override
  Future<void> clearTokens() async => accessToken = null;

  @override
  Future<String?> getAccessToken() async => accessToken;

  @override
  Future<String?> getRefreshToken() async => null;

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    this.accessToken = accessToken;
  }
}
