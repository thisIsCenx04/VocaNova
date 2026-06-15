import 'package:vocanova_mobile/features/progress/domain/progress_summary.dart';

class ProgressOverviewState {
  const ProgressOverviewState({
    this.summary,
    this.isLoading = true,
    this.isOffline = false,
    this.errorMessage,
  });

  final ProgressSummary? summary;
  final bool isLoading;
  final bool isOffline;
  final String? errorMessage;
}
