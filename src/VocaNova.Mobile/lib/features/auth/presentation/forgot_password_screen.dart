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
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

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
    final l10n = AppLocalizations.of(context)!;
    return AuthFormScaffold(
      title: switch (_step) {
        ForgotPasswordStep.phone => l10n.authForgotTitleReset,
        ForgotPasswordStep.otp => l10n.authForgotTitleVerify,
        ForgotPasswordStep.password => l10n.authForgotTitleCreate,
      },
      subtitle: switch (_step) {
        ForgotPasswordStep.phone => l10n.authForgotSubtitlePhone,
        ForgotPasswordStep.otp => l10n.authForgotSubtitleOtp(
          _phoneController.text.trim(),
        ),
        ForgotPasswordStep.password => l10n.authForgotSubtitlePassword,
      },
      showBackButton: true,
      onBack: () => context.go(AppRoutes.login),
      form: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AuthProgressHeader(
            value: (_step.index + 1) / 3,
            label: l10n.authStepProgress(_step.index + 1, 3),
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
    final l10n = AppLocalizations.of(context)!;
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
              label: l10n.authEmailOrPhoneLabel,
              hint: '0901234567',
            ),
            validator: AuthValidators.phone,
            onFieldSubmitted: (_) => _sendResetOtp(),
          ),
          const SizedBox(height: 24),
          AuthPrimaryButton(
            buttonKey: const Key('forgot-send-otp'),
            onPressed: _isLoading ? null : _sendResetOtp,
            child: _buttonContent(l10n.authSendResetCodeButton),
          ),
        ],
      ),
    );
  }

  Widget _otpStep() {
    final l10n = AppLocalizations.of(context)!;
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
              ? l10n.authOtpMaxAttemptsReached
              : l10n.authOtpVerifiedOnSave,
          widgetKey: const Key('forgot-otp-message'),
        ),
        const SizedBox(height: 16),
        TextButton(
          key: const Key('forgot-resend-otp'),
          onPressed: !_isLoading && _secondsRemaining == 0 ? _resendOtp : null,
          child: Text(
            _secondsRemaining == 0
                ? l10n.authResendCode
                : l10n.authResendInSeconds(_secondsRemaining),
          ),
        ),
        TextButton(
          onPressed: _isLoading
              ? null
              : () => setState(() => _step = ForgotPasswordStep.phone),
          child: Text(l10n.authChangePhoneNumber),
        ),
      ],
    );
  }

  Widget _passwordStep() {
    final l10n = AppLocalizations.of(context)!;
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
              label: l10n.authNewPasswordLabel,
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
              label: l10n.authConfirmNewPasswordLabel,
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
            child: _buttonContent(l10n.authSaveNewPasswordButton),
          ),
          TextButton(
            onPressed: _isLoading
                ? null
                : () => setState(() => _step = ForgotPasswordStep.otp),
            child: Text(l10n.authEnterOtpAgain),
          ),
        ],
      ),
    );
  }

  Widget _visibilityButton({
    required bool obscure,
    required VoidCallback onPressed,
  }) {
    final l10n = AppLocalizations.of(context)!;
    return IconButton(
      tooltip: obscure ? l10n.authShowPassword : l10n.authHidePassword,
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
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(AppLocalizations.of(context)!.authOtpResentMessage)),
      );
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
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(AppLocalizations.of(context)!.authPasswordChangedMessage),
        ),
      );
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
            ? AppLocalizations.of(context)!.authOtpMaxAttemptsReached
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
