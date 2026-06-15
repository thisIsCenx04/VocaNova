import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:vocanova_mobile/app/router/app_routes.dart';
import 'package:vocanova_mobile/features/quiz/application/quiz_config_notifier.dart';

class QuizConfigScreen extends ConsumerStatefulWidget {
  const QuizConfigScreen({this.initialListId, super.key});

  final int? initialListId;

  @override
  ConsumerState<QuizConfigScreen> createState() => _QuizConfigScreenState();
}

class _QuizConfigScreenState extends ConsumerState<QuizConfigScreen> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(quizConfigProvider.notifier).loadTopics());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(quizConfigProvider);
    ref.listen(quizConfigProvider.select((value) => value.errorMessage), (
      previous,
      next,
    ) {
      if (next != null && next != previous) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(next)));
      }
    });
    return Scaffold(
      appBar: AppBar(title: const Text('Cấu hình kiểm tra')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
        children: [
          if (widget.initialListId != null)
            Card(
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: Text(
                  'Mở từ danh sách #${widget.initialListId}. '
                  'API hiện tạo bài kiểm tra từ toàn bộ từ đã học và các bộ lọc bên dưới.',
                ),
              ),
            ),
          _Section(
            title: 'Phạm vi từ',
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    _choice(
                      label: 'Tất cả',
                      selected: state.scopeType == 'all',
                      onSelected: () => _notifier.setScope('all'),
                    ),
                    _choice(
                      label: 'Từ ngày',
                      selected: state.scopeType == 'start_date',
                      onSelected: () => _notifier.setScope('start_date'),
                    ),
                    _choice(
                      label: 'Đến ngày',
                      selected: state.scopeType == 'end_date',
                      onSelected: () => _notifier.setScope('end_date'),
                    ),
                    _choice(
                      label: 'Khoảng ngày',
                      selected: state.scopeType == 'date_range',
                      onSelected: () => _notifier.setScope('date_range'),
                    ),
                  ],
                ),
                if (state.scopeType == 'start_date' ||
                    state.scopeType == 'date_range') ...[
                  const SizedBox(height: 10),
                  _DateButton(
                    key: const Key('date-from'),
                    label: 'Ngày bắt đầu',
                    date: state.dateFrom,
                    onTap: () => _pickDate(true),
                  ),
                ],
                if (state.scopeType == 'end_date' ||
                    state.scopeType == 'date_range') ...[
                  const SizedBox(height: 10),
                  _DateButton(
                    key: const Key('date-to'),
                    label: 'Ngày kết thúc',
                    date: state.dateTo,
                    onTap: () => _pickDate(false),
                  ),
                ],
              ],
            ),
          ),
          _Section(
            title: 'Chủ đề',
            child: state.isLoadingTopics
                ? const Center(child: CircularProgressIndicator())
                : Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      FilterChip(
                        key: const Key('all-topics'),
                        label: const Text('Tất cả'),
                        selected: state.selectedTopicIds.isEmpty,
                        onSelected: (_) => _notifier.clearTopics(),
                      ),
                      for (final topic in state.topics)
                        FilterChip(
                          key: Key('quiz-topic-${topic.topicId}'),
                          label: Text(topic.displayName),
                          selected: state.selectedTopicIds.contains(
                            topic.topicId,
                          ),
                          onSelected: (_) =>
                              _notifier.toggleTopic(topic.topicId),
                        ),
                    ],
                  ),
          ),
          _Section(
            title: 'Chế độ',
            child: Column(
              children: [
                _ModeTile(
                  value: 'standard',
                  title: 'Standard',
                  description: 'Kiểm tra thông thường, không giới hạn.',
                  selected: state.mode == 'standard',
                  onTap: _notifier.setMode,
                ),
                _ModeTile(
                  value: 'timed',
                  title: 'Timed',
                  description: 'Hoàn thành trước khi hết thời gian.',
                  selected: state.mode == 'timed',
                  onTap: _notifier.setMode,
                ),
                _ModeTile(
                  value: 'challenge',
                  title: 'Challenge',
                  description: 'Thử thách với thứ tự từ ngẫu nhiên.',
                  selected: state.mode == 'challenge',
                  onTap: _notifier.setMode,
                ),
                _ModeTile(
                  value: 'elimination',
                  title: 'Elimination',
                  description: 'Bài kiểm tra kết thúc khi hết mạng.',
                  selected: state.mode == 'elimination',
                  onTap: _notifier.setMode,
                ),
                if (state.mode == 'timed')
                  TextField(
                    key: const Key('time-limit-input'),
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Thời gian (giây)',
                    ),
                    onChanged: (value) =>
                        _notifier.setTimeLimit(int.tryParse(value)),
                  ),
                if (state.mode == 'elimination')
                  TextField(
                    key: const Key('lives-input'),
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(labelText: 'Số mạng'),
                    onChanged: (value) =>
                        _notifier.setLives(int.tryParse(value)),
                  ),
              ],
            ),
          ),
          _Section(
            title: 'Loại câu hỏi',
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _choice(
                  label: 'Word → Meaning',
                  selected: state.questionType == 1,
                  onSelected: () => _notifier.setQuestionType(1),
                ),
                _choice(
                  label: 'Meaning → Word',
                  selected: state.questionType == 2,
                  onSelected: () => _notifier.setQuestionType(2),
                ),
                _choice(
                  label: 'Description',
                  selected: state.questionType == 3,
                  onSelected: () => _notifier.setQuestionType(3),
                ),
              ],
            ),
          ),
          _Section(
            title: 'Cách trả lời',
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _choice(
                  label: 'Multiple Choice',
                  selected: state.answerMethod == 'multiple_choice',
                  onSelected: () =>
                      _notifier.setAnswerMethod('multiple_choice'),
                ),
                _choice(
                  label: 'Exact Typing',
                  selected: state.answerMethod == 'exact_typing',
                  onSelected: () => _notifier.setAnswerMethod('exact_typing'),
                ),
                _choice(
                  label: 'AI Typing',
                  selected: state.answerMethod == 'ai_typing',
                  onSelected: () => _notifier.setAnswerMethod('ai_typing'),
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),
          FilledButton.icon(
            key: const Key('start-quiz-button'),
            onPressed: state.isCreating ? null : _startQuiz,
            icon: state.isCreating
                ? const SizedBox.square(
                    dimension: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.play_arrow),
            label: const Text('Bắt đầu'),
          ),
        ],
      ),
    );
  }

  QuizConfigNotifier get _notifier => ref.read(quizConfigProvider.notifier);

  Widget _choice({
    required String label,
    required bool selected,
    required VoidCallback onSelected,
  }) {
    return ChoiceChip(
      label: Text(label),
      selected: selected,
      onSelected: (_) => onSelected(),
    );
  }

  Future<void> _pickDate(bool from) async {
    final state = ref.read(quizConfigProvider);
    final selected = await showDatePicker(
      context: context,
      initialDate: from
          ? state.dateFrom ?? DateTime.now()
          : state.dateTo ?? DateTime.now(),
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (selected == null) return;
    from ? _notifier.setDateFrom(selected) : _notifier.setDateTo(selected);
  }

  Future<void> _startQuiz() async {
    final session = await _notifier.createSession();
    if (session != null && mounted) {
      context.push(
        AppRoutes.quizSession(session.sessionId.toString()),
        extra: session,
      );
    }
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 14),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 12),
            child,
          ],
        ),
      ),
    );
  }
}

class _DateButton extends StatelessWidget {
  const _DateButton({
    required this.label,
    required this.date,
    required this.onTap,
    super.key,
  });

  final String label;
  final DateTime? date;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return OutlinedButton.icon(
      onPressed: onTap,
      icon: const Icon(Icons.calendar_month),
      label: Text(
        date == null
            ? label
            : '$label: ${DateFormat('dd/MM/yyyy').format(date!)}',
      ),
    );
  }
}

class _ModeTile extends StatelessWidget {
  const _ModeTile({
    required this.value,
    required this.title,
    required this.description,
    required this.selected,
    required this.onTap,
  });

  final String value;
  final String title;
  final String description;
  final bool selected;
  final ValueChanged<String> onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      color: selected
          ? Theme.of(context).colorScheme.primary.withValues(alpha: 0.18)
          : null,
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        key: Key('quiz-mode-$value'),
        onTap: () => onTap(value),
        leading: Icon(
          selected ? Icons.radio_button_checked : Icons.radio_button_off,
        ),
        title: Text(title),
        subtitle: Text(description),
      ),
    );
  }
}
