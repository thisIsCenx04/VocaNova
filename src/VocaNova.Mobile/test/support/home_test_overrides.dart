import 'package:vocanova_mobile/features/auth/application/auth_notifier.dart';
import 'package:vocanova_mobile/features/auth/domain/auth_state.dart';
import 'package:vocanova_mobile/features/auth/domain/user_profile.dart';
import 'package:vocanova_mobile/features/lists/application/lists_notifier.dart';
import 'package:vocanova_mobile/features/lists/application/lists_state.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_state.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/wrong_words_state.dart';

/// Stub overrides that keep the Home screen off the network/storage in widget
/// tests: each data notifier builds a settled state and its loader is a no-op,
/// so the screen renders immediately with no pending timers or infinite spinners.
const homeStubUser = UserProfile(
  userId: 1,
  displayName: 'An',
  role: 'user',
  status: 'active',
);

class FakeAuthNotifier extends AuthNotifier {
  @override
  AuthState build() =>
      const AuthState(status: AuthStatus.authenticated, user: homeStubUser);
  @override
  Future<void> loadCurrentUser() async {}
}

class FakeProgressOverviewNotifier extends ProgressOverviewNotifier {
  @override
  ProgressOverviewState build() =>
      const ProgressOverviewState(isLoading: false);
  @override
  Future<void> load() async {}
}

class FakeListsNotifier extends ListsNotifier {
  @override
  ListsState build() => const ListsState(isLoading: false);
  @override
  Future<void> load() async {}
}

class FakeWrongWordsNotifier extends WrongWordsNotifier {
  @override
  WrongWordsState build() => const WrongWordsState(isLoading: false);
  @override
  Future<void> load() async {}
}
