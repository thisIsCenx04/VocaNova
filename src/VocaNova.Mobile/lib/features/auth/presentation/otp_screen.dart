import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_form_scaffold.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_request_error.dart';
import 'package:vocanova_mobile/features/auth/presentation/otp_code_fields.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class OtpScreen extends ConsumerStatefulWidget {
  const OtpScreen({
    required this.phone,
    this.purpose = 'verify',
    this.registerPayload,
    super.key,
  });

  final String phone;
  final String purpose;
  final RegisterOtpPayload? registerPayload;

  @override
  ConsumerState<OtpScreen> createState() => _OtpScreenState();
}

class _OtpScreenState extends ConsumerState<OtpScreen> {
  static const maxAttempts = 5;
  static const resendSeconds = 60;

  final _otpKey = GlobalKey<OtpCodeFieldsState>();
  Timer? _timer;
  int _secondsRemaining = resendSeconds;
  int _failedAttempts = 0;
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _startCountdown();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final attemptsLeft = maxAttempts - _failedAttempts;
    final l10n = AppLocalizations.of(context)!;
    return AuthFormScaffold(
      title: l10n.authVerifyEmailTitle,
      subtitle: l10n.authOtpSubtitle(widget.phone),
      showBackButton: true,
      onBack: () {
        if (context.canPop()) {
          context.pop();
        } else {
          context.go(AppRoutes.login);
        }
      },
      form: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          OtpCodeFields(
            key: _otpKey,
            enabled: !_isLoading && attemptsLeft > 0,
            onCompleted: _verify,
          ),
          const SizedBox(height: 32),
          AuthPrimaryButton(
            onPressed: _isLoading || attemptsLeft <= 0
                ? null
                : () {
                    final code = _otpKey.currentState?.code ?? '';
                    if (code.length == OtpCodeFieldsState.length) {
                      _verify(code);
                    }
                  },
            child: _isLoading
                ? authLoadingIndicator()
                : Text(l10n.authVerifyButton),
          ),
          const SizedBox(height: 28),
          AuthHelperText(
            attemptsLeft > 0
                ? l10n.authAttemptsRemaining(attemptsLeft)
                : l10n.authOtpMaxAttemptsReached,
            widgetKey: const Key('otp-attempt-message'),
          ),
          const SizedBox(height: 12),
          AuthInlineLink(
            text: l10n.authDidntReceiveCodePrefix,
            actionText: _secondsRemaining == 0
                ? l10n.authResendCode
                : l10n.authResendInSeconds(_secondsRemaining),
            onPressed: !_isLoading && _secondsRemaining == 0 ? _resend : null,
          ),
        ],
      ),
    );
  }

  Future<void> _verify(String code) async {
    if (_isLoading || _failedAttempts >= maxAttempts) {
      return;
    }
    setState(() => _isLoading = true);
    try {
      final verified = widget.purpose == 'register'
          ? await _completeRegister(code)
          : await ref
                .read(authRepositoryProvider)
                .verifyOtp(phone: widget.phone, otpCode: code);
      if (!mounted) {
        return;
      }
      if (verified) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              AppLocalizations.of(context)!.authOtpVerifiedSuccessMessage,
            ),
          ),
        );
        if (widget.purpose == 'register') {
          context.go(AppRoutes.onboarding);
        } else if (context.canPop()) {
          context.pop(true);
        } else {
          context.go(AppRoutes.login);
        }
      }
    } catch (error) {
      if (!mounted) {
        return;
      }
      setState(() => _failedAttempts++);
      _otpKey.currentState?.clear();
      _showError(
        _failedAttempts >= maxAttempts
            ? AppLocalizations.of(context)!.authOtpMaxAttemptsReached
            : authRequestError(error),
      );
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<bool> _completeRegister(String code) async {
    final payload = widget.registerPayload;
    if (payload == null) {
      throw FormatException(
        AppLocalizations.of(context)!.authRegistrationDataMissing,
      );
    }

    return ref
        .read(authProvider.notifier)
        .register(
          phone: widget.phone,
          password: payload.password,
          displayName: payload.displayName,
          otpCode: code,
          dateOfBirth: payload.dateOfBirth,
          regionId: payload.regionId,
          occupationId: payload.occupationId,
          educationLevelId: payload.educationLevelId,
        );
  }

  Future<void> _resend() async {
    setState(() => _isLoading = true);
    try {
      await ref
          .read(authRepositoryProvider)
          .sendOtp(phone: widget.phone, purpose: widget.purpose);
      if (!mounted) {
        return;
      }
      setState(() => _failedAttempts = 0);
      _otpKey.currentState?.clear();
      _startCountdown();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(AppLocalizations.of(context)!.authOtpResentMessage)),
      );
    } catch (error) {
      if (mounted) {
        _showError(authRequestError(error));
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  void _startCountdown() {
    _timer?.cancel();
    setState(() => _secondsRemaining = resendSeconds);
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted || _secondsRemaining <= 1) {
        timer.cancel();
        if (mounted) {
          setState(() => _secondsRemaining = 0);
        }
        return;
      }
      setState(() => _secondsRemaining--);
    });
  }

  void _showError(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}

class RegisterOtpPayload {
  const RegisterOtpPayload({
    required this.displayName,
    required this.password,
    this.dateOfBirth,
    this.regionId,
    this.occupationId,
    this.educationLevelId,
  });

  final String displayName;
  final String password;

  /// Optional learning-profile answers collected on the sign-up form; they are carried
  /// through OTP verification so the profile can be created together with the account.
  final DateTime? dateOfBirth;
  final int? regionId;
  final int? occupationId;
  final int? educationLevelId;
}
