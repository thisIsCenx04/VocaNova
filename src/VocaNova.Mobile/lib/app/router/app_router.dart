import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/router/auth_guard.dart';
import 'package:vocanova_mobile/app/router/main_shell.dart';
import 'package:vocanova_mobile/core/storage/secure_storage.dart';
import 'package:vocanova_mobile/core/storage/token_storage.dart';
import 'package:vocanova_mobile/features/auth/presentation/forgot_password_screen.dart';
import 'package:vocanova_mobile/features/auth/presentation/login_screen.dart';
import 'package:vocanova_mobile/features/auth/presentation/onboarding_screen.dart';
import 'package:vocanova_mobile/features/auth/presentation/otp_screen.dart';
import 'package:vocanova_mobile/features/auth/presentation/register_screen.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/word_search_screen.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/word_detail_screen.dart';
import 'package:vocanova_mobile/features/dictionary/domain/word_summary.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topic_detail_screen.dart';
import 'package:vocanova_mobile/features/dictionary/presentation/topics_screen.dart';
import 'package:vocanova_mobile/features/home/presentation/home_screen.dart';
import 'package:vocanova_mobile/features/notifications/presentation/notifications_screen.dart';
import 'package:vocanova_mobile/features/lists/presentation/lists_screen.dart';
import 'package:vocanova_mobile/features/lists/presentation/list_detail_screen.dart';
import 'package:vocanova_mobile/features/progress/presentation/progress_overview_screen.dart';
import 'package:vocanova_mobile/features/progress/presentation/progress_charts_screen.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';
import 'package:vocanova_mobile/features/quiz/presentation/quiz_config_screen.dart';
import 'package:vocanova_mobile/features/quiz/presentation/quiz_result_screen.dart';
import 'package:vocanova_mobile/features/quiz/presentation/quiz_session_screen.dart';
import 'package:vocanova_mobile/features/quiz/presentation/wrong_words_screen.dart';
import 'package:vocanova_mobile/features/settings/presentation/profile_screen.dart';
import 'package:vocanova_mobile/features/settings/presentation/settings_screen.dart';

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
      GoRoute(path: AppRoutes.login, builder: (_, _) => const LoginScreen()),
      GoRoute(
        path: AppRoutes.register,
        builder: (_, _) => const RegisterScreen(),
      ),
      GoRoute(
        path: AppRoutes.forgotPassword,
        builder: (_, _) => const ForgotPasswordScreen(),
      ),
      GoRoute(
        path: AppRoutes.otp,
        builder: (_, state) => OtpScreen(
          phone: state.uri.queryParameters['phone'] ?? '',
          purpose: state.uri.queryParameters['purpose'] ?? 'verify',
          registerPayload: state.extra is RegisterOtpPayload
              ? state.extra! as RegisterOtpPayload
              : null,
        ),
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
                builder: (_, _) => const WordSearchScreen(),
              ),
              GoRoute(
                path: AppRoutes.word,
                builder: (_, state) => WordDetailScreen(
                  wordId: int.parse(state.pathParameters['id']!),
                ),
              ),
              GoRoute(
                path: AppRoutes.topics,
                builder: (_, _) => const TopicsScreen(),
              ),
              GoRoute(
                path: AppRoutes.topic,
                builder: (_, state) => TopicDetailScreen(
                  topicId: int.parse(state.pathParameters['id']!),
                  isPersonal: state.uri.queryParameters['personal'] == 'true',
                  initialTopic: state.extra is TopicSummary
                      ? state.extra! as TopicSummary
                      : null,
                ),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.lists,
                builder: (_, _) => const ListsScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.quizConfig,
                builder: (_, state) => QuizConfigScreen(
                  initialListId: int.tryParse(
                    state.uri.queryParameters['listId'] ?? '',
                  ),
                ),
              ),
              GoRoute(
                path: AppRoutes.progress,
                builder: (_, _) => const ProgressOverviewScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.profile,
                builder: (_, _) => const ProfileScreen(),
              ),
              GoRoute(
                path: AppRoutes.settings,
                builder: (_, _) => const SettingsScreen(),
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: AppRoutes.onboarding,
        builder: (_, _) => const OnboardingScreen(),
      ),
      GoRoute(
        path: AppRoutes.progressCharts,
        builder: (_, _) => const ProgressChartsScreen(),
      ),
      GoRoute(
        path: AppRoutes.notifications,
        builder: (_, _) => const NotificationsScreen(),
      ),
      GoRoute(
        path: AppRoutes.list,
        builder: (_, state) =>
            ListDetailScreen(listId: int.parse(state.pathParameters['id']!)),
      ),
      GoRoute(
        path: AppRoutes.quizActive,
        builder: (_, state) => state.extra is QuizSessionStart
            ? QuizSessionScreen(session: state.extra! as QuizSessionStart)
            : const QuizSessionUnavailableScreen(),
      ),
      GoRoute(
        path: AppRoutes.quizResult,
        builder: (_, state) {
          final sessionId = int.tryParse(
            state.uri.queryParameters['sessionId'] ?? '',
          );
          return sessionId == null
              ? const QuizResultUnavailableScreen()
              : QuizResultScreen(sessionId: sessionId);
        },
      ),
      GoRoute(
        path: AppRoutes.wrongWords,
        builder: (_, _) => const WrongWordsScreen(),
      ),
    ],
  );
}
