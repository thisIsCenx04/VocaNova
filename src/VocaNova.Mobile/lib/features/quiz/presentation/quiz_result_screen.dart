import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_result_notifier.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_result.dart';

class QuizResultScreen extends ConsumerWidget {
  const QuizResultScreen({required this.sessionId, super.key});

  final int sessionId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final result = ref.watch(quizResultProvider(sessionId));
    return Scaffold(
      appBar: AppBar(title: const Text('Kết quả kiểm tra')),
      body: result.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, _) => _ResultError(
          onRetry: () => ref.invalidate(quizResultProvider(sessionId)),
        ),
        data: (data) => _ResultContent(result: data),
      ),
    );
  }
}

class _ResultContent extends StatelessWidget {
  const _ResultContent({required this.result});

  final QuizResult result;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 32),
      children: [
        Center(
          child: TweenAnimationBuilder<double>(
            key: const Key('score-animation'),
            tween: Tween(begin: 0, end: result.accuracy / 100),
            duration: const Duration(milliseconds: 900),
            builder: (context, value, _) => SizedBox.square(
              dimension: 150,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  CircularProgressIndicator(
                    value: value,
                    strokeWidth: 12,
                    backgroundColor: Theme.of(
                      context,
                    ).colorScheme.surfaceContainerHighest,
                  ),
                  Center(
                    child: Text(
                      '${(value * 100).round()}%',
                      style: Theme.of(context).textTheme.headlineMedium
                          ?.copyWith(fontWeight: FontWeight.w900),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
        const SizedBox(height: 20),
        Wrap(
          alignment: WrapAlignment.center,
          spacing: 10,
          runSpacing: 10,
          children: [
            _Stat(
              label: 'Đúng',
              value: '${result.correctCount}/${result.questionCount}',
            ),
            _Stat(label: 'Độ chính xác', value: '${result.accuracy.round()}%'),
            _Stat(label: 'Chuỗi tốt nhất', value: '${result.maxStreak}'),
            _Stat(label: 'Thời gian', value: _duration(result.durationSec)),
          ],
        ),
        const SizedBox(height: 22),
        Text(
          'Chi tiết từng câu',
          style: Theme.of(
            context,
          ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
        ),
        const SizedBox(height: 10),
        for (final answer in result.answers) _AnswerBreakdown(answer: answer),
        const SizedBox(height: 12),
        FilledButton.icon(
          key: const Key('review-wrong-words-button'),
          onPressed: () => context.push(AppRoutes.wrongWords),
          icon: const Icon(Icons.replay),
          label: const Text('Xem lại sai'),
        ),
        const SizedBox(height: 10),
        OutlinedButton.icon(
          key: const Key('retry-quiz-button'),
          onPressed: () => context.go(AppRoutes.quizConfig),
          icon: const Icon(Icons.refresh),
          label: const Text('Làm lại'),
        ),
      ],
    );
  }

  static String _duration(int? seconds) {
    if (seconds == null) return '--';
    final minutes = seconds ~/ 60;
    final remainder = seconds % 60;
    return '$minutes:${remainder.toString().padLeft(2, '0')}';
  }
}

class _Stat extends StatelessWidget {
  const _Stat({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: SizedBox(
        width: 145,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            children: [
              Text(value, style: Theme.of(context).textTheme.titleLarge),
              Text(label, textAlign: TextAlign.center),
            ],
          ),
        ),
      ),
    );
  }
}

class _AnswerBreakdown extends StatelessWidget {
  const _AnswerBreakdown({required this.answer});

  final QuizResultAnswer answer;

  @override
  Widget build(BuildContext context) {
    final correct = answer.isCorrect == true;
    return Card(
      key: Key('result-answer-${answer.questionNumber}'),
      child: ListTile(
        leading: Icon(
          correct ? Icons.check_circle : Icons.cancel,
          color: correct ? Colors.green : Colors.red,
        ),
        title: Text('${answer.questionNumber}. ${answer.displayContent}'),
        subtitle: Text(
          'Bạn trả lời: ${answer.userAnswer ?? "Chưa trả lời"}\n'
          'Đáp án: ${answer.expectedAnswer}',
        ),
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
