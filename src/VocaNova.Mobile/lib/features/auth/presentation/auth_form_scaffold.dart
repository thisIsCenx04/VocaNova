import 'package:flutter/material.dart';
import 'package:vocanova_mobile/app/theme/app_text_styles.dart';

class AuthFormScaffold extends StatelessWidget {
  const AuthFormScaffold({
    required this.title,
    required this.subtitle,
    required this.form,
    super.key,
  });

  final String title;
  final String subtitle;
  final Widget form;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'VocaNova',
                    style: AppTextStyles.heading.copyWith(
                      color: Theme.of(context).colorScheme.primary,
                    ),
                  ),
                  const SizedBox(height: 32),
                  Text(
                    title,
                    style: Theme.of(context).textTheme.headlineMedium,
                  ),
                  const SizedBox(height: 8),
                  Text(subtitle, style: Theme.of(context).textTheme.bodyLarge),
                  const SizedBox(height: 28),
                  form,
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
