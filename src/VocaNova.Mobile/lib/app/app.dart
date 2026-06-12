import 'package:flutter/material.dart';
import 'package:vocanova_mobile/app/theme/app_theme.dart';
import 'package:vocanova_mobile/features/home/presentation/home_screen.dart';

class VocaNovaApp extends StatelessWidget {
  const VocaNovaApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'VocaNova',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: ThemeMode.dark,
      home: const HomeScreen(),
    );
  }
}
