import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';

abstract final class RootRedirect {
  static String target({required bool isAuthenticated}) {
    return isAuthenticated ? AppRoutes.home : AppRoutes.login;
  }
}

class AuthGuard {
  const AuthGuard({required this.tokenStorage});

  final TokenStorage tokenStorage;

  static const _publicRoutes = {
    AppRoutes.login,
    AppRoutes.register,
    AppRoutes.otp,
  };

  Future<String?> redirect(BuildContext context, GoRouterState state) async {
    final accessToken = await tokenStorage.getAccessToken();
    final isAuthenticated = accessToken != null && accessToken.isNotEmpty;
    final location = state.matchedLocation;

    if (location == AppRoutes.root) {
      return RootRedirect.target(isAuthenticated: isAuthenticated);
    }

    if (!isAuthenticated && !_publicRoutes.contains(location)) {
      return AppRoutes.login;
    }

    if (isAuthenticated && location == AppRoutes.login) {
      return AppRoutes.home;
    }

    return null;
  }
}
