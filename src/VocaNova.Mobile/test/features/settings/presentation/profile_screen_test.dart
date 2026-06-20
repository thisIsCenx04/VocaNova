import 'dart:typed_data';

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
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final notifier = FakeAuthNotifier();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [authProvider.overrideWith(() => notifier)],
        child: MaterialApp(
          theme: ThemeData.dark(),
          home: const ProfileScreen(),
        ),
      ),
    );
    await tester.pump();
    expect(tester.takeException(), isNull, reason: 'initial profile layout');

    expect(find.text('090*****67'), findsOneWidget);
    expect(find.byKey(const Key('profile-avatar')), findsOneWidget);
    expect(find.byKey(const Key('edit-learning-profile')), findsOneWidget);

    await tester.tap(find.text('Nhut'));
    await tester.pumpAndSettle();
    expect(tester.takeException(), isNull, reason: 'edit profile sheet layout');
    final fieldContext = tester.element(
      find.byKey(const Key('display-name-field')),
    );
    final sheetTheme = Theme.of(fieldContext);
    expect(sheetTheme.brightness, Brightness.light);
    expect(sheetTheme.inputDecorationTheme.fillColor, const Color(0xFFFAFAFA));
    expect(
      (sheetTheme.inputDecorationTheme.enabledBorder! as OutlineInputBorder)
          .borderRadius,
      BorderRadius.circular(14),
    );
    await tester.enterText(
      find.byKey(const Key('display-name-field')),
      'Nhut Updated',
    );
    await tester.tap(find.byKey(const Key('suggested-avatar-Purple cat')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('save-display-name')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('save-display-name')));
    await tester.pumpAndSettle();

    expect(find.text('Nhut Updated'), findsOneWidget);
    expect(notifier.updatedName, 'Nhut Updated');
    expect(notifier.uploadedAvatarName, 'cat.png');
    expect(notifier.uploadedAvatarBytes, isNotEmpty);

    await tester.ensureVisible(find.byKey(const Key('edit-profile-button')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('edit-profile-button')));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.byKey(const Key('change-password-button')));
    await tester.tap(find.byKey(const Key('change-password-button')));
    await tester.pumpAndSettle();
    await tester.enterText(
      find.byKey(const Key('current-password-field')),
      'OldPassword1',
    );
    await tester.enterText(
      find.byKey(const Key('new-password-field')),
      'NewPassword1',
    );
    await tester.enterText(
      find.byKey(const Key('confirm-new-password-field')),
      'NewPassword1',
    );
    await tester.tap(find.byKey(const Key('save-password-button')));
    await tester.pumpAndSettle();

    expect(notifier.currentPassword, 'OldPassword1');
    expect(notifier.newPassword, 'NewPassword1');

    await tester.ensureVisible(find.byKey(const Key('logout-button')));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('logout-button')), findsOneWidget);
  });
}

class FakeAuthNotifier extends AuthNotifier {
  String? updatedName;
  String? currentPassword;
  String? newPassword;
  String? uploadedAvatarName;
  Uint8List? uploadedAvatarBytes;

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

  @override
  Future<bool> uploadAvatar({
    required Uint8List bytes,
    required String fileName,
  }) async {
    uploadedAvatarBytes = bytes;
    uploadedAvatarName = fileName;
    state = AuthState(
      status: AuthStatus.authenticated,
      user: UserProfile(
        userId: 7,
        phone: '0901234567',
        displayName: state.user!.displayName,
        avatarUrl: 'https://res.cloudinary.com/demo/avatar.png',
        role: 'user',
        status: 'active',
      ),
    );
    return true;
  }

  @override
  Future<bool> changePassword({
    required String currentPassword,
    required String newPassword,
  }) async {
    this.currentPassword = currentPassword;
    this.newPassword = newPassword;
    return true;
  }
}
