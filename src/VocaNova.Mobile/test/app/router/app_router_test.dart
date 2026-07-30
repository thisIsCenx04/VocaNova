import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:vocanova_mobile/app/router/app_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/home/application/home_topics_provider.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_notifier.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

import '../../support/home_test_overrides.dart';

void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  testWidgets('root redirects unauthenticated user to login', (tester) async {
    final router = createRouter();

    await pumpRouter(tester, router);

    expect(router.state.matchedLocation, AppRoutes.login);
    expect(find.text('Sign in'), findsWidgets);
  });

  testWidgets('root redirects authenticated user to home with bottom nav', (
    tester,
  ) async {
    final router = createRouter(accessToken: 'access-token');

    await pumpRouter(tester, router);

    expect(router.state.matchedLocation, AppRoutes.home);
    expect(find.text('Hi, An'), findsOneWidget);
    expect(find.text('Home'), findsOneWidget);
    expect(find.text('Search'), findsOneWidget);
    expect(find.text('Lists'), findsOneWidget);
    expect(find.text('Practice'), findsOneWidget);
    expect(find.text('Profile'), findsOneWidget);
  });

  testWidgets('auth guard redirects protected route to login', (tester) async {
    final router = createRouter(initialLocation: AppRoutes.settings);

    await pumpRouter(tester, router);

    expect(router.state.matchedLocation, AppRoutes.login);
  });

  testWidgets('bottom navigation changes shell branch', (tester) async {
    final router = createRouter(accessToken: 'access-token');
    await pumpRouter(tester, router);

    await tester.tap(find.text('Search'));
    await tester.pumpAndSettle();

    expect(router.state.matchedLocation, AppRoutes.search);
    expect(find.text('Search'), findsWidgets);
  });

  testWidgets('settings stays inside the profile shell', (tester) async {
    final router = createRouter(
      accessToken: 'access-token',
      initialLocation: AppRoutes.settings,
    );

    await pumpRouter(tester, router);

    expect(router.state.matchedLocation, AppRoutes.settings);
    expect(find.text('Settings'), findsOneWidget);
    expect(find.text('Home'), findsOneWidget);
    expect(find.text('Profile'), findsWidgets);
  });

  testWidgets('authenticated user can open dynamic detail routes', (
    tester,
  ) async {
    final router = createRouter(
      accessToken: 'access-token',
      initialLocation: AppRoutes.wordDetail('42'),
    );

    await pumpRouter(tester, router, settle: false);

    expect(router.state.matchedLocation, AppRoutes.wordDetail('42'));
    expect(find.byKey(const Key('word-detail-screen')), findsOneWidget);
    expect(find.text('Home'), findsOneWidget);
    expect(find.text('Search'), findsOneWidget);
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

Future<void> pumpRouter(
  WidgetTester tester,
  GoRouter router, {
  bool settle = true,
}) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        notificationsUnreadCountProvider.overrideWith((ref) async => 0),
        homeTopicsProvider.overrideWith((ref) async => const []),
        authProvider.overrideWith(FakeAuthNotifier.new),
        progressOverviewProvider.overrideWith(
          FakeProgressOverviewNotifier.new,
        ),
        listsProvider.overrideWith(FakeListsNotifier.new),
        wrongWordsProvider.overrideWith(FakeWrongWordsNotifier.new),
      ],
      child: MaterialApp.router(
        routerConfig: router,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
      ),
    ),
  );
  if (settle) {
    await tester.pumpAndSettle();
  } else {
    await tester.pump();
  }
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
