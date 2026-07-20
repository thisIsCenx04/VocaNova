import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/app/app.dart';
import 'package:vocanova_mobile/app/router/app_router.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/home/application/home_topics_provider.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/notifications/application/notifications_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_notifier.dart';

import 'support/home_test_overrides.dart';

void main() {
  testWidgets('VocaNova app renders the themed home screen', (tester) async {
    final router = AppRouter.create(
      tokenStorage: TestTokenStorage(accessToken: 'token'),
    ).router;

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
        child: VocaNovaApp(router: router),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('VocaNova'), findsOneWidget);
    expect(find.text('Hi, An'), findsOneWidget);
    expect(find.text('DAILY GOAL'), findsOneWidget);
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
