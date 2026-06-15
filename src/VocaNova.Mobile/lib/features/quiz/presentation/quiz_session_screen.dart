import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_session_notifier.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_session_state.dart';
import 'package:vocanova_mobile/features/quiz/domain/quiz_config.dart';

class QuizSessionScreen extends ConsumerWidget {
  const QuizSessionScreen({required this.session, super.key});

  final QuizSessionStart session;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final provider = quizSessionProvider(session);
    final state = ref.watch(provider);
    final notifier = ref.read(provider.notifier);
    ref.listen(provider.select((value) => value.errorMessage), (_, next) {
      if (next != null) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(next)));
      }
    });
    ref.listen(provider, (previous, next) {
      final timedOut =
          next.remainingSeconds == 0 &&
          next.isFinished &&
          previous?.isFinished != true;
      if (timedOut) _openResult(context);
    });

    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) _confirmAbandon(context, notifier);
      },
      child: Scaffold(
        appBar: AppBar(
          automaticallyImplyLeading: false,
          title: const Text('Bài kiểm tra'),
          actions: [
            IconButton(
              key: const Key('abandon-quiz-button'),
              tooltip: 'Bỏ bài',
              onPressed: state.isFinishing
                  ? null
                  : () => _confirmAbandon(context, notifier),
              icon: const Icon(Icons.close),
            ),
          ],
        ),
        body: SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _SessionHeader(state: state),
                const SizedBox(height: 28),
                Text(
                  'Câu ${state.questionNumber}',
                  style: Theme.of(context).textTheme.labelLarge?.copyWith(
                    color: Theme.of(context).colorScheme.primary,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  state.question.displayContent,
                  key: const Key('quiz-question'),
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 24),
                Expanded(
                  child: ListView.separated(
                    itemCount: state.question.choices.length,
                    separatorBuilder: (_, _) => const SizedBox(height: 12),
                    itemBuilder: (context, index) {
                      final answer = state.question.choices[index];
                      return _AnswerButton(
                        key: Key('quiz-answer-$index'),
                        label: answer,
                        style: _answerStyle(context, state, answer),
                        isLoading:
                            state.isSubmitting &&
                            state.selectedAnswer == answer,
                        onPressed: state.hasAnswered || state.isSubmitting
                            ? null
                            : () => notifier.submitAnswer(answer),
                      );
                    },
                  ),
                ),
                if (state.answerResult?.aiExplanation != null) ...[
                  Card(
                    key: const Key('ai-explanation'),
                    child: Padding(
                      padding: const EdgeInsets.all(14),
                      child: Text(state.answerResult!.aiExplanation!),
                    ),
                  ),
                  const SizedBox(height: 12),
                ],
                if (state.hasAnswered)
                  FilledButton(
                    key: const Key('next-question-button'),
                    onPressed: state.isFinishing
                        ? null
                        : () {
                            if (state.isFinished) {
                              _openResult(context);
                            } else {
                              notifier.nextQuestion();
                            }
                          },
                    child: Text(
                      state.isFinishing
                          ? 'Đang kết thúc...'
                          : state.isFinished
                          ? 'Xem kết quả'
                          : 'Tiếp theo',
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  ButtonStyle? _answerStyle(
    BuildContext context,
    QuizSessionState state,
    String answer,
  ) {
    final result = state.answerResult;
    if (result == null) return null;
    if (answer == result.expectedAnswer) {
      return OutlinedButton.styleFrom(
        backgroundColor: Colors.green.shade100,
        foregroundColor: Colors.green.shade900,
        side: BorderSide(color: Colors.green.shade700, width: 2),
      );
    }
    if (answer == state.selectedAnswer && !result.isCorrect) {
      return OutlinedButton.styleFrom(
        backgroundColor: Colors.red.shade100,
        foregroundColor: Colors.red.shade900,
        side: BorderSide(color: Colors.red.shade700, width: 2),
      );
    }
    return null;
  }

  Future<void> _confirmAbandon(
    BuildContext context,
    QuizSessionNotifier notifier,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Bỏ bài kiểm tra?'),
        content: const Text('Tiến độ hiện tại sẽ được kết thúc và lưu lại.'),
        actions: [
          TextButton(
            onPressed: () => context.pop(false),
            child: const Text('Tiếp tục làm'),
          ),
          FilledButton(
            onPressed: () => context.pop(true),
            child: const Text('Bỏ bài'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    final finished = await notifier.finish();
    if (finished && context.mounted) _openResult(context);
  }

  void _openResult(BuildContext context) {
    context.go(AppRoutes.quizResultFor(session.sessionId.toString()));
  }
}

class _SessionHeader extends StatelessWidget {
  const _SessionHeader({required this.state});

  final QuizSessionState state;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                'Câu ${state.questionNumber}/${state.questionCount}',
                key: const Key('quiz-progress-label'),
                style: Theme.of(context).textTheme.titleMedium,
              ),
            ),
            if (state.remainingSeconds != null)
              _StatusChip(
                key: const Key('quiz-timer'),
                icon: Icons.timer_outlined,
                label: _time(state.remainingSeconds!),
              ),
            if (state.remainingLives != null)
              _StatusChip(
                key: const Key('quiz-lives'),
                icon: Icons.favorite,
                label: '${state.remainingLives}',
                color: Colors.pink,
              ),
          ],
        ),
        const SizedBox(height: 10),
        LinearProgressIndicator(
          key: const Key('quiz-progress-bar'),
          value: state.questionNumber / state.questionCount,
        ),
      ],
    );
  }

  String _time(int seconds) {
    final minutes = seconds ~/ 60;
    final remainder = seconds % 60;
    return '$minutes:${remainder.toString().padLeft(2, '0')}';
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({
    required this.icon,
    required this.label,
    this.color,
    super.key,
  });

  final IconData icon;
  final String label;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(left: 12),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 20, color: color),
          const SizedBox(width: 4),
          Text(label),
        ],
      ),
    );
  }
}

class _AnswerButton extends StatelessWidget {
  const _AnswerButton({
    required this.label,
    required this.onPressed,
    required this.isLoading,
    this.style,
    super.key,
  });

  final String label;
  final VoidCallback? onPressed;
  final bool isLoading;
  final ButtonStyle? style;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton(
      style: style,
      onPressed: onPressed,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 14),
        child: isLoading
            ? const SizedBox.square(
                dimension: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : Text(label),
      ),
    );
  }
}

class QuizSessionUnavailableScreen extends StatelessWidget {
  const QuizSessionUnavailableScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Bài kiểm tra')),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text(
                'Không thể khôi phục bài kiểm tra đang chạy. '
                'Vui lòng tạo một bài kiểm tra mới.',
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: () => context.go(AppRoutes.quizConfig),
                child: const Text('Tạo bài kiểm tra'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
