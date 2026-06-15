import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_notifier.dart';
import 'package:vocanova_mobile/features/progress/application/progress_overview_state.dart';
import 'package:vocanova_mobile/features/progress/domain/progress_summary.dart';

class ProgressOverviewScreen extends ConsumerStatefulWidget {
  const ProgressOverviewScreen({super.key});

  @override
  ConsumerState<ProgressOverviewScreen> createState() =>
      _ProgressOverviewScreenState();
}

class _ProgressOverviewScreenState
    extends ConsumerState<ProgressOverviewScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(progressOverviewProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(progressOverviewProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Tiến độ học tập')),
      body: Column(
        children: [
          if (state.isOffline) const _OfflineBanner(),
          Expanded(child: _content(state)),
        ],
      ),
    );
  }

  Widget _content(ProgressOverviewState state) {
    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.summary == null) {
      return _ErrorState(
        message: state.errorMessage ?? 'Chưa có dữ liệu tiến độ.',
        onRetry: ref.read(progressOverviewProvider.notifier).load,
      );
    }
    return RefreshIndicator(
      onRefresh: ref.read(progressOverviewProvider.notifier).load,
      child: _OverviewContent(summary: state.summary!),
    );
  }
}

class _OverviewContent extends StatelessWidget {
  const _OverviewContent({required this.summary});

  final ProgressSummary summary;

  @override
  Widget build(BuildContext context) {
    return ListView(
      key: const Key('progress-overview-list'),
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
      children: [
        Card(
          key: const Key('streak-card'),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Row(
              children: [
                const Icon(
                  Icons.local_fire_department,
                  color: Colors.orange,
                  size: 54,
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${summary.currentStreakDays} ngày liên tiếp',
                        style: Theme.of(context).textTheme.headlineSmall
                            ?.copyWith(fontWeight: FontWeight.w900),
                      ),
                      Text(
                        'Kỷ lục: ${summary.longestStreakDays} ngày',
                        style: Theme.of(context).textTheme.bodyLarge,
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        Card(
          key: const Key('accuracy-card'),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Row(
              children: [
                SizedBox.square(
                  dimension: 112,
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      CircularProgressIndicator(
                        key: const Key('accuracy-gauge'),
                        value: summary.accuracy7Days / 100,
                        strokeWidth: 10,
                        backgroundColor: Theme.of(
                          context,
                        ).colorScheme.surfaceContainerHighest,
                      ),
                      Center(
                        child: Text(
                          '${summary.accuracy7Days.round()}%',
                          style: Theme.of(context).textTheme.titleLarge
                              ?.copyWith(fontWeight: FontWeight.w900),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 20),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Độ chính xác 7 ngày',
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w800),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        '${summary.correct7Days}/${summary.totalAnswers7Days} câu đúng',
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        GridView.count(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          crossAxisCount: 2,
          mainAxisSpacing: 12,
          crossAxisSpacing: 12,
          childAspectRatio: 1.25,
          children: [
            _MetricCard(
              key: const Key('words-in-progress-card'),
              icon: Icons.menu_book,
              value: '${summary.totalWordsInProgress}',
              label: 'Từ đang học',
            ),
            _MetricCard(
              key: const Key('mastered-words-card'),
              icon: Icons.workspace_premium,
              value: '${summary.masteredWords}',
              label: 'Từ đã thành thạo',
            ),
            _MetricCard(
              key: const Key('sessions-month-card'),
              icon: Icons.quiz,
              value: '${summary.sessionsThisMonth}',
              label: 'Bài kiểm tra tháng này',
            ),
          ],
        ),
      ],
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.icon,
    required this.value,
    required this.label,
    super.key,
  });

  final IconData icon;
  final String value;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, color: Theme.of(context).colorScheme.primary),
            const SizedBox(height: 8),
            Text(
              value,
              style: Theme.of(
                context,
              ).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w900),
            ),
            Text(label, textAlign: TextAlign.center),
          ],
        ),
      ),
    );
  }
}

class _OfflineBanner extends StatelessWidget {
  const _OfflineBanner();

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const Key('progress-offline-banner'),
      width: double.infinity,
      color: Colors.amber.withValues(alpha: 0.24),
      padding: const EdgeInsets.all(10),
      child: const Text(
        'Bạn đang ngoại tuyến. Đang hiển thị dữ liệu tiến độ đã lưu.',
        textAlign: TextAlign.center,
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message),
          const SizedBox(height: 12),
          FilledButton(onPressed: onRetry, child: const Text('Thử lại')),
        ],
      ),
    );
  }
}
