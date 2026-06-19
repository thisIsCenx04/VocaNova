import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_form_scaffold.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_request_error.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_validators.dart';
import 'package:vocanova_mobile/features/auth/presentation/otp_code_fields.dart';

enum ForgotPasswordStep { phone, otp, password }

class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  ConsumerState<ForgotPasswordScreen> createState() =>
      _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  static const maxAttempts = 5;
  static const resendSeconds = 60;

  final _phoneFormKey = GlobalKey<FormState>();
  final _passwordFormKey = GlobalKey<FormState>();
  final _otpKey = GlobalKey<OtpCodeFieldsState>();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  ForgotPasswordStep _step = ForgotPasswordStep.phone;
  Timer? _timer;
  int _secondsRemaining = resendSeconds;
  int _failedAttempts = 0;
  String _otpCode = '';
  bool _isLoading = false;
  bool _obscurePassword = true;
  bool _obscureConfirmation = true;

  @override
  void dispose() {
    _timer?.cancel();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AuthFormScaffold(
      title: switch (_step) {
        ForgotPasswordStep.phone => 'Reset password',
        ForgotPasswordStep.otp => 'Verify your code',
        ForgotPasswordStep.password => 'Create password',
      },
      subtitle: switch (_step) {
        ForgotPasswordStep.phone =>
          "Enter your phone number and we'll send a code to reset your password.",
        ForgotPasswordStep.otp =>
          'Enter the 6-digit code sent to ${_phoneController.text.trim()}.',
        ForgotPasswordStep.password =>
          'Create a new password for your account.',
      },
      showBackButton: true,
      onBack: () => context.go(AppRoutes.login),
      form: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AuthProgressHeader(
            value: (_step.index + 1) / 3,
            label: 'Bước ${_step.index + 1}/3',
          ),
          const SizedBox(height: 24),
          switch (_step) {
            ForgotPasswordStep.phone => _phoneStep(),
            ForgotPasswordStep.otp => _otpStep(),
            ForgotPasswordStep.password => _passwordStep(),
          },
        ],
      ),
    );
  }

  Widget _phoneStep() {
    return Form(
      key: _phoneFormKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            key: const Key('forgot-phone'),
            controller: _phoneController,
            enabled: !_isLoading,
            keyboardType: TextInputType.phone,
            textInputAction: TextInputAction.done,
            decoration: authInputDecoration(
              label: 'Email or phone number',
              hint: '0901234567',
            ),
            validator: AuthValidators.phone,
            onFieldSubmitted: (_) => _sendResetOtp(),
          ),
          const SizedBox(height: 24),
          AuthPrimaryButton(
            buttonKey: const Key('forgot-send-otp'),
            onPressed: _isLoading ? null : _sendResetOtp,
            child: _buttonContent('Send reset code'),
          ),
        ],
      ),
    );
  }

  Widget _otpStep() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        OtpCodeFields(
          key: _otpKey,
          enabled: !_isLoading && _failedAttempts < maxAttempts,
          onCompleted: (code) {
            setState(() {
              _otpCode = code;
              _step = ForgotPasswordStep.password;
            });
          },
        ),
        const SizedBox(height: 16),
        AuthHelperText(
          _failedAttempts >= maxAttempts
              ? 'Bạn đã nhập sai OTP quá 5 lần. Vui lòng gửi lại mã.'
              : 'Mã OTP sẽ được kiểm tra khi bạn lưu mật khẩu mới.',
          widgetKey: const Key('forgot-otp-message'),
        ),
        const SizedBox(height: 16),
        TextButton(
          key: const Key('forgot-resend-otp'),
          onPressed: !_isLoading && _secondsRemaining == 0 ? _resendOtp : null,
          child: Text(
            _secondsRemaining == 0
                ? 'Resend code'
                : 'Resend in ${_secondsRemaining}s',
          ),
        ),
        TextButton(
          onPressed: _isLoading
              ? null
              : () => setState(() => _step = ForgotPasswordStep.phone),
          child: const Text('Change phone number'),
        ),
      ],
    );
  }

  Widget _passwordStep() {
    return Form(
      key: _passwordFormKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            key: const Key('forgot-new-password'),
            controller: _passwordController,
            enabled: !_isLoading,
            obscureText: _obscurePassword,
            textInputAction: TextInputAction.next,
            decoration: authInputDecoration(
              label: 'New password',
              suffixIcon: _visibilityButton(
                obscure: _obscurePassword,
                onPressed: () =>
                    setState(() => _obscurePassword = !_obscurePassword),
              ),
            ),
            validator: AuthValidators.strongPassword,
          ),
          const SizedBox(height: 16),
          TextFormField(
            key: const Key('forgot-confirm-password'),
            controller: _confirmPasswordController,
            enabled: !_isLoading,
            obscureText: _obscureConfirmation,
            textInputAction: TextInputAction.done,
            decoration: authInputDecoration(
              label: 'Confirm new password',
              suffixIcon: _visibilityButton(
                obscure: _obscureConfirmation,
                onPressed: () => setState(
                  () => _obscureConfirmation = !_obscureConfirmation,
                ),
              ),
            ),
            validator: (value) =>
                AuthValidators.confirmPassword(value, _passwordController.text),
            onFieldSubmitted: (_) => _resetPassword(),
          ),
          const SizedBox(height: 24),
          AuthPrimaryButton(
            buttonKey: const Key('forgot-reset-password'),
            onPressed: _isLoading ? null : _resetPassword,
            child: _buttonContent('Save new password'),
          ),
          TextButton(
            onPressed: _isLoading
                ? null
                : () => setState(() => _step = ForgotPasswordStep.otp),
            child: const Text('Enter OTP again'),
          ),
        ],
      ),
    );
  }

  Widget _visibilityButton({
    required bool obscure,
    required VoidCallback onPressed,
  }) {
    return IconButton(
      tooltip: obscure ? 'Show password' : 'Hide password',
      onPressed: onPressed,
      icon: Icon(
        obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined,
      ),
    );
  }

  Widget _buttonContent(String label) {
    return _isLoading ? authLoadingIndicator() : Text(label);
  }

  Future<void> _sendResetOtp() async {
    if (!_phoneFormKey.currentState!.validate()) {
      return;
    }
    setState(() => _isLoading = true);
    try {
      await ref
          .read(authRepositoryProvider)
          .forgotPassword(_phoneController.text.trim());
      if (!mounted) return;
      setState(() {
        _step = ForgotPasswordStep.otp;
        _failedAttempts = 0;
      });
      _startCountdown();
    } catch (error) {
      if (mounted) _showError(authRequestError(error));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _resendOtp() async {
    setState(() => _isLoading = true);
    try {
      await ref
          .read(authRepositoryProvider)
          .forgotPassword(_phoneController.text.trim());
      if (!mounted) return;
      setState(() => _failedAttempts = 0);
      _otpKey.currentState?.clear();
      _startCountdown();
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('OTP code resent.')));
    } catch (error) {
      if (mounted) _showError(authRequestError(error));
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _resetPassword() async {
    if (!_passwordFormKey.currentState!.validate()) {
      return;
    }
    setState(() => _isLoading = true);
    try {
      await ref
          .read(authRepositoryProvider)
          .resetPassword(
            phone: _phoneController.text.trim(),
            otpCode: _otpCode,
            newPassword: _passwordController.text,
          );
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Password changed.')));
      context.go(AppRoutes.login);
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _failedAttempts++;
        _step = ForgotPasswordStep.otp;
      });
      WidgetsBinding.instance.addPostFrameCallback((_) {
        _otpKey.currentState?.clear();
      });
      _showError(
        _failedAttempts >= maxAttempts
            ? 'Bạn đã nhập sai OTP quá 5 lần. Vui lòng gửi lại mã.'
            : authRequestError(error),
      );
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _startCountdown() {
    _timer?.cancel();
    setState(() => _secondsRemaining = resendSeconds);
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (!mounted || _secondsRemaining <= 1) {
        timer.cancel();
        if (mounted) setState(() => _secondsRemaining = 0);
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
