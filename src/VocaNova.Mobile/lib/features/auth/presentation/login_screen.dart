import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_form_scaffold.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_validators.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _phoneController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    ref.listen(authProvider, _handleAuthState);
    final isLoading = ref.watch(authProvider).status == AuthStatus.loading;
    final l10n = AppLocalizations.of(context)!;

    return AuthFormScaffold(
      title: l10n.authSignInTitle,
      subtitle: l10n.authSignInSubtitle,
      form: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              key: const Key('login-phone'),
              controller: _phoneController,
              enabled: !isLoading,
              keyboardType: TextInputType.phone,
              textInputAction: TextInputAction.next,
              decoration: authInputDecoration(
                label: l10n.authPhoneNumberLabel,
                hint: '0901234567',
              ),
              validator: AuthValidators.phone,
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('login-password'),
              controller: _passwordController,
              enabled: !isLoading,
              obscureText: _obscurePassword,
              textInputAction: TextInputAction.done,
              decoration: authInputDecoration(
                label: l10n.authPasswordLabel,
                suffixIcon: IconButton(
                  tooltip: _obscurePassword
                      ? l10n.authShowPassword
                      : l10n.authHidePassword,
                  onPressed: () {
                    setState(() => _obscurePassword = !_obscurePassword);
                  },
                  icon: Icon(
                    _obscurePassword
                        ? Icons.visibility_outlined
                        : Icons.visibility_off_outlined,
                  ),
                ),
              ),
              validator: AuthValidators.loginPassword,
              onFieldSubmitted: (_) => _submit(),
            ),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton(
                onPressed: isLoading
                    ? null
                    : () => context.push(AppRoutes.forgotPassword),
                child: Text(l10n.authForgotPasswordLink),
              ),
            ),
            AuthPrimaryButton(
              buttonKey: const Key('login-submit'),
              onPressed: isLoading ? null : _submit,
              child: isLoading
                  ? authLoadingIndicator()
                  : Text(l10n.authSignInTitle),
            ),
            const SizedBox(height: 16),
            const AuthDivider(),
            const SizedBox(height: 16),
            GoogleAuthButton(
              onPressed: isLoading
                  ? null
                  : () => ref.read(authProvider.notifier).signInWithGoogle(),
            ),
            const SizedBox(height: 12),
            AuthInlineLink(
              text: l10n.authNewHerePrefix,
              actionText: l10n.authCreateAccountTitle,
              onPressed: isLoading
                  ? null
                  : () => context.go(AppRoutes.register),
            ),
          ],
        ),
      ),
    );
  }

  void _submit() {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    ref
        .read(authProvider.notifier)
        .login(_phoneController.text.trim(), _passwordController.text);
  }

  void _handleAuthState(AuthState? previous, AuthState next) {
    if (previous?.status == next.status) {
      return;
    }
    if (next.status == AuthStatus.error && next.errorMessage != null) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text(next.errorMessage!)));
    } else if (next.status == AuthStatus.authenticated) {
      context.go(AppRoutes.home);
    }
  }
}
