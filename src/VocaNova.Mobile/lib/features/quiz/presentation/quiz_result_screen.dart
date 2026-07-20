import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/app/theme/app_colors.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_result_notifier.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_result.dart';

const _correctColor = Color(0xFF16A34A);
const _wrongColor = Color(0xFFDC2626);

class QuizResultScreen extends ConsumerWidget {
  const QuizResultScreen({required this.sessionId, super.key});

  final int sessionId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final result = ref.watch(quizResultProvider(sessionId));
    return Scaffold(
      body: SafeArea(
        child: result.when(
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, _) => _ResultError(
            onRetry: () => ref.invalidate(quizResultProvider(sessionId)),
          ),
          data: (data) => _ResultContent(result: data),
        ),
      ),
    );
  }
}

class _ResultContent extends StatelessWidget {
  const _ResultContent({required this.result});

  final QuizResult result;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(20, 24, 20, 12),
            children: [
              _Header(result: result),
              const SizedBox(height: 24),
              _StatsRow(result: result),
              const SizedBox(height: 22),
              _ResultsList(answers: result.answers),
            ],
          ),
        ),
        _ActionBar(result: result),
      ],
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.result});

  final QuizResult result;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      children: [
        Center(child: _AccuracyRing(accuracy: result.accuracy)),
        const SizedBox(height: 18),
        Text(
          _headline(result.accuracy),
          style: theme.textTheme.headlineSmall?.copyWith(
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 6),
        Text(
          '${result.correctCount}/${result.questionCount} câu đúng · '
          '${_duration(result.durationSec)}',
          style: theme.textTheme.bodyMedium?.copyWith(
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
      ],
    );
  }

  static String _headline(double accuracy) {
    if (accuracy >= 80) return 'Tuyệt vời!';
    if (accuracy >= 50) return 'Làm tốt lắm!';
    return 'Cố gắng thêm nhé!';
  }
}

class _AccuracyRing extends StatelessWidget {
  const _AccuracyRing({required this.accuracy});

  final double accuracy;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return TweenAnimationBuilder<double>(
      key: const Key('score-animation'),
      tween: Tween(begin: 0, end: (accuracy / 100).clamp(0, 1)),
      duration: const Duration(milliseconds: 900),
      curve: Curves.easeOutCubic,
      builder: (context, value, _) => SizedBox.square(
        dimension: 132,
        child: Stack(
          fit: StackFit.expand,
          children: [
            CircularProgressIndicator(
              value: value,
              strokeWidth: 10,
              strokeCap: StrokeCap.round,
              backgroundColor: AppColors.primary.withValues(alpha: 0.12),
              valueColor: const AlwaysStoppedAnimation(AppColors.primary),
            ),
            Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    '${(value * 100).round()}%',
                    style: theme.textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.w900,
                      color: AppColors.primary,
                    ),
                  ),
                  Text(
                    'chính xác',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: theme.colorScheme.onSurfaceVariant,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatsRow extends StatelessWidget {
  const _StatsRow({required this.result});

  final QuizResult result;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: _StatTile(
            value: '${result.correctCount}',
            label: 'Đúng',
            color: _correctColor,
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: _StatTile(
            value: '${result.wrongCount}',
            label: 'Sai',
            color: _wrongColor,
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: _StatTile(
            value: '${result.maxStreak}',
            label: 'Chuỗi tốt nhất',
            color: AppColors.primary,
          ),
        ),
      ],
    );
  }
}

class _StatTile extends StatelessWidget {
  const _StatTile({
    required this.value,
    required this.label,
    required this.color,
  });

  final String value;
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 8),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.10),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        children: [
          Text(
            value,
            style: TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w900,
              color: color,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            label,
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 12,
              color: color.withValues(alpha: 0.85),
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

class _ResultsList extends StatelessWidget {
  const _ResultsList({required this.answers});

  final List<QuizResultAnswer> answers;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'KẾT QUẢ',
          style: theme.textTheme.labelMedium?.copyWith(
            fontWeight: FontWeight.w800,
            letterSpacing: 0.8,
            color: theme.colorScheme.onSurfaceVariant,
          ),
        ),
        const SizedBox(height: 10),
        Container(
          decoration: BoxDecoration(
            color: theme.colorScheme.surfaceContainerHighest.withValues(
              alpha: 0.35,
            ),
            borderRadius: BorderRadius.circular(16),
          ),
          child: Column(
            children: [
              for (var i = 0; i < answers.length; i++) ...[
                if (i > 0)
                  Divider(
                    height: 1,
                    indent: 16,
                    endIndent: 16,
                    color: theme.dividerColor.withValues(alpha: 0.4),
                  ),
                _AnswerRow(answer: answers[i]),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _AnswerRow extends StatelessWidget {
  const _AnswerRow({required this.answer});

  final QuizResultAnswer answer;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final correct = answer.isCorrect == true;
    final color = correct ? _correctColor : _wrongColor;
    return Padding(
      key: Key('result-answer-${answer.questionNumber}'),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      child: Row(
        children: [
          Icon(
            correct ? Icons.check_circle : Icons.cancel,
            color: color,
            size: 20,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  answer.displayContent,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
                if (!correct)
                  Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Text(
                      'Bạn trả lời: ${answer.userAnswer ?? "Chưa trả lời"} · '
                      'Đáp án: ${answer.expectedAnswer}',
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          _ResultBadge(correct: correct),
        ],
      ),
    );
  }
}

class _ResultBadge extends StatelessWidget {
  const _ResultBadge({required this.correct});

  final bool correct;

  @override
  Widget build(BuildContext context) {
    final color = correct ? _correctColor : _wrongColor;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        correct ? 'Đúng' : 'Sai',
        style: TextStyle(
          color: color,
          fontWeight: FontWeight.w700,
          fontSize: 12,
        ),
      ),
    );
  }
}

class _ActionBar extends StatelessWidget {
  const _ActionBar({required this.result});

  final QuizResult result;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 16),
      decoration: BoxDecoration(
        color: Theme.of(context).scaffoldBackgroundColor,
        border: Border(
          top: BorderSide(
            color: Theme.of(context).dividerColor.withValues(alpha: 0.4),
          ),
        ),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  key: const Key('review-wrong-words-button'),
                  onPressed: result.wrongCount > 0
                      ? () => context.push(AppRoutes.wrongWords)
                      : null,
                  icon: const Icon(Icons.menu_book_outlined, size: 18),
                  label: const Text('Xem lại từ sai'),
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size.fromHeight(48),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: OutlinedButton.icon(
                  key: const Key('retry-quiz-button'),
                  onPressed: () => context.go(AppRoutes.quizConfig),
                  icon: const Icon(Icons.refresh, size: 18),
                  label: const Text('Làm lại'),
                  style: OutlinedButton.styleFrom(
                    minimumSize: const Size.fromHeight(48),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          FilledButton(
            key: const Key('done-button'),
            onPressed: () => context.go(AppRoutes.home),
            style: FilledButton.styleFrom(
              minimumSize: const Size.fromHeight(52),
            ),
            child: const Text('Hoàn tất'),
          ),
        ],
      ),
    );
  }
}

class _ResultError extends StatelessWidget {
  const _ResultError({required this.onRetry});

  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Text('Không thể tải kết quả kiểm tra.'),
          const SizedBox(height: 12),
          FilledButton(onPressed: onRetry, child: const Text('Thử lại')),
        ],
      ),
    );
  }
}

class QuizResultUnavailableScreen extends StatelessWidget {
  const QuizResultUnavailableScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: Center(child: Text('Mã bài kiểm tra không hợp lệ.')),
    );
  }
}

String _duration(int? seconds) {
  if (seconds == null) return '--';
  final minutes = seconds ~/ 60;
  final remainder = seconds % 60;
  if (minutes == 0) return '${remainder}s';
  return '${minutes}m ${remainder}s';
}
