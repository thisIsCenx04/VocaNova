import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';
import 'package:vocanova_mobile/features/settings/presentation/profile_screen.dart';

void main() {
  test('maskPhone hides the middle digits', () {
    expect(maskPhone('0901234567'), '090*****67');
    expect(maskPhone(null), 'Chưa cập nhật');
  });

  testWidgets('shows profile and updates display name', (tester) async {
    final notifier = FakeAuthNotifier();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [authProvider.overrideWith(() => notifier)],
        child: const MaterialApp(home: ProfileScreen()),
      ),
    );
    await tester.pump();

    expect(find.text('090*****67'), findsOneWidget);
    expect(find.byKey(const Key('profile-avatar')), findsOneWidget);
    expect(find.byKey(const Key('edit-learning-profile')), findsOneWidget);
    expect(find.byKey(const Key('logout-button')), findsOneWidget);

    await tester.tap(find.text('Nhut'));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('display-name-field')),
      'Nhut Updated',
    );
    await tester.tap(find.byKey(const Key('save-display-name')));
    await tester.pumpAndSettle();

    expect(find.text('Nhut Updated'), findsOneWidget);
    expect(notifier.updatedName, 'Nhut Updated');
  });
}

class FakeAuthNotifier extends AuthNotifier {
  String? updatedName;

  @override
  AuthState build() {
    return const AuthState(
      status: AuthStatus.authenticated,
      user: UserProfile(
        userId: 7,
        phone: '0901234567',
        displayName: 'Nhut',
        role: 'user',
        status: 'active',
      ),
    );
  }

  @override
  Future<bool> updateProfile({
    required String displayName,
    String? avatarUrl,
  }) async {
    updatedName = displayName;
    state = AuthState(
      status: AuthStatus.authenticated,
      user: UserProfile(
        userId: 7,
        phone: '0901234567',
        displayName: displayName,
        role: 'user',
        status: 'active',
      ),
    );
    return true;
  }
}
