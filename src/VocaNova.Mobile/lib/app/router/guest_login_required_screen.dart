import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class GuestLoginRequiredScreen extends StatelessWidget {
  const GuestLoginRequiredScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(28),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                l10n.guestLoginRequiredMessage,
                key: const Key('guest-login-required-message'),
                textAlign: TextAlign.center,
                style: AppTextStyles.button.copyWith(
                  color: Theme.of(context).colorScheme.onSurface,
                  fontSize: 18,
                  height: 27 / 18,
                ),
              ),
              const SizedBox(height: 20),
              FilledButton(
                key: const Key('guest-login-button'),
                onPressed: () => context.go(AppRoutes.login),
                style: FilledButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  minimumSize: const Size(160, 48),
                ),
                child: Text(l10n.authSignInTitle),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class AuthGate extends StatelessWidget {
  const AuthGate({
    required this.isAuthenticated,
    required this.child,
    super.key,
  });

  final bool? isAuthenticated;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    if (isAuthenticated == null) {
      return const Center(child: CircularProgressIndicator());
    }

    return isAuthenticated! ? child : const GuestLoginRequiredScreen();
  }
}
