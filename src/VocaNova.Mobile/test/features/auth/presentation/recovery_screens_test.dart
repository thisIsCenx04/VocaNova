import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mocktail/mocktail.dart';
import 'package:vocanova_mobile/core/network/app_exception.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/data/auth_repository.dart';
import 'package:vocanova_mobile/features/auth/presentation/forgot_password_screen.dart';
import 'package:vocanova_mobile/features/auth/presentation/otp_screen.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

void main() {
  late MockAuthRepository repository;

  setUp(() {
    repository = MockAuthRepository();
  });

  testWidgets('labels OTP verification as phone-number verification', (
    tester,
  ) async {
    await pumpScreen(tester, const OtpScreen(phone: '0901234567'), repository);

    expect(find.text('Verify your phone number'), findsOneWidget);
    expect(find.text('Verify your email'), findsNothing);
  });

  testWidgets('OTP auto-submits after entering all six digits', (tester) async {
    when(
      () => repository.verifyOtp(phone: '0901234567', otpCode: '123456'),
    ).thenThrow(const AppException('Mã OTP không đúng.'));
    await pumpScreen(tester, const OtpScreen(phone: '0901234567'), repository);

    await enterOtp(tester, '123456');
    await tester.pumpAndSettle();

    verify(
      () => repository.verifyOtp(phone: '0901234567', otpCode: '123456'),
    ).called(1);
    expect(find.text('Mã OTP không đúng.'), findsOneWidget);
  });

  testWidgets('OTP blocks input and shows message after five failures', (
    tester,
  ) async {
    when(
      () => repository.verifyOtp(
        phone: '0901234567',
        otpCode: any(named: 'otpCode'),
      ),
    ).thenThrow(const AppException('Mã OTP không đúng.'));
    await pumpScreen(tester, const OtpScreen(phone: '0901234567'), repository);

    for (var attempt = 0; attempt < 5; attempt++) {
      await enterOtp(tester, '123456');
      await tester.pumpAndSettle();
    }

    expect(
      find.text(
        "You've entered the wrong OTP more than 5 times. Please resend the code.",
      ),
      findsWidgets,
    );
    final firstDigit = tester.widget<TextField>(
      find.byKey(const Key('otp-digit-0')),
    );
    expect(firstDigit.enabled, isFalse);
  });

  testWidgets('forgot password completes phone OTP password flow', (
    tester,
  ) async {
    when(
      () => repository.forgotPassword('0901234567'),
    ).thenAnswer((_) async => 300);
    when(
      () => repository.verifyResetOtp(phone: '0901234567', otpCode: '123456'),
    ).thenAnswer((_) async => true);
    when(
      () => repository.resetPassword(
        phone: '0901234567',
        otpCode: '123456',
        newPassword: 'NewPassword1',
      ),
    ).thenAnswer((_) async => true);
    await pumpForgotRouter(tester, repository);

    await tester.enterText(find.byKey(const Key('forgot-phone')), '0901234567');
    await tester.tap(find.byKey(const Key('forgot-send-otp')));
    await tester.pumpAndSettle();
    expect(find.text('Step 2/3'), findsOneWidget);

    await enterOtp(tester, '123456');
    await tester.pumpAndSettle();
    expect(find.text('Step 3/3'), findsOneWidget);

    await tester.enterText(
      find.byKey(const Key('forgot-new-password')),
      'NewPassword1',
    );
    await tester.enterText(
      find.byKey(const Key('forgot-confirm-password')),
      'NewPassword1',
    );
    await tester.tap(find.byKey(const Key('forgot-reset-password')));
    await tester.pumpAndSettle();

    verify(
      () => repository.resetPassword(
        phone: '0901234567',
        otpCode: '123456',
        newPassword: 'NewPassword1',
      ),
    ).called(1);
    expect(find.text('Login destination'), findsOneWidget);
  });

  testWidgets('forgot password stays on phone step when account is absent', (
    tester,
  ) async {
    when(
      () => repository.forgotPassword('0901234567'),
    ).thenThrow(const AppException('User not found.', statusCode: 404));
    await pumpScreen(tester, const ForgotPasswordScreen(), repository);

    await tester.enterText(find.byKey(const Key('forgot-phone')), '0901234567');
    await tester.tap(find.byKey(const Key('forgot-send-otp')));
    await tester.pumpAndSettle();

    expect(find.text('Step 1/3'), findsOneWidget);
    expect(find.byKey(const Key('forgot-phone')), findsOneWidget);
    expect(find.byKey(const Key('otp-digit-0')), findsNothing);
  });

  testWidgets('forgot password rejects OTP before showing password fields', (
    tester,
  ) async {
    when(
      () => repository.forgotPassword('0901234567'),
    ).thenAnswer((_) async => 300);
    when(
      () => repository.verifyResetOtp(phone: '0901234567', otpCode: '999999'),
    ).thenThrow(const AppException('Invalid OTP.', statusCode: 401));
    await pumpScreen(tester, const ForgotPasswordScreen(), repository);

    await tester.enterText(find.byKey(const Key('forgot-phone')), '0901234567');
    await tester.tap(find.byKey(const Key('forgot-send-otp')));
    await tester.pumpAndSettle();
    await enterOtp(tester, '999999');
    await tester.pumpAndSettle();

    expect(find.text('Step 2/3'), findsOneWidget);
    expect(find.byKey(const Key('forgot-new-password')), findsNothing);
    verify(
      () => repository.verifyResetOtp(phone: '0901234567', otpCode: '999999'),
    ).called(1);
  });

  testWidgets('forgot password enables resend after 60 seconds', (
    tester,
  ) async {
    when(
      () => repository.forgotPassword('0901234567'),
    ).thenAnswer((_) async => 300);
    await pumpScreen(tester, const ForgotPasswordScreen(), repository);

    await tester.enterText(find.byKey(const Key('forgot-phone')), '0901234567');
    await tester.tap(find.byKey(const Key('forgot-send-otp')));
    await tester.pumpAndSettle();
    await tester.pump(const Duration(seconds: 60));

    final resend = tester.widget<TextButton>(
      find.byKey(const Key('forgot-resend-otp')),
    );
    expect(resend.onPressed, isNotNull);
    await tester.tap(find.byKey(const Key('forgot-resend-otp')));
    await tester.pump();

    verify(() => repository.forgotPassword('0901234567')).called(2);
  });
}

Future<void> enterOtp(WidgetTester tester, String code) async {
  for (var index = 0; index < code.length; index++) {
    await tester.enterText(find.byKey(Key('otp-digit-$index')), code[index]);
  }
  await tester.pump();
}

Future<void> pumpScreen(
  WidgetTester tester,
  Widget screen,
  AuthRepository repository,
) async {
  await tester.pumpWidget(
    ProviderScope(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
      child: MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: screen,
      ),
    ),
  );
  await tester.pump();
}

Future<void> pumpForgotRouter(
  WidgetTester tester,
  AuthRepository repository,
) async {
  final router = GoRouter(
    initialLocation: '/forgot-password',
    routes: [
      GoRoute(
        path: '/forgot-password',
        builder: (_, _) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: '/login',
        builder: (_, _) => const Scaffold(body: Text('Login destination')),
      ),
    ],
  );
  await tester.pumpWidget(
    ProviderScope(
      overrides: [authRepositoryProvider.overrideWithValue(repository)],
      child: MaterialApp.router(
        routerConfig: router,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
      ),
    ),
  );
  await tester.pumpAndSettle();
}

class MockAuthRepository extends Mock implements AuthRepository {}
