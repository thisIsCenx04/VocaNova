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
      title: 'Quên mật khẩu',
      subtitle: switch (_step) {
        ForgotPasswordStep.phone => 'Nhập số điện thoại đã đăng ký.',
        ForgotPasswordStep.otp =>
          'Nhập mã 6 số đã gửi đến ${_phoneController.text.trim()}.',
        ForgotPasswordStep.password =>
          'Tạo mật khẩu mới cho tài khoản của bạn.',
      },
      form: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          LinearProgressIndicator(value: (_step.index + 1) / 3),
          const SizedBox(height: 8),
          Text(
            'Bước ${_step.index + 1}/3',
            textAlign: TextAlign.right,
            style: Theme.of(context).textTheme.bodySmall,
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
            decoration: const InputDecoration(
              labelText: 'Số điện thoại',
              hintText: 'Ví dụ: 0901234567',
              prefixIcon: Icon(Icons.phone_outlined),
            ),
            validator: AuthValidators.phone,
            onFieldSubmitted: (_) => _sendResetOtp(),
          ),
          const SizedBox(height: 24),
          ElevatedButton(
            key: const Key('forgot-send-otp'),
            onPressed: _isLoading ? null : _sendResetOtp,
            child: _buttonContent('Gửi mã OTP'),
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
        Text(
          _failedAttempts >= maxAttempts
              ? 'Bạn đã nhập sai OTP quá 5 lần. Vui lòng gửi lại mã.'
              : 'Mã OTP sẽ được kiểm tra khi bạn lưu mật khẩu mới.',
          key: const Key('forgot-otp-message'),
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: 16),
        TextButton(
          key: const Key('forgot-resend-otp'),
          onPressed: !_isLoading && _secondsRemaining == 0 ? _resendOtp : null,
          child: Text(
            _secondsRemaining == 0
                ? 'Gửi lại mã'
                : 'Gửi lại mã sau ${_secondsRemaining}s',
          ),
        ),
        TextButton(
          onPressed: _isLoading
              ? null
              : () => setState(() => _step = ForgotPasswordStep.phone),
          child: const Text('Đổi số điện thoại'),
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
            decoration: InputDecoration(
              labelText: 'Mật khẩu mới',
              prefixIcon: const Icon(Icons.lock_outline),
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
            decoration: InputDecoration(
              labelText: 'Xác nhận mật khẩu mới',
              prefixIcon: const Icon(Icons.lock_outline),
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
          ElevatedButton(
            key: const Key('forgot-reset-password'),
            onPressed: _isLoading ? null : _resetPassword,
            child: _buttonContent('Lưu mật khẩu mới'),
          ),
          TextButton(
            onPressed: _isLoading
                ? null
                : () => setState(() => _step = ForgotPasswordStep.otp),
            child: const Text('Nhập lại mã OTP'),
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
      tooltip: obscure ? 'Hiện mật khẩu' : 'Ẩn mật khẩu',
      onPressed: onPressed,
      icon: Icon(
        obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined,
      ),
    );
  }

  Widget _buttonContent(String label) {
    return _isLoading
        ? const SizedBox.square(
            dimension: 22,
            child: CircularProgressIndicator(strokeWidth: 2),
          )
        : Text(label);
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
      ).showSnackBar(const SnackBar(content: Text('Đã gửi lại mã OTP.')));
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
      ).showSnackBar(const SnackBar(content: Text('Đổi mật khẩu thành công.')));
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
