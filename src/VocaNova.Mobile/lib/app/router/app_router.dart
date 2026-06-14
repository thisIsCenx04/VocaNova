import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/router/auth_guard.dart';
import 'package:vocanova_mobile/app/router/main_shell.dart';
import 'package:vocanova_mobile/core/storage/secure_storage.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';
import 'package:vocanova_mobile/features/home/presentation/home_screen.dart';
import 'package:vocanova_mobile/features/shared/presentation/placeholder_screen.dart';

class AppRouter {
  AppRouter._(this.tokenStorage);

  static final AppRouter instance = AppRouter._(SecureStorage.instance);

  final TokenStorage tokenStorage;
  late final AuthGuard _authGuard = AuthGuard(tokenStorage: tokenStorage);

  static AppRouter create({required TokenStorage tokenStorage}) {
    return AppRouter._(tokenStorage);
  }

  late final GoRouter router = GoRouter(
    initialLocation: AppRoutes.root,
    redirect: _authGuard.redirect,
    routes: [
      GoRoute(path: AppRoutes.root, builder: (_, _) => const SizedBox.shrink()),
      GoRoute(
        path: AppRoutes.login,
        builder: (_, _) => const PlaceholderScreen(title: 'Đăng nhập'),
      ),
      GoRoute(
        path: AppRoutes.register,
        builder: (_, _) => const PlaceholderScreen(title: 'Đăng ký'),
      ),
      GoRoute(
        path: AppRoutes.otp,
        builder: (_, _) => const PlaceholderScreen(title: 'Xác thực OTP'),
      ),
      StatefulShellRoute.indexedStack(
        builder: (_, _, navigationShell) {
          return MainShell(navigationShell: navigationShell);
        },
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.home,
                builder: (_, _) => const HomeScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.search,
                builder: (_, _) => const PlaceholderScreen(title: 'Tra từ'),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.lists,
                builder: (_, _) =>
                    const PlaceholderScreen(title: 'Danh sách từ'),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.progress,
                builder: (_, _) => const PlaceholderScreen(title: 'Tiến độ'),
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: AppRoutes.onboarding,
        builder: (_, _) => const PlaceholderScreen(title: 'Thiết lập học tập'),
      ),
      GoRoute(
        path: AppRoutes.word,
        builder: (_, state) => PlaceholderScreen(
          title: 'Chi tiết từ #${state.pathParameters['id']}',
        ),
      ),
      GoRoute(
        path: AppRoutes.list,
        builder: (_, state) => PlaceholderScreen(
          title: 'Danh sách #${state.pathParameters['id']}',
        ),
      ),
      GoRoute(
        path: AppRoutes.quizConfig,
        builder: (_, _) => const PlaceholderScreen(title: 'Cấu hình kiểm tra'),
      ),
      GoRoute(
        path: AppRoutes.quizActive,
        builder: (_, _) => const PlaceholderScreen(title: 'Bài kiểm tra'),
      ),
      GoRoute(
        path: AppRoutes.quizResult,
        builder: (_, _) => const PlaceholderScreen(title: 'Kết quả kiểm tra'),
      ),
      GoRoute(
        path: AppRoutes.settings,
        builder: (_, _) => const PlaceholderScreen(title: 'Cài đặt'),
      ),
      GoRoute(
        path: AppRoutes.profile,
        builder: (_, _) => const PlaceholderScreen(title: 'Hồ sơ'),
      ),
    ],
  );
}
