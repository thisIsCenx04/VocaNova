import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_router.dart';
import 'package:vocanova_mobile/app/settings/app_settings_notifier.dart';
import 'package:vocanova_mobile/app/theme/app_theme.dart';

class VocaNovaApp extends StatelessWidget {
  VocaNovaApp({super.key, GoRouter? router})
    : router = router ?? AppRouter.instance.router;

  final GoRouter router;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: AppSettingsNotifier.instance,
      builder: (context, _) {
        final settings = AppSettingsNotifier.instance.state;
        return MaterialApp.router(
          title: 'VocaNova',
          debugShowCheckedModeBanner: false,
          theme: AppTheme.light(),
          darkTheme: AppTheme.dark(),
          themeMode: settings.themeMode,
          locale: settings.locale,
          supportedLocales: const [Locale('vi'), Locale('en')],
          localizationsDelegates: GlobalMaterialLocalizations.delegates,
          routerConfig: router,
        );
      },
    );
  }
}
