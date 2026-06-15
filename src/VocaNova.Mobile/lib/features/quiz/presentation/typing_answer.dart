part of 'quiz_session_screen.dart';

class _TypingAnswer extends StatefulWidget {
  const _TypingAnswer({
    required this.answerMethod,
    required this.state,
    required this.onSubmit,
    super.key,
  });

  final String answerMethod;
  final QuizSessionState state;
  final ValueChanged<String> onSubmit;

  @override
  State<_TypingAnswer> createState() => _TypingAnswerState();
}

class _TypingAnswerState extends State<_TypingAnswer> {
  final _controller = TextEditingController();
  final _focusNode = FocusNode();

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final locked = widget.state.hasAnswered || widget.state.isSubmitting;
    final isAi = widget.answerMethod == 'ai_typing';
    return ListView(
      children: [
        TextField(
          key: const Key('typing-answer-input'),
          controller: _controller,
          focusNode: _focusNode,
          autofocus: true,
          enabled: !locked,
          textInputAction: TextInputAction.done,
          minLines: isAi ? 3 : 1,
          maxLines: isAi ? 5 : 1,
          decoration: InputDecoration(
            labelText: isAi ? 'Nhập câu trả lời của bạn' : 'Nhập đáp án',
            helperText: isAi
                ? 'AI sẽ đánh giá mức độ chính xác về ý nghĩa.'
                : 'Không phân biệt hoa thường và dấu câu cuối.',
            border: const OutlineInputBorder(),
          ),
          onSubmitted: (_) => _submit(),
        ),
        const SizedBox(height: 14),
        FilledButton.icon(
          key: const Key('submit-typing-answer-button'),
          onPressed: locked ? null : _submit,
          icon: widget.state.isSubmitting
              ? const SizedBox.square(
                  dimension: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Icon(Icons.send),
          label: Text(
            widget.state.isSubmitting && isAi
                ? 'AI đang đánh giá...'
                : 'Gửi câu trả lời',
          ),
        ),
      ],
    );
  }

  void _submit() {
    final answer = _controller.text.trim();
    if (answer.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập câu trả lời.')),
      );
      _focusNode.requestFocus();
      return;
    }
    FocusScope.of(context).unfocus();
    widget.onSubmit(answer);
  }
}

class _TypingFeedback extends StatelessWidget {
  const _TypingFeedback({required this.answerMethod, required this.result});

  final String answerMethod;
  final QuizAnswerResult result;

  @override
  Widget build(BuildContext context) {
    final isAi = answerMethod == 'ai_typing';
    final color = result.isCorrect ? Colors.green : Colors.red;
    return Card(
      key: const Key('typing-feedback'),
      color: color.shade50,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              result.isCorrect ? 'Chính xác' : 'Chưa chính xác',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                color: color.shade800,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 6),
            Text('Đáp án: ${result.expectedAnswer}'),
            if (isAi && result.aiScore != null) ...[
              const SizedBox(height: 8),
              Text(
                'Điểm AI: ${(result.aiScore! * 100).round()}%',
                key: const Key('ai-score'),
              ),
            ],
            if (isAi && result.aiExplanation != null) ...[
              const SizedBox(height: 6),
              Text(result.aiExplanation!, key: const Key('ai-explanation')),
            ],
            if (isAi && result.aiSuggestion != null) ...[
              const SizedBox(height: 6),
              Text(
                'Gợi ý: ${result.aiSuggestion}',
                key: const Key('ai-suggestion'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
