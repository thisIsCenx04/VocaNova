import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';

void main() {
  testWidgets('root redirects unauthenticated user to login', (tester) async {
    final router = createRouter();

    await pumpRouter(tester, router);

    expect(router.state.matchedLocation, AppRoutes.login);
    expect(find.text('Đăng nhập'), findsWidgets);
  });

  testWidgets('root redirects authenticated user to home with bottom nav', (
    tester,
  ) async {
    final router = createRouter(accessToken: 'access-token');

    await pumpRouter(tester, router);

    expect(router.state.matchedLocation, AppRoutes.home);
    expect(find.text('Học từ vựng mỗi ngày'), findsOneWidget);
    expect(find.byType(NavigationBar), findsOneWidget);
    expect(find.text('Trang chủ'), findsOneWidget);
    expect(find.text('Tra từ'), findsOneWidget);
    expect(find.text('Danh sách'), findsOneWidget);
    expect(find.text('Tiến độ'), findsOneWidget);
  });

  testWidgets('auth guard redirects protected route to login', (tester) async {
    final router = createRouter(initialLocation: AppRoutes.settings);

    await pumpRouter(tester, router);

    expect(router.state.matchedLocation, AppRoutes.login);
  });

  testWidgets('bottom navigation changes shell branch', (tester) async {
    final router = createRouter(accessToken: 'access-token');
    await pumpRouter(tester, router);

    await tester.tap(find.text('Tra từ'));
    await tester.pumpAndSettle();

    expect(router.state.matchedLocation, AppRoutes.search);
    expect(find.text('Tra từ'), findsWidgets);
  });

  testWidgets('authenticated user can open dynamic detail routes', (
    tester,
  ) async {
    final router = createRouter(
      accessToken: 'access-token',
      initialLocation: AppRoutes.wordDetail('42'),
    );

    await pumpRouter(tester, router);

    expect(router.state.matchedLocation, AppRoutes.wordDetail('42'));
    expect(find.text('Chi tiết từ #42'), findsWidgets);
  });
}

GoRouter createRouter({String? accessToken, String? initialLocation}) {
  final router = AppRouter.create(
    tokenStorage: TestTokenStorage(accessToken: accessToken),
  ).router;
  if (initialLocation != null) {
    router.go(initialLocation);
  }
  return router;
}

Future<void> pumpRouter(WidgetTester tester, GoRouter router) async {
  await tester.pumpWidget(MaterialApp.router(routerConfig: router));
  await tester.pumpAndSettle();
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
