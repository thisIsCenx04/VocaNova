import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_form_scaffold.dart';
import 'package:vocanova_mobile/features/auth/presentation/auth_validators.dart';

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
    ref.listen(authProvider, _handleAuthState);
    final isLoading = ref.watch(authProvider).status == AuthStatus.loading;

    return AuthFormScaffold(
      title: 'Tạo tài khoản',
      subtitle: 'Bắt đầu xây dựng vốn từ của riêng bạn.',
      form: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            TextFormField(
              key: const Key('register-display-name'),
              controller: _displayNameController,
              enabled: !isLoading,
              textInputAction: TextInputAction.next,
              decoration: const InputDecoration(
                labelText: 'Tên hiển thị',
                prefixIcon: Icon(Icons.person_outline),
              ),
              validator: AuthValidators.displayName,
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-phone'),
              controller: _phoneController,
              enabled: !isLoading,
              keyboardType: TextInputType.phone,
              textInputAction: TextInputAction.next,
              decoration: const InputDecoration(
                labelText: 'Số điện thoại',
                hintText: 'Ví dụ: 0901234567',
                prefixIcon: Icon(Icons.phone_outlined),
              ),
              validator: AuthValidators.phone,
            ),
            const SizedBox(height: 16),
            TextFormField(
              key: const Key('register-password'),
              controller: _passwordController,
              enabled: !isLoading,
              obscureText: _obscurePassword,
              textInputAction: TextInputAction.next,
              decoration: InputDecoration(
                labelText: 'Mật khẩu',
                prefixIcon: const Icon(Icons.lock_outline),
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
              enabled: !isLoading,
              obscureText: _obscureConfirmation,
              textInputAction: TextInputAction.done,
              decoration: InputDecoration(
                labelText: 'Xác nhận mật khẩu',
                prefixIcon: const Icon(Icons.lock_outline),
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
            ElevatedButton(
              key: const Key('register-submit'),
              onPressed: isLoading ? null : _submit,
              child: isLoading
                  ? const SizedBox.square(
                      dimension: 22,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Đăng ký'),
            ),
            const SizedBox(height: 12),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Text('Đã có tài khoản?'),
                TextButton(
                  onPressed: isLoading
                      ? null
                      : () => context.go(AppRoutes.login),
                  child: const Text('Đăng nhập'),
                ),
              ],
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
      tooltip: obscure ? 'Hiện mật khẩu' : 'Ẩn mật khẩu',
      onPressed: onPressed,
      icon: Icon(
        obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined,
      ),
    );
  }

  void _submit() {
    if (!_formKey.currentState!.validate()) {
      return;
    }
    ref
        .read(authProvider.notifier)
        .register(
          phone: _phoneController.text.trim(),
          password: _passwordController.text,
          displayName: _displayNameController.text.trim(),
        );
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
      context.go(AppRoutes.onboarding);
    }
  }
}
