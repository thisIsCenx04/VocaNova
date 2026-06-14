import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/data/auth_repository.dart';
import 'package:vocanova_mobile/features/auth/presentation/login_screen.dart';
import 'package:vocanova_mobile/features/auth/presentation/register_screen.dart';

void main() {
  late MockAuthRepository repository;

  setUp(() {
    repository = MockAuthRepository();
  });

  testWidgets('login shows Vietnamese inline validation errors', (
    tester,
  ) async {
    await pumpAuthScreen(tester, const LoginScreen(), repository);

    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pump();

    expect(find.text('Vui lòng nhập số điện thoại.'), findsOneWidget);
    expect(find.text('Vui lòng nhập mật khẩu.'), findsOneWidget);
  });

  testWidgets('register cross-validates password confirmation', (tester) async {
    await pumpAuthScreen(tester, const RegisterScreen(), repository);

    await tester.enterText(
      find.byKey(const Key('register-display-name')),
      'Nhut',
    );
    await tester.enterText(
      find.byKey(const Key('register-phone')),
      '0901234567',
    );
    await tester.enterText(
      find.byKey(const Key('register-password')),
      'Password1',
    );
    await tester.enterText(
      find.byKey(const Key('register-confirm-password')),
      'Password2',
    );
    await tester.tap(find.byKey(const Key('register-submit')));
    await tester.pump();

    expect(find.text('Mật khẩu xác nhận không khớp.'), findsOneWidget);
    verifyNever(
      () => repository.register(
        phone: any(named: 'phone'),
        password: any(named: 'password'),
        displayName: any(named: 'displayName'),
      ),
    );
  });

  testWidgets('login disables submit and shows progress while loading', (
    tester,
  ) async {
    final pendingLogin = Completer<AuthTokens>();
    when(
      () => repository.login(phone: '0901234567', password: 'Password1'),
    ).thenAnswer((_) => pendingLogin.future);
    await pumpAuthScreen(tester, const LoginScreen(), repository);

    await tester.enterText(find.byKey(const Key('login-phone')), '0901234567');
    await tester.enterText(
      find.byKey(const Key('login-password')),
      'Password1',
    );
    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pump();

    final button = tester.widget<ElevatedButton>(
      find.byKey(const Key('login-submit')),
    );
    expect(button.onPressed, isNull);
    expect(find.byType(CircularProgressIndicator), findsOneWidget);

    pendingLogin.completeError(const AppException('Máy chủ đang gặp sự cố.'));
    await tester.pumpAndSettle();
  });

  testWidgets('login shows snackbar when API fails', (tester) async {
    when(
      () => repository.login(phone: '0901234567', password: 'wrong'),
    ).thenThrow(const AppException('Số điện thoại hoặc mật khẩu không đúng.'));
    await pumpAuthScreen(tester, const LoginScreen(), repository);

    await tester.enterText(find.byKey(const Key('login-phone')), '0901234567');
    await tester.enterText(find.byKey(const Key('login-password')), 'wrong');
    await tester.tap(find.byKey(const Key('login-submit')));
    await tester.pumpAndSettle();

    expect(
      find.text('Số điện thoại hoặc mật khẩu không đúng.'),
      findsOneWidget,
    );
  });
}

Future<void> pumpAuthScreen(
  WidgetTester tester,
  Widget screen,
  AuthRepository repository,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
      child: MaterialApp(home: screen),
    ),
  );
  await tester.pump();
}

class MockAuthRepository extends Mock implements AuthRepository {}
