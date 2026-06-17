import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_form_scaffold.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_request_error.dart';
import 'package:vocanova_mobile/features/auth/presentation/otp_code_fields.dart';

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
    return AuthFormScaffold(
      title: 'Xác thực OTP',
      subtitle: 'Nhập mã 6 số đã gửi đến ${widget.phone}.',
      form: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          OtpCodeFields(
            key: _otpKey,
            enabled: !_isLoading && attemptsLeft > 0,
            onCompleted: _verify,
          ),
          const SizedBox(height: 16),
          Text(
            attemptsLeft > 0
                ? 'Bạn còn $attemptsLeft lần nhập.'
                : 'Bạn đã nhập sai OTP quá 5 lần. Vui lòng gửi lại mã.',
            key: const Key('otp-attempt-message'),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 20),
          if (_isLoading)
            const Center(child: CircularProgressIndicator())
          else
            TextButton(
              key: const Key('otp-resend'),
              onPressed: _secondsRemaining == 0 ? _resend : null,
              child: Text(
                _secondsRemaining == 0
                    ? 'Gửi lại mã'
                    : 'Gửi lại mã sau ${_secondsRemaining}s',
              ),
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
          const SnackBar(content: Text('Xác thực OTP thành công.')),
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
            ? 'Bạn đã nhập sai OTP quá 5 lần. Vui lòng gửi lại mã.'
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
      throw const FormatException('Missing registration data.');
    }

    return ref
        .read(authProvider.notifier)
        .register(
          phone: widget.phone,
          password: payload.password,
          displayName: payload.displayName,
          otpCode: code,
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
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Đã gửi lại mã OTP.')));
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
  const RegisterOtpPayload({required this.displayName, required this.password});

  final String displayName;
  final String password;
}
