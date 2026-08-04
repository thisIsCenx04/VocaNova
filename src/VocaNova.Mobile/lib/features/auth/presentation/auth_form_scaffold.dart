import 'package:flutter/material.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class AuthFormScaffold extends StatelessWidget {
  const AuthFormScaffold({
    required this.title,
    required this.subtitle,
    required this.form,
    this.showBackButton = false,
    this.onBack,
    super.key,
  });

  final String title;
  final String subtitle;
  final Widget form;
  final bool showBackButton;
  final VoidCallback? onBack;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      backgroundColor: AppColors.lightBackground,
      body: SafeArea(
        child: Align(
          alignment: Alignment.topCenter,
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 430),
            child: SingleChildScrollView(
              padding: EdgeInsets.fromLTRB(24, showBackButton ? 8 : 64, 24, 32),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (showBackButton) ...[
                    TextButton.icon(
                      onPressed: onBack ?? Navigator.of(context).maybePop,
                      style: TextButton.styleFrom(
                        foregroundColor: AppColors.muted,
                        padding: EdgeInsets.zero,
                        minimumSize: const Size(57, 21),
                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                        alignment: Alignment.centerLeft,
                      ),
                      icon: const Icon(Icons.chevron_left, size: 18),
                      label: Text(AppLocalizations.of(context)!.authBackButton),
                    ),
                    const SizedBox(height: 20),
                  ],
                  Text(
                    title,
                    style: AppTextStyles.heading.copyWith(
                      color: AppColors.lightOnSurface,
                      letterSpacing: -0.3,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    subtitle,
                    style: AppTextStyles.body.copyWith(color: AppColors.muted),
                  ),
                  SizedBox(height: showBackButton ? 24 : 32),
                  IconTheme(
                    data: IconThemeData(
                      color: theme.colorScheme.onSurface.withValues(alpha: 0.5),
                      size: 18,
                    ),
                    child: DefaultTextStyle(
                      style: AppTextStyles.body.copyWith(
                        color: AppColors.lightOnSurface,
                      ),
                      child: form,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class AuthDivider extends StatelessWidget {
  const AuthDivider({super.key});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        const Expanded(
          child: Divider(color: AppColors.lightDivider, height: 1),
        ),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          child: Text(
            AppLocalizations.of(context)!.authOrDivider,
            style: AppTextStyles.caption.copyWith(
              color: const Color(0xFFC0C0C0),
            ),
          ),
        ),
        const Expanded(
          child: Divider(color: AppColors.lightDivider, height: 1),
        ),
      ],
    );
  }
}

class GoogleAuthButton extends StatelessWidget {
  const GoogleAuthButton({required this.onPressed, this.label, super.key});

  final VoidCallback? onPressed;
  final String? label;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      key: const Key('google-login'),
      onPressed: onPressed,
      icon: const Text(
        'G',
        style: TextStyle(
          color: Color(0xFF4285F4),
          fontSize: 18,
          fontWeight: FontWeight.w700,
        ),
      ),
      label: Text(label ?? AppLocalizations.of(context)!.authContinueWithGoogle),
    );
  }
}

class AuthInlineLink extends StatelessWidget {
  const AuthInlineLink({
    required this.text,
    required this.actionText,
    required this.onPressed,
    super.key,
  });

  final String text;
  final String actionText;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Wrap(
        alignment: WrapAlignment.center,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          Text(
            text,
            style: AppTextStyles.caption.copyWith(
              color: AppColors.muted,
              fontSize: 14,
              height: 21 / 14,
            ),
          ),
          TextButton(
            onPressed: onPressed,
            style: TextButton.styleFrom(
              padding: const EdgeInsets.symmetric(horizontal: 4),
              minimumSize: Size.zero,
              tapTargetSize: MaterialTapTargetSize.shrinkWrap,
            ),
            child: Text(
              actionText,
              style: AppTextStyles.button.copyWith(
                color: AppColors.primary,
                fontSize: 16,
                height: 24 / 16,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class AuthPrimaryButton extends StatelessWidget {
  const AuthPrimaryButton({
    required this.onPressed,
    required this.child,
    this.buttonKey,
    super.key,
  });

  final Key? buttonKey;
  final VoidCallback? onPressed;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 52,
      child: ElevatedButton(key: buttonKey, onPressed: onPressed, child: child),
    );
  }
}

class AuthProgressHeader extends StatelessWidget {
  const AuthProgressHeader({
    required this.value,
    required this.label,
    super.key,
  });

  final double value;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(999),
          child: LinearProgressIndicator(
            minHeight: 4,
            value: value,
            backgroundColor: AppColors.lightDivider,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          label,
          textAlign: TextAlign.right,
          style: AppTextStyles.caption.copyWith(color: AppColors.muted),
        ),
      ],
    );
  }
}

class AuthHelperText extends StatelessWidget {
  const AuthHelperText(this.text, {this.widgetKey, super.key});

  final String text;
  final Key? widgetKey;

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      key: widgetKey,
      textAlign: TextAlign.center,
      style: AppTextStyles.caption.copyWith(
        color: AppColors.muted,
        fontSize: 14,
        height: 21 / 14,
      ),
    );
  }
}

InputDecoration authInputDecoration({
  required String label,
  bool requiredField = false,
  String? hint,
  Widget? suffixIcon,
}) {
  return InputDecoration(
    label: requiredField
        ? Text.rich(
            TextSpan(
              children: [
                TextSpan(text: label),
                const TextSpan(
                  text: ' *',
                  style: TextStyle(color: Colors.red),
                ),
              ],
            ),
          )
        : null,
    labelText: requiredField ? null : label,
    hintText: hint,
    suffixIcon: suffixIcon,
  );
}

Widget authLoadingIndicator() {
  return const SizedBox.square(
    dimension: 22,
    child: CircularProgressIndicator(strokeWidth: 2),
  );
}
