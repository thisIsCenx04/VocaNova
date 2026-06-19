import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_form_scaffold.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_request_error.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_validators.dart';
import 'package:vocanova_mobile/features/auth/presentation/otp_screen.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _displayNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _obscurePassword = true;
  bool _obscureConfirmation = true;
  bool _isLoading = false;

  @override
  void dispose() {
    _displayNameController.dispose();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AuthFormScaffold(
      title: 'Create account',
      subtitle: 'Start learning today',
      showBackButton: true,
      onBack: () => context.go(AppRoutes.login),
      form: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              key: const Key('register-display-name'),
              controller: _displayNameController,
              enabled: !_isLoading,
              textInputAction: TextInputAction.next,
              decoration: authInputDecoration(
                label: 'Full name',
                hint: 'Nguyen Van An',
              ),
              validator: AuthValidators.displayName,
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-phone'),
              controller: _phoneController,
              enabled: !_isLoading,
              keyboardType: TextInputType.phone,
              textInputAction: TextInputAction.next,
              decoration: authInputDecoration(
                label: 'Phone number',
                hint: '+84 90 000 0000',
              ),
              validator: AuthValidators.phone,
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-password'),
              controller: _passwordController,
              enabled: !_isLoading,
              obscureText: _obscurePassword,
              textInputAction: TextInputAction.next,
              decoration: authInputDecoration(
                label: 'Password',
                hint: 'At least 8 characters',
                suffixIcon: _visibilityButton(
                  obscure: _obscurePassword,
                  onPressed: () {
                    setState(() => _obscurePassword = !_obscurePassword);
                  },
                ),
              ),
              validator: AuthValidators.strongPassword,
              onChanged: (_) {
                if (_confirmPasswordController.text.isNotEmpty) {
                  _formKey.currentState?.validate();
                }
              },
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-confirm-password'),
              controller: _confirmPasswordController,
              enabled: !_isLoading,
              obscureText: _obscureConfirmation,
              textInputAction: TextInputAction.done,
              decoration: authInputDecoration(
                label: 'Confirm password',
                hint: 'Repeat your password',
                suffixIcon: _visibilityButton(
                  obscure: _obscureConfirmation,
                  onPressed: () {
                    setState(
                      () => _obscureConfirmation = !_obscureConfirmation,
                    );
                  },
                ),
              ),
              validator: (value) => AuthValidators.confirmPassword(
                value,
                _passwordController.text,
              ),
              onFieldSubmitted: (_) => _submit(),
            ),
            const SizedBox(height: 24),
            AuthPrimaryButton(
              buttonKey: const Key('register-submit'),
              onPressed: _isLoading ? null : _submit,
              child: _isLoading
                  ? authLoadingIndicator()
                  : const Text('Create account'),
            ),
            const SizedBox(height: 16),
            const AuthDivider(),
            const SizedBox(height: 16),
            GoogleAuthButton(
              onPressed: _isLoading
                  ? null
                  : () => ref.read(authProvider.notifier).signInWithGoogle(),
            ),
            const SizedBox(height: 12),
            AuthInlineLink(
              text: 'Already have an account? ',
              actionText: 'Sign in',
              onPressed: _isLoading ? null : () => context.go(AppRoutes.login),
            ),
          ],
        ),
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

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    final phone = _phoneController.text.trim();
    final payload = RegisterOtpPayload(
      displayName: _displayNameController.text.trim(),
      password: _passwordController.text,
    );

    setState(() => _isLoading = true);
    try {
      await ref
          .read(authRepositoryProvider)
          .sendOtp(phone: phone, purpose: 'register');
      if (!mounted) {
        return;
      }
      context.go(AppRoutes.otpFor(phone, purpose: 'register'), extra: payload);
    } catch (error) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(authRequestError(error))));
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }
}
