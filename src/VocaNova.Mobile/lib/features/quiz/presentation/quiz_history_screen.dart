import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_history_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_history_state.dart';
import 'package:vocanova_mobile/features/quiz/domain/models/quiz_history.dart';
import 'package:vocanova_mobile/l10n/gen/app_localizations.dart';

class QuizHistoryScreen extends ConsumerStatefulWidget {
  const QuizHistoryScreen({super.key});

  @override
  ConsumerState<QuizHistoryScreen> createState() => _QuizHistoryScreenState();
}

class _QuizHistoryScreenState extends ConsumerState<QuizHistoryScreen> {
  late final ScrollController _controller;

  @override
  void initState() {
    super.initState();
    _controller = ScrollController()..addListener(_onScroll);
    Future.microtask(() => ref.read(quizHistoryProvider.notifier).load());
  }

  @override
  void dispose() {
    _controller
      ..removeListener(_onScroll)
      ..dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(quizHistoryProvider);
    final l10n = AppLocalizations.of(context)!;
    ref.listen(quizHistoryProvider.select((value) => value.errorMessage), (
      _,
      next,
    ) {
      if (next != null) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(next)));
      }
    });
    return Scaffold(
      appBar: AppBar(title: Text(l10n.quizHistoryTitle)),
      body: _content(state),
    );
  }

  Widget _content(QuizHistoryState state) {
    final l10n = AppLocalizations.of(context)!;
    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (state.items.isEmpty) {
      return Center(child: Text(l10n.quizHistoryEmpty));
    }
    return RefreshIndicator(
      onRefresh: ref.read(quizHistoryProvider.notifier).load,
      child: ListView.builder(
        key: const Key('quiz-history-list'),
        controller: _controller,
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
        itemCount: state.items.length + (state.isLoadingMore ? 1 : 0),
        itemBuilder: (_, index) {
          if (index == state.items.length) {
            return const Center(child: CircularProgressIndicator());
          }
          final item = state.items[index];
          return _QuizHistoryCard(
            item: item,
            onTap: () => context.push(
              AppRoutes.quizResultFor(item.sessionId.toString()),
            ),
          );
        },
      ),
    );
  }

  void _onScroll() {
    if (_controller.position.extentAfter < 300) {
      ref.read(quizHistoryProvider.notifier).loadMore();
    }
  }
}

class _QuizHistoryCard extends StatelessWidget {
  const _QuizHistoryCard({required this.item, required this.onTap});

  final QuizHistoryItem item;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final scheme = Theme.of(context).colorScheme;
    final date = DateFormat('dd/MM/yyyy HH:mm').format(item.startedAt);
    return Card(
      key: Key('quiz-history-${item.sessionId}'),
      margin: const EdgeInsets.only(bottom: 10),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      date,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  _StatusPill(status: item.status),
                ],
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  _MiniStat(
                    label: l10n.quizResultCorrectLabel,
                    value: '${item.correctCount}',
                    color: Colors.green,
                  ),
                  _MiniStat(
                    label: l10n.quizResultWrongLabel,
                    value: '${item.wrongCount}',
                    color: Colors.red,
                  ),
                  _MiniStat(
                    label: l10n.quizResultAccuracyLabel,
                    value: '${item.accuracy.round()}%',
                    color: scheme.primary,
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Text(
                '${_modeLabel(context, item.mode)} · '
                '${_answerLabel(context, item.answerMethod)} · '
                '${l10n.quizHistoryQuestionCount(item.questionCount)}',
                style: Theme.of(
                  context,
                ).textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _MiniStat extends StatelessWidget {
  const _MiniStat({
    required this.label,
    required this.value,
    required this.color,
  });

  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            value,
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
              color: color,
              fontWeight: FontWeight.w800,
            ),
          ),
          Text(label, style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    final color = status == 'completed' ? Colors.green : Colors.orange;
    return DecoratedBox(
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
        child: Text(
          status,
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
            color: color.shade700,
            fontWeight: FontWeight.w800,
          ),
        ),
      ),
    );
  }
}

String _modeLabel(BuildContext context, String mode) {
  final l10n = AppLocalizations.of(context)!;
  return switch (mode) {
    'standard' => l10n.quizModeStandard,
    'timed' => l10n.quizModeTimed,
    'challenge' => l10n.quizModeChallenge,
    'elimination' => l10n.quizModeElimination,
    _ => mode,
  };
}

String _answerLabel(BuildContext context, String method) {
  final l10n = AppLocalizations.of(context)!;
  return switch (method) {
    'multiple_choice' => l10n.quizAnswerMultipleChoice,
    'exact_typing' => l10n.quizAnswerTyping,
    'ai_typing' => l10n.quizAnswerAiTyping,
    _ => method,
  };
}
